using System;

namespace Messenger
{
    public sealed class MessengerSubscriptionToken
        : IDisposable
    {
        public Guid Id { get; private set; }
        private readonly object[] _dependentObjects;
        private readonly Action _disposeMe;

        public MessengerSubscriptionToken(Guid id, Action disposeMe, params object[] dependentObjects)
        {
            Id = id;
            _disposeMe = disposeMe;
            _dependentObjects = dependentObjects;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                _disposeMe();
            }
        }
    }
}