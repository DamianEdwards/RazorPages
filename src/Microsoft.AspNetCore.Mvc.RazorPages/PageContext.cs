
namespace Microsoft.AspNetCore.Mvc.RazorPages
{
    public class PageContext : ActionContext
    {
        public PageContext(ActionContext actionContext)
            : base(actionContext)
        {
        }
    }
}
