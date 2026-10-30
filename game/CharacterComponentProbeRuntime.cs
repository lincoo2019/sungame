public sealed class CharacterComponentProbeRuntime : CharacterComponentRuntime
{
	public string ActiveStateId => base.StateMachine?.CurrentStateHandle?.StableId ?? string.Empty;
}
