using System.Collections.Generic;
using Godot;

[Tool]
public partial class TowerDefensePlantGravebomber : TowerDefensePlant
{
	private ExplodeComponent _explodeComponent;

	private GravebusterComponent _gravebusterComponent;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_explodeComponent = componentManager.GetRuntime<ExplodeComponent>();
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

	public void GravebusterOver(TowerDefenseGravestone graveStone)
	{
		_explodeComponent.Explode();
		if (instance.hypnoses)
		{
			return;
		}
		for (int i = base.gridPos.X - 1; i <= base.gridPos.X + 1; i++)
		{
			if (i < 1 || i > TowerDefenseManager.Instance.GetMapGridNum().X)
			{
				continue;
			}
			for (int j = base.gridPos.Y - 1; j <= base.gridPos.Y + 1; j++)
			{
				if (j < 1 || j > TowerDefenseManager.Instance.GetMapGridNum().Y)
				{
					continue;
				}
				foreach (TowerDefenseCharacter item in new List<TowerDefenseCharacter>(TowerDefenseManager.GetMapCell(new Vector2I(i, j)).characterList))
				{
					if (item is TowerDefenseGravestone)
					{
						item.Destroy();
					}
				}
			}
		}
	}
}
