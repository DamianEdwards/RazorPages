using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages.Compilation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    public class PageActionInvokerProvider : IActionInvokerProvider
    {
        private readonly DiagnosticListener _diagnosticSource;
        private readonly ILogger _logger;
        private readonly IFilterProvider[] _filterProviders;
        private readonly IPageFactory _factory;
        private readonly IPageCompilationService _compilationService;
        private readonly IValueProviderFactory[] _valueProviderFactories;

        public PageActionInvokerProvider(
            IPageFactory factory,
            IPageCompilationService compilationService, 
            DiagnosticListener diagnosticSource,
            ILoggerFactory loggerFactory,
            IEnumerable<IFilterProvider> filterProviders,
            IOptions<MvcOptions> options)
        {
            _factory = factory;
            _diagnosticSource = diagnosticSource;
            _compilationService = compilationService;

            _filterProviders = filterProviders.OrderBy(fp => fp.Order).ToArray();
            _logger = loggerFactory.CreateLogger<PageActionInvoker>();
            _valueProviderFactories = options.Value.ValueProviderFactories.ToArray();
        }

        public int Order { get; set; }

        public void OnProvidersExecuting(ActionInvokerProviderContext context)
        {
            var actionDescriptor = context.ActionContext.ActionDescriptor as PageActionDescriptor;
            if (actionDescriptor != null)
            {
                var itemCount = actionDescriptor.FilterDescriptors?.Count ?? 0;
                var filterItems = new List<FilterItem>(itemCount);
                for (var i = 0; i < itemCount; i++)
                {
                    var item = new FilterItem(actionDescriptor.FilterDescriptors[i]);
                    filterItems.Add(item);
                }

                var filterProviderContext = new FilterProviderContext(context.ActionContext, filterItems);

                for (var i = 0; i < _filterProviders.Length; i++)
                {
                    _filterProviders[i].OnProvidersExecuting(filterProviderContext);
                }

                for (var i = _filterProviders.Length - 1; i >= 0; i--)
                {
                    _filterProviders[i].OnProvidersExecuted(filterProviderContext);
                }

                var filters = new IFilterMetadata[filterProviderContext.Results.Count];
                for (var i = 0; i < filterProviderContext.Results.Count; i++)
                {
                    filters[i] = filterProviderContext.Results[i].Filter;
                }

                var compiledType = _compilationService.Compile(actionDescriptor);

                var compiledActionDescriptor = new CompiledPageActionDescriptor(actionDescriptor)
                {
                    PageType = compiledType.GetTypeInfo(),
                };

                context.Result = new PageActionInvoker(
                    _diagnosticSource,
                    _logger,
                    _factory,
                    filters,
                    _valueProviderFactories,
                    context.ActionContext,
                    compiledActionDescriptor);
            }
        }

        public void OnProvidersExecuted(ActionInvokerProviderContext context)
        {
        }
    }
}
