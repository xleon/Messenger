using System;
using Messenger.ThreadRunners;

namespace Messenger.Subscriptions
{
    public abstract class TypedSubscription<TMessage> : BaseSubscription
        where TMessage : MessengerMessage
    {
        protected TypedSubscription(IActionRunner actionRunner, string tag)
            : base(actionRunner, tag)
        {
        }

        public sealed override bool Invoke(object message)
        {
            var typedMessage = message as TMessage;
            if (typedMessage == null)
				throw new Exception("Unexpected message "+ message);

            return TypedInvoke(typedMessage);
        }

        protected abstract bool TypedInvoke(TMessage message);
    }
}