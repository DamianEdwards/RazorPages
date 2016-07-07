using System.Collections.Generic;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Mvc.RazorPages
{
    public class RazorPagesOptions
    {
        public IList<IFileProvider> FileProviders { get; } = new List<IFileProvider>();
    }
}
