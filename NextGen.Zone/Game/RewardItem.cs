using NextGen.Database.DataStore;
using NextGen.FiestaLib;
using NextGen.FiestaLib.Data;
using NextGen.FiestaLib.Networking;
using NextGen.Zone.Data;
using NextGen.Database.Storage;
using MySqlConnector;

namespace NextGen.Zone.Game
{
    public sealed class RewardItem : Item
    {
        
        public override ushort ID { get; set; }
        public override sbyte Slot { get; set; }
        public override UpgradeStats UpgradeStats { get; set; }
        public int CharID { get; set; }
        public ushort PageID { get; set; }
        public override ItemInfo ItemInfo { get { return DataProvider.Instance.GetItemInfo(this.ID); } }
        public  void AddToDatabase()
        {
            Program.CharDBManager.GetClient().ExecuteQuery("INSERT INTO  Rewarditems (CharID,Slot,ItemID,PageID) VALUES (@charId,@slot,@itemId,@pageId)",
                new MySqlParameter("@charId", this.CharID),
                new MySqlParameter("@slot", this.Slot),
                new MySqlParameter("@itemId", this.ID),
                new MySqlParameter("@pageId", this.PageID));
        }
        public void RemoveFromDatabase()
        {
            Program.CharDBManager.GetClient().ExecuteQuery("DELETE FROM Rewarditems WHERE CharID=@charId AND ItemID=@itemId",
                new MySqlParameter("@charId", this.CharID),
                new MySqlParameter("@itemId", this.ID));
        }
        public override void WriteInfo(Packet pPacket, bool WriteStats = true)
        {
            byte length;
            byte statCount;

            if (ItemInfo.Slot == ItemSlot.None)
            {
                length = GetInfoLength(ItemInfo.Class);
                statCount = 0;
            }
            else
            {
                length = GetEquipLength(this);
                statCount = GetInfoStatCount(this);
            }
            byte lenght = 9;//later
            pPacket.WriteByte(lenght);
            pPacket.WriteByte((byte)this.Slot);//itemslot
            pPacket.WriteByte(0x08);//unk
            if (WriteStats)
            {
                if (ItemInfo.Slot == ItemSlot.None)
                    this.WriteStats(pPacket);
                else
                    WriteEquipStats(pPacket);
            }

        }

        public static  RewardItem LoadFromDatabase(System.Data.DataRow row)
        {
           RewardItem ppItem = new RewardItem
            {
                Slot = GetDataTypes.GetSByte(row["Slot"]),
                ID = GetDataTypes.GetUshort(row["ItemID"]),
                CharID = GetDataTypes.GetInt(row["CharID"]),
                PageID = GetDataTypes.GetByte(row["PageID"])
            };
            
            return ppItem;
        }
    }
}
