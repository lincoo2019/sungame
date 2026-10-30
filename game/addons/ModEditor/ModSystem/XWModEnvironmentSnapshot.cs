using System.Collections.Generic;

namespace PVZHE.ModEditor.ModSystem;

public sealed record XWModEnvironmentSnapshot(int SnapshotVersion, int HostApiVersion, IReadOnlyList<XWModSnapshotEntry> Mods, string Sha256);
