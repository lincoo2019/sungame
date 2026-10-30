internal readonly record struct AdobeAnimateCpuVisualFailure(string Code, string ResourceIdentity, string Detail, bool Transient = false)
{
	public bool IsValid => !string.IsNullOrEmpty(Code);
}
