using System;
using Microsoft.AspNetCore.Mvc.Razor;
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

            writer.WriteLine();
            writer.WriteLineHiddenDirective();

        }
    }
}
