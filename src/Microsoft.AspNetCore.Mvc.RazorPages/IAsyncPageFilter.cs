using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Microsoft.AspNetCore.Mvc.RazorPages
{
    public interface IAsyncPageFilter : IFilterMetadata
    {
        Task OnPageExecutionAsync(PageExecutingContext context, PageExecutionDelegate next);
    }
}
