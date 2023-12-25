using System.Linq.Expressions;

namespace EasilyNET.IdentityServer.MongoStorage;

/// <summary>
/// 仓储接口定义
/// </summary>
public interface IRepository
{
    /// <summary>
    /// 获取全部
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    IQueryable<T> All<T>() where T : class, new();

    /// <summary>
    /// Where查询表达式
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="expression"></param>
    /// <returns></returns>
    IQueryable<T> Where<T>(Expression<Func<T, bool>> expression) where T : class, new();

    /// <summary>
    /// 获取单个对象
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="expression"></param>
    /// <returns></returns>
    T Single<T>(Expression<Func<T, bool>> expression) where T : class, new();

    /// <summary>
    /// 删除
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="expression"></param>
    void Delete<T>(Expression<Func<T, bool>> expression) where T : class, new();

    /// <summary>
    /// 添加
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="item"></param>
    void Add<T>(T item) where T : class, new();

    /// <summary>
    /// 批量添加
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="items"></param>
    void Add<T>(IEnumerable<T> items) where T : class, new();
}