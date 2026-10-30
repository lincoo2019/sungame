using Godot;
using Godot.Collections;

public sealed class CustomVisualComponent : CharacterComponentRuntime
{
	public void ClearCustom()
	{
		if (TryGetVisual(out var customData, out var sprite))
		{
			customData.ClearCustomFliters(sprite);
		}
	}

	public void SetCustom(string custom)
	{
		if (!string.IsNullOrEmpty(custom) && TryGetVisual(out var customData, out var sprite))
		{
			customData.SetCustomFliters(sprite, custom);
		}
	}

	public void SetCustoms(Array<string> customList)
	{
		if (!TryGetVisual(out var customData, out var sprite))
		{
			return;
		}
		customData.ClearCustomFliters(sprite);
		if (customList == null)
		{
			return;
		}
		foreach (string custom in customList)
		{
			if (!string.IsNullOrEmpty(custom))
			{
				customData.SetCustomFliters(sprite, custom);
			}
		}
	}

	private bool TryGetVisual(out CharacterCustomData customData, out AdobeAnimateSprite sprite)
	{
		customData = null;
		sprite = null;
		TowerDefenseCharacter owner = base.Owner;
		if (!base.Alive || base.Lifecycle != ComponentRuntimeLifecycle.Active || !GodotObject.IsInstanceValid(owner) || !GodotObject.IsInstanceValid(owner.config?.customData) || !GodotObject.IsInstanceValid(owner.sprite))
		{
			return false;
		}
		customData = owner.config.customData;
		sprite = owner.sprite;
		return true;
	}
}
