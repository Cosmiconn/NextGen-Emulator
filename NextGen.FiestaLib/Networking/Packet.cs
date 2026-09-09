using System;
using System.IO;
using System.Text;
using NextGen.Util;

namespace NextGen.FiestaLib.Networking
{
	public sealed class Packet : IDisposable
	{

		private MemoryStream memoryStream;
		private BinaryReader reader;
		private BinaryWriter writer;

		public ushort OpCode { get; private set; }
		public byte Header { get; private set; } //new packet system
		public byte Type { get; private set; }

		public int Length { get { return (int)this.memoryStream.Length; } }
		public int Cursor { get { return (int)this.memoryStream.Position; } }
		public int Remaining { get { return (int)(this.memoryStream.Length - this.memoryStream.Position); } }

		public Packet()
		{
			this.memoryStream = new MemoryStream();
			this.writer = new BinaryWriter(this.memoryStream);
		}

		public Packet(ushort pOpCode)
		{
			this.memoryStream = new MemoryStream();
			this.writer = new BinaryWriter(this.memoryStream);
			this.Header = (byte)(pOpCode >> 10);
			this.Type = (byte)(pOpCode & 1023);
			this.OpCode = pOpCode;
			WriteUShort(pOpCode);
		}
		public Packet(byte pHeader, byte pType)
		{
			this.memoryStream = new MemoryStream();
			this.writer = new BinaryWriter(this.memoryStream);
			this.Header = pHeader;
			this.Type = pType;
			ushort realheader = (ushort)((pHeader << 10) + (pType & 1023));
			this.OpCode = realheader;
			WriteUShort(realheader);
		}

		public Packet(byte[] pData)
		{
			this.memoryStream = new MemoryStream(pData);
			this.reader = new BinaryReader(this.memoryStream);

			ushort opCode;
			this.TryReadUShort(out opCode);
			this.Header = (byte)(opCode >> 10);
			this.Type = (byte)(opCode & 1023);
			this.OpCode = opCode;
		}

		public Packet(SH2Type type) : this(2, (byte)type) { }
		public Packet(SH3Type type) : this(3, (byte)type) { }
		public Packet(SH4Type type) : this(4, (byte)type) { }
		public Packet(SH5Type type) : this(5, (byte)type) { }
		public Packet(SH6Type type) : this(6, (byte)type) { }
		public Packet(SH7Type type) : this(7, (byte)type) { }
		public Packet(SH8Type type) : this(8, (byte)type) { }
		public Packet(SH9Type type) : this(9, (byte)type) { }
		public Packet(SH12Type type) : this(12, (byte)type) { }
		public Packet(SH15Type type) : this(15, (byte)type) { }
		public Packet(SH17Type type) : this(17, (byte)type) { }
		public Packet(SH18Type type) : this(18, (byte)type) { }
        public Packet(SH19Type type) : this(19, (byte)type) { }
		public Packet(SH20Type type) : this(20, (byte)type) { }
		public Packet(SH21Type type) : this(21, (byte)type) { }
		public Packet(SH25Type type) : this(25, (byte)type) { }
		public Packet(SH28Type type) : this(28, (byte)type) { }
		public Packet(SH29Type type) : this(29, (byte)type) { }
		public Packet(SH31Type type) : this(31, (byte)type) { }
		public Packet(SH14Type type) : this(14, (byte)type) { }
        public Packet(SH37Type type) : this(37, (byte)type) { }
        public Packet(SH38Type type) : this(38, (byte)type) { }
        public Packet(SH42Type type) : this(42, (byte)type) { }

		public void Dispose()
		{
			if (this.writer != null) this.writer.Close();
			if (this.reader != null) this.reader.Close();
			this.memoryStream = null;
			this.writer = null;
			this.reader = null;
		}

        // ... existing packet read/write members remain unchanged ...
    }
}
