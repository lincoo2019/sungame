using System.Collections.Generic;
using Godot;

[Tool]
public partial class TowerDefensePlantCoffeeSheild : TowerDefensePlant
{
	public static readonly StringName SHIELD_TYPE = new StringName("CommonShield");

	public const double SHIELD_HP = 4000.0;

	private ExplodeComponent _explodeComponent;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_explodeComponent = componentManager.GetRuntime<ExplodeComponent>();
			_explodeComponent.OnExplode += Explode;
			BuffComponent buffComponent = buff;
			if (buffComponent != null && !buffComponent.IsReleased)
			{
				buff.OnBuffDelete += OnBuffDelete;
			}
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
		BuffComponent buffComponent = buff;
		if (buffComponent != null && !buffComponent.IsReleased)
		{
			buff.OnBuffDelete -= OnBuffDelete;
		}
	}

	private void OnBuffDelete(string key)
	{
		if (key == "Hypnoses")
		{
			_SyncShieldHypnoses();
		}
	}

	public void Explode()
	{
		foreach (TowerDefenseCellInstance shieldCell in GetShieldCells())
		{
			foreach (TowerDefenseCharacter character in shieldCell.characterList)
			{
				if (GodotObject.IsInstanceValid(character) && character.camp == base.camp)
				{
					character.WakeUp();
				}
			}
			TowerDefenseItemSheild.CreateOnCellWithHP(shieldCell, SHIELD_TYPE, 4000.0);
		}
		if (instance.hypnoses)
		{
			Callable.From(_SyncShieldHypnoses).CallDeferred();
		}
	}

	private List<TowerDefenseCellInstance> GetShieldCells()
	{
		List<TowerDefenseCellInstance> list = new List<TowerDefenseCellInstance>();
		if (GodotObject.IsInstanceValid(cell))
		{
			list.Add(cell);
		}
		if (GodotObject.IsInstanceValid(targetPlant) && targetPlant.config is TowerDefensePlantConfig towerDefensePlantConfig)
		{
			foreach (Vector2I item in towerDefensePlantConfig.extendGrid)
			{
				TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(targetPlant.gridPos + item);
				if (GodotObject.IsInstanceValid(mapCell) && !list.Contains(mapCell))
				{
					list.Add(mapCell);
				}
			}
		}
		return list;
	}

	public override void Hypnoses(double time = -1.0, bool canFliter = true, TowerDefenseCharacterBuffHypnoses hypnosesConfig = null)
	{
		base.Hypnoses(time, canFliter, hypnosesConfig);
		_SyncShieldHypnoses();
	}

	private void _SyncShieldHypnoses()
	{
		foreach (TowerDefenseCellInstance shieldCell in GetShieldCells())
		{
			TowerDefenseItemSheild itemShield = shieldCell.itemShield;
			if (GodotObject.IsInstanceValid(itemShield) && itemShield.instance.hypnoses != instance.hypnoses)
			{
				itemShield.Hypnoses();
			}
		}
	}
}
