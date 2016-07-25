using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    public interface IPageProviderAccessor
    {
        IFileProvider FileProvider { get; }
    }
}
