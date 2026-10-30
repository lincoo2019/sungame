using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombieYetiFootball : TowerDefenseZombie
{
	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			timeScaleInit = 1.5;
			base.sprite.SetFliters(new Array { "anim_face" }, open: false);
		}
	}

	public override void AttackProcessing(double delta)
	{
		base.AttackProcessing(delta);
		base.sprite.timeScale = timeScale * 1.0;
	}

	public override void DieProcessing(double delta)
	{
		base.DieProcessing(delta);
		base.sprite.timeScale = timeScale * 1.0;
	}

	public override async void DestroySet()
	{
		if ((!Global.Instance.isEditor || !(Global.Instance.enterLevelMode == "DiyLevel")) && !(Global.Instance.enterLevelMode == "LoadLevel") && !(Global.Instance.enterLevelMode == "OnlineLevel"))
		{
			float num = GD.Randf();
			Vector2 pos = GetLogicalGlobalPosition() - new Vector2(0f, 40f);
			if ((double)num <= 0.02)
			{
				TowerDefenseManager.Instance.FallingObjectItemCreate(ObjectManagerConfig.OBJECT.COIN_DIAMOND, pos, 120.0, new Vector2((float)GD.RandRange(-100.0, 100.0), -400f), 980.0).gridPos = base.gridPos;
			}
			else if ((double)num < 0.2)
			{
				TowerDefenseManager.Instance.FallingObjectItemCreate(ObjectManagerConfig.OBJECT.COIN_GOLD, pos, 120.0, new Vector2((float)GD.RandRange(-100.0, 100.0), -400f), 980.0).gridPos = base.gridPos;
			}
			else
			{
				TowerDefenseManager.Instance.FallingObjectItemCreate(ObjectManagerConfig.OBJECT.COIN_SILVER, pos, 120.0, new Vector2((float)GD.RandRange(-100.0, 100.0), -400f), 980.0).gridPos = base.gridPos;
			}
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	public override bool TryConsumeIncomingBuff(TowerDefenseCharacterBuffConfig buffConfig)
	{
		if (buffConfig != null)
		{
			return ApplyTemperatureResponse(buffConfig.key);
		}
		return false;
	}

	public void GetBuffAdd(string key)
	{
		if (ApplyTemperatureResponse(key))
		{
			buff?.DeleteBuff(key);
		}
	}

	private bool ApplyTemperatureResponse(string key)
	{
		switch (key)
		{
		case "IceSpeedDown":
		case "Frozen":
			timeScaleInit = 3.0;
			timeScale = timeScaleInit;
			base.sprite.SetFliters(new Array { "anim_face" }, open: false);
			return true;
		case "RedHeat":
		case "FireHit":
			timeScaleInit = 0.5;
			timeScale = timeScaleInit;
			base.sprite.SetFliters(new Array { "anim_face" }, open: true);
			return true;
		default:
			return false;
		}
	}
}
