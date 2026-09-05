using System;
using System.Data;
using MySqlConnector;
using NextGen.Database.DataStore;
using NextGen.FiestaLib.Networking;
using NextGen.Zone.Game;

namespace NextGen.Zone
{
    public class PremiumItem
    {
        public int UniqueID { get; set; }
        public int ShopID { get; set; }
        public int CharID { get; set; }
        public byte PageID { get; set; }
        public byte Slot { get; set; }

        public void WritePremiumInfo(Packet packet)
        {
            packet.WriteInt(this.UniqueID);
            packet.WriteInt(this.ShopID);
            packet.WriteInt(0);//unk
            packet.WriteInt(0);//unk
      
        }
        public virtual void RemoveFromDatabase()
        {
            Program.CharDBManager.GetClient().ExecuteQuery("DELETE FROM PremiumItems WHERE CharID=@charId AND UniqueID=@uniqueId",
                new MySqlParameter("@charId", this.CharID),
                new MySqlParameter("@uniqueId", this.UniqueID));
        }
        public virtual void AddToDatabase()
        {
            Program.CharDBManager.GetClient().ExecuteQuery("INSERT INTO PremiumItems (CharID,ShopID,UniqueID,PageID) VALUES (@charId,@shopId,@uniqueId,@pageId)",
                new MySqlParameter("@charId", this.CharID),
                new MySqlParameter("@shopId", this.ShopID),
                new MySqlParameter("@uniqueId", this.UniqueID),
                new MySqlParameter("@pageId", this.PageID));
        }
        public static PremiumItem LoadFromDatabase(DataRow row)
        {
            PremiumItem ppItem= new PremiumItem
            {
                UniqueID = GetDataTypes.GetInt(row["UniqueID"]),
                Slot = GetDataTypes.GetByte(row["PageID"]),
                ShopID = GetDataTypes.GetInt(row["ShopID"]),
                CharID = GetDataTypes.GetInt(row["CharID"]),
                PageID = GetDataTypes.GetByte(row["PageID"])
            };
            return ppItem;
        }

    }
}
