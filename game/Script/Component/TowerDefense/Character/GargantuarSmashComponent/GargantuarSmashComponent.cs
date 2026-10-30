using Godot;

public sealed class GargantuarSmashComponent : CharacterComponentRuntime
{
	public string smashAudio = "GargantuarThump";

	public double cameraShakeDuration = 2.0;

	public double cameraShakeInterval = 0.05;

	public int cameraShakeCount = 4;

	private TowerDefenseZombieGargantuarBase _parent;

	private bool _configured;

	private GargantuarSmashComponentDefinition Definition => base.ComponentDefinition as GargantuarSmashComponentDefinition;

	protected override void OnBound()
	{
		_parent = base.Owner as TowerDefenseZombieGargantuarBase;
		if (!_configured)
		{
			GargantuarSmashComponentDefinition definition = Definition;
			smashAudio = definition?.smashAudio ?? "GargantuarThump";
			cameraShakeDuration = definition?.cameraShakeDuration ?? 2.0;
			cameraShakeInterval = definition?.cameraShakeInterval ?? 0.05;
			cameraShakeCount = definition?.cameraShakeCount ?? 4;
			_configured = true;
		}
	}

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		_parent = null;
	}

	protected override void OnReleased()
	{
		_parent = null;
	}

	public void SmashAttack(int gridRadius = 0)
	{
		if (!base.Alive || base.Lifecycle != ComponentRuntimeLifecycle.Active || !GodotObject.IsInstanceValid(_parent))
		{
			return;
		}
		if (cameraShakeCount > 0 && cameraShakeDuration > 0.0 && GodotObject.IsInstanceValid(ViewManager.Instance))
		{
			Vector2 dir = new Vector2((float)GD.RandRange(-1.0, 1.0), (float)GD.RandRange(-1.0, 1.0));
			ViewManager.Instance.CameraShake(dir, cameraShakeDuration, Mathf.Max(0.001, cameraShakeInterval), cameraShakeCount);
		}
		if (!string.IsNullOrEmpty(smashAudio) && GodotObject.IsInstanceValid(AudioManager.Instance))
		{
			AudioManager.Instance.AudioPlay(smashAudio);
		}
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
		{
			return;
		}
		AttackComponent attackComponent = _parent.attackComponent;
		if (attackComponent == null || attackComponent.IsReleased || !(_parent.config is TowerDefenseZombieConfig towerDefenseZombieConfig))
		{
			return;
		}
		if (gridRadius <= 0)
		{
			_parent.attackComponent.SmashAttackCell(towerDefenseZombieConfig.smashAttack);
			return;
		}
		Vector2I gridPos = _parent.gridPos;
		for (int i = -gridRadius; i <= gridRadius; i++)
		{
			for (int j = -gridRadius; j <= gridRadius; j++)
			{
				_parent.attackComponent.SmashAttackCell(towerDefenseZombieConfig.smashAttack, gridPos + new Vector2I(i, j));
			}
		}
	}
}
