using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc.RazorPages
{
    public class PageViewResult : IActionResult
    {
        private readonly Page _page;

        public PageViewResult(Page page)
        {
            _page = page;
        }

        public Task ExecuteResultAsync(ActionContext context)
        {
            return _page.RenderAsync();
        }
    }
}
