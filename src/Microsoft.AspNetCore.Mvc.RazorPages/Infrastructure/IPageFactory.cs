
namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    public interface IPageFactory
    {
        object CreatePage(PageContext context);

        void ReleasePage(PageContext context, object page);
    }
}
