using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    public class DefaultPageFileProviderAccessor : IPageFileProviderAccessor
    {
        public DefaultPageFileProviderAccessor(IOptions<RazorPagesOptions> options)
        {
            FileProvider = new CompositeFileProvider(options.Value.FileProviders);
        }

        public IFileProvider FileProvider { get; }
    }
}
