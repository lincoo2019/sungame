using Godot;

[Tool]
[GlobalClass]
public partial class TowerDefensePlant : TowerDefenseCharacter
{
	private sealed class PlantStatePhysicsFastCallbackTarget : IStateMachinePhysicsFastCallbackTarget
	{
		private readonly TowerDefensePlant _owner;

		internal PlantStatePhysicsFastCallbackTarget(TowerDefensePlant owner)
		{
			_owner = owner;
		}

		void IStateMachinePhysicsFastCallbackTarget.InvokeStateMachinePhysicsFastCallback(int callbackId, double delta)
		{
			if (callbackId == 1)
			{
				_owner.PlantProcessing(delta);
			}
		}
	}

	private const int PlantPhysicsCallbackId = 1;

	public WaterInteractionComponent waterInteractionComponent;

	public PlantAnimeComponent plantAnimeComponent;

	public PuzzleShaderComponent puzzleShaderComponent;

	private StateHandle _plantStateHandle;

	private PlantStatePhysicsFastCallbackTarget _plantStatePhysicsFastCallbackTarget;

	private bool _plantStateHandleConnected;

	public TowerDefenseCharacter targetZombie;

	public TowerDefenseCharacter targetPlant;

	public double zombiePlaceDamage;

	[Export(PropertyHint.None, "")]
	public string plantAnimeClip = "";

	private bool _izmOneShotTouched;

	private bool _izmOneShotTouchHookBound;

	public override bool ShouldUpdateGridPos()
	{
		return false;
	}

