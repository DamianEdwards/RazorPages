using System;
using Microsoft.AspNetCore.Mvc.RazorPages.Compilation;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    public class DefaultPageActivator : IPageActivator
    {
        private readonly IRazorPagesCompilationService _compilationService;
        private readonly IFileProvider _fileProvider;

        public DefaultPageActivator(
            IRazorPagesCompilationService compilationService,
            IRazorPagesFileProviderAccessor fileProvider)
        {
            _compilationService = compilationService;
            _fileProvider = fileProvider.FileProvider;
        }

        public object Create(PageContext context)
        {
            var actionDescriptor = (RazorPageActionDescriptor)context.ActionDescriptor;
            var file = _fileProvider.GetFileInfo(actionDescriptor.RelativePath);

            Type type;
            using (var stream = file.CreateReadStream())
            {
                type = _compilationService.Compile(stream, actionDescriptor.RelativePath);
            }

            return Activator.CreateInstance(type);
        }

        public void Release(PageContext context, object page)
        {
            (page as IDisposable)?.Dispose();
        }
    }
}
