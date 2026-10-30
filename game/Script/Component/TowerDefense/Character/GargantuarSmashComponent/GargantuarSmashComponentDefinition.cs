using Godot;

[GlobalClass]
[Tool]
public partial class GargantuarSmashComponentDefinition : CharacterComponentDefinition
{
	[Export(PropertyHint.None, "")]
	public string smashAudio = "GargantuarThump";

	[Export(PropertyHint.Range, "0,10,0.05")]
	public double cameraShakeDuration = 2.0;

	[Export(PropertyHint.Range, "0.001,1,0.001")]
	public double cameraShakeInterval = 0.05;

	[Export(PropertyHint.Range, "0,32,1")]
	public int cameraShakeCount = 4;

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new GargantuarSmashComponent();
	}
}
