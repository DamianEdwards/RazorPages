using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    public class DefaultRazorPagesOptionsSetup : IConfigureOptions<RazorPagesOptions>
    {
        private readonly IHostingEnvironment _hostingEnvironment;

        public DefaultRazorPagesOptionsSetup(IHostingEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
        }

        public void Configure(RazorPagesOptions options)
        {
            if (_hostingEnvironment.ContentRootFileProvider != null)
            {
                options.FileProviders.Add(_hostingEnvironment.ContentRootFileProvider);
            }
        }
    }
}
