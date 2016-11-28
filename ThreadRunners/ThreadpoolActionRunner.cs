using System;
using System.Threading.Tasks;

namespace Messenger.ThreadRunners
{
    public class ThreadPoolActionRunner
        : IActionRunner
    {
        public void Run(Action action)
        {
            Task.Run(action);
        }
    }
}