using Godot;

[GlobalClass]
public partial class TowerDefenseMapShaderTime : TowerDefenseMap
{
	private static readonly string[] ConventionalShaderTimeNodes = new string[3] { "Pool", "Pool1", "Pool2" };

	public TowerDefenseMapShaderTime()
	{
		ConfigureShaderTimeNodes(ConventionalShaderTimeNodes);
	}
}
