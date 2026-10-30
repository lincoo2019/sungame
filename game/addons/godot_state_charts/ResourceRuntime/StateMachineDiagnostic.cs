public sealed class StateMachineDiagnostic
{
	public string Code { get; }

	public string Name { get; }

	public StateMachineDiagnosticSeverity Severity { get; }

	public string Message { get; }

	public string StableId { get; }

	public StateMachineDiagnostic(string code, string name, StateMachineDiagnosticSeverity severity, string message, string stableId = "")
	{
		Code = code;
		Name = name;
		Severity = severity;
		Message = message;
		StableId = stableId ?? string.Empty;
	}
}
