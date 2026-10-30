using System;
using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombieSoccer : TowerDefenseZombie
{
	private struct IdleSoccerBallPredicate : ITowerDefenseCharacterRectPredicate, ITowerDefenseCharacterCandidatePredicate
	{
		public TowerDefenseEnum.CHARACTER_CAMP Camp;

		public bool CanConsider(TowerDefenseCharacter candidate)
		{
			if (candidate is TowerDefenseItemSoccerBall ball)
			{
				return IsKickableSameCampBall(ball, Camp);
			}
			return false;
		}

		public bool Matches(TowerDefenseCharacter candidate)
		{
			return true;
		}
	}

	private const string ClipPut = "Put";

	private const string ClipKick = "Kick";

	private const string TimerPut = "Put";

	public bool hasBall = true;

	private bool isPutting;

	private bool isKicking;

	private bool _wantPut;

	private bool _ballPlacedDuringPut;

	private CharacterTimerComponent _timerComponent;

	private AttackComponent _attackComponent2;

	private TowerDefenseItemSoccerBall _kickTarget;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_timerComponent = componentManager.GetRuntime<CharacterTimerComponent>("character.timer");
			_timerComponent.OnTimeout += OnTimerTimeout;
			_timerComponent.Run("Put");
			_attackComponent2 = componentManager.GetRuntime<AttackComponent>("character.attack.1");
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (!Engine.IsEditorHint())
		{
			CharacterTimerComponent timerComponent = _timerComponent;
			if (timerComponent != null && !timerComponent.IsReleased)
			{
				_timerComponent.timeScale = GetPutTimerScale();
			}
		}
	}

	private double GetPutTimerScale()
	{
		if (die || !hasBall || isPutting || isKicking)
		{
			return 0.0;
		}
		if (GodotObject.IsInstanceValid(base.sprite) && base.sprite.pause)
		{
			return 0.0;
		}
		return Math.Max(0.0, timeScale);
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		CharacterTimerComponent timerComponent = _timerComponent;
		if (timerComponent != null && !timerComponent.IsReleased)
		{
			_timerComponent.OnTimeout -= OnTimerTimeout;
		}
	}

	private void OnTimerTimeout(string timerName)
	{
		if (!die && hasBall && !isPutting && !isKicking && timerName == "Put")
		{
			_wantPut = true;
		}
	}

	public override void WalkProcessing(double delta)
	{
		if (isPutting || isKicking || die)
		{
			return;
		}
		bool flag = false;
		if (hasBall && (_wantPut || FindIdleSoccerBallInActionArea() != null))
		{
			_wantPut = false;
			StartPut();
			flag = true;
		}
		else if (!hasBall)
		{
			TowerDefenseItemSoccerBall towerDefenseItemSoccerBall = FindIdleSoccerBallInActionArea();
			if (towerDefenseItemSoccerBall != null)
			{
				StartKick(towerDefenseItemSoccerBall);
				flag = true;
			}
		}
		if (!flag)
		{
			base.WalkProcessing(delta);
		}
	}

	private void StartPut()
	{
		isPutting = true;
		_ballPlacedDuringPut = false;
		GroundMoveComponent obj = groundMoveComponent;
		if (obj != null && !obj.IsReleased)
		{
			groundMoveComponent.SetAlive(false);
		}
		base.sprite.SetAnimation("Put", loop: false, 0.2);
	}

	private void StartKick(TowerDefenseItemSoccerBall target)
	{
		isKicking = true;
		_kickTarget = target;
		GroundMoveComponent obj = groundMoveComponent;
		if (obj != null && !obj.IsReleased)
		{
			groundMoveComponent.SetAlive(false);
		}
		base.sprite.SetAnimation("Kick", loop: false, 0.2);
	}

	public override void AnimeEvent(string command, Variant argument)
	{
		base.AnimeEvent(command, argument);
		if (!(command == "kick"))
		{
			if (command == "put")
			{
				PlaceHeldBall();
			}
		}
		else
		{
			KickSoccerBall(_kickTarget);
		}
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (die)
		{
			return;
		}
		if (clip == "Put")
		{
			PlaceHeldBall();
			isPutting = false;
			GroundMoveComponent obj = groundMoveComponent;
			if (obj != null && !obj.IsReleased)
			{
				groundMoveComponent.SetAlive(true);
			}
			base.sprite.SetAnimation(base.walkAnimeClip, loop: true, 0.2);
		}
		else if (clip == "Kick")
		{
			_kickTarget = null;
			isKicking = false;
			GroundMoveComponent obj2 = groundMoveComponent;
			if (obj2 != null && !obj2.IsReleased)
			{
				groundMoveComponent.SetAlive(true);
			}
			base.sprite.SetAnimation(base.walkAnimeClip, loop: true, 0.2);
		}
	}

	private TowerDefenseItemSoccerBall FindIdleSoccerBallInActionArea()
	{
		AttackComponent attackComponent = _attackComponent2;
		if (attackComponent == null || attackComponent.IsReleased || !_attackComponent2.TryGetCheckAreaWorldRect(out var worldRect))
		{
			return null;
		}
		TowerDefenseBattleCharacterRegistry towerDefenseBattleCharacterRegistry = TowerDefenseManager.Instance?.characterRegistry;
		if (!GodotObject.IsInstanceValid(towerDefenseBattleCharacterRegistry))
		{
			return null;
		}
		IdleSoccerBallPredicate predicate = new IdleSoccerBallPredicate
		{
			Camp = base.camp
		};
		if (!towerDefenseBattleCharacterRegistry.TryFindItemIntersectingRect(worldRect, base.gridPos.Y, includeAllLineCheck: true, ref predicate, out var match))
		{
			return null;
		}
		return (TowerDefenseItemSoccerBall)match;
	}

	private bool IsKickableSameCampBall(TowerDefenseItemSoccerBall ball)
	{
		return IsKickableSameCampBall(ball, base.camp);
	}

	private static bool IsKickableSameCampBall(TowerDefenseItemSoccerBall ball, TowerDefenseEnum.CHARACTER_CAMP camp)
	{
		if (!GodotObject.IsInstanceValid(ball))
		{
			return false;
		}
		if (ball.die || ball.nearDie || ball.inWater)
		{
			return false;
		}
		if (ball.camp != camp)
		{
			return false;
		}
		BowlingComponent bowlingComponent = ball.bowlingComponent;
		if (bowlingComponent == null || bowlingComponent.IsReleased)
		{
			return false;
		}
		if (!ball.bowlingComponent.Alive)
		{
			return !ball.bowlingComponent.isRoll;
		}
		return false;
	}

	private void PlaceHeldBall()
	{
		if (hasBall)
		{
			SpawnSoccerBall();
			ApplyNoBallVisuals();
			hasBall = false;
			_ballPlacedDuringPut = true;
		}
	}

	private void ApplyNoBallVisuals()
	{
		base.sprite.SetFliter("ball", open: false);
		base.walkAnimeClip = "Walk2";
		base.attackAnimeClip = "Eat2";
	}

	private void SpawnSoccerBall()
	{
		if (!TowerDefenseManager.HasGameplayAuthority)
		{
			return;
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("ItemSoccerBall");
		if (packetConfig == null)
		{
			return;
		}
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		if ((base.EconomyOwnerAccountId.IsValid ? packetConfig.Create(base.EconomyOwnerAccountId, logicalGlobalPosition, base.gridPos, base.groundHeight) : packetConfig.Create(logicalGlobalPosition, base.gridPos, base.groundHeight)) is TowerDefenseItemSoccerBall towerDefenseItemSoccerBall)
		{
			towerDefenseItemSoccerBall.groundHeight = base.groundHeight;
			Node2D node2D = TowerDefenseManager.GetCharacterNode();
			if (GodotObject.IsInstanceValid(node2D))
			{
				ApplyOwnerStateToSoccerBall(towerDefenseItemSoccerBall);
				node2D.AddChild(towerDefenseItemSoccerBall, forceReadableName: false, InternalMode.Disabled);
				ApplyOwnerStateToSoccerBall(towerDefenseItemSoccerBall);
				TowerDefenseManager.PublishSpawnedCharacter("ItemSoccerBall", towerDefenseItemSoccerBall, useCreate: true);
			}
		}
	}

	private void ApplyOwnerStateToSoccerBall(TowerDefenseItemSoccerBall ball)
	{
		if (GodotObject.IsInstanceValid(ball))
		{
			ball.camp = base.camp;
			if (GodotObject.IsInstanceValid(ball.instance) && GodotObject.IsInstanceValid(instance))
			{
				ball.instance.hypnoses = instance.hypnoses;
			}
			CharacterMoveComponent moveComponent = ball.moveComponent;
			if (moveComponent != null && !moveComponent.IsReleased && GodotObject.IsInstanceValid(ball.instance))
			{
				ball.moveComponent.moveScale = (ball.instance.hypnoses ? (-1.0) : 1.0);
			}
		}
	}

	private void KickSoccerBall(TowerDefenseItemSoccerBall ball)
	{
		if (!IsKickableSameCampBall(ball))
		{
			return;
		}
		ApplyOwnerStateToSoccerBall(ball);
		ball.BuffDelete("Frozen");
		CharacterMoveComponent moveComponent = ball.moveComponent;
		if (moveComponent == null || moveComponent.IsReleased)
		{
			return;
		}
		BowlingComponent bowlingComponent = ball.bowlingComponent;
		if (bowlingComponent != null && !bowlingComponent.IsReleased)
		{
			double num = Math.Abs(ball.bowlingComponent.rollXVelocityMin);
			double num2 = Math.Abs(ball.bowlingComponent.rollXVelocityMax);
			if (num > num2)
			{
				double num3 = num2;
				num2 = num;
				num = num3;
			}
			double num4 = ball.moveComponent.moveScale;
			if (Math.Abs(num4) <= 0.01)
			{
				num4 = 1.0;
			}
			double num5 = GD.RandRange(num, num2);
			ball.moveComponent.velocity = new Vector2((float)(GetKickDirection() * num5 / num4), ball.moveComponent.velocity.Y);
			ball.StartKickedRoll();
		}
	}

	private double GetKickDirection()
	{
		if (base.camp == TowerDefenseEnum.CHARACTER_CAMP.PLANT)
		{
			return 1.0;
		}
		if (base.camp == TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE)
		{
			return -1.0;
		}
		if (!(base.Scale.X < 0f))
		{
			return -1.0;
		}
		return 1.0;
	}

	public override Dictionary ExportVariantSave()
	{
		Dictionary dictionary = base.ExportVariantSave();
		dictionary["hasBall"] = hasBall;
		dictionary["isPutting"] = isPutting;
		dictionary["isKicking"] = isKicking;
		dictionary["wantPut"] = _wantPut;
		dictionary["ballPlacedDuringPut"] = _ballPlacedDuringPut;
		return dictionary;
	}

	public override void ImportVariantSave(Dictionary data)
	{
		base.ImportVariantSave(data);
		hasBall = data.GetValueOrDefault("hasBall", true).AsBool();
		isPutting = data.GetValueOrDefault("isPutting", false).AsBool();
		isKicking = data.GetValueOrDefault("isKicking", false).AsBool();
		_wantPut = data.GetValueOrDefault("wantPut", false).AsBool();
		_ballPlacedDuringPut = data.GetValueOrDefault("ballPlacedDuringPut", false).AsBool();
		if (!hasBall)
		{
			ApplyNoBallVisuals();
		}
		if (isPutting || isKicking)
		{
			GroundMoveComponent obj = groundMoveComponent;
			if (obj != null && !obj.IsReleased)
			{
				groundMoveComponent.SetAlive(false);
			}
		}
	}
}
