using Microsoft.AspNetCore.Mvc.Abstractions;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    public class RazorPageActionInvokerProvider : IActionInvokerProvider
    {
        public int Order { get; set; }

        public void OnProvidersExecuting(ActionInvokerProviderContext context)
        {
            var actionDescriptor = context.ActionContext.ActionDescriptor as RazorPageActionDescriptor;
            if (actionDescriptor != null)
            {
                context.Result = new RazorPageActionInvoker(context.ActionContext);
            }
        }

        public void OnProvidersExecuted(ActionInvokerProviderContext context)
        {
        }
    }
}
