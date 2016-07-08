using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Razor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Compilation
{
    public class DefaultRazorPagesCompilationService : IRazorPagesCompilationService
    {
        private readonly RazorPagesRazorEngineHost _host;
        private readonly ApplicationPartManager _partManager;

        public DefaultRazorPagesCompilationService(
            ApplicationPartManager partManager,
            RazorPagesRazorEngineHost host)
        {
            _partManager = partManager;
            _host = host;
        }

        public Type Compile(Stream stream, string relativePath)
        {
            var engine = new RazorTemplateEngine(_host);
            var generatorResults = engine.GenerateCode(
                stream,
                Path.GetFileNameWithoutExtension(Path.GetFileName(relativePath)),
                "RazorPages",
                relativePath);

            if (!generatorResults.Success)
            {
                Throw(stream, relativePath, generatorResults.ParserErrors);
                throw null;
            }

            var tree = CSharpSyntaxTree.ParseText(SourceText.From(generatorResults.GeneratedCode, Encoding.UTF8));
            var compilation = CSharpCompilation.Create(
                assemblyName: Path.GetRandomFileName(),
                syntaxTrees: new SyntaxTree[] { tree },
                references: GetCompilationReferences(),
                options: new CSharpCompilationOptions(outputKind: OutputKind.DynamicallyLinkedLibrary));

            using (var pe = new MemoryStream())
            {
                using (var pdb = new MemoryStream())
                {
                    var emitResult = compilation.Emit(peStream: pe, pdbStream: pdb);
                    if (!emitResult.Success)
                    {
                        Throw(stream, relativePath, generatorResults.GeneratedCode, compilation.AssemblyName, emitResult.Diagnostics);
                    }


                    pe.Seek(0, SeekOrigin.Begin);
                    pdb.Seek(0, SeekOrigin.Begin);

                    var assembly = LoadStream(pe, pdb);
                    var type = assembly.GetExportedTypes().FirstOrDefault(a => !a.IsNested);
                    return type;
                }
            }
        }

        private void Throw(Stream stream, string relativePath, IEnumerable<RazorError> errors)
        {
            var groups = errors.GroupBy(e => e.Location.FilePath ?? relativePath, StringComparer.Ordinal);

            var failures = new List<CompilationFailure>();
            foreach (var group in groups)
            {
                var filePath = group.Key;
                var fileContent = ReadFileContentsSafely(stream);
                var compilationFailure = new CompilationFailure(
                    filePath,
                    fileContent,
                    compiledContent: string.Empty,
                    messages: group.Select(parserError => CreateDiagnosticMessage(parserError, filePath)));
                failures.Add(compilationFailure);
            }

            throw new CompilationException(failures);
        }
        
        private void Throw(
            Stream stream,
            string relativePath,
            string generatedCode,
            string assemblyName,
            IEnumerable<Diagnostic> diagnostics)
        {
            var diagnosticGroups = diagnostics
                .Where(IsError)
                .GroupBy(diagnostic => GetFilePath(relativePath, diagnostic), StringComparer.Ordinal);

            var source = ReadFileContentsSafely(stream);

            var failures = new List<CompilationFailure>();
            foreach (var group in diagnosticGroups)
            {
                var sourceFilePath = group.Key;
                string sourceFileContent;
                if (string.Equals(assemblyName, sourceFilePath, StringComparison.Ordinal))
                {
                    // The error is in the generated code and does not have a mapping line pragma
                    sourceFileContent = source;
                    sourceFilePath = "who cares";
                }

                var failure = new CompilationFailure(
                    sourceFilePath,
                    source,
                    generatedCode,
                    group.Select(CreateDiagnosticMessage));

                failures.Add(failure);
            }

            throw new CompilationException(failures);
        }

        private static string GetFilePath(string relativePath, Diagnostic diagnostic)
        {
            if (diagnostic.Location == Location.None)
            {
                return relativePath;
            }

            return diagnostic.Location.GetMappedLineSpan().Path;
        }

        private static bool IsError(Diagnostic diagnostic)
        {
            return diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error;
        }

        private DiagnosticMessage CreateDiagnosticMessage(RazorError error, string relativePath)
        {
            var location = error.Location;
            return new DiagnosticMessage(
                message: error.Message,
                formattedMessage: $"{error} ({location.LineIndex},{location.CharacterIndex}) {error.Message}",
                filePath: relativePath,
                startLine: error.Location.LineIndex + 1,
                startColumn: error.Location.CharacterIndex,
                endLine: error.Location.LineIndex + 1,
                endColumn: error.Location.CharacterIndex + error.Length);
        }

        private static DiagnosticMessage CreateDiagnosticMessage(Diagnostic diagnostic)
        {
            var mappedLineSpan = diagnostic.Location.GetMappedLineSpan();
            return new DiagnosticMessage(
                diagnostic.GetMessage(),
                CSharpDiagnosticFormatter.Instance.Format(diagnostic),
                mappedLineSpan.Path,
                mappedLineSpan.StartLinePosition.Line + 1,
                mappedLineSpan.StartLinePosition.Character + 1,
                mappedLineSpan.EndLinePosition.Line + 1,
                mappedLineSpan.EndLinePosition.Character + 1);
        }

        private string ReadFileContentsSafely(Stream stream)
        {
            try
            {
                stream.Seek(0, SeekOrigin.Begin);

                var reader = new StreamReader(stream);
                return reader.ReadToEnd();
            }
            catch
            {
                // Ignore any failures
            }

            return null;
        }

        private IList<MetadataReference> GetCompilationReferences()
        {
            var feature = new MetadataReferenceFeature();
            _partManager.PopulateFeature(feature);
            return feature.MetadataReferences;
        }

        private Assembly LoadStream(MemoryStream assemblyStream, MemoryStream pdbStream)
        {
#if NET451
            return Assembly.Load(assemblyStream.ToArray(), pdbStream.ToArray());
#else
            return System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromStream(assemblyStream, pdbStream);
#endif
        }
    }
}
