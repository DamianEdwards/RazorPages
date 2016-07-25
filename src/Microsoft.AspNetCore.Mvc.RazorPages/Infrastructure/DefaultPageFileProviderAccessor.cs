using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    public class DefaultPageFileProviderAccessor : IPageProviderAccessor
    {
        public DefaultPageFileProviderAccessor(IOptions<RazorPagesOptions> options)
        {
            FileProvider = new CompositeFileProvider(options.Value.FileProviders);
        }

        public IFileProvider FileProvider { get; }
    }
}
