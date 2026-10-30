using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantPlanternTanglekelp : TowerDefensePlant
{
	public TanglekelpComponent tanglekelpComponent;

	public AttackComponent attackComponent;

	public AttackComponent attackComponent2;

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> eventList = new Array<TowerDefenseCharacterEventBase>();

	[Export(PropertyHint.None, "")]
	public double attackInterval = 0.5;

	public bool over;

	public double attackTimer;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			tanglekelpComponent = componentManager.GetRuntime<TanglekelpComponent>();
			attackComponent = componentManager.GetRuntime<AttackComponent>("character.attack.0");
			attackComponent2 = componentManager.GetRuntime<AttackComponent>("character.attack.1");
			Vector2 mapGridSize = TowerDefenseManager.Instance.GetMapGridSize();
			attackComponent2.SetCheckAreaRectangleSize(0, mapGridSize * 2.75f);
			attackComponent.SetCheckAreaRectangleWidth(0, mapGridSize.X * 2.75f);
			if (base.currentCustom.Contains("Custom0"))
			{
				tanglekelpComponent.grabFliterOpen = new string[2] { "skin7", "skin8" };
				tanglekelpComponent.grabFliterClose = new string[2] { "Layer 29", "Layer 32" };
			}
		}
	}

	public override void OnCustomSwitched(string customKey)
	{
		if (customKey == "Custom0")
		{
			tanglekelpComponent.grabFliterOpen = new string[2] { "skin7", "skin8" };
			tanglekelpComponent.grabFliterClose = new string[2] { "Layer 29", "Layer 32" };
		}
		else
		{
			tanglekelpComponent.grabFliterOpen = System.Array.Empty<string>();
			tanglekelpComponent.grabFliterClose = System.Array.Empty<string>();
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (Engine.IsEditorHint())
		{
			return;
		}
		if (attackTimer >= attackInterval)
		{
			if (attackComponent2.CanAttack())
			{
				TowerDefenseExplode.CreateExplode(GetLogicalGlobalPosition(), new Vector2(1.3f, 1.3f), eventList, null, base.camp, instance.collisionFlags);
				attackTimer = 0.0;
			}
		}
		else
		{
			attackTimer += delta;
		}
	}

	public override async void DestroySet()
	{
		if (over)
		{
			return;
		}
		over = true;
		instance.collisionFlags = 33;
		List<TowerDefenseCharacter> list = new List<TowerDefenseCharacter>(attackComponent.GetTargetList());
		if (list.Count <= 0)
		{
			return;
		}
		if (base.inWater)
		{
			foreach (TowerDefenseCharacter item in list)
			{
				if (GodotObject.IsInstanceValid(item) && !item.IsHardControlImmune && CanCollision(item.instance.maskFlags) && item.config.canDragIntoWater)
				{
					DragTargetIntoWaterOnDestroy(item);
				}
			}
		}
		else
		{
			foreach (TowerDefenseCharacter item2 in list)
			{
				if (GodotObject.IsInstanceValid(item2) && CanCollision(item2.instance.maskFlags))
				{
					Hitrack(item2);
				}
			}
		}
		await ToSignal(GetTree().CreateTimer(0.6, processAlways: false), SceneTreeTimer.SignalName.Timeout);
	}

	private async void DragTargetIntoWaterOnDestroy(TowerDefenseCharacter target)
	{
		bool flag = target is TowerDefenseZombie && GodotObject.IsInstanceValid(target.instance) && target.instance.zombiePhysique == TowerDefenseEnum.ZOMBIE_PHYSIQUE.BOSS;
		if (!GodotObject.IsInstanceValid(target) || target.IsHardControlImmune || flag)
		{
			return;
		}
		TanglekelpComponent obj = tanglekelpComponent;
		if (obj == null || obj.Lifecycle != ComponentRuntimeLifecycle.Active || !tanglekelpComponent.Alive || !GodotObject.IsInstanceValid(tanglekelpComponent.grabSpriteScene))
		{
			return;
		}
		Node2D node2D = TowerDefenseManager.GetCharacterNode();
		if (!GodotObject.IsInstanceValid(node2D))
		{
			return;
		}
		Node2D runner = new Node2D();
		node2D.AddChild(runner, forceReadableName: false, InternalMode.Disabled);
		Vector2 globalPosition = (runner.GlobalPosition = target.GetLogicalGlobalPosition());
		AdobeAnimateSprite adobeAnimateSprite = tanglekelpComponent.grabSpriteScene.Instantiate<AdobeAnimateSprite>(PackedScene.GenEditState.Disabled);
		Godot.Collections.Array array = new Godot.Collections.Array();
		string[] grabFliterOpen = tanglekelpComponent.grabFliterOpen;
		foreach (string text in grabFliterOpen)
		{
			array.Add(text);
		}
		Godot.Collections.Array array2 = new Godot.Collections.Array();
		grabFliterOpen = tanglekelpComponent.grabFliterClose;
		foreach (string text2 in grabFliterOpen)
		{
			array2.Add(text2);
		}
		try
		{
			adobeAnimateSprite.SetFliters(array, open: true);
			adobeAnimateSprite.SetFliters(array2, open: false);
			runner.AddChild(adobeAnimateSprite, forceReadableName: false, InternalMode.Disabled);
			adobeAnimateSprite.Visible = true;
			adobeAnimateSprite.SetAnimation(tanglekelpComponent.grabAnimeClips, loop: false);
			adobeAnimateSprite.GlobalPosition = globalPosition;
			AudioManager.Instance.AudioPlay("Floop");
			AudioManager.Instance.AudioPlay("PlantWater");
			target.CreateSplash();
			if (GodotObject.IsInstanceValid(target.sprite))
			{
				target.sprite.pause = true;
			}
			target.SetHitBoxMonitorSuppressed(HitBoxSuppressionReason.PlanternTanglekelp, suppressed: true);
			await runner.ToSignal(runner.GetTree().CreateTimer(0.5, processAlways: false), SceneTreeTimer.SignalName.Timeout);
			if (!GodotObject.IsInstanceValid(target))
			{
				return;
			}
			AudioManager.Instance.AudioPlay("ZombieEnteringWater");
			if (target.config.dragHurt != -1.0)
			{
				double dragHurt = target.config.dragHurt;
				target.Hurt(100000.0, playSplatAudio: true, default(Vector2), createDamagePart: true, dragHurt);
				if (GodotObject.IsInstanceValid(target.sprite))
				{
					target.sprite.pause = false;
				}
				target.SetHitBoxMonitorSuppressed(HitBoxSuppressionReason.PlanternTanglekelp, suppressed: false);
			}
			else
			{
				target.die = true;
				target.Destroy();
			}
		}
		finally
		{
			if (GodotObject.IsInstanceValid(runner))
			{
				runner.QueueFree();
			}
		}
	}

	public virtual void Hitrack(TowerDefenseCharacter character)
	{
		bool flag = character is TowerDefenseZombie && GodotObject.IsInstanceValid(character.instance) && character.instance.zombiePhysique == TowerDefenseEnum.ZOMBIE_PHYSIQUE.BOSS;
		if (!GodotObject.IsInstanceValid(character) || character.IsHardControlImmune || flag)
		{
			return;
		}
		if (character is TowerDefenseZombie)
		{
			character.ySpeed = -200.0;
			Tween tween = character.CreateTween();
			tween.SetEase(Tween.EaseType.Out);
			tween.SetTrans(Tween.TransitionType.Cubic);
			tween.TweenMethod(to: instance.hypnoses ? ((float)(TowerDefenseManager.Instance.GetMapGroundLeft() + 60.0)) : ((float)(character.groundRight - 60.0)), method: Callable.From(delegate(float value)
			{
				SetLogicalGlobalPositionX(character, value);
			}), from: character.GetLogicalGlobalPosition().X, duration: 2.0);
		}
		if (!IsVehicleDizzinessImmune(character))
		{
			TowerDefenseCharacterBuffDizziness towerDefenseCharacterBuffDizziness = new TowerDefenseCharacterBuffDizziness();
			towerDefenseCharacterBuffDizziness.time = 3.0;
			character.BuffAdd(towerDefenseCharacterBuffDizziness);
		}
	}

	private static bool IsVehicleDizzinessImmune(TowerDefenseCharacter character)
	{
		if (GodotObject.IsInstanceValid(character) && character.config is TowerDefenseZombieConfig towerDefenseZombieConfig)
		{
			return towerDefenseZombieConfig.physique == TowerDefenseEnum.ZOMBIE_PHYSIQUE.CAR;
		}
		return false;
	}

	private static void SetLogicalGlobalPositionX(TowerDefenseCharacter character, float value)
	{
		if (GodotObject.IsInstanceValid(character))
		{
			character.SetLogicalGlobalPosition(new Vector2(value, character.GetLogicalGlobalPosition().Y));
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			{ "attackInterval", attackInterval },
			{ "over", over },
			{ "attackTimer", attackTimer }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		attackInterval = data.GetValueOrDefault("attackInterval", 0.5).AsDouble();
		over = data.GetValueOrDefault("over", false).AsBool();
		attackTimer = data.GetValueOrDefault("attackTimer", 0.0).AsDouble();
	}
}
