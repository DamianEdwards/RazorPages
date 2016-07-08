using Microsoft.AspNetCore.Razor;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Compilation
{
    public class RazorPagesRazorEngineHost : RazorEngineHost
    {
        public RazorPagesRazorEngineHost()
            : base(new CSharpRazorCodeLanguage())
        {
            this.DefaultBaseClass = typeof(Page).FullName;
        }
    }
}
