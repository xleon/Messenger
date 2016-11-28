using System;

namespace Messenger.ThreadRunners
{
    public interface IActionRunner
    {
        void Run(Action action);
    }
}