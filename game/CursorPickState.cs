public readonly struct CursorPickState(string pickType, string pickName)
{
	public string PickType { get; } = pickType ?? "";

	public string PickName { get; } = pickName ?? "";
}
