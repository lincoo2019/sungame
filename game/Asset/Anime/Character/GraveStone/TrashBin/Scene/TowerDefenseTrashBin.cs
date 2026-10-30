using Godot;

[Tool]
public partial class TowerDefenseTrashBin : TowerDefenseGravestone
{
	private const string DisableMetaKey = "disabled_by_trash_bin";

	private const string RefCountMetaKey = "character_disabled_ref_count";

	public bool over;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			RemoveFromGroup("Gravestone");
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (Engine.IsEditorHint() || !GodotObject.IsInstanceValid(cell) || nearDie || die)
		{
			return;
		}
		foreach (TowerDefenseCharacter character in cell.GetCharacterList())
		{
			if (character is TowerDefensePlant towerDefensePlant && CanTarget(towerDefensePlant) && !(towerDefensePlant is TowerDefensePlantBowlingBase))
			{
				if (!towerDefensePlant.HasMeta("disabled_by_trash_bin"))
				{
					towerDefensePlant.SetMeta("disabled_by_trash_bin", true);
					int num = towerDefensePlant.GetMeta("character_disabled_ref_count", 0).AsInt32() + 1;
					towerDefensePlant.SetMeta("character_disabled_ref_count", num);
				}
				towerDefensePlant.characterDisabled = true;
				towerDefensePlant.invisible = true;
				towerDefensePlant.instance.canBeCollection = false;
				towerDefensePlant.skipDestroySet = true;
				if (towerDefensePlant is TowerDefensePlantCardless towerDefensePlantCardless)
				{
					towerDefensePlantCardless.instance.invincible = false;
					TowerDefenseManager.Instance.ChangeCostRemove(towerDefensePlantCardless.changeCost);
				}
			}
		}
	}

	public override void DestroySet()
	{
		if (GodotObject.IsInstanceValid(cell))
		{
			foreach (TowerDefenseCharacter character in cell.GetCharacterList())
			{
				if (!(character is TowerDefensePlant towerDefensePlant) || towerDefensePlant is TowerDefensePlantBowlingBase || !towerDefensePlant.HasMeta("disabled_by_trash_bin"))
				{
					continue;
				}
				if (towerDefensePlant.HasMeta("disabled_by_trash_bin"))
				{
					towerDefensePlant.RemoveMeta("disabled_by_trash_bin");
					int num = towerDefensePlant.GetMeta("character_disabled_ref_count", 1).AsInt32() - 1;
					if (num <= 0)
					{
						towerDefensePlant.RemoveMeta("character_disabled_ref_count");
						towerDefensePlant.characterDisabled = false;
					}
					else
					{
						towerDefensePlant.SetMeta("character_disabled_ref_count", num);
					}
				}
				towerDefensePlant.invisible = false;
				towerDefensePlant.instance.canBeCollection = true;
				towerDefensePlant.skipDestroySet = false;
				if (towerDefensePlant is TowerDefensePlantCardless towerDefensePlantCardless)
				{
					towerDefensePlantCardless.instance.invincible = true;
					TowerDefenseManager.Instance.ChangeCostAdd(towerDefensePlantCardless.changeCost);
				}
			}
		}
		base.DestroySet();
	}
}
