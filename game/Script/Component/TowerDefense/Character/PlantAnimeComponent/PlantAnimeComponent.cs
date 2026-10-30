using Godot;

public sealed class PlantAnimeComponent : CharacterComponentRuntime
{
	public TowerDefensePlant parent;

	public bool fallbackToIdle = true;

	public bool syncSpriteTimeScale = true;

	public bool loopAnimation;

	private bool _configured;

	private PlantAnimeComponentDefinition Definition => base.ComponentDefinition as PlantAnimeComponentDefinition;

	protected override void OnBound()
	{
		parent = base.Owner as TowerDefensePlant;
		if (!_configured)
		{
			PlantAnimeComponentDefinition definition = Definition;
			fallbackToIdle = definition?.fallbackToIdle ?? true;
			syncSpriteTimeScale = definition?.syncSpriteTimeScale ?? true;
			loopAnimation = definition?.loopAnimation ?? false;
			_configured = true;
		}
	}

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		parent = null;
	}

	protected override void OnReleased()
	{
		parent = null;
	}

	public void PlantEntered()
	{
		if (base.Lifecycle != ComponentRuntimeLifecycle.Active || !GodotObject.IsInstanceValid(parent))
		{
			return;
		}
		if (!TryGetAnimation(out var clip))
		{
			if (fallbackToIdle)
			{
				parent.Idle();
			}
		}
		else
		{
			parent.sprite.SetAnimation(clip, loopAnimation);
		}
	}

	public void PlantProcessing(float _delta)
	{
		if (base.Lifecycle == ComponentRuntimeLifecycle.Active && syncSpriteTimeScale && GodotObject.IsInstanceValid(parent?.sprite) && !Mathf.IsEqualApprox(parent.sprite.timeScale, parent.timeScale))
		{
			parent.sprite.timeScale = parent.timeScale;
		}
	}

	public void PlantExited()
	{
		_ = base.Lifecycle;
		_ = 2;
	}

	public bool AnimeCompleted(string clip)
	{
		if (base.Lifecycle != ComponentRuntimeLifecycle.Active)
		{
			return false;
		}
		if (TryGetAnimation(out var clip2) && clip == clip2)
		{
			if (fallbackToIdle)
			{
				parent.Idle();
			}
			return true;
		}
		return false;
	}

	private bool TryGetAnimation(out string clip)
	{
		clip = (GodotObject.IsInstanceValid(parent) ? parent.plantAnimeClip : "");
		if (!string.IsNullOrEmpty(clip) && GodotObject.IsInstanceValid(parent?.sprite) && GodotObject.IsInstanceValid(parent.sprite.flashAnimeData))
		{
			return parent.sprite.flashAnimeData.HasClip(clip);
		}
		return false;
	}
}