	public override void _Ready()
	{
		base._Ready();
		if (!editorPreviewMode && !Engine.IsEditorHint())
		{
			if (GodotObject.IsInstanceValid(componentManager))
			{
				plantAnimeComponent = componentManager.GetRuntime<PlantAnimeComponent>();
				waterInteractionComponent = componentManager.GetRuntime<WaterInteractionComponent>();
				puzzleShaderComponent = componentManager.GetRuntime<PuzzleShaderComponent>();
			}
			AddToGroup("Plant", persistent: true);
			instance.hitpointsEmpty += delegate
			{
				Destroy();
			};
			PuzzleShaderComponent obj = puzzleShaderComponent;
			if (obj != null && !obj.IsReleased)
			{
				puzzleShaderComponent.Init();
			}
			ShowHealthComponent obj2 = showHealthComponent;
			if (obj2 != null && !obj2.IsReleased)
			{
				showHealthComponent.SetAlive(GameSaveManager.Instance.GetConfigValue("ShowPlantHealth").AsBool());
			}
			if (inGame)
			{
				BattleEventBus.Instance.OnShowPlantHealth += OnShowPlantHealth;
			}
			if (base.config.plantGridType.Contains(TowerDefenseEnum.PLANTGRIDTYPE.AIR))
			{
				shadowComponent.shadowDisabled = true;
				shadowSprite.Visible = false;
			}
			ConnectPlantStateHandle();
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		if (!Engine.IsEditorHint() && GodotObject.IsInstanceValid(BattleEventBus.Instance))
		{
			BattleEventBus.Instance.OnShowPlantHealth -= OnShowPlantHealth;
		}
	}

	private void ConnectPlantStateHandle()
	{
		if (_plantStateHandleConnected)
		{
			return;
		}
		IStateMachineController stateMachine = base.StateMachine;
		if (stateMachine == null || !stateMachine.IsInitialized)
		{
			return;
		}
		_plantStateHandle = base.StateMachine?.GetStateById("plant.plant");
		StateHandle plantStateHandle = _plantStateHandle;
		if (plantStateHandle != null && plantStateHandle.IsValid)
		{
			if (_plantStatePhysicsFastCallbackTarget == null)
			{
				_plantStatePhysicsFastCallbackTarget = new PlantStatePhysicsFastCallbackTarget(this);
			}
			_plantStateHandle.Entered += PlantEntered;
			_plantStateHandle.Exited += PlantExited;
			if (!_plantStateHandle.TrySetPhysicsFastCallback(_plantStatePhysicsFastCallbackTarget, 1))
			{
				_plantStateHandle.PhysicsProcessing += PlantProcessing;
			}
			_plantStateHandleConnected = true;
		}
	}

	private void DisconnectPlantStateHandle()
	{
		if (_plantStateHandleConnected && _plantStateHandle != null)
		{
			_plantStateHandle.Entered -= PlantEntered;
			_plantStateHandle.Exited -= PlantExited;
			if (!_plantStateHandle.ClearPhysicsFastCallback(_plantStatePhysicsFastCallbackTarget))
			{
				_plantStateHandle.PhysicsProcessing -= PlantProcessing;
			}
		}
		_plantStateHandle = null;
		_plantStateHandleConnected = false;
	}

	private void OnShowPlantHealth(bool show)
	{
		ShowHealthComponent obj = showHealthComponent;
		if (obj != null && !obj.IsReleased)
		{
			showHealthComponent.SetAlive(show);
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (!TowerDefenseCharacter.CachedEditorHint)
		{
			ulong currentPhysicsFrame = TowerDefenseProcessModeDispatch.CurrentPhysicsFrame;
			if (GodotObject.IsInstanceValid(targetZombie))
			{
				SetGlobalPositionForPhysicsFrame(targetZombie.GetGlobalPositionForPhysicsFrame(currentPhysicsFrame), currentPhysicsFrame);
			}
			if (GodotObject.IsInstanceValid(targetPlant))
			{
				SetGlobalPositionForPhysicsFrame(targetPlant.GetGlobalPositionForPhysicsFrame(currentPhysicsFrame), currentPhysicsFrame);
			}
			if ((ulong)((long)currentPhysicsFrame + (long)randFreshIndex) % 5uL == 0L && instance != null)
			{
				instance.RefreshDamagePoint();
			}
		}
	}

	public void OnTargetZombieDestroyed()
	{
		targetZombie = null;
		Destroy();
	}

	public void OnTargetPlantDestroyed()
	{
		targetPlant = null;
		Destroy();
	}

	public override void IdleEntered()
	{
		base.IdleEntered();
		PuzzleShaderComponent obj = puzzleShaderComponent;
		if (obj != null && !obj.IsReleased)
		{
			puzzleShaderComponent.IdleEntered();
		}
	}

	protected static bool IsIzmLevelMode()
	{
		TowerDefenseManager towerDefenseManager = TowerDefenseManager.Instance;
		if (GodotObject.IsInstanceValid(towerDefenseManager))
		{
			return towerDefenseManager.IsIZMMode();
		}
		return false;
	}

	protected bool IsIzmOneShotGateOpen()
	{
		if (!IsIzmLevelMode() || _izmOneShotTouched)
		{
			return true;
		}
		BindIzmOneShotTouchHook();
		return _izmOneShotTouched;
	}

	private void BindIzmOneShotTouchHook()
	{
		if (!_izmOneShotTouchHookBound)
		{
			_izmOneShotTouchHookBound = true;
			base.OnBodyHurt += MarkIzmOneShotTouched;
			base.OnArmorHurt += MarkIzmOneShotTouched;
		}
	}

	private void MarkIzmOneShotTouched(int num)
	{
		_izmOneShotTouched = true;
	}

	public override void IdleExited()
	{
		base.IdleExited();
		PuzzleShaderComponent obj = puzzleShaderComponent;
		if (obj != null && !obj.IsReleased)
		{
			puzzleShaderComponent.IdleExited();
		}
	}

	public void PlantEntered()
	{
		PlantAnimeComponent obj = plantAnimeComponent;
		if (obj != null && obj.Lifecycle == ComponentRuntimeLifecycle.Active)
		{
			plantAnimeComponent.PlantEntered();
		}
		else
		{
			Idle();
		}
	}

	public void PlantProcessing(double delta)
	{
		PlantAnimeComponent obj = plantAnimeComponent;
		if (obj != null && obj.Lifecycle == ComponentRuntimeLifecycle.Active)
		{
			plantAnimeComponent.PlantProcessing((float)delta);
		}
	}

	public void PlantExited()
	{
		PlantAnimeComponent obj = plantAnimeComponent;
		if (obj != null && obj.Lifecycle == ComponentRuntimeLifecycle.Active)
		{
			plantAnimeComponent.PlantExited();
		}
	}

	public override void AnimeEvent(string command, Variant argument)
	{
		base.AnimeEvent(command, argument);
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		PlantAnimeComponent obj = plantAnimeComponent;
		if (obj != null && obj.Lifecycle == ComponentRuntimeLifecycle.Active)
		{
			plantAnimeComponent.AnimeCompleted(clip);
		}
	}

	public override void InWater()
	{
		base.InWater();
		WaterInteractionComponent obj = waterInteractionComponent;
		if (obj != null && !obj.IsReleased)
		{
			waterInteractionComponent.InWater();
		}
	}

	public override void OutWater()
	{
		base.OutWater();
		WaterInteractionComponent obj = waterInteractionComponent;
		if (obj != null && !obj.IsReleased)
		{
			waterInteractionComponent.OutWater();
		}
	}

	public override void Cover(TowerDefenseCharacter character)
	{
		base.Cover(character);
		if (GodotObject.IsInstanceValid(character) && character.config is TowerDefensePlantConfig { coverEvents: { } coverEvents })
		{
			for (int i = 0; i < coverEvents.Count; i++)
			{
				coverEvents[i]?.Execute(GetLogicalGlobalPosition(), this);
			}
		}
	}
}
