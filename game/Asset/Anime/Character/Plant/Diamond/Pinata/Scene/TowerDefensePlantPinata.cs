using System.Collections.Generic;
using Godot;

[Tool]
public partial class TowerDefensePlantPinata : TowerDefensePlant
{
	private AttackComponent _attackComponent;

	private ExplodeComponent _explodeComponent;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_attackComponent = componentManager.GetRuntime<AttackComponent>("character.attack.0");
			_explodeComponent = componentManager.GetRuntime<ExplodeComponent>();
			_explodeComponent.OnExplode += Explode;
			_attackComponent.SetCheckAreaRectangleSize(0, TowerDefenseManager.Instance.GetMapGridSize() * 2.75f);
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		ExplodeComponent explodeComponent = _explodeComponent;
		if (explodeComponent != null && !explodeComponent.IsReleased)
		{
			_explodeComponent.OnExplode -= Explode;
		}
	}

	public void Explode()
	{
		foreach (TowerDefenseCharacter item in new List<TowerDefenseCharacter>(_attackComponent.GetCharcterList()))
		{
			if (item is TowerDefenseZombie towerDefenseZombie && !towerDefenseZombie.instance.hypnoses)
			{
				towerDefenseZombie.instance.ArmorClear();
			}
		}
		if (instance.hypnoses)
		{
			return;
		}
		Vector2I mapGridNum = TowerDefenseManager.Instance.GetMapGridNum();
		for (int i = base.gridPos.X - 1; i <= base.gridPos.X + 1; i++)
		{
			if (i < 1 || i > mapGridNum.X)
			{
				continue;
			}
			for (int j = base.gridPos.Y - 1; j <= base.gridPos.Y + 1; j++)
			{
				if (j < 1 || j > mapGridNum.Y)
				{
					continue;
				}
				foreach (TowerDefenseCharacter item2 in new List<TowerDefenseCharacter>(TowerDefenseManager.GetMapCell(new Vector2I(i, j)).characterList))
				{
					if (item2 is TowerDefenseGravestone || item2 is TowerDefenseCrater || (item2 is TowerDefenseItem towerDefenseItem && towerDefenseItem.config.name == "ItemLadder"))
					{
						item2.Destroy();
					}
				}
			}
		}
		if (GodotObject.IsInstanceValid(packet))
		{
			packet.overrideCost = 100;
		}
	}
}
