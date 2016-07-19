using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Microsoft.AspNetCore.Mvc.RazorPages
{
    public class PageExecutingContext : FilterContext
    {
        public PageExecutingContext(PageContext pageContext, IList<IFilterMetadata> filters)
            : base(pageContext, filters)
        {
        }
    }
}
