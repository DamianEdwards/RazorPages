using Microsoft.AspNetCore.Mvc.Abstractions;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    public class PageActionDescriptor : ActionDescriptor
    {
        public string RelativePath { get; set; }

        public string ViewEnginePath { get; set; }
    }
}
