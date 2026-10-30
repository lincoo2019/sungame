using Godot;

[Tool]
public partial class TowerDefensePlantGravebusterG : TowerDefensePlant
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
				_gravebusterComponent.OnOver += Over;
			}
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		GravebusterComponent gravebusterComponent = _gravebusterComponent;
		if (gravebusterComponent != null && !gravebusterComponent.IsReleased)
		{
			_gravebusterComponent.OnOver -= Over;
		}
	}

	public void Over(TowerDefenseGravestone _graveStone)
	{
		TowerDefenseCellInstance towerDefenseCellInstance = cell;
		Vector2I vector2I = base.gridPos;
		if (GodotObject.IsInstanceValid(towerDefenseCellInstance))
		{
			towerDefenseCellInstance.Clear();
			TowerDefenseManager.GetPacketConfig("CraterG")?.Plant(vector2I, playAudio: false, noLimit: true);
			if (Global.IsMultiplayerMode && MultiPlayerManager.IsHost)
			{
				MultiPlayerManager.Instance.SendCraterCreate(vector2I.X, vector2I.Y, "CraterG");
			}
		}
	}
}
