using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Messenger.Subscriptions;
using Messenger.ThreadRunners;

namespace Messenger
{
    public class MessengerHub : IMessenger
    {
        private readonly Dictionary<Type, Dictionary<Guid, BaseSubscription>> _subscriptions =
            new Dictionary<Type, Dictionary<Guid, BaseSubscription>>();

        public MessengerSubscriptionToken Subscribe<TMessage>(Action<TMessage> deliveryAction, MessengerReference reference = MessengerReference.Weak, string tag = null)
            where TMessage : MessengerMessage
        {
            return SubscribeInternal(deliveryAction, new SimpleActionRunner(), reference, tag);
        }

        public MessengerSubscriptionToken SubscribeOnMainThread<TMessage>(Action<TMessage> deliveryAction,
                                                                    MessengerReference reference = MessengerReference.Weak, string tag = null)
            where TMessage : MessengerMessage
        {
            return SubscribeInternal(deliveryAction, new MainThreadActionRunner(), reference, tag);
        }

        public MessengerSubscriptionToken SubscribeOnThreadPoolThread<TMessage>(Action<TMessage> deliveryAction, MessengerReference reference = MessengerReference.Weak, string tag = null)
            where TMessage : MessengerMessage
        {
            return SubscribeInternal(deliveryAction, new ThreadPoolActionRunner(), reference, tag);
        }

        private MessengerSubscriptionToken SubscribeInternal<TMessage>(Action<TMessage> deliveryAction, IActionRunner actionRunner, MessengerReference reference, string tag)
            where TMessage : MessengerMessage
        {
            if (deliveryAction == null)
            {
                throw new ArgumentNullException(nameof(deliveryAction));
            }

            BaseSubscription subscription;

            switch (reference)
            {
                case MessengerReference.Strong:
                    subscription = new StrongSubscription<TMessage>(actionRunner, deliveryAction, tag);
                    break;

                case MessengerReference.Weak:
                    subscription = new WeakSubscription<TMessage>(actionRunner, deliveryAction, tag);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(reference), "reference type unexpected " + reference);
            }

            lock (this)
            {
                Dictionary<Guid, BaseSubscription> messageSubscriptions;
                if (!_subscriptions.TryGetValue(typeof(TMessage), out messageSubscriptions))
                {
                    messageSubscriptions = new Dictionary<Guid, BaseSubscription>();
                    _subscriptions[typeof(TMessage)] = messageSubscriptions;
                }
				//Console.WriteLine("Adding subscription {0} for {1}", subscription.Id, typeof(TMessage).Name);
                messageSubscriptions[subscription.Id] = subscription;

                PublishSubscriberChangeMessage<TMessage>(messageSubscriptions);
            }

            return new MessengerSubscriptionToken(
                            subscription.Id,
                            () => InternalUnsubscribe<TMessage>(subscription.Id),
                            deliveryAction);
        }

        public void Unsubscribe<TMessage>(MessengerSubscriptionToken mvxSubscriptionId) where TMessage : MessengerMessage
        {
            InternalUnsubscribe<TMessage>(mvxSubscriptionId.Id);
        }

        private void InternalUnsubscribe<TMessage>(Guid subscriptionGuid) where TMessage : MessengerMessage
        {
            lock (this)
            {
                Dictionary<Guid, BaseSubscription> messageSubscriptions;

                if (_subscriptions.TryGetValue(typeof(TMessage), out messageSubscriptions))
                {
                    if (messageSubscriptions.ContainsKey(subscriptionGuid))
                    {
						//Console.WriteLine("Removing subscription {0}", subscriptionGuid);
                        messageSubscriptions.Remove(subscriptionGuid);
                        // Note - we could also remove messageSubscriptions if empty here
                        //      - but this isn't needed in our typical apps
                    }
                }

                PublishSubscriberChangeMessage<TMessage>(messageSubscriptions);
            }
        }

        protected virtual void PublishSubscriberChangeMessage<TMessage>(
            Dictionary<Guid, BaseSubscription> messageSubscriptions)
            where TMessage : MessengerMessage
        {
            PublishSubscriberChangeMessage(typeof(TMessage), messageSubscriptions);
        }

        protected virtual void PublishSubscriberChangeMessage(
            Type messageType,
            Dictionary<Guid, BaseSubscription> messageSubscriptions)
        {
            var newCount = messageSubscriptions?.Count ?? 0;
            Publish(new MessengerSubscriberChangeMessage(this, messageType, newCount));
        }

        public bool HasSubscriptionsFor<TMessage>()
            where TMessage : MessengerMessage
        {
            lock (this)
            {
                Dictionary<Guid, BaseSubscription> messageSubscriptions;
                if (!_subscriptions.TryGetValue(typeof(TMessage), out messageSubscriptions))
                {
                    return false;
                }
                return messageSubscriptions.Any();
            }
        }

        public int CountSubscriptionsFor<TMessage>() where TMessage : MessengerMessage
        {
            lock (this)
            {
                Dictionary<Guid, BaseSubscription> messageSubscriptions;
                if (!_subscriptions.TryGetValue(typeof(TMessage), out messageSubscriptions))
                {
                    return 0;
                }
                return messageSubscriptions.Count;
            }
        }

