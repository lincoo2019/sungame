using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantPumpkinFireIce : TowerDefensePlant
{
	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> allEventList = new Array<TowerDefenseCharacterEventBase>();

	private PointLight2D _light;

	public int coldCheckInterval = 2;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_light = GetNode<PointLight2D>("%Light");
			if (!inGame && !editorMapPreviewMode)
			{
				((PumpkinFireIceSprite)base.sprite).back.ZIndex = 0;
			}
		}
	}

	public override void BatchUpdate(double delta)
	{
		if (!Engine.IsEditorHint())
		{
			base.BatchUpdate(delta);
			if ((ulong)((long)TowerDefenseProcessModeDispatch.CurrentPhysicsFrame + (long)randFreshIndex) % 30uL == 0L && GodotObject.IsInstanceValid(_light))
			{
				_light.Visible = TowerDefenseManager.GetMapIsNight() && GameSaveManager.Instance.GetConfigValue("MapEffect").AsBool();
			}
			if (coldCheckInterval > 0)
			{
				coldCheckInterval--;
				return;
			}
			ProtectCellPlants();
			coldCheckInterval = 2;
		}
	}

	private void ProtectCellPlants()
	{
		if (!inGame || die || nearDie || isDestroy || !TowerDefenseManager.HasGameplayAuthority)
		{
			return;
		}
		TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(base.gridPos);
		if (!GodotObject.IsInstanceValid(mapCell))
		{
			return;
		}
		foreach (TowerDefenseCharacter character in mapCell.characterList)
		{
			if (!(character is TowerDefensePlant towerDefensePlant) || !GodotObject.IsInstanceValid(towerDefensePlant) || towerDefensePlant.die || towerDefensePlant.nearDie || towerDefensePlant.isDestroy || towerDefensePlant.camp != base.camp || towerDefensePlant.config.plantGridType.Contains(TowerDefenseEnum.PLANTGRIDTYPE.AIR))
			{
				continue;
			}
			foreach (TowerDefenseCharacterEventBase allEvent in allEventList)
			{
				if (GodotObject.IsInstanceValid(allEvent))
				{
					allEvent.Execute(towerDefensePlant.GetLogicalGlobalPosition(), towerDefensePlant);
				}
			}
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary { ["coldCheckInterval"] = coldCheckInterval };
	}

	public override void ImportVariantSave(Dictionary data)
	{
		coldCheckInterval = (data.ContainsKey("coldCheckInterval") ? data["coldCheckInterval"].AsInt32() : 2);
	}
}
