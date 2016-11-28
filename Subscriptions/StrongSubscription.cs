using System;
using Messenger.ThreadRunners;

namespace Messenger.Subscriptions
{
    public class StrongSubscription<TMessage> : TypedSubscription<TMessage>
        where TMessage : MessengerMessage
    {
        private readonly Action<TMessage> _action;

        public override bool IsAlive => true;

        protected override bool TypedInvoke(TMessage message)
        {
            Call(() => _action?.Invoke(message));
            return true;
        }

        public StrongSubscription(IActionRunner actionRunner, Action<TMessage> action, string tag)
            : base(actionRunner, tag)
        {
            _action = action;
        }
    }
}