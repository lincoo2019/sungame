using Godot.Collections;

public sealed class StateMachineSceneImportResult
{
	public StateMachineDefinition Definition { get; set; }

	public StateMachineLayout Layout { get; set; }

	public StateMachineValidationResult Diagnostics { get; set; }

	public Dictionary<string, string> LegacyPathToStableId { get; set; } = new Dictionary<string, string>();

	public bool Succeeded
	{
		get
		{
			if (Definition != null)
			{
				return Diagnostics?.IsValid ?? false;
			}
			return false;
		}
	}
}
