using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Compilation
{
    /// <summary>
    /// Specifies the list of <see cref="MetadataReference"/> used in Razor compilation.
    /// </summary>
    public class MetadataReferenceFeature
    {
        /// <summary>
        /// Gets the <see cref="MetadataReference"/> instances.
        /// </summary>
        public IList<MetadataReference> MetadataReferences { get; } = new List<MetadataReference>();
    }
}