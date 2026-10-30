using Godot;

[GlobalClass]
public partial class TowerDefenseCharacterBuffPogo : TowerDefenseCharacterBuffConfig
{
	[Export(PropertyHint.None, "")]
	public double time = 8.0;

	[Export(PropertyHint.None, "")]
	public double jumpSpeed = -300.0;

	[Export(PropertyHint.None, "")]
	public double pogoGravity = 490.0;

	[Export(PropertyHint.None, "")]
	public double currentTime;

	private bool _movementStateApplied;

	private Variant _savedGravity;

	private bool _hasSavedGravity;

	private bool _savedHandleWaterHeight;

	private bool _hasSavedHandleWaterHeight;

	public override void _Init()
	{
		key = "Pogo";
	}

	public override void Enter()
	{
		if ((character.instance.collisionFlags & 2) != 0)
		{
			character.buff.DeleteBuff("Pogo");
		}
		else if (character is TowerDefenseZombie && character.instance.zombiePhysique == TowerDefenseEnum.ZOMBIE_PHYSIQUE.BOSS)
		{
			character.buff.DeleteBuff("Pogo");
		}
		else if (character is TowerDefenseItem)
		{
			character.buff.DeleteBuff("Pogo");
		}
		else if (character is TowerDefenseGravestone)
		{
			character.buff.DeleteBuff("Pogo");
		}
		else if (character is TowerDefenseCrater)
		{
			character.buff.DeleteBuff("Pogo");
		}
		else
		{
			ApplyMovementState();
		}
	}

	private void ApplyMovementState()
	{
		if (_movementStateApplied || !GodotObject.IsInstanceValid(character))
		{
			return;
		}
		_savedGravity = character.Get("gravity");
		_hasSavedGravity = _savedGravity.VariantType != Variant.Type.Nil;
		GroundHeightComponent groundHeightComponent = character.groundHeightComponent;
		if (groundHeightComponent != null && !groundHeightComponent.IsReleased)
		{
			_savedHandleWaterHeight = character.groundHeightComponent.handleWaterHeight;
			_hasSavedHandleWaterHeight = true;
		}
		character.OnLand += OnLand;
		_movementStateApplied = true;
		character.Set("ySpeed", jumpSpeed);
		character.Set("gravity", pogoGravity);
		if (character.inWater)
		{
			character.groundHeight = 0.0;
			GroundHeightComponent groundHeightComponent2 = character.groundHeightComponent;
			if (groundHeightComponent2 != null && !groundHeightComponent2.IsReleased)
			{
				character.groundHeightComponent.handleWaterHeight = false;
			}
		}
	}

	public override void EnterReadOnlyClient()
	{
		ApplyMovementState();
	}

	public override bool Step(double delta)
	{
		currentTime += delta;
		if (!character.nearDie && !character.die)
		{
			return currentTime >= time;
		}
		return true;
	}

	public override void StepReadOnlyClient(double delta)
	{
		Step(delta);
	}

	public override void Exit()
	{
		CleanupMovementState();
		if (!character.instance.hypnoses)
		{
			character.instance.ArmorClear();
		}
		if (character is TowerDefenseZombie && character.instance.zombiePhysique == TowerDefenseEnum.ZOMBIE_PHYSIQUE.CAR)
		{
			character.Hurt(1000000.0);
		}
	}

	public override void ExitReadOnlyClient()
	{
		CleanupMovementState();
	}

	public override void Remove()
	{
		CleanupMovementState();
	}

	public override void Cancel()
	{
		CleanupMovementState();
	}

	private void CleanupMovementState()
	{
		if (!_movementStateApplied)
		{
			return;
		}
		_movementStateApplied = false;
		if (!GodotObject.IsInstanceValid(character))
		{
			return;
		}
		character.OnLand -= OnLand;
		if (_hasSavedGravity)
		{
			character.Set("gravity", _savedGravity);
		}
		_hasSavedGravity = false;
		if (_hasSavedHandleWaterHeight)
		{
			GroundHeightComponent groundHeightComponent = character.groundHeightComponent;
			if (groundHeightComponent != null && !groundHeightComponent.IsReleased)
			{
				character.groundHeightComponent.handleWaterHeight = _savedHandleWaterHeight;
			}
		}
		_hasSavedHandleWaterHeight = false;
		if (!character.inWater)
		{
			return;
		}
		Variant variant = character.Get("waterHeight");
		if (variant.VariantType != Variant.Type.Nil)
		{
			character.groundHeight = 0.0 - (double)variant;
			return;
		}
		GroundHeightComponent groundHeightComponent2 = character.groundHeightComponent;
		if (groundHeightComponent2 != null && !groundHeightComponent2.IsReleased)
		{
			character.groundHeight = 0f - character.groundHeightComponent.waterHeight;
		}
	}

	public override void Refresh(TowerDefenseCharacterBuffConfig config)
	{
		if (config is TowerDefenseCharacterBuffPogo towerDefenseCharacterBuffPogo)
		{
			time = Mathf.Max(time, towerDefenseCharacterBuffPogo.time);
			jumpSpeed = towerDefenseCharacterBuffPogo.jumpSpeed;
			pogoGravity = towerDefenseCharacterBuffPogo.pogoGravity;
			currentTime = 0.0;
		}
	}

	public void OnLand()
	{
		character.Set("ySpeed", jumpSpeed);
		if (character.inWater)
		{
			character.groundHeight = 0.0;
		}
	}
}
