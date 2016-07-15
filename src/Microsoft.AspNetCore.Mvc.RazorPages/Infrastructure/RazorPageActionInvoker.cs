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
        private readonly PageContext _pageContext;
        private readonly IRazorPagesCompilationService _compilationService;
        private readonly IFileProvider _fileProvider;

        public RazorPageActionInvoker(
            IRazorPagesCompilationService compilationService,
            IFileProvider fileProvider,
            ActionContext actionContext)
        {
            _compilationService = compilationService;
            _fileProvider = fileProvider;
            _pageContext = new PageContext(actionContext);
        }

        public Task InvokeAsync()
        {
            var actionDescriptor = (RazorPageActionDescriptor)_pageContext.ActionDescriptor;
            var file = _fileProvider.GetFileInfo(actionDescriptor.RelativePath);

            Type type;
            using (var stream = file.CreateReadStream())
            {
                type = _compilationService.Compile(file.CreateReadStream(), actionDescriptor.RelativePath);
            }

            var page = (Page)Activator.CreateInstance(type);

            page.PageContext = _pageContext;
            return page.ExecuteAsync();
        }
    }
}
