namespace NextGen.Zone.Data
{
    /// <summary>
    /// Single mutation boundary for the three Pine external command families
    /// whose source/native owner-plan shape is shared by UnderHall and
    /// UnderHall2.
    ///
    /// Implementations are intentionally external: this contract does not map
    /// Pine runtime handles to emulator MapObjectID values, does not reuse
    /// ZoneCharacter.ChangeMap by assumption and does not route mob operations
    /// through the legacy MobBreedLocation/Mobspawn subsystem.
    /// </summary>
    public interface IKingdomQuestPineSharedExternalOwner
    {
        bool TryLinkTo(
            KingdomQuestPineLinkToOwnerPlan plan,
            KingdomQuestPineVariableStack variables,
            ref int nativeState,
            out bool completed);

        bool TryMobRegen(
            KingdomQuestPineMobRegenOwnerPlan plan,
            ref int nativeState,
            out bool completed);

        bool TrySummonMob(
            KingdomQuestPineSummonMobOwnerPlan plan,
            ref int nativeState,
            out bool completed);
    }
}
