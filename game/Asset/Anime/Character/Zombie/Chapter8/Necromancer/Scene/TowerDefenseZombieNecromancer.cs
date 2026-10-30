using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombieNecromancer : TowerDefenseZombie
{
	private StateHandle _spawnStateHandle;

	private bool _roleStateSignalsConnected;

	[Export(PropertyHint.None, "")]
	public double spawnTime = 10.0;

	public double spawnTimer;

	public bool over;

	private void ConnectRoleStateSignals()
	{
		if (_roleStateSignalsConnected)
		{
			return;
		}
		IStateMachineController stateMachine = base.StateMachine;
		if (stateMachine != null && stateMachine.IsInitialized)
		{
			_spawnStateHandle = base.StateMachine?.GetStateById("zombie.necromancer.spawn");
			StateHandle spawnStateHandle = _spawnStateHandle;
			if (spawnStateHandle != null && spawnStateHandle.IsValid)
			{
				_spawnStateHandle.Entered += SpawnEntered;
				_spawnStateHandle.Exited += SpawnExited;
				_spawnStateHandle.Processing += SpawnProcessing;
				_roleStateSignalsConnected = true;
			}
		}
	}

	private void DisconnectRoleStateSignals()
	{
		if (_roleStateSignalsConnected)
		{
			if (_spawnStateHandle != null)
			{
				_spawnStateHandle.Entered -= SpawnEntered;
				_spawnStateHandle.Exited -= SpawnExited;
				_spawnStateHandle.Processing -= SpawnProcessing;
			}
			_spawnStateHandle = null;
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
		if (!Engine.IsEditorHint() && !editorPreviewMode)
		{
			ConnectRoleStateSignals();
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (!Engine.IsEditorHint() && TowerDefenseManager.Instance.IsGameRunning() && inGame && base.IsInsideComponentBattlefield && !die && !nearDie && !base.sprite.pause)
		{
			if (spawnTimer < spawnTime)
			{
				spawnTimer += delta * timeScale;
				return;
			}
			SendStateEvent("ToSpawn");
			spawnTimer = 0.0;
		}
	}

	public void SpawnEntered()
	{
		AudioManager.Instance.AudioPlay("Skeleton");
		base.sprite.SetAnimation("Spawn", loop: true, 0.2);
		Vector2 mapGridSize = TowerDefenseManager.Instance.GetMapGridSize();
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		SpawnPlantZombie(logicalGlobalPosition - new Vector2((float)((double)mapGridSize.X * 1.25), 0f));
		SpawnPlantZombie(logicalGlobalPosition - new Vector2((float)((double)mapGridSize.X * 1.25 + 20.0), 0f));
	}

	public virtual void SpawnProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 1.5;
	}

	public void SpawnExited()
	{
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (clip == "Spawn")
		{
			Walk();
		}
	}

	public void SpawnPlantZombie(Vector2 pos)
	{
		if (Global.Instance.isMultiplayerMode && !MultiPlayerManager.Instance.isHost)
		{
			return;
		}
		TowerDefenseCharacter zombie = TowerDefenseCharacter.CreateCharacter("ZombieSkeleton", pos, base.gridPos - new Vector2I(1, 0), 0.0);
		double _hitpointScale = instance.hitpointScale;
		Vector2 _scale = transformPoint.Scale;
		Callable.From(delegate
		{
			if (GodotObject.IsInstanceValid(zombie))
			{
				if (GodotObject.IsInstanceValid(zombie.instance))
				{
					zombie.instance.hitpointScale = _hitpointScale;
				}
				if (GodotObject.IsInstanceValid(zombie.transformPoint))
				{
					zombie.transformPoint.Scale = _scale;
				}
			}
		}).CallDeferred();
		zombie.Rise(2.5);
		if (instance.hypnoses)
		{
			zombie.Hypnoses();
		}
		if (Global.Instance.isMultiplayerMode && MultiPlayerManager.Instance.isHost)
		{
			TowerDefenseControlNew currentControl = TowerDefenseManager.Instance.currentControl;
			if (GodotObject.IsInstanceValid(currentControl))
			{
				int nextSyncId = currentControl.GetNextSyncId();
				currentControl.RegisterSyncCharacter(nextSyncId, zombie);
				MultiPlayerManager.Instance.SendSpawnCharacterAt("ZombieSkeleton", (base.gridPos - new Vector2I(1, 0)).X, (base.gridPos - new Vector2I(1, 0)).Y, nextSyncId, _hitpointScale, _scale.X, instance.hypnoses, 2.5, useCreate: true, pos.X, pos.Y);
			}
		}
	}

	public override async void DestroySet()
	{
		if (!base.inWater && !over)
		{
			over = true;
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			CraterCreate(nolimit: true, "CraterN");
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			{ "spawnTimer", spawnTimer },
			{ "over", over }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		spawnTimer = data.GetValueOrDefault("spawnTimer", 0.0).AsDouble();
		over = data.GetValueOrDefault("over", false).AsBool();
	}
}
