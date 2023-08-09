using EasilyNET.AutoDependencyInjection.Abstractions;
using EasilyNET.AutoDependencyInjection.Abstracts;
using EasilyNET.AutoDependencyInjection.Core.Attributes;
using EasilyNET.Core.Misc;
using System.Diagnostics;
using System.Reflection;

// ReSharper disable SuggestBaseTypeForParameterInConstructor

namespace EasilyNET.AutoDependencyInjection.PropertyInjection;

/// <summary>
/// 属性注入注射器类
/// </summary>
/// <param name="provider"></param>
internal sealed class PropertyInjector(IPropertyInjectionServiceProvider provider) : IPropertyInjector
{
    private static BindingFlags BindingFlags => BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    /// <inheritdoc />
    public object InjectProperties(object instance)
    {
        var type = instance.GetType();
        //找到所有需要注入的成员，进行注入
        GetAllMembers(type).Where(o => o.HasAttribute<InjectionAttribute>()).ToList().ForEach(member => InjectMember(instance, member));
        return instance;
    }


    
    /// <summary>
    /// 得到所有成员
    /// </summary>
    /// <param name="type">类型</param>
    /// <param name="members">成员集合</param>
    /// <returns></returns>
    private  IEnumerable<MemberInfo> GetAllMembers(Type type, List<MemberInfo> members = null)
    {
        members ??= new();
        members.AddRange(type.GetMembers(BindingFlags));
        return type.BaseType == null
                   ? members
                   : GetAllMembers(type.BaseType, members);
    }

    /// <summary>
    /// 需要注入的成员（属性或字段）
    /// </summary>
    /// <param name="instance">实例</param>
    /// <param name="member">成员信息</param>
    private void InjectMember(object instance, MemberInfo member)
    {
        if (member.MemberType == MemberTypes.Property)
        {
            InjectProperty(instance, (PropertyInfo)member);
            return;
        }
        InjectField(instance, (FieldInfo)member);
    }

    /// <summary>
    /// 属性注入
    /// </summary>
    /// <param name="instance">实例</param>
    /// <param name="prop">属性信息</param>
    private void InjectProperty(object instance, PropertyInfo prop)
    {
        if (prop.CanWrite)
        {
            prop.SetValue(instance, GetService(prop.PropertyType));
        }
    }

    /// <summary>
    /// 字段注入
    /// </summary>
    /// <param name="instance">实例</param>
    /// <param name="field">字段信息</param>
    private void InjectField(object instance, FieldInfo field) => field.SetValue(instance, GetService(field.FieldType));

    /// <summary>
    /// 获取服务
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    /// <exception cref="NullReferenceException"></exception>
    private object GetService(Type type) => provider.GetService(type) ?? throw new NullReferenceException($"找不到类型服务 {type.Name}");
}