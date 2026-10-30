public sealed class ProjectResourceUidRepairResult
{
	public int ScannedFiles;

	public int ModifiedFiles;

	public int FixedUids;

	public int FixedPaths;

	public int CollectedUidMappings;

	public int MissingPaths;

	public int MissingUids;

	public int Errors;

	public string UidMappings = "";

	public string Details = "";
}
