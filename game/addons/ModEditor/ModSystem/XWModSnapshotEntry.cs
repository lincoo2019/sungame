namespace PVZHE.ModEditor.ModSystem;

public sealed record XWModSnapshotEntry(string Id, string Version, string PackageSha256, int RuntimeApiVersion, string EffectiveMode, int LoadOrderIndex);
