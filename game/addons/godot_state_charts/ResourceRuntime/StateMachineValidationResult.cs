using System.Collections.Generic;
using System.Collections.ObjectModel;

public sealed class StateMachineValidationResult
{
	private readonly ReadOnlyCollection<StateMachineDiagnostic> _diagnostics;

	public bool IsValid { get; }

	public IReadOnlyList<StateMachineDiagnostic> Diagnostics => _diagnostics;

	public StateMachineValidationResult(IEnumerable<StateMachineDiagnostic> diagnostics)
	{
		List<StateMachineDiagnostic> list = ((diagnostics == null) ? new List<StateMachineDiagnostic>() : new List<StateMachineDiagnostic>(diagnostics));
		_diagnostics = list.AsReadOnly();
		IsValid = true;
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i].Severity == StateMachineDiagnosticSeverity.Error)
			{
				IsValid = false;
				break;
			}
		}
	}
}
