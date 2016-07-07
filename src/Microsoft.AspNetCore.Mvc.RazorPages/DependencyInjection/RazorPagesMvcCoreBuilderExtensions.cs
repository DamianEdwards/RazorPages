using System;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class RazorPagesMvcCoreBuilderExtensions
    {
        public static IMvcCoreBuilder AddRazorPages(this IMvcCoreBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            RegisterServices(builder.Services);

            return builder;
        }

        public static IMvcCoreBuilder AddRazorPages(this IMvcCoreBuilder builder, Action<RazorPagesOptions> setupAction)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (setupAction == null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }

            RegisterServices(builder.Services);
            builder.Services.Configure(setupAction);

            return builder;
        }

        private static void RegisterServices(IServiceCollection services)
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IActionDescriptorProvider, RazorPageActionDescriptorProvider>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IActionInvokerProvider, RazorPageActionInvokerProvider>());
            services.TryAddSingleton<IRazorPagesFileProviderAccessor, DefaultRazorPagesFileProviderAccessor>();

            services.TryAddEnumerable(ServiceDescriptor.Transient<IConfigureOptions<RazorPagesOptions>, DefaultRazorPagesOptionsSetup>());
        }
    }
}
