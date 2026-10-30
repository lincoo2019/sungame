using Godot;

[GlobalClass]
[Tool]
public partial class WeatheringComponentDefinition : CharacterComponentDefinition
{
	public override CharacterComponentRuntime CreateRuntime()
	{
		return new WeatheringComponent();
	}
}
