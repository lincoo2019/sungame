public readonly struct StateMachineBindingDiagnostic(string code, string message, string callbackKey = "", string stableId = "")
{
	public string Code { get; } = code ?? string.Empty;

	public string Message { get; } = message ?? string.Empty;

	public string CallbackKey { get; } = callbackKey ?? string.Empty;

	public string StableId { get; } = stableId ?? string.Empty;

	public bool IsEmpty
	{
		get
		{
			if (string.IsNullOrEmpty(Code))
			{
				return string.IsNullOrEmpty(Message);
			}
			return false;
		}
	}
}
