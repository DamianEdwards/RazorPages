
using System;
using System.IO;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Compilation
{
    public interface IRazorPagesCompilationService
    {
        Type Compile(Stream stream, string relativePath);
    }
}
