using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    public class PageActionInvoker : IActionInvoker
    {
        private readonly IPageFactory _factory;
        private readonly IModelMetadataProvider _metadataProvider;
        private readonly ITempDataDictionaryFactory _tempDataFactory;
        private readonly MvcViewOptions _viewOptions;
        private readonly DiagnosticListener _diagnosticSource;
        private readonly ILogger _logger;

        private readonly ActionContext _actionContext;
        private readonly CompiledPageActionDescriptor _actionDescriptor;
        private readonly IList<IValueProviderFactory> _valueProviderFactories;

        private readonly IFilterMetadata[] _filters;
        private FilterCursor _cursor; // Mutable struct. DO NOT make this readonly

        private AuthorizationFilterContext _authorizationContext;
        private ResourceExecutingContext _resourceExecutingContext;
        private ResourceExecutedContext _resourceExecutedContext;

        public PageActionInvoker(
            DiagnosticListener diagnosticSource,
            ILogger logger,
            IPageFactory factory,
            IModelMetadataProvider metadataProvider,
            ITempDataDictionaryFactory tempDataFactory,
            IOptions<MvcViewOptions> viewOptions,
            IFilterMetadata[] filters,
            IReadOnlyList<IValueProviderFactory> valueProviderFactories,
            ActionContext actionContext,
            CompiledPageActionDescriptor actionDescriptor)
        {
            _diagnosticSource = diagnosticSource;
            _logger = logger;
            _factory = factory;
            _metadataProvider = metadataProvider;
            _tempDataFactory = tempDataFactory;
            _viewOptions = viewOptions.Value;
            _filters = filters;
            _valueProviderFactories = new CopyOnWriteList<IValueProviderFactory>(valueProviderFactories);
            _actionContext = actionContext;
            _actionDescriptor = actionDescriptor;

            _cursor = new FilterCursor(_filters);
        }

        public async Task InvokeAsync()
        {
            var next = State.InvokeBegin;
            var scope = Scope.Invoker;
            var state = (object)null;
            var isCompleted = false;

            while (!isCompleted)
            {
                await Next(ref next, ref scope, ref state, ref isCompleted);
            }
        }

        private Task Next(ref State next, ref Scope scope, ref object state, ref bool isCompleted)
        {
            switch (next)
            {
                case State.InvokeBegin:
                    {
                        goto case State.AuthorizationBegin;
                    }

                case State.AuthorizationBegin:
                    {
                        _cursor.Reset();
                        goto case State.AuthorizationNext;
                    }

                case State.AuthorizationNext:
                    {
                        var current = _cursor.GetNextFilter<IAuthorizationFilter, IAsyncAuthorizationFilter>();
                        if (current.FilterAsync != null)
                        {
                            if (_authorizationContext == null)
                            {
                                _authorizationContext = new AuthorizationFilterContext(_actionContext, _filters);
                            }

                            state = current.FilterAsync;
                            goto case State.AuthorizationAsyncBegin;
                        }
                        else if (current.Filter != null)
                        {
                            if (_authorizationContext == null)
                            {
                                _authorizationContext = new AuthorizationFilterContext(_actionContext, _filters);
                            }

                            state = current.Filter;
                            goto case State.AuthorizationSync;
                        }
                        else
                        {
                            goto case State.AuthorizationEnd;
                        }
                    }

                case State.AuthorizationAsyncBegin:
                    {
                        Debug.Assert(_authorizationContext != null);

                        var filter = (IAsyncAuthorizationFilter)state;
                        var authorizationContext = _authorizationContext;

                        _diagnosticSource.BeforeOnAuthorizationAsync(authorizationContext, filter);

                        var task = filter.OnAuthorizationAsync(authorizationContext);
                        if (task.Status != TaskStatus.RanToCompletion)
                        {
                            next = State.AuthorizationAsyncEnd;
                            return task;
                        }

                        goto case State.AuthorizationAsyncEnd;
                    }

                case State.AuthorizationAsyncEnd:
                    {
                        var filter = (IAsyncAuthorizationFilter)state;
                        var authorizationContext = _authorizationContext;

                        _diagnosticSource.AfterOnAuthorizationAsync(authorizationContext, filter);

                        if (authorizationContext.Result != null)
                        {
                            goto case State.AuthorizationShortCircuit;
                        }

                        goto case State.AuthorizationNext;
                    }

                case State.AuthorizationSync:
                    {
                        Debug.Assert(_authorizationContext != null);

                        var filter = (IAuthorizationFilter)state;
                        var authorizationContext = _authorizationContext;

                        _diagnosticSource.BeforeOnAuthorization(authorizationContext, filter);

                        filter.OnAuthorization(authorizationContext);

                        _diagnosticSource.AfterOnAuthorization(authorizationContext, filter);

                        if (authorizationContext.Result != null)
                        {
                            goto case State.AuthorizationShortCircuit;
                        }

                        goto case State.AuthorizationNext;
                    }

                case State.AuthorizationShortCircuit:
                    {
                        // If an authorization filter short circuits, the result is the last thing we execute
                        // so just return that task instead of calling back into the state machine.
                        isCompleted = true;
                        return InvokeResultAsync(_authorizationContext.Result);
                    }

                case State.AuthorizationEnd:
                    {
                        goto case State.ResourceBegin;
                    }

                case State.ResourceBegin:
                    {
                        _cursor.Reset();
                        goto case State.ResourceNext;
                    }

                case State.ResourceNext:
                    {
                        var current = _cursor.GetNextFilter<IResourceFilter, IAsyncResourceFilter>();
                        if (current.FilterAsync != null)
                        {
                            if (_resourceExecutingContext == null)
                            {
                                _resourceExecutingContext = new ResourceExecutingContext(
                                    _actionContext,
                                    _filters,
                                    _valueProviderFactories);
                            }

                            state = current.FilterAsync;
                            goto case State.ResourceAsyncBegin;
                        }
                        else if (current.Filter != null)
                        {
                            if (_resourceExecutingContext == null)
                            {
                                _resourceExecutingContext = new ResourceExecutingContext(
                                    _actionContext,
                                    _filters,
                                    _valueProviderFactories);
                            }

                            state = current.Filter;
                            goto case State.ResourceSyncBegin;
                        }
                        else if (scope == Scope.Resource)
                        {
                            Debug.Assert(_resourceExecutingContext != null);
                            goto case State.ResourceInside;
                        }
                        else
                        {
                            Debug.Assert(scope == Scope.Invoker);
                            goto case State.PageBegin;
                        }
                    }

                case State.ResourceAsyncBegin:
                    {
                        Debug.Assert(_resourceExecutingContext != null);

                        var filter = (IAsyncResourceFilter)state;
                        var resourceExecutingContext = _resourceExecutingContext;

                        _diagnosticSource.BeforeOnResourceExecution(resourceExecutingContext, filter);

                        var task = filter.OnResourceExecutionAsync(resourceExecutingContext, InvokeNextResourceFilterAwaitedAsync);
                        if (task.Status != TaskStatus.RanToCompletion)
                        {
                            next = State.ResourceAsyncEnd;
                            return task;
                        }

                        goto case State.ResourceAsyncEnd;
                    }

                case State.ResourceAsyncEnd:
                    {
                        var filter = (IAsyncResourceFilter)state;
                        if (_resourceExecutedContext == null)
                        {
                            // If we get here then the filter didn't call 'next' indicating a short circuit
                            Debug.Assert(_resourceExecutingContext.Result != null);
                            _resourceExecutedContext = new ResourceExecutedContext(_resourceExecutingContext, _filters)
                            {
                                Canceled = true,
                                Result = _resourceExecutingContext.Result, // Can be null
                            };
                        }

                        _diagnosticSource.AfterOnResourceExecution(_resourceExecutedContext, filter);

                        if (_resourceExecutingContext.Result != null)
                        {
                            goto case State.ResourceShortCircuit;
                        }

                        goto case State.ResourceEnd;
                    }

                case State.ResourceSyncBegin:
                    {
                        Debug.Assert(_resourceExecutingContext != null);

                        var filter = (IResourceFilter)state;
                        var resourceExecutingContext = _resourceExecutingContext;

                        _diagnosticSource.BeforeOnResourceExecuting(resourceExecutingContext, filter);

                        filter.OnResourceExecuting(resourceExecutingContext);

                        _diagnosticSource.AfterOnResourceExecuting(resourceExecutingContext, filter);

                        if (resourceExecutingContext.Result != null)
                        {
                            _resourceExecutedContext = new ResourceExecutedContext(resourceExecutingContext, _filters)
                            {
                                Canceled = true,
                                Result = _resourceExecutingContext.Result,
                            };

                            goto case State.ResourceShortCircuit;
                        }

                        var task = InvokeNextResourceFilter();
                        if (task.Status != TaskStatus.RanToCompletion)
                        {
                            next = State.ResourceSyncEnd;
                            return task;
                        }

                        goto case State.ResourceSyncEnd;
                    }

                case State.ResourceSyncEnd:
                    {
                        var filter = (IResourceFilter)state;
                        var resourceExecutedContext = _resourceExecutedContext;

                        _diagnosticSource.BeforeOnResourceExecuted(resourceExecutedContext, filter);

                        filter.OnResourceExecuted(resourceExecutedContext);

                        _diagnosticSource.AfterOnResourceExecuted(resourceExecutedContext, filter);

                        goto case State.ResourceEnd;
                    }

                case State.ResourceShortCircuit:
                    {
                        Debug.Assert(_resourceExecutedContext != null);

                        var task = InvokeResultAsync(_resourceExecutingContext.Result);
                        if (task.Status != TaskStatus.RanToCompletion)
                        {
                            next = State.ResourceEnd;
                            return task;
                        }

                        goto case State.ResourceEnd;
                    }

                case State.ResourceInside:
                    {
                        goto case State.PageBegin;
                    }

                case State.ResourceEnd:
                    {
                        if (scope == Scope.Resource)
                        {
                            isCompleted = true;
                            return TaskCache.CompletedTask;
                        }

                        Debug.Assert(scope == Scope.Invoker);
                        Rethrow(_resourceExecutedContext);

                        goto case State.InvokeEnd;
                    }
                case State.PageBegin:
                    {
                        next = State.PageEnd;
                        return ExecutePageAsync();
                    }
                case State.PageEnd:
                    {
                        if (scope == Scope.Resource)
                        {
                            Debug.Assert(_resourceExecutedContext == null);
                            _resourceExecutedContext = new ResourceExecutedContext(_actionContext, _filters);

                            isCompleted = true;
                            return TaskCache.CompletedTask;
                        }

                        Debug.Assert(scope == Scope.Invoker);
                        goto case State.InvokeEnd;
                    }
                case State.InvokeEnd:
                    {
                        isCompleted = true;
                        return TaskCache.CompletedTask;
                    }
            }

            return ExecutePageAsync();
        }

        private async Task InvokeNextResourceFilter()
        {
            try
            {
                var next = State.ResourceNext;
                var state = (object)null;
                var scope = Scope.Resource;
                var isCompleted = false;
                while (!isCompleted)
                {
                    await Next(ref next, ref scope, ref state, ref isCompleted);
                }
            }
            catch (Exception exception)
            {
                _resourceExecutedContext = new ResourceExecutedContext(_resourceExecutingContext, _filters)
                {
                    ExceptionDispatchInfo = ExceptionDispatchInfo.Capture(exception),
                };
            }

            Debug.Assert(_resourceExecutedContext != null);
        }

        private async Task<ResourceExecutedContext> InvokeNextResourceFilterAwaitedAsync()
        {
            Debug.Assert(_resourceExecutingContext != null);

            if (_resourceExecutingContext.Result != null)
            {
                // If we get here, it means that an async filter set a result AND called next(). This is forbidden.
                //var message = Resources.FormatAsyncResourceFilter_InvalidShortCircuit(
                //    typeof(IAsyncResourceFilter).Name,
                //    nameof(ResourceExecutingContext.Result),
                //    typeof(ResourceExecutingContext).Name,
                //    typeof(ResourceExecutionDelegate).Name);

                var message = "Oopsie daisies!";
                throw new InvalidOperationException(message);
            }

            await InvokeNextResourceFilter();

            Debug.Assert(_resourceExecutedContext != null);
            return _resourceExecutedContext;
        }

        private Task ExecutePageAsync()
        {
            Type viewDataType = null;
            var viewDataProperty = _actionDescriptor.PageType.GetDeclaredProperty("ViewData");
            if (viewDataProperty != null)
            {
                var viewDataPropertyType = viewDataProperty.PropertyType.GetTypeInfo();
                if (viewDataPropertyType.IsGenericType && viewDataPropertyType.GetGenericTypeDefinition() == typeof(ViewDataDictionary<>))
                {
                    viewDataType = viewDataPropertyType.GetGenericArguments()[0];
                }
            }

            ViewDataDictionary viewData;
            if (viewDataType == null)
            {
                viewData = new ViewDataDictionary<object>(_metadataProvider, _actionContext.ModelState);
            }
            else
            {
                viewData = (ViewDataDictionary)Activator.CreateInstance(typeof(ViewDataDictionary<>).MakeGenericType(viewDataType), _metadataProvider, _actionContext.ModelState);
            }
            
            var tempData = _tempDataFactory.GetTempData(_actionContext.HttpContext);

            var pageContext = new PageContext(_actionContext, viewData, tempData, _viewOptions.HtmlHelperOptions)
            {
                ActionDescriptor = _actionDescriptor,
                ValueProviderFactories = _valueProviderFactories,
            };

            var page = (Page)_factory.CreatePage(pageContext);
            return page.ExecuteAsync();
        }

        private async Task InvokeResultAsync(IActionResult result)
        {
            var actionContext = _actionContext;

            _diagnosticSource.BeforeActionResult(actionContext, result);

            try
            {
                await result.ExecuteResultAsync(actionContext);
            }
            finally
            {
                _diagnosticSource.AfterActionResult(actionContext, result);
            }
        }

        private static void Rethrow(ResourceExecutedContext context)
        {
            if (context == null)
            {
                return;
            }

            if (context.ExceptionHandled)
            {
                return;
            }

            if (context.ExceptionDispatchInfo != null)
            {
                context.ExceptionDispatchInfo.Throw();
            }

            if (context.Exception != null)
            {
                throw context.Exception;
            }
        }

        private enum Scope
        {
            Invoker,
            Resource,
            Exception,
            Action,
            Result,
        }

        private enum State
        {
            InvokeBegin,
            InvokeEnd,
            AuthorizationBegin,
            AuthorizationNext,
            AuthorizationAsyncBegin,
            AuthorizationAsyncEnd,
            AuthorizationSync,
            AuthorizationShortCircuit,
            AuthorizationEnd,
            ResourceBegin,
            ResourceNext,
            ResourceAsyncBegin,
            ResourceAsyncEnd,
            ResourceSyncBegin,
            ResourceSyncEnd,
            ResourceShortCircuit,
            ResourceInside,
            ResourceEnd,
            PageBegin,
            PageEnd,
        }

        /// <summary>
        /// A one-way cursor for filters.
        /// </summary>
        /// <remarks>
        /// This will iterate the filter collection once per-stage, and skip any filters that don't have
        /// the one of interfaces that applies to the current stage.
        /// </remarks>
        private struct FilterCursor
        {
            private int _index;
            private readonly IFilterMetadata[] _filters;

            public FilterCursor(int index, IFilterMetadata[] filters)
            {
                _index = index;
                _filters = filters;
            }

            public FilterCursor(IFilterMetadata[] filters)
            {
                _index = 0;
                _filters = filters;
            }

            public void Reset()
            {
                _index = 0;
            }

            public FilterCursorItem<TFilter, TFilterAsync> GetNextFilter<TFilter, TFilterAsync>()
                where TFilter : class
                where TFilterAsync : class
            {
                while (_index < _filters.Length)
                {
                    var filter = _filters[_index] as TFilter;
                    var filterAsync = _filters[_index] as TFilterAsync;

                    _index += 1;

                    if (filter != null || filterAsync != null)
                    {
                        return new FilterCursorItem<TFilter, TFilterAsync>(_index, filter, filterAsync);
                    }
                }

                return default(FilterCursorItem<TFilter, TFilterAsync>);
            }
        }

        private struct FilterCursorItem<TFilter, TFilterAsync>
        {
            public readonly int Index;
            public readonly TFilter Filter;
            public readonly TFilterAsync FilterAsync;

            public FilterCursorItem(int index, TFilter filter, TFilterAsync filterAsync)
            {
                Index = index;
                Filter = filter;
                FilterAsync = filterAsync;
            }
        }
    }
}
