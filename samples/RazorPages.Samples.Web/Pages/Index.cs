using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace RazorPages.Samples.Web.Pages
{
    public class Index : Page
    {
        public string Message2 { get; set; } = "Hello, world!";
    }
}
