using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Microsoft.AspNetCore.Mvc.RazorPages
{
    public class PageExecutedContext : FilterContext
    {
        public PageExecutedContext(PageContext pageContext, IList<IFilterMetadata> filters)
            : base(pageContext, filters)
        {
        }
    }
}
