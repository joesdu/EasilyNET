namespace EasilyNET.Mongo.AspNetCore.ObjectResults;

/// <summary>
///     <para xml:lang="en">Delete objects result</para>
///     <para xml:lang="zh">删除对象结果</para>
/// </summary>
public class DeleteObjectsResult
{
    /// <summary>
    ///     <para xml:lang="en">Deleted objects</para>
    ///     <para xml:lang="zh">已删除的对象</para>
    /// </summary>
    public List<DeletedObject> Deleted { get; set; } = [];

    /// <summary>
    ///     <para xml:lang="en">Errors</para>
    ///     <para xml:lang="zh">错误</para>
    /// </summary>
    public List<DeleteError> Errors { get; set; } = [];
}

/// <summary>
///     <para xml:lang="en">Deleted object</para>
///     <para xml:lang="zh">已删除的对象</para>
/// </summary>
public class DeletedObject
{
    /// <summary>
    ///     <para xml:lang="en">Object key</para>
    ///     <para xml:lang="zh">对象键</para>
    /// </summary>
    public string Key { get; set; } = string.Empty;
}

/// <summary>
///     <para xml:lang="en">Delete error</para>
///     <para xml:lang="zh">删除错误</para>
/// </summary>
public class DeleteError
{
    /// <summary>
    ///     <para xml:lang="en">Object key</para>
    ///     <para xml:lang="zh">对象键</para>
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    ///     <para xml:lang="en">Error code</para>
    ///     <para xml:lang="zh">错误代码</para>
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    ///     <para xml:lang="en">Error message</para>
    ///     <para xml:lang="zh">错误消息</para>
    /// </summary>
    public string Message { get; set; } = string.Empty;
}