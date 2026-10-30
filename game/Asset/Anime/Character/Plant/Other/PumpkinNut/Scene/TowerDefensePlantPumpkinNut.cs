using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantPumpkinNut : TowerDefensePlant
{
	private static readonly StringName SHIELD_TYPE = new StringName("Default");

	public const double SHIELD_HP = 4000.0;

	public const double SHIELD_REGEN_PER_SEC = 200.0;

	private CharacterTimerComponent _timerComponent;

	public override void _Ready()
	{
		base._Ready();
		if (Engine.IsEditorHint())
		{
			return;
		}
		if (!inGame && !editorMapPreviewMode)
		{
			PumpkinNutSprite pumpkinNutSprite = base.sprite as PumpkinNutSprite;
			if (GodotObject.IsInstanceValid(pumpkinNutSprite) && GodotObject.IsInstanceValid(pumpkinNutSprite.back))
			{
				pumpkinNutSprite.back.ZIndex = 0;
			}
		}
		_timerComponent = componentManager.GetRuntime<CharacterTimerComponent>("character.timer");
		CharacterTimerComponent timerComponent = _timerComponent;
		if (timerComponent != null && !timerComponent.IsReleased)
		{
			_timerComponent.OnTimeout += Timeout;
			_timerComponent.Run("Regen", 1.0);
		}
		BuffComponent buffComponent = buff;
		if (buffComponent != null && !buffComponent.IsReleased)
		{
			buff.OnBuffDelete += OnBuffDelete;
		}
		if (GodotObject.IsInstanceValid(cell))
		{
			TowerDefenseItemSheild.CreateOnCellWithHP(cell, SHIELD_TYPE, 4000.0);
			if (instance.hypnoses)
			{
				Callable.From(_SyncShieldHypnoses).CallDeferred();
			}
		}
	}

	public override void _ExitTree()
	{
		BuffComponent buffComponent = buff;
		if (buffComponent != null && !buffComponent.IsReleased)
		{
			buff.OnBuffDelete -= OnBuffDelete;
		}
		CharacterTimerComponent timerComponent = _timerComponent;
		if (timerComponent != null && !timerComponent.IsReleased)
		{
			_timerComponent.OnTimeout -= Timeout;
		}
		base._ExitTree();
	}

	private void OnBuffDelete(string key)
	{
		if (key == "Hypnoses")
		{
			_SyncShieldHypnoses();
		}
	}

	public void Timeout(string timerName)
	{
		if (timerName == "Regen" && GodotObject.IsInstanceValid(instance) && !instance.sleep)
		{
			_timerComponent.Run("Regen", 1.0);
			RegenShield();
		}
	}

	private void RegenShield()
	{
		if (!GodotObject.IsInstanceValid(cell))
		{
			return;
		}
		TowerDefenseItemSheild itemShield = cell.itemShield;
		if (!GodotObject.IsInstanceValid(itemShield) || !GodotObject.IsInstanceValid(itemShield.instance) || itemShield.instance.hitpoints <= 0.0)
		{
			TowerDefenseItemSheild.CreateOnCellWithHP(cell, SHIELD_TYPE, 200.0);
			CostHealth(200.0);
			if (instance.hypnoses)
			{
				Callable.From(_SyncShieldHypnoses).CallDeferred();
			}
			return;
		}
		double hitpoints = itemShield.instance.hitpoints;
		if (!(hitpoints >= 4000.0))
		{
			double num = Mathf.Min(4000.0 - hitpoints, 200.0);
			itemShield.ShieldAddHitpoints(num, SHIELD_TYPE);
			CostHealth(num);
		}
	}

	private void CostHealth(double num)
	{
		if (!(num <= 0.0))
		{
			instance.SkipInvincibleDealHurt(num, playSplatAudio: false, Vector2.Zero, createDamagePart: false);
		}
	}

	public override void Hypnoses(double time = -1.0, bool canFliter = true, TowerDefenseCharacterBuffHypnoses hypnosesConfig = null)
	{
		base.Hypnoses(time, canFliter, hypnosesConfig);
		_SyncShieldHypnoses();
	}

	private void _SyncShieldHypnoses()
	{
		if (GodotObject.IsInstanceValid(cell) && GodotObject.IsInstanceValid(cell.itemShield))
		{
			TowerDefenseItemSheild itemShield = cell.itemShield;
			if (itemShield.instance.hypnoses != instance.hypnoses)
			{
				itemShield.Hypnoses();
			}
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary();
	}

	public override void ImportVariantSave(Dictionary data)
	{
	}
}
