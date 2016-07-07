using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    public interface IRazorPagesFileProviderAccessor
    {
        IFileProvider FileProvider { get; }
    }
}