        public bool HasSubscriptionsForTag<TMessage>(string tag) where TMessage : MessengerMessage
        {
            lock (this)
            {
                Dictionary<Guid, BaseSubscription> messageSubscriptions;
                if (!_subscriptions.TryGetValue(typeof(TMessage), out messageSubscriptions))
                {
                    return false;
                }
                return messageSubscriptions.Any(x => x.Value.Tag == tag);
            }
        }

        public int CountSubscriptionsForTag<TMessage>(string tag) where TMessage : MessengerMessage
        {
            lock (this)
            {
                Dictionary<Guid, BaseSubscription> messageSubscriptions;
                if (!_subscriptions.TryGetValue(typeof(TMessage), out messageSubscriptions))
                {
                    return 0;
                }
                return messageSubscriptions.Count(x => x.Value.Tag == tag);
            }
        }

        public IList<string> GetSubscriptionTagsFor<TMessage>() where TMessage : MessengerMessage
        {
            lock (this)
            {
                Dictionary<Guid, BaseSubscription> messageSubscriptions;
                if (!_subscriptions.TryGetValue(typeof(TMessage), out messageSubscriptions))
                {
                    return new List<string>(0);
                }
                return messageSubscriptions.Select(x => x.Value.Tag).ToList();
            }
        }

        public void Publish<TMessage>(TMessage message) where TMessage : MessengerMessage
        {
            if (typeof(TMessage) == typeof(MessengerMessage))
            {
				//Console.WriteLine("MessengerMessage publishing not allowed - this normally suggests non-specific generic used in calling code - switching to message.GetType()");
                Publish(message, message.GetType());
                return;
            }
            Publish(message, typeof(TMessage));
        }

        public void Publish(MessengerMessage message)
        {
            Publish(message, message.GetType());
        }

        public void Publish(MessengerMessage message, Type messageType)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            List<BaseSubscription> toNotify = null;
            lock (this)
            {
                /*
				MvxTrace.Trace("Found {0} subscriptions of all types", _subscriptions.Count);
				foreach (var t in _subscriptions.Keys)
				{
					MvxTrace.Trace("Found  subscriptions for {0}", t.Name);
				}
				*/
                Dictionary<Guid, BaseSubscription> messageSubscriptions;
                if (_subscriptions.TryGetValue(messageType, out messageSubscriptions))
                {
                    //MvxTrace.Trace("Found {0} messages of type {1}", messageSubscriptions.Values.Count, typeof(TMessage).Name);
                    toNotify = messageSubscriptions.Values.ToList();
                }
            }

            if (toNotify == null || toNotify.Count == 0)
            {
				//Console.WriteLine("Nothing registered for messages of type {0}", messageType.Name);
                return;
            }

            var allSucceeded = true;
            foreach (var subscription in toNotify)
            {
                allSucceeded &= subscription.Invoke(message);
            }

            if (!allSucceeded)
            {
				//Console.WriteLine("One or more listeners failed - purge scheduled");
                SchedulePurge(messageType);
            }
        }

        public void RequestPurge(Type messageType)
        {
            SchedulePurge(messageType);
        }

        public void RequestPurgeAll()
        {
            lock (this)
            {
                SchedulePurge(_subscriptions.Keys.ToArray());
            }
        }

        private readonly Dictionary<Type, bool> _scheduledPurges = new Dictionary<Type, bool>();

        private void SchedulePurge(params Type[] messageTypes)
        {
            lock (this)
            {
                var threadPoolTaskAlreadyRequested = _scheduledPurges.Count > 0;
                foreach (var messageType in messageTypes)
                    _scheduledPurges[messageType] = true;

                if (!threadPoolTaskAlreadyRequested)
                {
                    Task.Run(() => DoPurge());
                }
            }
        }

        private void DoPurge()
        {
            List<Type> toPurge = null;
            lock (this)
            {
                toPurge = _scheduledPurges.Select(x => x.Key).ToList();
                _scheduledPurges.Clear();
            }

            foreach (var type in toPurge)
            {
                PurgeMessagesOfType(type);
            }
        }

        private void PurgeMessagesOfType(Type type)
        {
            lock (this)
            {
                Dictionary<Guid, BaseSubscription> messageSubscriptions;
                if (!_subscriptions.TryGetValue(type, out messageSubscriptions))
                {
                    return;
                }

                var deadSubscriptionIds = new List<Guid>();
                foreach (var subscription in messageSubscriptions)
                {
                    if (!subscription.Value.IsAlive)
                    {
                        deadSubscriptionIds.Add(subscription.Key);
                    }
                }

				//Console.WriteLine("Purging {0} subscriptions", deadSubscriptionIds.Count);
                foreach (var id in deadSubscriptionIds)
                {
                    messageSubscriptions.Remove(id);
                }

                PublishSubscriberChangeMessage(type, messageSubscriptions);
            }
        }
    }
}