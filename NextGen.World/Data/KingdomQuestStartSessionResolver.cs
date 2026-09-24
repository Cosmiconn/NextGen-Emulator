using System;
using System.Collections.Generic;
using NextGen.World.Networking;

namespace NextGen.World.Data
{
    /// <summary>
    /// Resolves the original KQTeam_LeaveParty session targets without
    /// inventing Raid state. This emulator has no Raid model at all, so every
    /// representable grouped World session is a normal Party session.
    /// </summary>
    public static class KingdomQuestStartSessionResolver
    {
        public static bool TryResolve(
            uint handle,
            IReadOnlyList<KingdomQuestMembershipEntry> members,
            out IReadOnlyList<WorldClient> clients)
        {
            clients = null;
            if (members == null)
                return false;

            var resolved = new List<WorldClient>(members.Count);
            for (int i = 0; i < members.Count; i++)
            {
                KingdomQuestMembershipEntry member = members[i];
                if (member == null || member.CharacterNumber > int.MaxValue)
                    return false;

                WorldClient client = ClientManager.Instance.GetClientByCharID(
                    (int)member.CharacterNumber);
                if (client == null ||
                    client.Character == null ||
                    client.Character.Character == null ||
                    !client.KingdomQuestHandle.HasValue ||
                    client.KingdomQuestHandle.Value != handle ||
                    unchecked((uint)client.Character.Character.ID) !=
                        member.CharacterNumber)
                    return false;

                // A non-null Party must be internally represented as a normal
                // Group. One-member groups are not a valid steady state in the
                // emulator and are rejected before any LeaveParty side effect.
                if (client.Character.Group != null)
                {
                    if (client.Character.GroupMember == null ||
                        !client.Character.Group.Members.Contains(
                            client.Character.GroupMember) ||
                        client.Character.Group.Members.Count < 2)
                        return false;
                }

                resolved.Add(client);
            }

            clients = resolved.AsReadOnly();
            return true;
        }

        public static void LeaveRepresentedParties(
            IReadOnlyList<WorldClient> clients)
        {
            if (clients == null) throw new ArgumentNullException("clients");

            for (int i = 0; i < clients.Count; i++)
            {
                WorldClient client = clients[i];
                if (client == null || client.Character == null)
                    throw new InvalidOperationException(
                        "KQ start session disappeared after preflight.");

                // KQTeam_LeaveParty refreshes party state per joiner. A prior
                // KQ joiner from the same Party may already have caused the
                // remainder to break up, so re-read Group on every iteration.
                if (client.Character.Group != null)
                    NextGen.World.GroupManager.Instance.LeaveParty(client);
            }
        }
    }
}
