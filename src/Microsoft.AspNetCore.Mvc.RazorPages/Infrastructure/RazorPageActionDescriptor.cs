using Microsoft.AspNetCore.Mvc.Abstractions;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    public class RazorPageActionDescriptor : ActionDescriptor
    {
        public string Path { get; set; }
    }
}
