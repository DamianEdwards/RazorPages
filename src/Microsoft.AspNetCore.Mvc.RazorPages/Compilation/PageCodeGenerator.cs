using System;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Razor.Chunks;
using Microsoft.AspNetCore.Razor.CodeGenerators;
using Microsoft.AspNetCore.Razor.CodeGenerators.Visitors;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Compilation
{
    public class PageCodeGenerator : CSharpCodeGenerator
    {
        public PageCodeGenerator(CodeGeneratorContext context) 
            : base(context)
        {
        }

        protected override CSharpCodeVisitor CreateCSharpCodeVisitor(
            CSharpCodeWriter writer,
            CodeGeneratorContext context)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var csharpCodeVisitor = base.CreateCSharpCodeVisitor(writer, context);

            var attributeRenderer = new MvcTagHelperAttributeValueCodeRenderer(new GeneratedTagHelperAttributeContext()
            {
                CreateModelExpressionMethodName = "CreateModelExpression",
                ModelExpressionProviderPropertyName = "ModelExpressionProvider",
                ModelExpressionTypeName = "Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExpression",
                ViewDataPropertyName = "ViewData",
            });
            csharpCodeVisitor.TagHelperRenderer.AttributeValueCodeRenderer = attributeRenderer;

            return csharpCodeVisitor;
        }

        protected override void BuildConstructor(CSharpCodeWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            base.BuildConstructor(writer);

            writer.WriteLineHiddenDirective();

            var injectVisitor = new InjectChunkVisitor(writer, Context, "Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute");
            injectVisitor.Accept(Context.ChunkTreeBuilder.Root.Children);

            var modelVisitor = new ModelChunkVisitor(writer, Context);
            modelVisitor.Accept(Context.ChunkTreeBuilder.Root.Children);
            if (modelVisitor.ModelType != null)
            {
                writer.WriteLine();

                // public ModelType Model => ViewData?.Model ?? default(ModelType);
                writer.Write("public ").Write(modelVisitor.ModelType).Write(" Model => ViewData?.Model ?? default(").Write(modelVisitor.ModelType).Write(");");

                writer.WriteLine();

                var viewDataType = $"global::Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary<{modelVisitor.ModelType}>";
                writer.Write("public new ").Write(viewDataType).Write(" ViewData").WriteLine();
                writer.Write("{").WriteLine();
                writer.IncreaseIndent(4);
                writer.Write("get { return (").Write(viewDataType).Write(")base.ViewData; }").WriteLine();
                writer.DecreaseIndent(4);
                writer.Write("}").WriteLine();
            }

            writer.WriteLine();
            writer.WriteLineHiddenDirective();

        }

        private class ModelChunkVisitor : CodeVisitor<CSharpCodeWriter>
        {
            public ModelChunkVisitor(CSharpCodeWriter writer, CodeGeneratorContext context)
                : base(writer, context)
            {
            }

            public string ModelType { get; set; }
            
            public override void Accept(Chunk chunk)
            {
                if (chunk is ModelChunk)
                {
                    Visit((ModelChunk)chunk);
                }
                else
                {
                    base.Accept(chunk);
                }
            }

            private void Visit(ModelChunk chunk)
            {
                ModelType = chunk.ModelType;
            }
        }
    }
}
