using System;
using Godot;

[Tool]
public partial class TowerDefenseZombieCat : TowerDefenseZombie
{
	private const int TailSpikeCollisionFlags = 9;

	private FireComponent _fireComponent;

	private int _currentFireNum;

	private double _fireInterval = 1.5;

	private int _fireNum = 2;

	private string _projectileName = "Spike";

	[Export(PropertyHint.None, "")]
	public double fireInterval
	{
		get
		{
			return _fireInterval;
		}
		set
		{
			_fireInterval = value;
			if (IsNodeReady())
			{
				FireComponent fireComponent = _fireComponent;
				if (fireComponent != null && !fireComponent.IsReleased)
				{
					_fireComponent.fireInterval = (float)value;
				}
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public int fireNum
	{
		get
		{
			return _fireNum;
		}
		set
		{
			_fireNum = value;
			if (IsNodeReady())
			{
				FireComponent fireComponent = _fireComponent;
				if (fireComponent != null && !fireComponent.IsReleased)
				{
					_fireComponent.fireNum = value;
				}
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public string projectileName
	{
		get
		{
			return _projectileName;
		}
		set
		{
			_projectileName = value;
			if (IsNodeReady())
			{
				FireComponent fireComponent = _fireComponent;
				if (fireComponent != null && !fireComponent.IsReleased)
				{
					((FireComponentProjectileSingle)_fireComponent.fireCheckList[0].projectile).projectileName = value;
				}
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_fireComponent = componentManager.GetRuntime<FireComponent>("character.fire");
			ConfigureWaterLineVisualLayers("Zombie_whitewater");
		}
	}

	public override void WalkProcessing(double delta)
	{
		if ((double)GetGlobalPositionForPhysicsFrame(TowerDefenseProcessModeDispatch.CurrentPhysicsFrame).X > groundRight)
		{
			base.sprite.timeScale = timeScale * base.walkSpeedScale * 2.0;
		}
		else
		{
			base.sprite.timeScale = timeScale * base.walkSpeedScale;
		}
		if (!base.IsRemoteNetworkReplica && !base.sprite.pause && attackComponent.CanAttack() && base.sprite.timeScale > 0.0)
		{
			ChangeLine();
		}
	}

	public override void AttackProcessing(double delta)
	{
		base.AttackProcessing(delta);
		base.sprite.timeScale = timeScale * 3.0;
	}

	public override void DieProcessing(double delta)
	{
		base.DieProcessing(delta);
		base.sprite.timeScale = timeScale * 2.0;
	}

	public override async void AnimeEvent(string command, Variant argument)
	{
		base.AnimeEvent(command, argument);
		if (command == "fire" && !nearDie && !die && (double)GetLogicalGlobalPosition().X <= groundRight)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			FireTailSpike();
		}
	}

	private void FireTailSpike()
	{
		FireComponent fireComponent = _fireComponent;
		if (fireComponent == null || fireComponent.IsReleased || _fireComponent.fireCheckList.Count <= 0)
		{
			_currentFireNum = 0;
			return;
		}
		TowerDefenseProjectileCreateData projetile = _fireComponent.fireCheckList[0].projectile.GetProjetile();
		if (!((_currentFireNum == 0) ? _fireComponent.CanFireByData(projetile, 9) : _fireComponent.CanFireCheckOnceByData(projetile, 9)))
		{
			_currentFireNum = 0;
			return;
		}
		AudioManager.Instance.AudioPlay("ProjectileThrow");
		BulletFieldSpawnOverrides overrides = new BulletFieldSpawnOverrides
		{
			gridYOverride = base.gridPos.Y,
			initialRotationOverride = (float)Math.PI * base.Scale.X
		};
		_fireComponent.CreateProjectileByData(0, new Vector2(-300f, 0f), projetile, 9, base.camp, Vector2.Zero, overrides);
		if (_currentFireNum == 0)
		{
			_fireComponent.Refresh();
		}
		_currentFireNum++;
		if (_currentFireNum >= fireNum)
		{
			_currentFireNum = 0;
		}
	}
}
