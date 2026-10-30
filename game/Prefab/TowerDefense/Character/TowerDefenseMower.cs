using System.Collections.Generic;
using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefenseMower : TowerDefenseItem
{
	public delegate void RunningEventHandler(TowerDefenseMower mower);

	private static PackedScene _MOWER_SPAWN;

	[Export(PropertyHint.None, "")]
	public string runAnimeClips = "Normal";

	[Export(PropertyHint.None, "")]
	public string runWaterAnimeClips = "Normal";

	[Export(PropertyHint.None, "")]
	public string attackAnimeClips = "";

	[Export(PropertyHint.None, "")]
	public string attackWaterAnimeClips = "";

	public CharacterMoveComponent moveComponent;

	public MowerHitComponent mowerHitComponent;

	private StateHandle _runStateHandle;

	private bool _runStateHandleConnected;

	private Vector2 _gridSize;

	private bool _isIZM2Mode;

	public bool run;

	private static PackedScene MOWER_SPAWN => _MOWER_SPAWN ?? (_MOWER_SPAWN = GD.Load<PackedScene>("uid://dy8bwagg440x0"));

	public event RunningEventHandler OnRunning;

	public bool CanStartRun()
	{
		if (TowerDefenseManager.Instance == null)
		{
			return false;
		}
		return TowerDefenseManager.Instance.IsGameRunning();
	}

	public override void _Ready()
	{
		if (Engine.IsEditorHint())
		{
			return;
		}
		base._Ready();
		if (!editorPreviewMode)
		{
			if (GodotObject.IsInstanceValid(componentManager))
			{
				moveComponent = componentManager.GetRuntime<CharacterMoveComponent>();
				mowerHitComponent = componentManager.GetRuntime<MowerHitComponent>();
			}
			GroundHeightComponent obj = groundHeightComponent;
			if (obj != null && !obj.IsReleased)
			{
				groundHeightComponent.SetAlive(alive: false);
			}
			_gridSize = TowerDefenseManager.Instance.GetMapGridSize();
			_isIZM2Mode = TowerDefenseManager.Instance.IsIZM2Mode();
			TowerDefenseEffectSpriteOnce towerDefenseEffectSpriteOnce = TowerDefenseManager.CreateEffectSpriteOnce(MOWER_SPAWN, base.gridPos);
			TowerDefenseGroundItemBase.characterNode.AddChild(towerDefenseEffectSpriteOnce, forceReadableName: false, InternalMode.Disabled);
			towerDefenseEffectSpriteOnce.GlobalPosition = GetLogicalGlobalPosition(transformPoint);
			base.sprite.pause = true;
			instance.invincible = true;
			AddToGroup("Mower", persistent: true);
			ConnectRunStateHandle();
			ActivateGameplayProcessing();
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
	}

	public override void ActivateGameplayProcessing()
	{
		base.ActivateGameplayProcessing();
		if (!run && GodotObject.IsInstanceValid(base.sprite))
		{
			base.sprite.pause = true;
		}
	}

	private void ConnectRunStateHandle()
	{
		if (_runStateHandleConnected)
		{
			return;
		}
		IStateMachineController stateMachine = base.StateMachine;
		if (stateMachine != null && stateMachine.IsInitialized)
		{
			_runStateHandle = base.StateMachine?.GetStateById("mower.run");
			StateHandle runStateHandle = _runStateHandle;
			if (runStateHandle != null && runStateHandle.IsValid)
			{
				_runStateHandle.Entered += RunEntered;
				_runStateHandle.Exited += RunExited;
				_runStateHandle.PhysicsProcessing += RunProcessing;
				_runStateHandleConnected = true;
			}
		}
	}

	private void DisconnectRunStateHandle()
	{
		if (_runStateHandleConnected && _runStateHandle != null)
		{
			_runStateHandle.Entered -= RunEntered;
			_runStateHandle.Exited -= RunExited;
			_runStateHandle.PhysicsProcessing -= RunProcessing;
		}
		_runStateHandle = null;
		_runStateHandleConnected = false;
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (TowerDefenseCharacter.CachedEditorHint)
		{
			return;
		}
		ProcessAabbHits();
		if (!run)
		{
			return;
		}
		Vector2 globalPositionForPhysicsFrame = GetGlobalPositionForPhysicsFrame(TowerDefenseProcessModeDispatch.CurrentPhysicsFrame);
		TowerDefenseBattleFeatureMap mapFeature = TowerDefenseManager.GetMapFeature();
		if (globalPositionForPhysicsFrame.X > mapFeature.config.edge.Z)
		{
			Destroy();
		}
		if (_isIZM2Mode && globalPositionForPhysicsFrame.X > TowerDefenseManager.Instance.GetMapCellPos(new Vector2I((int)Mathf.Floor((float)TowerDefenseManager.Instance.GetMapGridNum().X / 3f) + 1, 0)).X)
		{
			TowerDefenseManager.Instance.CreateMower(base.gridPos.Y);
			TowerDefenseEffectSpriteOnce towerDefenseEffectSpriteOnce = TowerDefenseManager.CreateEffectSpriteOnce(MOWER_SPAWN, base.gridPos);
			TowerDefenseGroundItemBase.characterNode.AddChild(towerDefenseEffectSpriteOnce, forceReadableName: false, InternalMode.Disabled);
			towerDefenseEffectSpriteOnce.GlobalPosition = GetLogicalGlobalPosition(transformPoint);
			Destroy();
		}
		if (mapFeature != null)
		{
			if (GodotObject.IsInstanceValid(cell))
			{
				Vector2 mapCellPos = TowerDefenseManager.Instance.GetMapCellPos(base.gridPos);
				cellPercentage = (globalPositionForPhysicsFrame - mapCellPos).X / _gridSize.X;
				base.inWater = cell.isWater;
			}
			else
			{
				base.inWater = false;
			}
		}
		if (!base.inWater && GodotObject.IsInstanceValid(cell))
		{
			double num = cell.GetGroundHeight(cellPercentage);
			if (Mathf.Abs(base.groundHeight - num) > 0.1)
			{
				base.groundHeight = Mathf.Lerp((float)base.groundHeight, (float)num, (float)(3.0 * delta));
			}
			else
			{
				base.groundHeight = num;
			}
		}
	}

	public void RunEntered()
	{
		this.OnRunning?.Invoke(this);
		CharacterMoveComponent characterMoveComponent = moveComponent;
		if (characterMoveComponent != null && !characterMoveComponent.IsReleased)
		{
			moveComponent.SetVelocity(Vector2.Right * 200f);
		}
		base.sprite.pause = false;
		AudioManager.Instance.AudioPlay("Mower");
		base.sprite.SetAnimation("Normal");
	}

	public void RunProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 3.0;
		if (CanSleep())
		{
			Sleep();
		}
	}

	public void RunExited()
	{
	}

	public void HitCheck(AabbArea2D area)
	{
		MowerHitComponent obj = mowerHitComponent;
		if (obj != null && !obj.IsReleased)
		{
			mowerHitComponent.HitCheck(area);
		}
	}

	private void ProcessAabbHits()
	{
		if (!CanStartRun() || !base.IsHitBoxEnabled)
		{
			return;
		}
		MowerHitComponent obj = mowerHitComponent;
		if (obj != null && !obj.IsReleased && TowerDefenseManager.Instance != null && TowerDefenseManager.Instance.characterRegistry != null)
		{
			Rect2 worldHitRect = base.WorldHitRect;
			List<TowerDefenseCharacter> charactersIntersectingRectList = TowerDefenseManager.Instance.characterRegistry.GetCharactersIntersectingRectList(worldHitRect, base.gridPos.Y, includeAllLineCheck: true);
			for (int i = 0; i < charactersIntersectingRectList.Count; i++)
			{
				mowerHitComponent.HitCheckCharacter(charactersIntersectingRectList[i]);
			}
		}
	}

	public void Hit(TowerDefenseCharacter character)
	{
		MowerHitComponent obj = mowerHitComponent;
		if (obj != null && !obj.IsReleased)
		{
			mowerHitComponent.Hit(character);
		}
	}

	public void Run()
	{
		if (CanStartRun())
		{
			ActivateGameplayProcessing();
			SendStateEvent("ToRun");
		}
	}

	public override void InWater()
	{
		base.InWater();
		if (runWaterAnimeClips != "")
		{
			base.sprite.SetAnimation(runWaterAnimeClips, loop: true, 0.1);
		}
	}

	public override void OutWater()
	{
		base.OutWater();
		if (runAnimeClips != "")
		{
			base.sprite.SetAnimation(runAnimeClips, loop: true, 0.1);
		}
	}

	public override void BlowBack(double num, double time = 1.0)
	{
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary { ["run"] = run };
	}

	public override void ImportVariantSave(Dictionary data)
	{
		run = data.GetValueOrDefault("run", false).AsBool();
	}
}
