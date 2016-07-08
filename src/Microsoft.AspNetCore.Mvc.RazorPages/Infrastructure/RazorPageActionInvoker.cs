using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.RazorPages.Compilation;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    public class RazorPageActionInvoker : IActionInvoker
    {
        private readonly ActionContext _actionContext;
        private readonly IRazorPagesCompilationService _compilationService;
        private readonly IFileProvider _fileProvider;

        public RazorPageActionInvoker(
            IRazorPagesCompilationService compilationService,
            IFileProvider fileProvider,
            ActionContext actionContext)
        {
            _compilationService = compilationService;
            _fileProvider = fileProvider;
            _actionContext = actionContext;
        }

        public Task InvokeAsync()
        {
            var actionDescriptor = (RazorPageActionDescriptor)_actionContext.ActionDescriptor;
            var file = _fileProvider.GetFileInfo(actionDescriptor.RelativePath);

            var type = _compilationService.Compile(file.CreateReadStream(), actionDescriptor.RelativePath);
            var page = (Page)Activator.CreateInstance(type);

            page.HttpContext = _actionContext.HttpContext;
            return page.ExecuteAsync();
        }
    }
}
