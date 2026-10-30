using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefensePlantAppleGreen : TowerDefensePlant
{
	public bool run;

	public double runTimer;

	private bool _effectActive;

	private const double RunDuration = 8.0;

	public override void _Ready()
	{
		base._Ready();
	}

	public override void _ExitTree()
	{
		if (!Engine.IsEditorHint())
		{
			ReleaseEffectLease();
		}
		base._ExitTree();
	}

	public override void IdleEntered()
	{
		if (inGame && GodotObject.IsInstanceValid(TowerDefenseManager.CurrentControl) && TowerDefenseManager.CurrentControl.isGameRunning && !characterDisabled)
		{
			if (run)
			{
				ResumeEffect();
			}
			else
			{
				RunApple();
			}
		}
	}

	public override void IdleProcessing(double delta)
	{
		base.IdleProcessing(delta);
		base.sprite.timeScale = timeScale * 2.0;
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (Engine.IsEditorHint() || !inGame || !GodotObject.IsInstanceValid(TowerDefenseManager.CurrentControl) || !TowerDefenseManager.CurrentControl.isGameRunning || !run)
		{
			return;
		}
		if (characterDisabled)
		{
			if (_effectActive)
			{
				PauseEffect();
			}
		}
		else if (!_effectActive)
		{
			ResumeEffect();
		}
		runTimer -= delta;
		if (runTimer <= 0.0)
		{
			Destroy();
		}
	}

	public void RunApple()
	{
		AudioManager.Instance.AudioPlay("Apple");
		base.sprite.SetAnimation("Running", loop: true, 0.2);
		instance.invincible = true;
		run = true;
		runTimer = 8.0;
		ActivateEffect();
	}

	private void ActivateEffect()
	{
		AddToGroup("AppleBack");
		_effectActive = true;
		TowerDefenseManager.Instance.backPacket = true;
		TowerDefenseManager.Instance.backZombie = true;
	}

	private void PauseEffect()
	{
		ReleaseEffectLease();
		base.sprite.pause = true;
	}

	private void ResumeEffect()
	{
		AddToGroup("AppleBack");
		_effectActive = true;
		TowerDefenseManager.Instance.backPacket = true;
		TowerDefenseManager.Instance.backZombie = true;
		base.sprite.pause = false;
	}

	public override async void DestroySet()
	{
		base.DestroySet();
		ReleaseEffectLease(clearGlobalEffect: false);
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		ClearGlobalEffectIfUnowned();
	}

	private void ReleaseEffectLease(bool clearGlobalEffect = true)
	{
		if (_effectActive || run)
		{
			_effectActive = false;
			RemoveFromGroup("AppleBack");
			if (clearGlobalEffect)
			{
				ClearGlobalEffectIfUnowned();
			}
		}
	}

	private void ClearGlobalEffectIfUnowned()
	{
		if (IsInsideTree() && GetTree().GetNodeCountInGroup("AppleBack") <= 0 && GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
		{
			TowerDefenseManager.Instance.backPacket = false;
			TowerDefenseManager.Instance.backZombie = false;
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			{ "run", run },
			{ "runTimer", runTimer }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		run = data.GetValueOrDefault("run", Variant.From<bool>(false)).AsBool();
		runTimer = data.GetValueOrDefault("runTimer", Variant.From<double>(0.0)).AsDouble();
	}
}
