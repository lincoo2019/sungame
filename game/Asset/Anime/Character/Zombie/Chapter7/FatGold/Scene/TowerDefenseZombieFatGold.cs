using System;
using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombieFatGold : TowerDefenseZombie
{
	private FireComponent _fireComponent;

	[Export(PropertyHint.None, "")]
	public double fireInterval = 3.0;

	[Export(PropertyHint.None, "")]
	public int fireNum = 1;

	[Export(PropertyHint.None, "")]
	public string projectileName = "CoinSilver";

	public int currentFireNum;

	private bool _shotThisThrow;

	private StateHandle _throwStateHandle;

	private bool _roleStateSignalsConnected;

	private void ConnectRoleStateSignals()
	{
		if (_roleStateSignalsConnected)
		{
			return;
		}
		IStateMachineController stateMachine = base.StateMachine;
		if (stateMachine != null && stateMachine.IsInitialized)
		{
			_throwStateHandle = base.StateMachine?.GetStateById("zombie.fat_gold.throw");
			StateHandle throwStateHandle = _throwStateHandle;
			if (throwStateHandle != null && throwStateHandle.IsValid)
			{
				_throwStateHandle.Entered += ThrowEntered;
				_throwStateHandle.Exited += ThrowExited;
				_throwStateHandle.PhysicsProcessing += ThrowProcessing;
				_roleStateSignalsConnected = true;
			}
		}
	}

	private void DisconnectRoleStateSignals()
	{
		if (_roleStateSignalsConnected)
		{
			if (_throwStateHandle != null)
			{
				_throwStateHandle.Entered -= ThrowEntered;
				_throwStateHandle.Exited -= ThrowExited;
				_throwStateHandle.PhysicsProcessing -= ThrowProcessing;
			}
			_throwStateHandle = null;
			_roleStateSignalsConnected = false;
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_fireComponent = componentManager.GetRuntime<FireComponent>("character.fire");
			_fireComponent.fireInterval = (float)fireInterval;
			_fireComponent.fireEventName = "";
			_fireComponent.fireAnimeClipsArray = new Array<string>();
			_fireComponent.fireAnimeClips = "";
			ConfigureWaterLineVisualLayers("Zombie_duckytube", "Zombie_whitewater", "Zombie_whitewater2");
			ConnectRoleStateSignals();
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (!Engine.IsEditorHint() && TowerDefenseManager.Instance.IsGameRunning() && inGame && !die && !nearDie && !base.IsRemoteNetworkReplica)
		{
			_fireComponent.fireInterval = (float)fireInterval;
			if (_fireComponent.CanFireByData(_fireComponent.fireCheckList[0].projectile.GetProjetile()))
			{
				SendStateEvent("ToThrow");
			}
		}
	}

	public override void DieProcessing(double delta)
	{
		base.DieProcessing(delta);
		base.sprite.timeScale = timeScale * 2.0;
	}

	public void ThrowEntered()
	{
		if (!base.IsProgressRestoreInFlight)
		{
			_shotThisThrow = false;
			_fireComponent.Refresh();
		}
		if (base.inWater)
		{
			base.sprite.SetAnimation("SwimFire", loop: false, 0.2);
		}
		else
		{
			base.sprite.SetAnimation("Fire", loop: false, 0.2);
		}
		if (!base.IsProgressRestoreInFlight)
		{
			FireCurrencyProjectile();
		}
	}

	public virtual void ThrowProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 2.0;
	}

	public void ThrowExited()
	{
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if ((clip == "SwimFire" || clip == "Fire") && currentFireNum == 0)
		{
			Walk();
		}
	}

	public override void AnimeEvent(string command, Variant argument)
	{
		base.AnimeEvent(command, argument);
		if (command == "fire" && !_shotThisThrow)
		{
			FireCurrencyProjectile();
		}
	}

	private void FireCurrencyProjectile()
	{
		if (base.IsRemoteNetworkReplica)
		{
			return;
		}
		_shotThisThrow = true;
		float num = GD.Randf();
		TowerDefenseProjectileCreateData towerDefenseProjectileCreateData;
		if ((double)num < 0.05)
		{
			projectileName = "CoinDiamond";
			towerDefenseProjectileCreateData = new TowerDefenseProjectileCreateData(projectileName);
			towerDefenseProjectileCreateData.baseDamage = 1000.0;
		}
		else if ((double)num < 0.25)
		{
			projectileName = "CoinGold";
			towerDefenseProjectileCreateData = new TowerDefenseProjectileCreateData(projectileName);
			towerDefenseProjectileCreateData.baseDamage = 500.0;
		}
		else
		{
			projectileName = "CoinSilver";
			towerDefenseProjectileCreateData = new TowerDefenseProjectileCreateData(projectileName);
			towerDefenseProjectileCreateData.baseDamage = 100.0;
		}
		towerDefenseProjectileCreateData.damageFlags = 3;
		towerDefenseProjectileCreateData.fireMethodFlags = 1;
		towerDefenseProjectileCreateData.collisionFlags = 1;
		BulletFieldSpawnOverrides overrides = new BulletFieldSpawnOverrides
		{
			gridYOverride = base.gridPos.Y,
			flipXOverride = (base.Scale.X < 0f)
		};
		_fireComponent.CreateProjectileByData(0, new Vector2(-500f, 0f), towerDefenseProjectileCreateData, -1, base.camp, Vector2.Zero, overrides);
		currentFireNum++;
		if (currentFireNum == fireNum)
		{
			currentFireNum = 0;
			return;
		}
		_shotThisThrow = false;
		if (base.inWater)
		{
			base.sprite.SetAnimation("SwimFire", loop: false, 0.1);
		}
		else
		{
			base.sprite.SetAnimation("Fire", loop: false, 0.1);
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			["currentFireNum"] = currentFireNum,
			["shotThisThrow"] = _shotThisThrow,
			["projectileName"] = projectileName
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		currentFireNum = Math.Clamp(data.GetValueOrDefault("currentFireNum", currentFireNum).AsInt32(), 0, Math.Max(0, fireNum - 1));
		_shotThisThrow = data.GetValueOrDefault("shotThisThrow", _shotThisThrow).AsBool();
		projectileName = data.GetValueOrDefault("projectileName", projectileName).AsString();
	}
}
