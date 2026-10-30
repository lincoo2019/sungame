using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseItemSnowBall : TowerDefenseItem
{
	private CharacterMoveComponent moveComponent;

	private BowlingComponent bowlingComponent;

	private MousePressComponent mousePressComponent;

	private int _hitNum;

	public bool hitFinish;

	private string _size = "";

	private readonly HashSet<TowerDefenseCharacter> _overlappingSnowballs = new HashSet<TowerDefenseCharacter>();

	private readonly HashSet<TowerDefenseCharacter> _overlapScratch = new HashSet<TowerDefenseCharacter>();

	public int hitNum
	{
		get
		{
			return _hitNum;
		}
		set
		{
			_hitNum = value;
			HitNumCheck();
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint() && !editorPreviewMode)
		{
			moveComponent = componentManager.GetRuntime<CharacterMoveComponent>();
			bowlingComponent = componentManager.GetRuntime<BowlingComponent>();
			mousePressComponent = componentManager.GetRuntime<MousePressComponent>();
			bowlingComponent.OnBowling += Bowling;
			bowlingComponent.OnEdgeRebound += EdgeRebound;
			mousePressComponent.OnPressed += Pressed;
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		BowlingComponent obj = bowlingComponent;
		if (obj != null && !obj.IsReleased)
		{
			bowlingComponent.OnBowling -= Bowling;
			bowlingComponent.OnEdgeRebound -= EdgeRebound;
		}
		if (mousePressComponent != null)
		{
			mousePressComponent.OnPressed -= Pressed;
		}
	}

	public override void BatchUpdate(double delta)
	{
		if (!Engine.IsEditorHint())
		{
			Vector2 globalPositionForPhysicsFrame = GetGlobalPositionForPhysicsFrame(TowerDefenseProcessModeDispatch.CurrentPhysicsFrame);
			ShadowComponent obj = shadowComponent;
			if (obj != null && !obj.IsReleased)
			{
				shadowComponent.SetSaveShadowPosition(new Vector2(globalPositionForPhysicsFrame.X, globalPositionForPhysicsFrame.Y + 30f));
			}
			base.BatchUpdate(delta);
			base.gridPos = TowerDefenseManager.Instance.GetMapGridPos(globalPositionForPhysicsFrame);
			ProcessSnowballOverlaps();
		}
	}

	public override void IdleEntered()
	{
		base.sprite.SetAnimation("Rise", loop: false);
		base.sprite.AddAnimation("Idle", 0.0);
	}

	public override double ExplodeHurt(double num, TowerDefenseEnum.EXPLOSION_DAMAGE_KIND damageKind = TowerDefenseEnum.EXPLOSION_DAMAGE_KIND.BOMB, bool playSplatAudio = true, Vector2 velocity = default(Vector2))
	{
		if (base.sprite.clip == "Roll" && damageKind == TowerDefenseEnum.EXPLOSION_DAMAGE_KIND.JALA)
		{
			Destroy();
		}
		return 0.0;
	}

	public void SetSize(string size)
	{
		ApplySizeConfiguration(size, applyInitialRuntimeState: true);
	}

	private bool ApplySizeConfiguration(string size, bool applyInitialRuntimeState)
	{
		switch (size)
		{
		case "Small":
		{
			TowerDefenseCharacterEventBase towerDefenseCharacterEventBase2 = bowlingComponent.hitEvent[0].Duplicate(deep: true) as TowerDefenseCharacterEventBase;
			towerDefenseCharacterEventBase2.Set("num", 100);
			bowlingComponent.hitEvent[0] = towerDefenseCharacterEventBase2;
			transformPoint.Scale = 0.5f * Vector2.One;
			ConfigureShadowScale(1f * Vector2.One);
			break;
		}
		case "Normal":
		{
			TowerDefenseCharacterEventBase towerDefenseCharacterEventBase3 = bowlingComponent.hitEvent[0].Duplicate(deep: true) as TowerDefenseCharacterEventBase;
			towerDefenseCharacterEventBase3.Set("num", 200);
			bowlingComponent.hitEvent[0] = towerDefenseCharacterEventBase3;
			transformPoint.Scale = 0.8f * Vector2.One;
			ConfigureShadowScale(1.6f * Vector2.One);
			break;
		}
		case "Large":
		{
			TowerDefenseCharacterEventBase towerDefenseCharacterEventBase4 = bowlingComponent.hitEvent[0].Duplicate(deep: true) as TowerDefenseCharacterEventBase;
			towerDefenseCharacterEventBase4.Set("num", 400);
			bowlingComponent.hitEvent[0] = towerDefenseCharacterEventBase4;
			transformPoint.Scale = 1.25f * Vector2.One;
			ConfigureShadowScale(2.5f * Vector2.One);
			break;
		}
		case "Max":
		{
			bowlingComponent.rollXVelocityMax = 150.0;
			bowlingComponent.rollXVelocityMin = 100.0;
			bowlingComponent.edgeReboundUse = false;
			bowlingComponent.hitLineUse = true;
			bowlingComponent.hitLineBackUse = false;
			if (applyInitialRuntimeState)
			{
				bowlingComponent.SetAlive(alive: true);
				mousePressComponent.SetAlive(alive: false);
				hitFinish = true;
			}
			TowerDefenseCharacterEventBase towerDefenseCharacterEventBase = bowlingComponent.hitEvent[0].Duplicate(deep: true) as TowerDefenseCharacterEventBase;
			towerDefenseCharacterEventBase.Set("num", 400);
			bowlingComponent.hitEvent[0] = towerDefenseCharacterEventBase;
			transformPoint.Scale = 1.5f * Vector2.One;
			ConfigureShadowScale(3f * Vector2.One);
			break;
		}
		default:
			return false;
		}
		_size = size;
		return true;
	}

	private void ConfigureShadowScale(Vector2 scale)
	{
		ShadowComponent obj = shadowComponent;
		if (obj != null && !obj.IsReleased)
		{
			shadowComponent.saveShadowScale = scale;
			shadowComponent.saveTransformPointScale = transformPoint.Scale;
			shadowComponent.MarkDirty();
		}
	}

	public void Pressed(Vector2 pos)
	{
		bowlingComponent.SetAlive(alive: true);
		mousePressComponent.SetAlive(alive: false);
	}

	public void Bowling(TowerDefenseCharacter character)
	{
		hitNum++;
	}

	public void EdgeRebound()
	{
		hitNum++;
	}

	public void HitNumCheck()
	{
		if (!hitFinish)
		{
			CharacterMoveComponent characterMoveComponent = moveComponent;
			if (characterMoveComponent != null && !characterMoveComponent.IsReleased && !(moveComponent.velocity.X < 0f) && hitNum >= 5)
			{
				bowlingComponent.edgeReboundUse = false;
				bowlingComponent.hitLineUse = false;
				hitFinish = true;
			}
		}
	}

	public override void InWater()
	{
		base.InWater();
		if (!(Mathf.Abs(transformPoint.Scale.X) >= 0.75f))
		{
			CreateSplash();
			Destroy();
		}
	}

	private void ProcessSnowballOverlaps()
	{
		_overlapScratch.Clear();
		if (!TryGetActiveWorldHitRect(out var rect))
		{
			_overlappingSnowballs.Clear();
			return;
		}
		List<TowerDefenseCharacter> charactersIntersectingRectList = TowerDefenseManager.Instance.characterRegistry.GetCharactersIntersectingRectList(rect);
		for (int i = 0; i < charactersIntersectingRectList.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = charactersIntersectingRectList[i];
			if (GodotObject.IsInstanceValid(towerDefenseCharacter) && towerDefenseCharacter != this && !(towerDefenseCharacter.config.name != "ItemSnowBall"))
			{
				_overlapScratch.Add(towerDefenseCharacter);
				if (!_overlappingSnowballs.Contains(towerDefenseCharacter))
				{
					HandleSnowballEntered(towerDefenseCharacter);
				}
			}
		}
		_overlappingSnowballs.RemoveWhere((TowerDefenseCharacter tdChar) => !GodotObject.IsInstanceValid(tdChar) || !_overlapScratch.Contains(tdChar));
		foreach (TowerDefenseCharacter item in _overlapScratch)
		{
			_overlappingSnowballs.Add(item);
		}
	}

	private void HandleSnowballEntered(TowerDefenseCharacter tdChar)
	{
		if (tdChar is TowerDefenseItemSnowBall towerDefenseItemSnowBall)
		{
			BowlingComponent obj = towerDefenseItemSnowBall.bowlingComponent;
			if (obj != null && obj.Alive)
			{
				return;
			}
		}
		tdChar.Set("hitNum", tdChar.Get("hitNum").AsInt32() + 1);
	}

	public override void Hypnoses(double time = -1.0, bool canFliter = true, TowerDefenseCharacterBuffHypnoses hypnosesConfig = null)
	{
		base.Hypnoses(time, canFliter, hypnosesConfig);
		if (instance.hypnoses)
		{
			CharacterMoveComponent characterMoveComponent = moveComponent;
			if (characterMoveComponent != null && !characterMoveComponent.IsReleased)
			{
				moveComponent.moveScale = -1.0;
			}
			bowlingComponent.SetAlive(alive: true);
			mousePressComponent.SetAlive(alive: false);
		}
		else
		{
			CharacterMoveComponent characterMoveComponent2 = moveComponent;
			if (characterMoveComponent2 != null && !characterMoveComponent2.IsReleased)
			{
				moveComponent.moveScale = 1.0;
			}
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			{ "hitNum", _hitNum },
			{ "hitFinish", hitFinish },
			{ "size", _size }
		};
	}

	public override bool ImportVariantSaveWhenEmpty()
	{
		return true;
	}

	public override void ImportVariantSave(Dictionary data)
	{
		string text = ResolveSavedSize(data);
		if (!string.IsNullOrEmpty(text))
		{
			ApplySizeConfiguration(text, applyInitialRuntimeState: false);
		}
		_hitNum = Math.Max(0, data.GetValueOrDefault("hitNum", _hitNum).AsInt32());
		hitFinish = data.GetValueOrDefault("hitFinish", hitFinish).AsBool();
		ApplyHitProgressInvariants();
	}

	private string ResolveSavedSize(Dictionary data)
	{
		string text = data.GetValueOrDefault("size", _size).AsString();
		if (!string.IsNullOrEmpty(text) || !GodotObject.IsInstanceValid(transformPoint))
		{
			return text;
		}
		float a = Mathf.Abs(transformPoint.Scale.X);
		if (Mathf.IsEqualApprox(a, 0.5f))
		{
			return "Small";
		}
		if (Mathf.IsEqualApprox(a, 0.8f))
		{
			return "Normal";
		}
		if (Mathf.IsEqualApprox(a, 1.25f))
		{
			return "Large";
		}
		if (Mathf.IsEqualApprox(a, 1.5f))
		{
			return "Max";
		}
		return "";
	}

	private void ApplyHitProgressInvariants()
	{
		if (hitFinish && !(_size == "Max") && bowlingComponent != null)
		{
			bowlingComponent.edgeReboundUse = false;
			bowlingComponent.hitLineUse = false;
		}
	}
}
