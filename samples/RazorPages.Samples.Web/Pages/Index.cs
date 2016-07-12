using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RazorPages.Samples.Web.Pages
{
    public class Index : Page
    {
        public string Message { get; set; } = "Hello, world!";

        public override Task OnLoadAsync()
        {
            var message = HttpContext.Request.Query["message"];
            if (!string.IsNullOrEmpty(message))
            {
                Message = message;
            }

            return base.OnLoadAsync();
        }
    }
}
