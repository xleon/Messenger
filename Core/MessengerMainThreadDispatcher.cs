using System;
using System.Reflection;
using System.Diagnostics;

namespace Messenger.Core
{
	public abstract class MessengerMainThreadDispatcher : MessengerSingleton<IMessengerMainThreadDispatcher>
	{
		protected static void ExceptionMaskedAction(Action action)
		{
			try
			{
				action();
			}
			catch(TargetInvocationException exception)
			{
				Debug.WriteLine("TargetInvocateException masked " + exception.InnerException.ToString());
			}
			catch(Exception exception)
			{
				// note - all exceptions masked!
				Debug.WriteLine("Exception masked " + exception.ToString());
			}
		}
	}
}
