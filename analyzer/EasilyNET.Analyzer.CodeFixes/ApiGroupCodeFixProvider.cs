using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EasilyNET.Analyzer;

/// <inheritdoc />
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ApiGroupCodeFixProvider))]
[Shared]
public class ApiGroupCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc />
    public override sealed ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(ApiGroupAnalyzer.DiagnosticId);

    /// <inheritdoc />
    public override sealed FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc />
    public override async sealed Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;
        var declaration = root?.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().First();
        const string codeFixTitle = "已存在其他ApiGroup特性设置DefaultDes为true,若是需要设置当前位置为true请将之前的修改为false";
        context.RegisterCodeFix(CodeAction.Create(codeFixTitle,
                c => MakeDefaultDesFalseAsync(context.Document, declaration, c),
                codeFixTitle),
            diagnostic);
    }

    private static async Task<Solution> MakeDefaultDesFalseAsync(Document document, ClassDeclarationSyntax classDecl, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var allClasses = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
        var allAttributes = allClasses.SelectMany(c => c.AttributeLists.SelectMany(al => al.Attributes))
                                      .Where(attr => attr.Name.ToString() == "ApiGroup")
                                      .ToList();
        var currentTitle = GetAttributeArgumentValue(classDecl.AttributeLists.SelectMany(al => al.Attributes).First(attr => attr.Name.ToString() == "ApiGroup"), 0);
        var conflictingAttributes = allAttributes.Where(attr => GetAttributeArgumentValue(attr, 0) == currentTitle && GetAttributeArgumentValue(attr, "defaultDes") == "true").ToList();
        var rootWithFixes = root;
        foreach (var attribute in conflictingAttributes)
        {
            var argumentList = attribute.ArgumentList;
            var useDesArgument = argumentList.Arguments.FirstOrDefault(arg => arg.ToString().Contains("defaultDes"));
            if (useDesArgument != null)
            {
                var newArgument = useDesArgument.WithExpression(SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression));
                var newArgumentList = argumentList.ReplaceNode(useDesArgument, newArgument);
                var newAttributeSyntax = attribute.WithArgumentList(newArgumentList);
                rootWithFixes = rootWithFixes.ReplaceNode(attribute, newAttributeSyntax);
            }
        }
        var newDocument = document.WithSyntaxRoot(rootWithFixes);
        return newDocument.Project.Solution;
    }

    private static string GetAttributeArgumentValue(AttributeSyntax attribute, int index) => attribute.ArgumentList?.Arguments.ElementAtOrDefault(index)?.Expression.ToString().Trim('"') ?? string.Empty;

    private static string GetAttributeArgumentValue(AttributeSyntax attribute, string name)
    {
        return attribute.ArgumentList?.Arguments
                        .FirstOrDefault(arg => arg.NameEquals?.Name.Identifier.Text == name)?.Expression.ToString().Trim('"') ??
               string.Empty;
    }
}