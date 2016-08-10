using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.RazorPages.Compilation.Rewriters;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Razor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Compilation
{
    public class DefaultPageCompilationService : IPageCompilationService
    {
        private readonly PageRazorEngineHost _host;
        private readonly ApplicationPartManager _partManager;
        private readonly IFileProvider _fileProvider;

        private readonly string _baseNamespace;

        public DefaultPageCompilationService(
            ApplicationPartManager partManager,
            PageRazorEngineHost host,
            IPageFileProviderAccessor fileProvider)
        {
            _partManager = partManager;
            _host = host;
            _fileProvider = fileProvider.FileProvider;

            // For now let's assume the first part is the "app" assembly, and the assembly name is the "base"
            // namespace.
            _baseNamespace = _partManager.ApplicationParts.Cast<AssemblyPart>().First().Assembly.GetName().Name;
        }

        public Type Compile(PageActionDescriptor actionDescriptor)
        {
            var file = _fileProvider.GetFileInfo(actionDescriptor.RelativePath);
            using (var stream = file.CreateReadStream())
            {
                return Compile(stream, actionDescriptor.RelativePath);
            }
        }

        public Type Compile(Stream stream, string relativePath)
        {
            var engine = new RazorTemplateEngine(_host);

            var baseClass = Path.GetFileNameWithoutExtension(Path.GetFileName(relativePath));
            var @class = "Generated_" + baseClass;
            var @namespace = GetNamespace(relativePath);
            var baseClassFullName = @namespace + "." + baseClass;
            var classFullName = @namespace + "." + @class;

            var generatorResults = engine.GenerateCode(
                stream,
                @class,
                @namespace,
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

            if (compilation.GetTypeByMetadataName(@namespace + "." + baseClass) != null)
            {
                // base class exists, use it.
                var original = tree;
                tree = CSharpSyntaxTree.Create((CSharpSyntaxNode)new BaseClassRewriter(@class, baseClassFullName).Visit(tree.GetRoot()));
                compilation = compilation.ReplaceSyntaxTree(original, tree);
            }

            var classSymbol = compilation.GetTypeByMetadataName(classFullName);

            HandlerMethod onGet = null;
            HandlerMethod onPost = null;

            foreach (var method in classSymbol.GetMembers().OfType<IMethodSymbol>())
            {
                if (method.Name.StartsWith("OnGet", StringComparison.Ordinal))
                {
                    if (onGet != null)
                    {
                        throw new InvalidOperationException("You can't have more than one OnGet method");
                    }

                    onGet = HandlerMethod.FromSymbol(method, "GET");
                }
                else if (method.Name.StartsWith("OnPost", StringComparison.Ordinal))
                {
                    if (onPost != null)
                    {
                        throw new InvalidOperationException("You can't have more than one OnPost method");
                    }

                    onPost= HandlerMethod.FromSymbol(method, "POST");
                }
            }

            GenerateExecuteAsyncMethod(ref compilation, onGet, onPost);

            using (var pe = new MemoryStream())
            {
                using (var pdb = new MemoryStream())
                {
                    var emitResult = compilation.Emit(
                        peStream: pe, 
                        pdbStream: pdb, 
                        options: new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb));
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

        private void GenerateExecuteAsyncMethod(ref CSharpCompilation compilation, HandlerMethod onGet, HandlerMethod onPost)
        {
            var builder = new StringBuilder();
            builder.AppendLine("public override async Task ExecuteAsync()");
            builder.AppendLine("{");
            
            if (onGet != null)
            {
                onGet.GenerateCode(builder);
            }

            if (onPost != null)
            {
                onPost.GenerateCode(builder);
            }

            builder.AppendLine("await (this.View().ExecuteResultAsync(this.PageContext));");

            builder.AppendLine("}");

            var parsed = CSharpSyntaxTree.ParseText(builder.ToString());
            var root = parsed.GetCompilationUnitRoot();
            var method = (MethodDeclarationSyntax)root.DescendantNodes(node => !(node is MethodDeclarationSyntax)).ToArray()[0];

            var original = compilation.SyntaxTrees[0];

            var tree = CSharpSyntaxTree.Create((CSharpSyntaxNode)new AddMemberRewriter(method).Visit(original.GetRoot()));
            compilation = compilation.ReplaceSyntaxTree(original, tree);
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

        private string GetNamespace(string relativePath)
        {
            var @namespace = new StringBuilder(_baseNamespace);
            var parts = Path.GetDirectoryName(relativePath).Split('/');
            foreach (var part in parts)
            {
                @namespace.Append(".");
                @namespace.Append(part);
            }

            return @namespace.ToString();
        }

        private class HandlerMethod
        {
            public static HandlerMethod FromSymbol(IMethodSymbol symbol, string verb)
            {
                var isAsync = false;

                INamedTypeSymbol returnType = null;
                if (symbol.ReturnsVoid)
                {
                    // No return type
                }
                else
                {
                    returnType = (INamedTypeSymbol)symbol.ReturnType as INamedTypeSymbol;

                    var getAwaiters = returnType.GetMembers("GetAwaiter");
                    if (getAwaiters.Length == 0)
                    {
                        // This is a synchronous method.
                    }
                    else
                    {
                        // This is an async method.
                        IMethodSymbol getAwaiter = null;
                        for (var i = 0; i < getAwaiters.Length; i++)
                        {
                            var method = getAwaiters[i] as IMethodSymbol;
                            if (method == null)
                            {
                                continue;
                            }

                            if (method.Parameters.Length == 0)
                            {
                                getAwaiter = method;
                                break;
                            }
                        }

                        if (getAwaiter == null)
                        {
                            throw new InvalidOperationException("could not find an GetAwaiter()");
                        }

                        IMethodSymbol getResult = null;
                        var getResults = getAwaiter.ReturnType.GetMembers("GetResult");
                        for (var i = 0; i < getResults.Length; i++)
                        {
                            var method = getResults[i] as IMethodSymbol;
                            if (method == null)
                            {
                                continue;
                            }

                            if (method.Parameters.Length == 0)
                            {
                                getResult = method;
                                break;
                            }
                        }

                        if (getResult == null)
                        {
                            throw new InvalidOperationException("could not find GetResult()");
                        }

                        returnType = getResult.ReturnsVoid ? null : (INamedTypeSymbol)getResult.ReturnType;
                        isAsync = true;
                    }
                }

                return new HandlerMethod()
                {
                    IsAsync = isAsync,
                    ReturnType = returnType,
                    Symbol = symbol,
                    Verb = verb,
                };
            }

            public bool IsAsync { get; private set; }

            public INamedTypeSymbol ReturnType { get; private set; }

            public IMethodSymbol Symbol { get; private set; }

            public string Verb { get; private set; }

            public void GenerateCode(StringBuilder builder)
            {
                builder.AppendFormat(@"
    if (string.Equals(this.Context.Request.Method, ""{0}"", global::System.StringComparison.Ordinal))
    {{",
                    Verb);
                builder.AppendLine();

                for (var i = 0; i < Symbol.Parameters.Length; i++)
                {
                    var parameter = Symbol.Parameters[i];
                    var parameterTypeFullName = parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                    builder.AppendFormat("var param{0} = await BindAsync<{1}>(\"{2}\");", i, parameterTypeFullName, parameter.Name);
                    builder.AppendLine();
                }
                
                if (IsAsync && ReturnType == null)
                {
                    // async Task
                    builder.AppendFormat("await {0}({1});", Symbol.Name, string.Join(", ", Symbol.Parameters.Select((p, i) => "param" + i)));
                    builder.AppendLine();
                }
                else if (IsAsync)
                {
                    // async IActionResult
                    builder.AppendFormat("global::Microsoft.AspNetCore.Mvc.IActionResult result = await {0}({1});", Symbol.Name, string.Join(", ", Symbol.Parameters.Select((p, i) => "param" + i)));
                    builder.AppendLine();
                    builder.AppendLine("if (result != null)");
                    builder.AppendLine("{");
                    builder.AppendLine("await result.ExecuteResultAsync(this.PageContext);");
                    builder.AppendLine("return;");
                    builder.AppendLine("}");
                }
                else if (ReturnType == null)
                {
                    // void
                    builder.AppendFormat("{0}({1});", Symbol.Name, string.Join(", ", Symbol.Parameters.Select((p, i) => "param" + i)));
                    builder.AppendLine();
                }
                else
                {
                    // IActionResult
                    builder.AppendFormat("global::Microsoft.AspNetCore.Mvc.IActionResult result = {0}({1});", Symbol.Name, string.Join(", ", Symbol.Parameters.Select((p, i) => "param" + i)));
                    builder.AppendLine();
                    builder.AppendLine("if (result != null)");
                    builder.AppendLine("{");
                    builder.AppendLine("await result.ExecuteResultAsync(this.PageContext);");
                    builder.AppendLine("return;");
                    builder.AppendLine("}");
                }

                builder.AppendLine(@"
        await (this.View().ExecuteResultAsync(this.PageContext));
        return;
    }");
            }
        }
    }
}
