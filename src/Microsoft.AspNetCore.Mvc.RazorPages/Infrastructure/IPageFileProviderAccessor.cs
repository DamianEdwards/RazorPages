using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    public interface IPageFileProviderAccessor
    {
        IFileProvider FileProvider { get; }
    }
}
