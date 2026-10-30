using System;
using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefenseZombieSeaDemon : TowerDefenseZombie
{
	private StateHandle _pointStateHandle;

	private bool _stateSignalsConnected;

	public double timer;

	public bool spawnNext;

	public bool jumpMove;

	public bool isPointing;

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

	public override Dictionary ExportVariantSave()
	{
		Dictionary dictionary = base.ExportVariantSave();
		dictionary["timer"] = timer;
		dictionary["spawnNext"] = spawnNext;
		dictionary["jumpMove"] = jumpMove;
		dictionary["isPointing"] = isPointing;
		return dictionary;
	}

	public override bool ImportVariantSaveWhenEmpty()
	{
		return true;
	}

	public override void ImportVariantSave(Dictionary data)
	{
		base.ImportVariantSave(data);
		timer = Math.Max(0.0, data.GetValueOrDefault("timer", timer).AsDouble());
		spawnNext = data.GetValueOrDefault("spawnNext", spawnNext).AsBool();
		jumpMove = data.GetValueOrDefault("jumpMove", jumpMove).AsBool();
		isPointing = data.GetValueOrDefault("isPointing", isPointing).AsBool();
	}

	private void ConnectStateSignals()
	{
		if (_stateSignalsConnected)
		{
			return;
		}
		IStateMachineController stateMachine = base.StateMachine;
		if (stateMachine != null && stateMachine.IsInitialized)
		{
			_pointStateHandle = base.StateMachine?.GetStateById("zombie.sea_demon.point");
			StateHandle pointStateHandle = _pointStateHandle;
			if (pointStateHandle != null && pointStateHandle.IsValid)
			{
				_pointStateHandle.Entered += PointEntered;
				_pointStateHandle.Exited += PointExited;
				_pointStateHandle.PhysicsProcessing += PointProcessing;
				_stateSignalsConnected = true;
			}
		}
	}

	private void DisconnectStateSignals()
	{
		if (_stateSignalsConnected)
		{
			if (_pointStateHandle != null)
			{
				_pointStateHandle.Entered -= PointEntered;
				_pointStateHandle.Exited -= PointExited;
				_pointStateHandle.PhysicsProcessing -= PointProcessing;
			}
			_pointStateHandle = null;
			_stateSignalsConnected = false;
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
			ConnectStateSignals();
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (!Engine.IsEditorHint() && GodotObject.IsInstanceValid(TowerDefenseManager.CurrentControl) && TowerDefenseManager.CurrentControl.isGameRunning && inGame && base.IsInsideComponentBattlefield && !spawnNext)
		{
			if (timer < 15.0)
			{
				timer += delta * timeScale;
				return;
			}
			spawnNext = true;
			timer = 0.0;
		}
	}

	public override void WalkEntered()
	{
		if (base.inWater)
		{
			if (!inSwimPlay && base.inSwimAnimeClip != "")
			{
				base.sprite.SetAnimation(base.inSwimAnimeClip, loop: false, 0.2);
				base.sprite.AddAnimation(base.swimAnimeClip, 0.0);
				inSwimPlay = true;
			}
			else
			{
				base.sprite.SetAnimation(base.swimAnimeClip, loop: true, 0.2);
			}
			ActivateGroundMovementAfterStateDelay(base.WalkStateHandle);
		}
		else if (jumpMove)
		{
			jumpMove = false;
			if (base.inWater)
			{
				base.sprite.SetAnimation(base.swimAnimeClip);
			}
			else
			{
				base.sprite.SetAnimation(base.walkAnimeClip);
			}
			ActivateGroundMovementAfterStateDelay(base.WalkStateHandle);
		}
		else
		{
			base.WalkEntered();
		}
	}

	public override void OutWater()
	{
		base.OutWater();
		GlobalPositionX -= base.Scale.X * transformPoint.Scale.X * 20f;
	}

	public override void DieEntered()
	{
		base.DieEntered();
		base.sprite.offset = new Vector2(-50f, -80f);
	}

	public void PointEntered()
	{
		isPointing = true;
		if (base.inWater)
		{
			base.sprite.SetAnimation("SwimPoint", loop: false);
		}
		else
		{
			base.sprite.SetAnimation("Point", loop: false);
		}
	}

	public void PointProcessing(double delta)
	{
		base.sprite.timeScale = timeScale;
	}

	public void PointExited()
	{
		isPointing = false;
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (!die && !nearDie && spawnNext && !isPointing && SendStateEvent("ToPoint"))
		{
			spawnNext = false;
			return;
		}
		switch (clip)
		{
		case "Point":
		case "SwimPoint":
			isPointing = false;
			Walk();
			break;
		case "Jump":
			jumpMove = true;
			GlobalPositionX -= base.Scale.X * transformPoint.Scale.X * 60f;
			break;
		}
	}

	public override void AnimeEvent(string command, Variant argument)
	{
		base.AnimeEvent(command, argument);
		if (!(command == "attack"))
		{
			if (command == "spawn")
			{
				SpawnSnorkle();
			}
		}
		else
		{
			attackComponent.AttackExecute(((TowerDefenseZombieConfig)base.config).smashAttack);
		}
	}

	public void SpawnSnorkle()
	{
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
		{
			return;
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("ZombieSnorkleTanglekelp");
		Vector2I mapGridNum = TowerDefenseManager.Instance.GetMapGridNum();
		double hitpointScale = instance.hitpointScale;
		Vector2 scale = transformPoint.Scale;
		float x = GetLogicalGlobalPosition().X;
		if (base.gridPos.Y > 1)
		{
			TowerDefenseCharacter snorkle = packetConfig.Create(new Vector2(x, (float)TowerDefenseManager.GetMapLineY(base.gridPos.Y - 1)), base.gridPos - new Vector2I(0, 1));
			TowerDefenseManager.GetCharacterNode().CallDeferred("add_child", snorkle);
			Callable.From(delegate
			{
				if (GodotObject.IsInstanceValid(snorkle))
				{
					if (GodotObject.IsInstanceValid(snorkle.instance))
					{
						snorkle.instance.hitpointScale = hitpointScale;
					}
					if (GodotObject.IsInstanceValid(snorkle.transformPoint))
					{
						snorkle.transformPoint.Scale = scale;
					}
				}
			}).CallDeferred();
			Callable.From(delegate
			{
				snorkle.Rise(1.5);
			}).CallDeferred();
			snorkle.invisible = base.invisible;
			if (instance.hypnoses)
			{
				Callable.From(delegate
				{
					if (GodotObject.IsInstanceValid(snorkle))
					{
						snorkle.Hypnoses();
					}
				}).CallDeferred();
			}
		}
		if (base.gridPos.Y < mapGridNum.Y)
		{
			TowerDefenseCharacter snorkle2 = packetConfig.Create(new Vector2(x, (float)TowerDefenseManager.GetMapLineY(base.gridPos.Y + 1)), base.gridPos + new Vector2I(0, 1));
			TowerDefenseManager.GetCharacterNode().CallDeferred("add_child", snorkle2);
			Callable.From(delegate
			{
				if (GodotObject.IsInstanceValid(snorkle2))
				{
					if (GodotObject.IsInstanceValid(snorkle2.instance))
					{
						snorkle2.instance.hitpointScale = hitpointScale;
					}
					if (GodotObject.IsInstanceValid(snorkle2.transformPoint))
					{
						snorkle2.transformPoint.Scale = scale;
					}
				}
			}).CallDeferred();
			Callable.From(delegate
			{
				snorkle2.Rise(1.5);
			}).CallDeferred();
			snorkle2.invisible = base.invisible;
			if (instance.hypnoses)
			{
				Callable.From(delegate
				{
					if (GodotObject.IsInstanceValid(snorkle2))
					{
						snorkle2.Hypnoses();
					}
				}).CallDeferred();
			}
		}
		TowerDefenseCharacter snorkle3 = packetConfig.Create(new Vector2(x, (float)TowerDefenseManager.GetMapLineY(base.gridPos.Y)), base.gridPos);
		TowerDefenseManager.GetCharacterNode().CallDeferred("add_child", snorkle3);
		Callable.From(delegate
		{
			if (GodotObject.IsInstanceValid(snorkle3))
			{
				if (GodotObject.IsInstanceValid(snorkle3.instance))
				{
					snorkle3.instance.hitpointScale = hitpointScale;
				}
				if (GodotObject.IsInstanceValid(snorkle3.transformPoint))
				{
					snorkle3.transformPoint.Scale = scale;
				}
			}
		}).CallDeferred();
		Callable.From(delegate
		{
			snorkle3.Rise(1.5);
		}).CallDeferred();
		snorkle3.invisible = base.invisible;
		if (!instance.hypnoses)
		{
			return;
		}
		Callable.From(delegate
		{
			if (GodotObject.IsInstanceValid(snorkle3))
			{
				snorkle3.Hypnoses();
			}
		}).CallDeferred();
	}
}
