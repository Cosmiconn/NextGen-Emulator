using System;
using System.Collections.Concurrent;
using System.Threading;
using NextGen.Util;

namespace NextGen.Login
{
	[ServerModule(Util.InitializationStage.DataStore)]
	public sealed class Worker
	{
		public static Worker Instance { get; private set; }
		public bool IsRunning { get; set; }

		private readonly ConcurrentQueue<Action> callbacks = new ConcurrentQueue<Action>();
		private readonly Thread main;
		private int sleep = 1;

		public Worker()
		{
			main = new Thread(Work);
			IsRunning = true;
			main.Start();
		}

		[InitializerMethod]
		public static bool Load()
		{
			try
			{
				Instance = new Worker();
				Instance.sleep = Settings.Instance.WorkInterval;
				return true;
			}
			catch (Exception ex) { Log.WriteLine(LogLevel.Exception, "Fehler beim Starten des Login-Workers: {0}", ex); return false; }
		}

		public void AddCallback(Action pCallback)
		{
			callbacks.Enqueue(pCallback);
		}

		private void ConnectEntity()
		{
			// Historically wired up an EF6 DbContext + a database-versioning updater here.
			// Both were unused/dead in the original codebase (EF6 is not supported on
			// modern .NET); removed during the .NET 10 modernization pass.
		}

		private void Work()
		{
			try
			{
				ConnectEntity(); //we do this here to ensure single threaded on handle!
			   // Program.Entity.Users.Count(); //force connection to be open & test
				//Log.WriteLine(LogLevel.Info, "Database Initialized at {0}", Settings.Instance.Entity.DataCatalog);
			}
			catch (Exception ex)
			{
				Log.WriteLine(LogLevel.Exception, "Error initializing database: {0}", ex.ToString());
				return;
			}
			Action action;
			while (this.IsRunning)
			{
				while (callbacks.TryDequeue(out action))
				{
					try
					{
                        UserWorkItem Work = new UserWorkItem(action);
                        Work.Queue();
						//action();
					}
					catch (Exception ex)
					{
						Log.WriteLine(LogLevel.Exception, ex.ToString());
					}
				}
				Thread.Sleep(sleep); 
			}
			Log.WriteLine(LogLevel.Info, "Server stopped handling callbacks.");
		}
	}
}
