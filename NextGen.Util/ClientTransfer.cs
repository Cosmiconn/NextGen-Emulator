using System;

namespace NextGen.Util
{
	public sealed class ClientTransfer
	{
		public string Hash { get; private set; }
		public ushort RandID { get; private set; }
		public string CharacterName { get; private set; }
        public int CharID { get; private set; }
		public int AccountID { get; private set; }
		public byte Admin { get; private set; }
		public string Username { get; private set; }
		public string HostIP { get; private set; }
		public DateTime Time { get; private set; }
		public TransferType Type { get; private set; }
		public short MapInstance { get; private set; }
        public bool HasPositionOverride { get; private set; }
        public ushort MapOverrideID { get; private set; }
        public int MapOverrideX { get; private set; }
        public int MapOverrideY { get; private set; }

		public ClientTransfer(int accountID, string userName,int CharID, byte admin, string hostIP, string hash)
		{
			this.Type = TransferType.World;
			this.AccountID = accountID;
			this.Username = userName;
            this.CharID = CharID;
			this.Admin = admin;
			this.HostIP = hostIP;
			this.Hash = hash;
			this.Time = DateTime.Now;
		}

		public ClientTransfer(int accountID, string userName, string charName,int CharID, ushort randid, byte admin, string hostIP, short mapInstance = 0,
            ushort? mapOverrideId = null, int mapOverrideX = 0, int mapOverrideY = 0)
		{
			this.Type = TransferType.Game;
			this.AccountID = accountID;
			this.Username = userName;
			this.Admin = admin;
			this.HostIP = hostIP;
			this.CharacterName = charName;
            this.CharID = CharID;
			this.RandID = randid;
			this.MapInstance = mapInstance < 0 ? (short)0 : mapInstance;
            this.HasPositionOverride = mapOverrideId.HasValue;
            this.MapOverrideID = mapOverrideId.GetValueOrDefault();
            this.MapOverrideX = mapOverrideX;
            this.MapOverrideY = mapOverrideY;
			this.Time = DateTime.Now;
		}
	}
}
