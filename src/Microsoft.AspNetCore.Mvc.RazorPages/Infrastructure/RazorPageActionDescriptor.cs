using Microsoft.AspNetCore.Mvc.Abstractions;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    public class RazorPageActionDescriptor : ActionDescriptor
    {
        public string RelativePath { get; set; }

        public string ViewEnginePath { get; set; }
    }
}
