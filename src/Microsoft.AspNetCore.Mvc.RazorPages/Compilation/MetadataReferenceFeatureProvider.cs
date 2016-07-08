using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Compilation
{
    /// <summary>
    /// An <see cref="IApplicationFeatureProvider{TFeature}"/> for <see cref="MetadataReferenceFeature"/> that 
    /// uses <see cref="DependencyContext"/> for registered <see cref="AssemblyPart"/> instances to create 
    /// <see cref="MetadataReference"/>.
    /// </summary>
    public class MetadataReferenceFeatureProvider : IApplicationFeatureProvider<MetadataReferenceFeature>
    {
        /// <inheritdoc />
        public void PopulateFeature(IEnumerable<ApplicationPart> parts, MetadataReferenceFeature feature)
        {
            if (parts == null)
            {
                throw new ArgumentNullException(nameof(parts));
            }

            if (feature == null)
            {
                throw new ArgumentNullException(nameof(feature));
            }

            var libraryPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var providerPart in parts.OfType<ICompilationReferencesProvider>())
            {
                var referencePaths = providerPart.GetReferencePaths();
                foreach (var path in referencePaths)
                {
                    if (libraryPaths.Add(path))
                    {
                        var metadataReference = CreateMetadataReference(path);
                        feature.MetadataReferences.Add(metadataReference);
                    }
                }
            }
        }

        private static MetadataReference CreateMetadataReference(string path)
        {
            using (var stream = File.OpenRead(path))
            {
                var moduleMetadata = ModuleMetadata.CreateFromStream(stream, PEStreamOptions.PrefetchMetadata);
                var assemblyMetadata = AssemblyMetadata.Create(moduleMetadata);

                return assemblyMetadata.GetReference(filePath: path);
            }
        }
    }
}