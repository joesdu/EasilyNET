using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EasilyNET.Analyzer;

/// <summary>
/// Tools
/// </summary>
public static class Tools
{
    /// <summary>
    /// 获取位置参数参数值
    /// </summary>
    /// <param name="attribute"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    public static string GetArgumentValue(AttributeSyntax attribute, int index)
    {
        var argument = attribute.ArgumentList?.Arguments.ElementAtOrDefault(index);
        return argument?.Expression is LiteralExpressionSyntax literal ? literal.Token.ValueText : string.Empty;
    }

    /// <summary>
    /// 获取具名参数参数值
    /// </summary>
    /// <param name="attribute"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static string GetArgumentValue(AttributeSyntax attribute, string name)
    {
        var argument = attribute.ArgumentList?.Arguments
                                .FirstOrDefault(arg => arg.NameEquals?.Name.Identifier.Text == name);
        return argument?.Expression is LiteralExpressionSyntax literal ? literal.Token.ValueText : string.Empty;
    }
}