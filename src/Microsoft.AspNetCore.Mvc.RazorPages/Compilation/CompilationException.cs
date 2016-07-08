using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Diagnostics;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Compilation
{
    public class CompilationException : Exception, ICompilationException
    {
        public CompilationException(List<CompilationFailure> compilationFailures)
        {
            CompilationFailures = compilationFailures;
        }

        public IEnumerable<CompilationFailure> CompilationFailures { get; }
    }
}
