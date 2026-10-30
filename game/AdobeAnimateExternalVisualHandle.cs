public readonly struct AdobeAnimateExternalVisualHandle(int index, uint generation)
{
	internal int Index { get; } = index;

	internal uint Generation { get; } = generation;

	public bool IsValid
	{
		get
		{
			if (Index >= 0)
			{
				return Generation != 0;
			}
			return false;
		}
	}

	public static AdobeAnimateExternalVisualHandle Invalid => new AdobeAnimateExternalVisualHandle(-1, 0u);
}
