namespace WinFormAutoDISample.Messenger;

/// <summary>
/// 消息传递器
/// </summary>
public interface IMessenger
{
    /// <summary>
    /// 注册消息
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    /// <param name="recipient"></param>
    /// <param name="action"></param>
    void Register<TMessage>(object recipient, Action<TMessage> action);

    /// <summary>
    /// 注销消息
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    /// <param name="recipient"></param>
    void Unregister<TMessage>(object recipient);

    /// <summary>
    /// 发送消息
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    /// <param name="message"></param>
    void Send<TMessage>(TMessage message);
}