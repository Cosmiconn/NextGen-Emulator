using MySqlConnector;

namespace NextGen.World.Data
{
	public static class DatabaseHelper
	{
		#region Queries

		public const string RemoveCharacterGroupQuery = "UPDATE `characters` SET GroupID = NULL WHERE Name = @name";
		public const string UpdateCharacterGroupQuery =
			"UPDATE `characters` SET GroupID = @groupId , IsGroupMaster = @isGroupMaster WHERE Name = @name";

		#endregion

		#region Methods

		public static void RemoveCharacterGroup(string pName)
		{
			using (var con = Program.DatabaseManager.GetClient())
			{
				con.ExecuteQuery(RemoveCharacterGroupQuery, new MySqlParameter("@name", pName));
			}
		}
		public static void UpdateCharacterGroup(GroupMember pMember)
		{
			using (var client = Program.DatabaseManager.GetClient())
			{
				client.ExecuteQuery(UpdateCharacterGroupQuery,
					new MySqlParameter("@groupId", pMember.Group.Id),
					new MySqlParameter("@isGroupMaster", pMember.Role == GroupRole.Master),
					new MySqlParameter("@name", pMember.Character.ID));
			}
		}


		#endregion
	}
}
