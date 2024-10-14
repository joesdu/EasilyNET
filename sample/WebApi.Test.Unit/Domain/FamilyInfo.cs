// ReSharper disable ClassNeverInstantiated.Global

using EasilyNET.Core.Enums;

namespace MongoCRUD.Models;

/// <summary>
/// FamilyInfo
/// </summary>
public class FamilyInfo
{
    /// <summary>
    /// 数据ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 家庭成员
    /// </summary>
    public List<Person> Members { get; set; } = [];
}

/// <summary>
/// 人
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public class Person
{
    /// <summary>
    /// 数据ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 和例子中的数据结构同步
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// Name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 年龄
    /// </summary>
    public int Age { get; set; }

    /// <summary>
    /// 性别
    /// </summary>
    public EGender Gender { get; set; } = EGender.男;

    /// <summary>
    /// 临时决定,使用.Net 6新增类型保存生日,同时让例子变得丰富,明白如何将MongoDB不支持的数据类型序列化
    /// </summary>
    public DateOnly Birthday { get; set; }
}