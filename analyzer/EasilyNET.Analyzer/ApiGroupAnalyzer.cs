using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EasilyNET.Analyzer;

/// <inheritdoc />
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ApiGroupAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// DiagnosticId
    /// </summary>
    public const string DiagnosticId = "ApiGroupAnalyzer001";

    private const string Category = "Usage";
    private static readonly LocalizableString Title = "ApiGroupAttribute defaultDes 冲突";
    private static readonly LocalizableString MessageFormat = "同一个标题的 ApiGroupAttribute 只能有一个 defaultDes=true";
    private static readonly LocalizableString Description = "确保同一个标题的 ApiGroupAttribute 只能有一个 defaultDes=true。";

    private static readonly DiagnosticDescriptor Rule = new(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, true, Description);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeSyntaxNode, SyntaxKind.ClassDeclaration);
    }

    private static void AnalyzeSyntaxNode(SyntaxNodeAnalysisContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;
        var attributes = classDeclaration.AttributeLists
                                         .SelectMany(al => al.Attributes)
                                         .Where(attr => attr.Name.ToString() == "ApiGroup")
                                         .ToList();
        var titleToAttributes = new Dictionary<string, List<AttributeSyntax>>();
        foreach (var attribute in attributes)
        {
            var title = GetAttributeArgumentValue(attribute, 0);
            if (!titleToAttributes.ContainsKey(title))
            {
                titleToAttributes[title] = [];
            }
            titleToAttributes[title].Add(attribute);
        }
        foreach (var kvp in titleToAttributes)
        {
            var title = kvp.Key;
            var attrs = kvp.Value;
            var defaultDesCount = attrs.Count(attr => GetAttributeArgumentValue(attr, "defaultDes") == "true");
            if (defaultDesCount > 1)
            {
                foreach (var attr in attrs)
                {
                    if (GetAttributeArgumentValue(attr, "defaultDes") == "true")
                    {
                        var diagnostic = Diagnostic.Create(Rule, attr.GetLocation());
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }
    }

    private static string GetAttributeArgumentValue(AttributeSyntax attribute, int index) => attribute.ArgumentList?.Arguments.ElementAtOrDefault(index)?.Expression.ToString().Trim('"') ?? string.Empty;

    private static string GetAttributeArgumentValue(AttributeSyntax attribute, string name)
    {
        return attribute.ArgumentList?.Arguments
                        .FirstOrDefault(arg => arg.NameEquals?.Name.Identifier.Text == name)?.Expression.ToString().Trim('"') ??
               string.Empty;
    }
}