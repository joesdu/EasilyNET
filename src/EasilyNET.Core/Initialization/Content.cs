// ReSharper disable UnusedMember.Global

namespace EasilyNET.Core.Initialization;

/// <summary>
/// 内容
/// </summary>
public sealed class Content
{
    /// <summary>
    /// 节点信息
    /// </summary>
    public List<Section> Items { get; set; } = [];

    /// <summary>
    /// 节名称获取节
    /// </summary>
    /// <param name="sectionName">节名称</param>
    /// <param name="sectionIndex">节序列位置</param>
    /// <returns></returns>
    public Section? this[string sectionName, int sectionIndex = 0]
    {
        get
        {
            if (Items.Count < 1)
            {
                return null;
            }
            var list = Items.Where(x => x.Name == sectionName).ToList();
            return list.Count <= sectionIndex ? null : list[sectionIndex];
        }
        set
        {
            if (Items.Count < 1 || sectionIndex < 0)
            {
                return;
            }
            var list = Items.Where(x => x.Name == sectionName).ToList();
            if (list.Count <= sectionIndex)
            {
                return;
            }
            var item = list[sectionIndex];
            if (value is null)
            {
                _ = Items.Remove(item);
            }
            else
            {
                item.Args = value.Args;
                item.Line = value.Line;
                item.Name = value.Name;
            }
        }
    }

    /// <summary>
    /// 节下标获取节
    /// </summary>
    /// <param name="index">从0开始</param>
    /// <exception cref="ArgumentOutOfRangeException">数组越界</exception>
    /// <returns></returns>
    public Section this[int index]
    {
        get =>
            Items.Count <= index
                ? throw new ArgumentOutOfRangeException($"{index}")
                : Items[index];
        set
        {
            if (Items.Count <= index)
            {
                return;
            }
            Items[index] = value;
        }
    }
}
