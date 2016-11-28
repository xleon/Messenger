using System;

namespace Messenger.Core
{
	public interface IMessengerMainThreadDispatcher
	{
		bool RequestMainThreadAction(Action action);
	}
}
