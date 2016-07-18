using System.Linq;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages.Compilation;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    public class RazorPageActionInvokerProvider : IActionInvokerProvider
    {
        private readonly IFileProvider _fileProvider;
        private readonly IRazorPagesCompilationService _compilationService;
        private readonly MvcOptions _options;
        private readonly IValueProviderFactory[] _valueProviderFactories;

        public RazorPageActionInvokerProvider(
            IRazorPagesCompilationService compilationService,
            IRazorPagesFileProviderAccessor fileProvider,
            IOptions<MvcOptions> options)
        {
            _compilationService = compilationService;
            _fileProvider = fileProvider.FileProvider;
            _options = options.Value;
            _valueProviderFactories = _options.ValueProviderFactories.ToArray();
        }

        public int Order { get; set; }

        public void OnProvidersExecuting(ActionInvokerProviderContext context)
        {
            var actionDescriptor = context.ActionContext.ActionDescriptor as RazorPageActionDescriptor;
            if (actionDescriptor != null)
            {
                context.Result = new RazorPageActionInvoker(
                    _compilationService,
                    _fileProvider,
                    _valueProviderFactories,
                    context.ActionContext);
            }
        }

        public void OnProvidersExecuted(ActionInvokerProviderContext context)
        {
        }
    }
}
