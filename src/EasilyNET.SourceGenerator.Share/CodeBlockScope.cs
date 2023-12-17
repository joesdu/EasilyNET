namespace EasilyNET.SourceGenerator.Share;

/// <summary>
/// 代码块范围
/// </summary>
internal class CodeBlockScope : IDisposable
{
    private readonly CodeGenerationContext _context;
    private readonly string _end;

    public CodeBlockScope(CodeGenerationContext context, string start, string end)
    {
        _end = end;
        _context = context;
        _context.WriteLines(start);
        _context.IndentLevel++;
    }

    public void Dispose()
    {
        _context.IndentLevel--;
        _context.WriteLines(_end);
    }
}