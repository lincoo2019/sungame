using Godot;

[Tool]
[GlobalClass]
public partial class TowerDefenseZombieSnorkleTanglekelp : TowerDefenseZombie
{
	private TanglekelpComponent _tanglekelpComponent;

	private float GlobalPositionX
	{
		get
		{
			return GetLogicalGlobalPosition().X;
		}
		set
		{
			Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
			logicalGlobalPosition.X = value;
			SetLogicalGlobalPosition(logicalGlobalPosition);
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_tanglekelpComponent = componentManager.GetRuntime<TanglekelpComponent>();
			if (_tanglekelpComponent != null)
			{
				_tanglekelpComponent.OnDragBegin += DragBegin;
			}
			base.sprite.OnAnimeStarted += AnimeStarted;
			EnableWaterDragIfSubmerged();
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		TanglekelpComponent tanglekelpComponent = _tanglekelpComponent;
		if (tanglekelpComponent != null && !tanglekelpComponent.IsReleased)
		{
			_tanglekelpComponent.OnDragBegin -= DragBegin;
		}
	}

	public override void AttackEntered()
	{
		if (IsWaterDragOnlyActive())
		{
			instance.maskFlags = 32;
			Walk();
			return;
		}
		base.AttackEntered();
		if (base.inWater)
		{
			instance.maskFlags = 9;
		}
	}

	public override void AttackProcessing(double delta)
	{
		if (IsWaterDragOnlyActive())
		{
			instance.maskFlags = 32;
			Walk();
		}
		else
		{
			base.AttackProcessing(delta);
		}
	}

	public override void AttackExited()
	{
		base.AttackExited();
		if (base.inWater)
		{
			instance.maskFlags = 32;
		}
	}

	public override void InWater()
	{
		base.InWater();
		EnableWaterDragIfSubmerged();
	}

	public override void WalkProcessing(double delta)
	{
		if (IsWaterDragOnlyActive())
		{
			SwimComponent obj = swimComponent;
			if (obj != null && !obj.IsReleased)
			{
				swimComponent.WalkProcessing((float)delta);
			}
		}
		else
		{
			base.WalkProcessing(delta);
		}
	}

	public override void OutWater()
	{
		TanglekelpComponent tanglekelpComponent = _tanglekelpComponent;
		if (tanglekelpComponent != null && !tanglekelpComponent.IsReleased)
		{
			_tanglekelpComponent.SetAlive(alive: false);
		}
		base.groundHeight = -100.0;
		base.z = -100.0;
		base.OutWater();
		CreateTween().TweenProperty(base.sprite, "offset", new Vector2(-50f, -80f), 0.25);
		GlobalPositionX -= base.Scale.X * transformPoint.Scale.X * 30f;
		instance.maskFlags = 9;
	}

	public override void DieEntered()
	{
		base.DieEntered();
		base.sprite.offset = new Vector2(-50f, -80f);
	}

	public void AnimeStarted(string clip)
	{
		if (clip == "Swim")
		{
			base.sprite.offset = new Vector2(-10f, -100f);
		}
	}

	public override void AnimeCompleted(string clip)
	{
		if (clip == "Jump")
		{
			instance.maskFlags = 32;
			base.sprite.offset = new Vector2(-10f, -100f);
			GlobalPositionX -= base.Scale.X * transformPoint.Scale.X * 40f;
		}
		base.AnimeCompleted(clip);
	}

	public override void DamagePointReach(string damangePointName)
	{
		base.DamagePointReach(damangePointName);
		if (damangePointName == "Head")
		{
			TanglekelpComponent tanglekelpComponent = _tanglekelpComponent;
			if (tanglekelpComponent != null && !tanglekelpComponent.IsReleased)
			{
				_tanglekelpComponent.SetAlive(alive: false);
			}
			base.sprite.GetNode<AdobeAnimateSpriteBase>("%Head").Visible = false;
		}
	}

	public override void Purify()
	{
		if (!GodotObject.IsInstanceValid(cell))
		{
			return;
		}
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
		{
			Destroy();
			return;
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("PlantTanglekelp");
		if (cell.CanPacketPlant(packetConfig))
		{
			TowerDefenseCharacter towerDefenseCharacter = packetConfig.Plant(base.gridPos);
			towerDefenseCharacter.WakeUp();
			if (instance.hypnoses)
			{
				towerDefenseCharacter.Hypnoses();
			}
			if (Global.IsMultiplayerMode && MultiPlayerManager.IsHost)
			{
				TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
				if (GodotObject.IsInstanceValid(currentControl))
				{
					int nextSyncId = currentControl.GetNextSyncId();
					currentControl.RegisterSyncCharacter(nextSyncId, towerDefenseCharacter);
					MultiPlayerManager.Instance.SendSpawnCharacterAt("PlantTanglekelp", base.gridPos.X, base.gridPos.Y, nextSyncId);
				}
			}
		}
		Destroy();
	}

	public void DragBegin(TowerDefenseCharacter target)
	{
		spritePause = true;
	}

	private bool IsWaterDragOnlyActive()
	{
		if (base.inWater)
		{
			TanglekelpComponent tanglekelpComponent = _tanglekelpComponent;
			if (tanglekelpComponent != null && tanglekelpComponent.Lifecycle == ComponentRuntimeLifecycle.Active)
			{
				return _tanglekelpComponent.Alive;
			}
		}
		return false;
	}

	private void EnableWaterDragIfSubmerged()
	{
		if (base.inWater)
		{
			TanglekelpComponent tanglekelpComponent = _tanglekelpComponent;
			if (tanglekelpComponent != null && tanglekelpComponent.Lifecycle == ComponentRuntimeLifecycle.Active)
			{
				_tanglekelpComponent.SetAlive(alive: true);
				instance.maskFlags = 32;
			}
		}
	}
}
