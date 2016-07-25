using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace RazorPages.Samples.Web.Pages
{
    public class Index : Page
    {
        public string Message2 { get; set; } = "Hello, world!";

        [RazorInject]
        public IHtmlHelper<dynamic> Html { get; set; }

        public class Customer
        {
            public int Id { get; set; }

            public string Name { get; set; }
        }
    }
}
