using System.Collections.Concurrent;

namespace WinFormAutoDISample.Messenger;

internal class Messenger : IMessenger
{
    private readonly ConcurrentDictionary<Type, List<WeakReference>> _recipients = new();

    public void Register<TMessage>(object recipient, Action<TMessage> action)
    {
        var messageType = typeof(TMessage);
        if (!_recipients.TryGetValue(messageType, out var value))
        {
            value = [];
            _recipients[messageType] = value;
        }
        value.Add(new(new RecipientAction<TMessage>(recipient, action)));
    }

    public void Unregister<TMessage>(object recipient)
    {
        var messageType = typeof(TMessage);
        if (_recipients.TryGetValue(messageType, out var recipes))
        {
            recipes.RemoveAll(wr => wr.Target is RecipientAction<TMessage> ra && ra.Recipient == recipient);
        }
    }

    public void Send<TMessage>(TMessage message)
    {
        var messageType = typeof(TMessage);
        if (!_recipients.TryGetValue(messageType, out var recipients)) return;
        foreach (var weakReference in recipients.ToArray())
        {
            if (weakReference.Target is RecipientAction<TMessage> ra)
            {
                ra.Action(message);
            }
            else
            {
                recipients.Remove(weakReference);
            }
        }
    }

    private class RecipientAction<TMessage>(object recipient, Action<TMessage> action)
    {
        public object Recipient { get; } = recipient;

        public Action<TMessage> Action { get; } = action;
    }
}