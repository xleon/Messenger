using System;
using Messenger.Core;

namespace Messenger.ThreadRunners
{
    public class MainThreadActionRunner
        : IActionRunner
    {
        public void Run(Action action)
        {
            var dispatcher = MessengerMainThreadDispatcher.Instance;
            if (dispatcher == null)
            {
				//Console.WriteLine("Not able to deliver message - no ui thread dispatcher available");
                return;
            }
            dispatcher.RequestMainThreadAction(action);
        }
    }
}