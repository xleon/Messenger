using System;
using Messenger.ThreadRunners;

namespace Messenger.Subscriptions
{
    public abstract class BaseSubscription
    {
        public Guid Id { get; private set; }
        public string Tag { get; private set; }
        public abstract bool IsAlive { get; }

        public abstract bool Invoke(object message);

        private readonly IActionRunner _actionRunner;

        protected BaseSubscription(IActionRunner actionRunner, string tag)
        {
            _actionRunner = actionRunner;
            Id = Guid.NewGuid();
            Tag = tag;
        }

        protected void Call(Action action)
        {
            _actionRunner.Run(action);
        }
    }
}