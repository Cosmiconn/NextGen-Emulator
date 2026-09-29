using System;
using NextGen.FiestaLib;

namespace NextGen.Zone.Data
{
    /// <summary>
    /// Shared native metadata for the Pine "broadcast all" branch.
    ///
    /// ShineBroadcast::sa_Step constructs AxialListWall for the resolved
    /// script message and traverses the current map through so_AllInMap.
    /// AxialListWall::ali_Work dispatches vtable +0x784; ShinePlayer owns that
    /// slot as so_ply_Notice. The downstream opcode family is Header 8/type 17.
    ///
    /// The player notice body contains an additional category byte whose value
    /// on these KQ call paths is not independently recovered. No packet is
    /// synthesized here.
    /// </summary>
    public static class KingdomQuestPineBroadcastAllNative
    {
        public const string NativeAllTarget = "all";
        public const int NoticeVtableOffset = 0x784;
        public const byte NativeNoticeHeader = 0x08;
        public const byte NativeNoticeType = (byte)SH8Type.GmNotice;
        public const bool NoticeCategoryByteResolved = false;

        public static bool IsAllTarget(string target)
        {
            return string.Equals(
                target,
                NativeAllTarget,
                StringComparison.Ordinal);
        }
    }
}
