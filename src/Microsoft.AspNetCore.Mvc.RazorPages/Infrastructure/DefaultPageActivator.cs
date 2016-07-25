using System;
using Microsoft.AspNetCore.Mvc.RazorPages.Compilation;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    public class DefaultPageActivator : IPageActivator
    {
        private readonly IPageCompilationService _compilationService;
        private readonly IFileProvider _fileProvider;

        public DefaultPageActivator(
            IPageCompilationService compilationService,
            IPageFileProviderAccessor fileProvider)
        {
            _compilationService = compilationService;
            _fileProvider = fileProvider.FileProvider;
        }

        public object Create(PageContext context)
        {
            return Activator.CreateInstance(context.ActionDescriptor.PageType.AsType());
        }

        public void Release(PageContext context, object page)
        {
            (page as IDisposable)?.Dispose();
        }
    }
}
