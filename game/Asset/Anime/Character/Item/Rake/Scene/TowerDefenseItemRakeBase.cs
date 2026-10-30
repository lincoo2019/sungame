using System.Collections.Generic;
using Godot;
using Godot.Collections;

[Tool]
public abstract partial class TowerDefenseItemRakeBase : TowerDefenseItem
{
	protected AttackComponent attackComponent;

	public bool over;

	private System.Collections.Generic.Dictionary<ulong, Rect2> _previousTargetRects = new System.Collections.Generic.Dictionary<ulong, Rect2>();

	private System.Collections.Generic.Dictionary<ulong, Rect2> _currentTargetRects = new System.Collections.Generic.Dictionary<ulong, Rect2>();

	private bool _hitCompleted;

	private bool _restoredFromProgress;

	private bool _cleanupScheduled;

	public override void PrepareForProgressRestore()
	{
		base.PrepareForProgressRestore();
		_restoredFromProgress = true;
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			attackComponent = componentManager.GetRuntime<AttackComponent>("character.attack.0");
			DestroyHitBoxRuntime();
			TargetRegistrationComponent obj = targetRegistrationComponent;
			if (obj != null && !obj.IsReleased)
			{
				targetRegistrationComponent.canProjectileCheck = false;
			}
		}
	}

	public override void IdleEntered()
	{
		base.sprite.SetAnimation("Idle", loop: false, 0.2);
		base.sprite.pause = true;
	}

	public override void IdleProcessing(double delta)
	{
		base.sprite.timeScale = timeScale;
		if (inGame && GodotObject.IsInstanceValid(TowerDefenseManager.CurrentControl) && TowerDefenseManager.CurrentControl.isGameRunning && !over && TryAcquireContactTarget())
		{
			AudioManager.Instance.AudioPlay("Bonk");
			base.sprite.pause = false;
			over = true;
		}
	}

	public override void ActivateGameplayProcessing()
	{
		bool num = !over && GodotObject.IsInstanceValid(base.sprite) && base.sprite.pause;
		base.ActivateGameplayProcessing();
		if (num && !over && GodotObject.IsInstanceValid(base.sprite))
		{
			base.sprite.pause = true;
		}
	}

	private bool TryAcquireContactTarget()
	{
		AttackComponent obj = attackComponent;
		if (obj != null && obj.CanAttackOnce())
		{
			return true;
		}
		if (attackComponent == null || !attackComponent.TryGetCheckAreaWorldRect(out var worldRect) || !GodotObject.IsInstanceValid(TowerDefenseManager.Instance?.characterRegistry))
		{
			return false;
		}
		_currentTargetRects.Clear();
		List<TowerDefenseCharacter> charactersForLineList = TowerDefenseManager.Instance.characterRegistry.GetCharactersForLineList(base.gridPos.Y);
		for (int i = 0; i < charactersForLineList.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = charactersForLineList[i];
			if (GodotObject.IsInstanceValid(towerDefenseCharacter) && towerDefenseCharacter != this && towerDefenseCharacter.HasHitBox)
			{
				Rect2 worldHitRect = towerDefenseCharacter.WorldHitRect;
				ulong instanceId = towerDefenseCharacter.GetInstanceId();
				_currentTargetRects[instanceId] = worldHitRect;
				bool num = AabbShapeUtil.Intersects(worldRect, worldHitRect);
				Rect2 value;
				bool flag = _previousTargetRects.TryGetValue(instanceId, out value) && value != worldHitRect && AabbShapeUtil.Intersects(worldRect, AabbShapeUtil.Union(value, worldHitRect));
				if ((num || flag) && attackComponent.TryCommitContactTarget(towerDefenseCharacter))
				{
					_previousTargetRects.Clear();
					_currentTargetRects.Clear();
					return true;
				}
			}
		}
		System.Collections.Generic.Dictionary<ulong, Rect2> currentTargetRects = _currentTargetRects;
		System.Collections.Generic.Dictionary<ulong, Rect2> previousTargetRects = _previousTargetRects;
		_previousTargetRects = currentTargetRects;
		_currentTargetRects = previousTargetRects;
		return false;
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (!(clip != "Idle") && !_hitCompleted)
		{
			_hitCompleted = true;
			ApplyHitEffect();
			ScheduleCleanupAfterHit();
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			{ "over", over },
			{ "hitCompleted", _hitCompleted }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		base.ImportVariantSave(data);
		_hitCompleted = data.GetValueOrDefault("hitCompleted", false).AsBool();
		over = data.GetValueOrDefault("over", false).AsBool() || _hitCompleted;
		if (_restoredFromProgress && _hitCompleted)
		{
			CallDeferred("ResumePendingCleanup");
		}
	}

	private void ResumePendingCleanup()
	{
		ScheduleCleanupAfterHit();
	}

	private async void ScheduleCleanupAfterHit()
	{
		if (_cleanupScheduled)
		{
			return;
		}
		_cleanupScheduled = true;
		SceneTree tree = GetTree();
		if (!GodotObject.IsInstanceValid(tree))
		{
			Destroy();
			return;
		}
		await ToSignal(tree.CreateTimer(0.5, processAlways: false), SceneTreeTimer.SignalName.Timeout);
		if (GodotObject.IsInstanceValid(this) && !IsQueuedForDeletion())
		{
			Destroy();
		}
	}

	protected abstract void ApplyHitEffect();
}
