using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    public class RazorPageActionInvoker : IActionInvoker
    {
        private readonly ActionContext _actionContext;

        public RazorPageActionInvoker(ActionContext actionContext)
        {
            _actionContext = actionContext;
        }

        public Task InvokeAsync()
        {
            return _actionContext.HttpContext.Response.WriteAsync($"Hello from {_actionContext.ActionDescriptor.DisplayName}");
        }
    }
}
