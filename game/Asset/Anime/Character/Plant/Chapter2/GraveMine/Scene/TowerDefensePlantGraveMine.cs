using Godot;

[Tool]
public partial class TowerDefensePlantGraveMine : TowerDefensePlant
{
	private GravebusterComponent _gravebusterComponent;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_gravebusterComponent = componentManager.GetRuntime<GravebusterComponent>();
			if (_gravebusterComponent != null)
			{
				_gravebusterComponent.OnOver += GravebusterOver;
			}
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		GravebusterComponent gravebusterComponent = _gravebusterComponent;
		if (gravebusterComponent != null && !gravebusterComponent.IsReleased)
		{
			_gravebusterComponent.OnOver -= GravebusterOver;
		}
	}

	public async void GravebusterOver(TowerDefenseGravestone graveStone)
	{
		if (!TowerDefenseManager.HasGameplayAuthority || !GodotObject.IsInstanceValid(cell))
		{
			return;
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("PlantSunMine");
		if (!GodotObject.IsInstanceValid(packetConfig) || !cell.CanPacketPlant(packetConfig))
		{
			return;
		}
		TowerDefensePlantSunMine sunMine = (base.HasEconomyOwner ? packetConfig.Plant(base.EconomyOwnerAccountId, base.gridPos) : packetConfig.Plant(base.gridPos)) as TowerDefensePlantSunMine;
		if (GodotObject.IsInstanceValid(sunMine))
		{
			if (instance.hypnoses)
			{
				sunMine.Hypnoses();
			}
			TowerDefenseManager.PublishSpawnedCharacter("PlantSunMine", sunMine, useCreate: false);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			if (GodotObject.IsInstanceValid(sunMine))
			{
				sunMine.ReadyRise();
			}
		}
	}
}
