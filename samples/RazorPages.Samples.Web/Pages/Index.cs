using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RazorPages.Samples.Web.Pages
{
    public class Index : Page
    {
        public string Message2 { get; set; } = "Hello, world!";

        public override Task OnLoadAsync()
        {
            var message2 = HttpContext.Request.Query["message2"];
            if (!string.IsNullOrEmpty(message2))
            {
                Message2 = message2;
            }

            return base.OnLoadAsync();
        }
    }
}
