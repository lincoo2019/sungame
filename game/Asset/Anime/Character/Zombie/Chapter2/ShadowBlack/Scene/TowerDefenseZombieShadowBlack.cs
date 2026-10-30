using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefenseZombieShadowBlack : TowerDefenseZombie
{
	private StateHandle _spawnStateHandle;

	private bool _stateSignalsConnected;

	public bool spawnOver;

	private void ConnectStateSignals()
	{
		if (_stateSignalsConnected)
		{
			return;
		}
		IStateMachineController stateMachine = base.StateMachine;
		if (stateMachine != null && stateMachine.IsInitialized)
		{
			_spawnStateHandle = base.StateMachine?.GetStateById("zombie.shadow_black.spawn");
			StateHandle spawnStateHandle = _spawnStateHandle;
			if (spawnStateHandle != null && spawnStateHandle.IsValid)
			{
				_spawnStateHandle.Entered += SpawnEntered;
				_spawnStateHandle.PhysicsProcessing += SpawnProcessing;
				_stateSignalsConnected = true;
			}
		}
	}

	private void DisconnectStateSignals()
	{
		if (_stateSignalsConnected)
		{
			if (_spawnStateHandle != null)
			{
				_spawnStateHandle.Entered -= SpawnEntered;
				_spawnStateHandle.PhysicsProcessing -= SpawnProcessing;
			}
			_spawnStateHandle = null;
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

	public override void WalkProcessing(double delta)
	{
		SwimComponent obj = swimComponent;
		if (obj != null && !obj.IsReleased)
		{
			swimComponent.WalkProcessing((float)delta);
		}
	}

	public override void DieEntered()
	{
		base.DieEntered();
		if (base.inWater)
		{
			base.sprite.SetAnimation(base.dieWaterAnimeClip, loop: false);
		}
		else
		{
			base.sprite.SetAnimation(base.dieAnimeClip, loop: false);
		}
	}

	public override void DieProcessing(double delta)
	{
		base.DieProcessing(delta);
		base.sprite.timeScale = timeScale * 2.0;
	}

	public virtual void SpawnEntered()
	{
		base.sprite.SetAnimation("Spawn", loop: false);
	}

	public virtual void SpawnProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 2.0;
	}

	public override void Walk()
	{
		if (spawnOver)
		{
			base.Walk();
		}
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (clip == "Spawn")
		{
			spawnOver = true;
			if (Global.IsEditor && GodotObject.IsInstanceValid(LevelEditorMapEditor.instance))
			{
				Idle();
			}
			else if (GodotObject.IsInstanceValid(TowerDefenseManager.CurrentControl) && !TowerDefenseManager.CurrentControl.isGameRunning)
			{
				Idle();
			}
			else
			{
				Walk();
			}
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary { { "spawnOver", spawnOver } };
	}

	public override void ImportVariantSave(Dictionary data)
	{
		spawnOver = data.GetValueOrDefault("spawnOver", false).AsBool();
	}
}
