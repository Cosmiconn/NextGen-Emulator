using System;
using NextGen.FiestaLib.Data;

namespace NextGen.World.Data
{
    /// <summary>
    /// Exact native CKQServer::IsExisted reconnect validation recovered from
    /// the supplied NA2016 WorldManager executable.
    ///
    /// SHINE_DATETIME is decoded exactly as that function constructs tm:
    /// year=(packed&0xF)+100, month=((packed>>4)&0xF)-1, then
    /// day/hour/minute/second from 5/5/6/6-bit fields. The native code adds
    /// ten to tm_min and lets mktime normalize it.
    /// </summary>
    public static class KingdomQuestReconnectRules
    {
        public const int ReconnectWindowMinutes = 10;

        public static bool TryDecodeNativeDate(
            uint packed, out DateTime localDateTime)
        {
            int year = 2000 + (int)(packed & 0x0Fu);
            int month = (int)((packed >> 4) & 0x0Fu);
            int day = (int)((packed >> 8) & 0x1Fu);
            int hour = (int)((packed >> 13) & 0x1Fu);
            int minute = (int)((packed >> 18) & 0x3Fu);
            int second = (int)((packed >> 24) & 0x3Fu);

            try
            {
                localDateTime = new DateTime(
                    year, month, day, hour, minute, second,
                    DateTimeKind.Local);
                return true;
            }
            catch (ArgumentOutOfRangeException)
            {
                localDateTime = default(DateTime);
                return false;
            }
        }

        public static bool TryIsExisting(
            uint handle,
            string savedMapName,
            uint savedNativeDate,
            DateTime now)
        {
            if (handle == uint.MaxValue ||
                string.IsNullOrEmpty(savedMapName))
                return false;

            KingdomQuestProtocolInfo definition;
            if (!KingdomQuestProtocolDefinitionRegistry.TryGet(
                    handle, out definition) ||
                definition.MapLink == null ||
                definition.MapLink.Length != 4)
                return false;

            bool mapLinkMatches = false;
            for (int i = 0; i < definition.MapLink.Length; i++)
            {
                KingdomQuestMapProtocolInfo link = definition.MapLink[i];
                if (link != null &&
                    string.Equals(
                        link.MapName, savedMapName,
                        StringComparison.Ordinal))
                {
                    mapLinkMatches = true;
                    break;
                }
            }

            if (!mapLinkMatches)
                return false;

            // Native IsExisted additionally resolves the matching MapName
            // through the live Zone map data. The emulator's session target is
            // the authoritative dynamic-map routing object produced by MAKE,
            // so require that same allocated native MapName here.
            KingdomQuestSessionTarget target;
            if (!KingdomQuestSessionTargetRegistry.TryGet(
                    handle, out target) ||
                !string.Equals(
                    target.NativeMapName, savedMapName,
                    StringComparison.Ordinal))
                return false;

            DateTime saved;
            if (!TryDecodeNativeDate(savedNativeDate, out saved))
                return false;

            DateTime localNow = now.Kind == DateTimeKind.Utc
                ? now.ToLocalTime()
                : (now.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(now, DateTimeKind.Local)
                    : now);

            // Native code performs only current_time < mktime(saved+10min).
            // Do not add a lower-bound/future-date policy that is not present.
            return localNow < saved.AddMinutes(ReconnectWindowMinutes);
        }
    }
}
