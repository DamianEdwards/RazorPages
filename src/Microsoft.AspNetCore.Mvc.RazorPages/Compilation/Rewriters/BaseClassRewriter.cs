using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Compilation.Rewriters
{
    public class BaseClassRewriter : CSharpSyntaxRewriter
    {
        private readonly string _baseClass;
        private readonly string _class;
        
        public BaseClassRewriter(string @class, string baseClass)
        {
            _class = @class;
            _baseClass = baseClass;
        }

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            if (node.Identifier.Text == _class)
            {
                return node.WithBaseList(
                    BaseList(
                        SingletonSeparatedList<BaseTypeSyntax>(
                            SimpleBaseType(
                                ParseTypeName(_baseClass)))));
            }

            return base.VisitClassDeclaration(node);
        }
    }
}
