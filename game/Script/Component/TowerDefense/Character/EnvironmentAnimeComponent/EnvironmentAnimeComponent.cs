using Godot;
using Godot.Collections;

public sealed class EnvironmentAnimeComponent : CharacterComponentRuntime
{
	public StringName dayAnimation = "Day";

	public StringName nightAnimation = "Night";

	public StringName waterDayAnimation = "Water";

	public StringName waterNightAnimation = "WaterNight";

	public float bobFrequency = 2f;

	public float bobAmplitude = 2f;

	public float bobBaseY;

	public TowerDefenseCharacter parent;

	public float timer;

	private bool _configured;

	private EnvironmentAnimeComponentDefinition Definition => base.ComponentDefinition as EnvironmentAnimeComponentDefinition;

	protected override void OnBound()
	{
		parent = base.Owner;
		if (!_configured)
		{
			EnvironmentAnimeComponentDefinition definition = Definition;
			dayAnimation = definition?.dayAnimation ?? new StringName("Day");
			nightAnimation = definition?.nightAnimation ?? new StringName("Night");
			waterDayAnimation = definition?.waterDayAnimation ?? new StringName("Water");
			waterNightAnimation = definition?.waterNightAnimation ?? new StringName("WaterNight");
			bobFrequency = definition?.bobFrequency ?? 2f;
			bobAmplitude = definition?.bobAmplitude ?? 2f;
			bobBaseY = definition?.bobBaseY ?? 0f;
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

	public void SetFrame(bool isNight, bool isWater)
	{
		if (base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && GodotObject.IsInstanceValid(parent?.sprite))
		{
			StringName stringName = ((!isNight) ? (isWater ? waterDayAnimation : dayAnimation) : (isWater ? waterNightAnimation : nightAnimation));
			if (!stringName.IsEmpty)
			{
				parent.sprite.SetAnimation(stringName);
			}
		}
	}

	public float WaterBob(float delta, float timeScale)
	{
		if (!base.Alive || base.Lifecycle != ComponentRuntimeLifecycle.Active || !GodotObject.IsInstanceValid(parent?.sprite))
		{
			return timer;
		}
		timer += Mathf.Max(0f, delta) * timeScale;
		parent.sprite.Position = new Vector2(parent.sprite.Position.X, bobBaseY + Mathf.Sin(timer * bobFrequency) * bobAmplitude);
		return timer;
	}

	public override Dictionary ExportComponentSave()
	{
		return new Dictionary { ["timer"] = timer };
	}

	public override void ImportComponentSave(Dictionary data, TowerDefenseLevelSaveConfigCSharp owner)
	{
		timer = data.GetValueOrDefault("timer", 0.0).AsSingle();
	}
}
