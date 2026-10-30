using Godot;
using Godot.Collections;

public sealed class ArmorVisualComponent : CharacterComponentRuntime
{
	public void ClearArmor(string armor)
	{
		if (TryGetVisual(out var armorData, out var sprite) && armorData.GetSlotConfig(armor) != null)
		{
			armorData.ClearArmorFliters(sprite, armor);
		}
	}

	public void ClearArmorAll()
	{
		if (TryGetVisual(out var armorData, out var sprite))
		{
			armorData.ClearArmorFlitersAll(sprite);
		}
	}

	public void SetArmor(string armor, int stage)
	{
		if (TryGetVisual(out var armorData, out var sprite))
		{
			SetArmor(armorData, sprite, armor, stage, clearFirst: true);
		}
	}

	public void SetArmors(Array<string> armorList)
	{
		if (!TryGetVisual(out var armorData, out var sprite))
		{
			return;
		}
		armorData.ClearArmorFlitersAll(sprite);
		if (armorList == null)
		{
			return;
		}
		foreach (string armor in armorList)
		{
			SetArmor(armorData, sprite, armor, 0, clearFirst: false);
		}
	}

	private static void SetArmor(CharacterArmorData armorData, AdobeAnimateSprite sprite, string armor, int stage, bool clearFirst)
	{
		if (string.IsNullOrEmpty(armor))
		{
			return;
		}
		ArmorSlotConfig slotConfig = armorData.GetSlotConfig(armor);
		if (slotConfig != null)
		{
			if (clearFirst)
			{
				armorData.ClearArmorFliters(sprite, armor);
			}
			if (!(slotConfig.replaceMethod == "Sprite"))
			{
				armorData.OpenArmorFliters(sprite, armor);
				armorData.SetArmorReplace(sprite, armor, stage);
			}
		}
	}

	private bool TryGetVisual(out CharacterArmorData armorData, out AdobeAnimateSprite sprite)
	{
		armorData = null;
		sprite = null;
		TowerDefenseCharacter owner = base.Owner;
		if (!base.Alive || !base.IsAttached || !GodotObject.IsInstanceValid(owner) || !GodotObject.IsInstanceValid(owner.config?.armorData) || !GodotObject.IsInstanceValid(owner.sprite))
		{
			return false;
		}
		armorData = owner.config.armorData;
		sprite = owner.sprite;
		return true;
	}
}
