namespace Messenger.Core
{
	public abstract class MessengerSingleton<TInterface> where TInterface : class
	{
		public static TInterface Instance { get; private set; }

		protected MessengerSingleton()
		{
			if (Instance != null)
				throw new System.Exception("You cannot create more than one instance of MessengerSingleton");

			Instance = this as TInterface;
		}
	}
}
