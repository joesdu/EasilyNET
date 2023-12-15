namespace EasilyNET.SourceGenerator.Share;

/// <summary>
/// 代码生成器上下文
/// </summary>
public class CodeGenerationContext
{
    /// <summary>
    /// 写入
    /// </summary>
    private readonly StringBuilder _writer = new();
    

    /// <summary>
    /// 缩进级别
    /// </summary>

    public int IndentLevel { get; private set; }

    /// <summary>
    /// 输出生成代码
    /// </summary>
    public string SourceCode => _writer.ToString();

    /// <summary>
    /// 输出生成代码转换(C#格式化后默认的格式)后文本,默认使用UTF8
    /// </summary>
    public virtual SourceText SourceText => SourceText.From(CSharpSyntaxTree.ParseText(SourceCode).GetRoot().NormalizeWhitespace().SyntaxTree.GetText().ToString(), Encoding.UTF8);

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 是否有效的
    /// </summary>
    public bool IsValid => string.IsNullOrEmpty(ErrorMessage);

    /// <summary>
    /// 写入行
    /// </summary>
    /// <param name="lines">行</param>
    /// <returns></returns>
    public CodeGenerationContext WriteLines(params string[] lines)
    {
        if (lines.Length == 0)
        {
            _writer.AppendLine();
        }
        lines = lines.SelectMany(it => it.Split('\n')).ToArray();
        foreach (var line in lines)
        {
            _writer.AppendLine($"{new string(' ', 4 * IndentLevel)}{line}");
        }
        return this;
    }

    /// <summary>
    /// 代码块
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    public IDisposable CodeBlock(string? start = null, string? end = null) => new CodeBlockScope(this, start ?? "{", end ?? "}");

    /// <summary>
    /// 缩进
    /// </summary>
    /// <returns></returns>
    public IDisposable Indent() => new IndentScope(this);

    /// <summary>
    /// 代码块范围
    /// </summary>
    private class CodeBlockScope : IDisposable
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

    /// <summary>
    /// 缩进范围
    /// </summary>
    private class IndentScope : IDisposable
    {
        private readonly CodeGenerationContext _context;

        public IndentScope(CodeGenerationContext context)
        {
            _context = context;
            _context.IndentLevel++;
        }

        public void Dispose() => _context.IndentLevel--;
    }
}