namespace EasilyNET.SourceGenerator.Share;

/// <summary>
/// 缩进范围
/// </summary>
internal class IndentScope : IDisposable
{
    private readonly CodeGenerationContext _context;

    public IndentScope(CodeGenerationContext context)
    {
        _context = context;
        _context.IndentLevel++;
    }

    public void Dispose() => _context.IndentLevel--;
}