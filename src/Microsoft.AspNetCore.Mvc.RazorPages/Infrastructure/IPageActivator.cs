
namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    public interface IPageActivator
    {
        object Create(PageContext context);

        void Release(PageContext context, object page);
    }
}
