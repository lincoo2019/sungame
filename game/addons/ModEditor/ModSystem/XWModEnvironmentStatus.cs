namespace PVZHE.ModEditor.ModSystem;

public sealed record XWModEnvironmentStatus(long Generation, bool Ready, string Reason, XWModEnvironmentSnapshot Snapshot);
