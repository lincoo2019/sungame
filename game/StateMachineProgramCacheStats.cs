public readonly struct StateMachineProgramCacheStats(int entryCount, int activeEntryCount, int pinnedEntryCount, int totalReferences)
{
	public readonly int EntryCount = entryCount;

	public readonly int ActiveEntryCount = activeEntryCount;

	public readonly int PinnedEntryCount = pinnedEntryCount;

	public readonly int TotalReferences = totalReferences;
}
