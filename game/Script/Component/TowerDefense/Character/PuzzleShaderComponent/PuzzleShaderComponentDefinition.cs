using Godot;

[GlobalClass]
[Tool]
public partial class PuzzleShaderComponentDefinition : CharacterComponentDefinition
{
	[Export(PropertyHint.None, "")]
	public bool enablePuzzleShader = true;

	[Export(PropertyHint.None, "")]
	public bool freezeInIzmIdle = true;

	[Export(PropertyHint.None, "")]
	public StringName shaderParameter = "puzzle";

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new PuzzleShaderComponent();
	}
}
