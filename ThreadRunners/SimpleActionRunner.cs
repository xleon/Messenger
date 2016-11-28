using System;

namespace Messenger.ThreadRunners
{
    public class SimpleActionRunner
        : IActionRunner
    {
        public void Run(Action action)
        {
            action?.Invoke();
        }
    }
}