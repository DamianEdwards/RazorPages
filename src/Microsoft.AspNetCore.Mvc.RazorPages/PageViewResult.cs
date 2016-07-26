using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.RazorPages
{
    public class PageViewResult : IActionResult
    {
        public PageViewResult(Page page)
        {
            Page = page;
        }

        public PageViewResult(Page page, object model)
        {
            Page = page;
            Model = model;
        }

        public string ContentType { get; set; }

        public object Model { get; }

        public Page Page { get; }

        public int? StatusCode { get; set; }

        public Task ExecuteResultAsync(ActionContext context)
        {
            var executor = context.HttpContext.RequestServices.GetRequiredService<PageResultExecutor>();
            return executor.ExecuteAsync(context, this);
        }
    }
}
