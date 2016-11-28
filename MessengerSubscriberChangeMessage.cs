using System;

namespace Messenger
{
   	public class MessengerSubscriberChangeMessage : MessengerMessage
    {
        public Type MessageType { get; private set; }
        public int SubscriberCount { get; private set; }

        public MessengerSubscriberChangeMessage(object sender, Type messageType, int countSubscribers = 0)
            : base(sender)
        {
            SubscriberCount = countSubscribers;
            MessageType = messageType;
        }
    }
}