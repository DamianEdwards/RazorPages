using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.RazorPages.Compilation;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    public class RazorPageActionInvokerProvider : IActionInvokerProvider
    {
        private readonly IFileProvider _fileProvider;
        private readonly IRazorPagesCompilationService _compilationService;

        public RazorPageActionInvokerProvider(
            IRazorPagesCompilationService compilationService,
            IRazorPagesFileProviderAccessor fileProvider)
        {
            _compilationService = compilationService;
            _fileProvider = fileProvider.FileProvider;
        }

        public int Order { get; set; }

        public void OnProvidersExecuting(ActionInvokerProviderContext context)
        {
            var actionDescriptor = context.ActionContext.ActionDescriptor as RazorPageActionDescriptor;
            if (actionDescriptor != null)
            {
                context.Result = new RazorPageActionInvoker(_compilationService, _fileProvider, context.ActionContext);
            }
        }

        public void OnProvidersExecuted(ActionInvokerProviderContext context)
        {
        }
    }
}
