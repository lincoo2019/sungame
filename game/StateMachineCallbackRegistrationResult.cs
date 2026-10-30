public readonly struct StateMachineCallbackRegistrationResult(bool success, string error = "", int callbackCount = 0)
{
	public bool Success { get; } = success;

	public string Error { get; } = error ?? string.Empty;

	public int CallbackCount { get; } = callbackCount;
}
