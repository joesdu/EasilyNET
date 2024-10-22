﻿using System.Collections.Immutable;
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
public sealed class ApiGroupCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds => [ApiGroupAnalyzer.DiagnosticId];

    /// <inheritdoc />
    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc />
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
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

    private static async Task<Solution> MakeDefaultDesFalseAsync(Document document, ClassDeclarationSyntax? classDecl, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var allClasses = root?.DescendantNodes().OfType<ClassDeclarationSyntax>();
        var allAttributes = allClasses?.SelectMany(c => c.AttributeLists.SelectMany(al => al.Attributes))
                                      .Where(attr => attr.Name.ToString().Contains("ApiGroup"))
                                      .ToList();
        var currentTitle = Tools.GetArgumentValue(classDecl.AttributeLists.SelectMany(al => al.Attributes).First(attr => attr.Name.ToString().Contains("ApiGroup")), 0);
        var conflictingAttributes = allAttributes?.Where(attr => Tools.GetArgumentValue(attr, 0) == currentTitle && Tools.GetArgumentValue(attr, "defaultDes") == "true").ToList();
        var rootWithFixes = root;
        foreach (var attribute in conflictingAttributes)
        {
            var argumentList = attribute.ArgumentList;
            var useDesArgument = argumentList?.Arguments.FirstOrDefault(arg => arg.ToString().Contains("defaultDes"));
            if (useDesArgument == null) continue;
            var newArgument = useDesArgument.WithExpression(SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression));
            var newArgumentList = argumentList?.ReplaceNode(useDesArgument, newArgument);
            var newAttributeSyntax = attribute.WithArgumentList(newArgumentList);
            rootWithFixes = rootWithFixes?.ReplaceNode(attribute, newAttributeSyntax);
        }
        var newDocument = document.WithSyntaxRoot(rootWithFixes);
        return newDocument.Project.Solution;
    }
}