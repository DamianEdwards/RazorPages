using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public class NullView : IView
    {
        public static readonly NullView Instance = new NullView();

        private NullView()
        {
        }

        public string Path => "";

        public Task RenderAsync(ViewContext context)
        {
            throw new NotImplementedException();
        }
    }
}
