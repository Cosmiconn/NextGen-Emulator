using System;
using System.Net.Sockets;
using NextGen.InterLib.NetworkObjects;
using NextGen.Util;

namespace NextGen.Login.InterServer
{
	[ServerModule(Util.InitializationStage.Services)]
	public sealed class WorldAcceptor : AbstractAcceptor
	{
		public static WorldAcceptor Instance { get; private set; }

		public WorldAcceptor(int port) : base(port)
		{
			this.OnIncommingConnection += new OnIncomingConnectionDelegate(WorldAcceptor_OnIncommingConnection);
			Log.WriteLine(LogLevel.Info, "Listening on port {0}", port);
		}

		private void WorldAcceptor_OnIncommingConnection(Socket session)
		{
			// So something with it X:
			Log.WriteLine(LogLevel.Info, "Incomming connection from {0}", session.RemoteEndPoint);
			WorldConnection wc = new WorldConnection(session);
		}

		[InitializerMethod]
		public static bool Load()
		{
			return Load(Settings.Instance.InterServerPort);
		}

		public static bool Load(int port)
		{
			try
			{
				Instance = new WorldAcceptor(port);
				return true;
			}
			catch (Exception ex) { Log.WriteLine(LogLevel.Exception, "Fehler beim Starten des World-Acceptors auf Port {0}: {1}", port, ex); return false; }
		}

	}
}
