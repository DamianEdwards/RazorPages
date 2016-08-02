using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Razor.Generator;
using Microsoft.AspNetCore.Razor.Parser;
using Microsoft.AspNetCore.Razor.Parser.SyntaxTree;
using Microsoft.AspNetCore.Razor.Tokenizer.Symbols;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Compilation
{
    public class PageCodeParser : CSharpCodeParser
    {
        private const string ModelKeyword = "model";
        private const string InjectKeyword = "inject";
        private bool _modelStatementFound;

        public PageCodeParser()
        {
            MapDirectives(ModelDirective, ModelKeyword);
            MapDirectives(InjectDirective, InjectKeyword);
        }

        protected virtual void ModelDirective()
        {
            // @model MyModelType
            Context.CurrentBlock.Type = BlockType.Directive;
            AssertDirective(ModelKeyword);

            var start = CurrentLocation;
            AcceptAndMoveNext();
            Output(SpanKind.MetaCode);

            if (_modelStatementFound)
            {
                Context.OnError(start, "only one model keyword allowed", ModelKeyword.Length);

                // Continue parsing and catch more errors since this is a semantic error.
            }

            AcceptWhile(IsSpacingToken(includeNewLines: false, includeComments: true));
            Output(SpanKind.Code);

            if (!NamespaceOrTypeName())
            {
                Context.OnError(start, "need a type name", ModelKeyword.Length);

                // On error, recover at the next line
                AcceptUntil(CSharpSymbolType.NewLine);
                return;
            }

            // We parsed the whitespace + type name in the current span, so let's extract the type name.
            // We have to do a GetContent() here because the name is potentially made up of multiple
            // tokens.
            var typeName = Span.GetContent().Value;

            AcceptWhile(IsSpacingToken(includeNewLines: false, includeComments: true));

            Optional(CSharpSymbolType.Semicolon);

            AcceptWhile(IsSpacingToken(includeNewLines: false, includeComments: true));

            if (At(CSharpSymbolType.NewLine))
            {
                AcceptAndMoveNext();
            }
            else if (EndOfFile)
            {
                // Do nothing
            }
            else
            {
                Context.OnError(start, "need a newline", InjectKeyword.Length);

                // On Error, recover at the next line
                AcceptUntil(CSharpSymbolType.NewLine);
                return;
            }

            Span.ChunkGenerator = new ModelChunkGenerator(typeName);

            CompleteBlock();
            Output(SpanKind.Code, AcceptedCharacters.AnyExceptNewline);

            _modelStatementFound = true;
        }

        protected virtual void InjectDirective()
        {
            // @inject MyApp.MyService MyServicePropertyName
            Context.CurrentBlock.Type = BlockType.Directive;
            AssertDirective(InjectKeyword);

            var start = CurrentLocation;
            AcceptAndMoveNext();
            Output(SpanKind.MetaCode);

            AcceptWhile(IsSpacingToken(includeNewLines: false, includeComments: true));
            Output(SpanKind.Code);
            
            if (!NamespaceOrTypeName())
            {
                Context.OnError(start, "need a type name", InjectKeyword.Length);

                // On error, recover at the next line
                AcceptUntil(CSharpSymbolType.NewLine);
                return;
            }

            // We parsed the whitespace + type name in the current span, so let's extract the type name.
            // We have to do a GetContent() here because the name is potentially made up of multiple
            // tokens.
            var typeName = Span.GetContent().Value;

            AcceptWhile(IsSpacingToken(includeNewLines: false, includeComments: true));

            if (!At(CSharpSymbolType.Identifier))
            {
                Context.OnError(start, "need a property name", InjectKeyword.Length);

                // On error, recover at the next line
                AcceptUntil(CSharpSymbolType.NewLine);
                return;
            }

            var propertyName = CurrentSymbol.Content;
            AcceptAndMoveNext();

            AcceptWhile(IsSpacingToken(includeNewLines: false, includeComments: true));

            Optional(CSharpSymbolType.Semicolon);

            AcceptWhile(IsSpacingToken(includeNewLines: false, includeComments: true));

            if (At(CSharpSymbolType.NewLine))
            {
                AcceptAndMoveNext();
            }
            else if (EndOfFile)
            {
                // Do nothing
            }
            else
            {
                Context.OnError(start, "need a newline", InjectKeyword.Length);

                // On Error, recover at the next line
                AcceptUntil(CSharpSymbolType.NewLine);
                return;
            }

            Span.ChunkGenerator = new InjectParameterGenerator(typeName, propertyName);

            CompleteBlock();
            Output(SpanKind.Code, AcceptedCharacters.AnyExceptNewline);
        }
    }
}
