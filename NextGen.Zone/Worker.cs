using System;
using System.Collections.Concurrent;
using System.Threading;
using NextGen.Util;

namespace NextGen.Zone
{
    [ServerModule(Util.InitializationStage.DataStore)]
    public sealed class Worker
    {
        public static Worker Instance { get; private set; }
		private readonly ConcurrentQueue<Action> callbacks = new ConcurrentQueue<Action>();
		private readonly Thread main;
		private int sleep = 1;
		private ulong ticksToSleep = 1500;
		public ulong TicksPerSecond { get; set; }
        public bool IsRunning { get; set; }

        public Worker()
        {
            sleep = Settings.Instance.SleepTime;
            ticksToSleep = Settings.Instance.TicksToSleep;
            main = new Thread(Work);
            TicksPerSecond = 0;
            IsRunning = true;
            main.Start();
            new PerformCounter();
        }

        public static bool Load()
        {
            try
            {
                Instance = new Worker();
                Instance.sleep = Settings.Instance.WorkInterval;
                return true;
            }
            catch (Exception ex) { Log.WriteLine(LogLevel.Exception, "Fehler beim Starten des Zone-Workers: {0}", ex); return false; }
        }

        public void AddCallback(Action pCallback)
        {
            callbacks.Enqueue(pCallback);
        }


        public void Stop()
        {
            if (main != null)
            {
                main.Abort();
            }
        }

        private void Work()
        {
            while (Program.ServiceInfo == null)
            {
                System.Threading.Thread.Sleep(200); // Wait..
            }
            try
            {
  
              // NextGen.Database.DatabaseHelper.Initialize(Settings.Instance.WorldConnString, "WorkerConn");
              //  Program.Entity.Characters.Count(); //test if database is online
                Log.WriteLine(LogLevel.Info, "Database Initialized.");
            }
            catch (Exception ex)
            {
                Log.WriteLine(LogLevel.Exception, "Error initializing database: {0}", ex.ToString());
                return;
            }
            Action action;
            ulong last = 0;
            DateTime lastCheck = DateTime.Now;
            DateTime lastPing = DateTime.Now;
			DateTime lastGC = DateTime.Now;
			DateTime lastClientTime = DateTime.Now;
            DateTime LastMountCheck = DateTime.Now;
            for (ulong i = 0; ; i++)
            {
                if (!this.IsRunning)
                {
                    break;
                }

                try
                {
                    DateTime now = Program.CurrentTime;

                    while (callbacks.TryDequeue(out action))
                    {
                        try
                        {

                            UserWorkItem Work = new UserWorkItem(action);
                            Work.Queue();
                          //  action();
                        }
                        catch (Exception ex)
                        {
                            Log.WriteLine(LogLevel.Exception, ex.ToString());
                        }
                    }

                    if (now.Subtract(lastCheck).TotalSeconds >= 1)
                    {
                        TicksPerSecond = i - last;
                        last = i;
                        lastCheck = now;
                        //Log.WriteLine(LogLevel.Debug, "TicksPerSecond: {0}", TicksPerSecond);
                        if (TicksPerSecond <= 100)
                        {
                            Log.WriteLine(LogLevel.Warn, "Server overload! Only {0} ticks per second!", TicksPerSecond);
                        }

                        // Am bestehenden 1-Sekunden-Takt mitlaufen lassen, nicht am
                        // 30s-Rhythmus von UpdateMountTicks - SubAbState.KeepTime
                        // (Buff-Laufzeit) liegt teils nur bei 20 Sekunden, siehe
                        // DOCUMENTATION.md Abschnitt 15.
                        if (ClientManager.Instance != null)
                        {
                            ClientManager.Instance.UpdateBuffTicks(now);
                        }
                    }

                    if (now.Subtract(lastPing).TotalSeconds >= 300)
                    {
                        ClientManager.Instance.PingCheck();
                        lastPing = now;
                    }

                    if (now.Subtract(lastGC).TotalSeconds >= 300)
                    {
                        GC.Collect();
                        lastGC = now;
                    }
                    if (now.Subtract(lastClientTime).TotalSeconds >= 60)
                    {
                     
                    }
                    if (now.Subtract(LastMountCheck).TotalSeconds >= 30)
                    {
                        ClientManager.Instance.UpdateMountTicks(now);
                        LastMountCheck = now;
                    }
                    if (i % 2000 == 0 && MapManager.Instance != null)
                    {
                        foreach (var val in MapManager.Instance.Maps)
                        {
                            foreach (var map in val.Value)
                            {
                               map.Update(now); //test
                            }
                        }
                    }

                    if (i % ticksToSleep == 0) // No max load but most ticks to be parsed. Epic win!
                    {
                        Program.CurrentTime = DateTime.Now; // Laaast update
                        Thread.Sleep(sleep);
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteLine(LogLevel.Exception, "Ohgod. {0}", ex.ToString());
                }
            }
            Log.WriteLine(LogLevel.Info, "Server stopped handling callbacks.");
        }
    }
}
