using System;
using Messenger.ThreadRunners;

namespace Messenger.Subscriptions
{
    public class WeakSubscription<TMessage> : TypedSubscription<TMessage>
        where TMessage : MessengerMessage
    {
        private readonly WeakReference _weakReference;

        public override bool IsAlive => _weakReference.IsAlive;

        protected override bool TypedInvoke(TMessage message)
        {
            if (!_weakReference.IsAlive)
                return false;

            var action = _weakReference.Target as Action<TMessage>;
            if (action == null)
                return false;

            Call(() =>
            {
                action?.Invoke(message);
            });
            return true;
        }

        public WeakSubscription(IActionRunner actionRunner, Action<TMessage> listener, string tag)
            : base(actionRunner, tag)
        {
            _weakReference = new WeakReference(listener);
        }
    }
}