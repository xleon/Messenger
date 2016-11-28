using System;

namespace Messenger
{
    /// <summary>
    /// Base class for messages that provides weak refrence storage of the sender
    /// </summary>
    public abstract class MessengerMessage
    {
        /// <summary>
        /// Gets the original sender of the message
        /// </summary>
        public object Sender { get; private set; }

        /// <summary>
        /// Initializes a new instance of the MvxMessage class.
        /// </summary>
        /// <param name="sender">Message sender (usually "this")</param>
        protected MessengerMessage(object sender)
        {
            if (sender == null)
                throw new ArgumentNullException(nameof(sender));

            Sender = sender;
        }
    }
}