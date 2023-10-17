using Yitter.IdGenerator;

namespace EasilyNET.Core.BaseType;

using YitterId = Yitter.IdGenerator.IIdGenerator;

/// <summary>
/// 雪花ID
/// </summary>
public sealed class SnowFlakeId : ISnowFlakeId
{
    /// <summary>
    /// 当前对象
    /// </summary>
    public static ISnowFlakeId Default = new SnowFlakeId(1);

    private SnowFlakeId(ushort workerId)
    {
        _IdGenInstance ??= new Lazy<IIdGenerator>(new DefaultIdGenerator(new IdGeneratorOptions(workerId)));
    }

    /// <summary>
    /// ID生成接口
    /// </summary>
    private static Lazy<IIdGenerator>? _IdGenInstance;

    /// <summary>
    /// 重新设置ID生成配置
    /// </summary>
    /// <param name="options">配置</param>
    public static void SetIdGenerator(IdGeneratorOptions options)
    {
        _IdGenInstance ??= new Lazy<IIdGenerator>(new DefaultIdGenerator(options));
    }

    /// <summary>
    ///  下一个Id
    /// </summary>
    /// <returns></returns>
    public long NextId()
    {
        return _IdGenInstance!.Value.NewLong();
    }
}

/// <summary>
/// 雪花ID接口
/// </summary>
public interface ISnowFlakeId
{
    /// <summary>
    /// 下一个Id
    /// </summary>
    /// <returns></returns>
    long NextId();
}