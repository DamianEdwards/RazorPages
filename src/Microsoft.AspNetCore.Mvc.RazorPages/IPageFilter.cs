using Microsoft.AspNetCore.Mvc.Filters;

namespace Microsoft.AspNetCore.Mvc.RazorPages
{
    public interface IPageFilter : IFilterMetadata
    {
        void OnPageExecuting(PageExecutingContext context);

        void OnPageExecuted(PageExecutedContext context);
    }
}
