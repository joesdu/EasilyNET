using System.Reflection;
using EasilyNET.AutoDependencyInjection.Abstractions;
using EasilyNET.AutoDependencyInjection.Abstracts;
using EasilyNET.AutoDependencyInjection.Core.Attributes;
using EasilyNET.Core.Misc;
using Microsoft.AspNetCore.Components;

namespace EasilyNET.AutoDependencyInjection.PropertyInjection;

/// <summary>
/// 属性注入注射器类
/// </summary>
internal sealed class PropertyInjector : IPropertyInjector
{
    private static BindingFlags BindingFlags =>
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;


    private readonly IPropertyInjectionServiceProvider _propertyInjectionServiceProvider;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="propertyInjectionServiceProvider"></param>
    public PropertyInjector(IPropertyInjectionServiceProvider propertyInjectionServiceProvider)
    {
        _propertyInjectionServiceProvider = propertyInjectionServiceProvider;
    }


    /// <summary>
    /// 判断是否需要属性注入
    /// </summary>
    /// <param name="instance"></param>
    private void IsInjectProperties(object? instance)
    {
        if (instance is null) return;
        var type = instance as Type ?? instance.GetType();
        //找到所有需要注入的成员，进行注入
        type.GetMembers(BindingFlags).Where(o => o.HasAttribute<InjectionAttribute>()).ToList()
            .ForEach(member => InjectMember(instance, member));
    }

    /// <summary>
    /// 把属性注入
    /// </summary>
    /// <param name="instance">要注入的实例</param>
    /// <returns></returns>
    public object InjectProperties(object instance)
    {
        IsInjectProperties(instance);
        return instance;
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
            InjectProperty(instance, (PropertyInfo) member);
            return;
        }

        InjectField(instance, (FieldInfo) member);
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

    private object GetService(Type type) =>
        _propertyInjectionServiceProvider.GetService(type) ??
        throw new Exception($"找不到类型服务 {type.Name}");
}