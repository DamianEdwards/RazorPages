
using System;
using System.IO;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Compilation
{
    public interface IPageCompilationService
    {
        Type Compile(Stream stream, string relativePath);
    }
}
