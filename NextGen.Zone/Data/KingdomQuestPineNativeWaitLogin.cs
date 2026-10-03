using NextGen.Zone.Game;

namespace NextGen.Zone.Data
{
    public interface IKingdomQuestPinePlayerPresence
    {
        bool TryHasPlayer(out bool present);
    }

    /// <summary>
    /// Player iterator 0x00428020: object type 2 and modes 1, 2, 4 or 6.
    /// Read the bound map's current objects, never the registration roster.
    /// </summary>
    public sealed class KingdomQuestPineMapPlayerPresence : IKingdomQuestPinePlayerPresence
    {
        private readonly Map map;

        public KingdomQuestPineMapPlayerPresence(Map map)
        {
            this.map = map;
        }

        public static bool IsPresentPlayerMode(byte mode)
        {
            return mode == 1 || mode == 2 || mode == 4 || mode == 6;
        }

        public bool TryHasPlayer(out bool present)
        {
            present = false;
            if (map == null || map.Objects == null)
                return false;
            foreach (MapObject obj in map.Objects.Values)
            {
                var player = obj as ZoneCharacter;
                if (player != null && ReferenceEquals(player.Map, map) &&
                    IsPresentPlayerMode((byte)player.State))
                {
                    present = true;
                    break;
                }
            }
            return true;
        }
    }

    /// <summary>
    /// ShineWaitUserLogin::sa_Step 0x004EE430. One instance per Pine
    /// ProcessStack: deadline lives at stack +0x1010C, NOT in frame state.
    /// State zero initializes deadline = now + 2400 and becomes one. The
    /// same call checks presence; present wins over strict unsigned timeout.
    /// </summary>
    public sealed class KingdomQuestPineNativeWaitLogin : IKingdomQuestPineWaitLoginSource
    {
        public const uint NativeStepAddress = 0x004EE430;
        public const uint TimeoutTicks = 2400;
        private readonly IKingdomQuestPineNativeTickSource clock;
        private readonly IKingdomQuestPinePlayerPresence players;
        private uint deadline;
        private bool initialized;

        public KingdomQuestPineNativeWaitLogin(
            IKingdomQuestPineNativeTickSource clock,
            IKingdomQuestPinePlayerPresence players)
        {
            this.clock = clock;
            this.players = players;
        }

        public bool TryStep(string targetIdentifier,
            KingdomQuestPineTokenValue destination, ref int nativeState,
            out bool completed)
        {
            completed = false;
            uint now;
            bool present;
            if (string.IsNullOrEmpty(targetIdentifier) || destination == null ||
                clock == null || players == null ||
                (nativeState != 0 && nativeState != 1) ||
                (nativeState != 0 && !initialized) ||
                !clock.TryGetCurrentTick(out now) ||
                !players.TryHasPlayer(out present))
                return false;

            if (nativeState == 0)
            {
                deadline = unchecked(now + TimeoutTicks);
                initialized = true;
                nativeState = 1;
            }

            // Native JB at 0x004EE59F: equality still waits. Deliberately
            // preserve the unsigned wrap behavior, not a signed delta test.
            if (present || deadline < now)
            {
                if (!destination.TrySetAscii(present ? "1" : "0"))
                    return false;
                completed = true;
            }
            return true;
        }
    }
}
