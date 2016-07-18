using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc.RazorPages.ModelBinding
{
    public interface IPageArgumentBinder
    {
        Task<object> BindAsync(PageContext pageContext, Type type, string name);
    }
}
