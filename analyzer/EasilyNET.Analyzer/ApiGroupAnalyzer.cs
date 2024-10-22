using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EasilyNET.Analyzer;

/// <inheritdoc />
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ApiGroupAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// DiagnosticId
    /// </summary>
    public const string DiagnosticId = nameof(ApiGroupAnalyzer);

    private const string Category = "Usage";
    private static readonly LocalizableString Title = "ApiGroupAttribute defaultDes 冲突";
    private static readonly LocalizableString MessageFormat = "同一个标题的 ApiGroupAttribute 只能有一个 defaultDes 设置为 true";
    private static readonly LocalizableString Description = "确保同一个标题的 ApiGroupAttribute 只能有一个 defaultDes 设置为 true.";

    private static readonly DiagnosticDescriptor Rule = new(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, true, Description);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeSyntaxNode, SyntaxKind.ClassDeclaration);
    }

    private static void AnalyzeSyntaxNode(SyntaxNodeAnalysisContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;
        var attributes = classDeclaration.AttributeLists
                                         .SelectMany(al => al.Attributes)
                                         .Where(attr => attr.Name.ToFullString().Contains("ApiGroup")).ToList();
        var titleToAttributes = new ConcurrentDictionary<string, HashSet<AttributeSyntax>>();
        foreach (var attribute in attributes)
        {
            var title = Tools.GetArgumentValue(attribute, 0);
            if (!titleToAttributes.ContainsKey(title))
            {
                titleToAttributes.TryAdd(title, []);
            }
            titleToAttributes[title].Add(attribute);
        }
        foreach (var kvp in titleToAttributes)
        {
            var attrs = kvp.Value;
            var defaultDesCount = attrs.Count(attr => Tools.GetArgumentValue(attr, "defaultDes") == "true" || Tools.GetArgumentValue(attr, 2) == "true");
            // ReSharper disable once InvertIf
            if (defaultDesCount > 1)
            {
                foreach (var diagnostic in attrs.Select(attr => Diagnostic.Create(Rule, attr.GetLocation())))
                {
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}