using System;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Compilation
{
    public interface IPageCompilationService
    {
        Type Compile(PageActionDescriptor actionDescriptor);
    }
}
