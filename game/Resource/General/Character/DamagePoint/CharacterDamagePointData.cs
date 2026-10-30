using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class CharacterDamagePointData : Resource
{
	private Array<CharacterDamagePointConfig> _damagePointList = new Array<CharacterDamagePointConfig>();

	[Export(PropertyHint.None, "")]
	public Dictionary damagePointDictionary = new Dictionary();

	[Export(PropertyHint.None, "")]
	public Array<string> fliterOpenAll = new Array<string>();

	[Export(PropertyHint.None, "")]
	public Array<string> fliterCloseAll = new Array<string>();

	[Export(PropertyHint.None, "")]
	public Array<CharacterDamagePointConfig> damagePointList
	{
		get
		{
			return _damagePointList;
		}
		set
		{
			_damagePointList = value ?? new Array<CharacterDamagePointConfig>();
			EmitChanged();
			Refresh();
		}
	}

	public void Refresh()
	{
		fliterOpenAll.Clear();
		fliterCloseAll.Clear();
		damagePointDictionary.Clear();
		List<CharacterDamagePointConfig> list = _damagePointList.Where((CharacterDamagePointConfig config) => config != null).ToList();
		list.Sort((CharacterDamagePointConfig a, CharacterDamagePointConfig b) => b.damagePersontage.CompareTo(a.damagePersontage));
		foreach (CharacterDamagePointConfig item in list)
		{
			Array<string> array = SplitFliter(item.animeFliterOpen);
			Array<string> array2 = SplitFliter(item.animeFliterClose);
			damagePointDictionary[item.damagePointName] = new Dictionary
			{
				["Config"] = item,
				["Open"] = array,
				["Close"] = array2
			};
			if (array.Count > 0)
			{
				foreach (string item2 in array)
				{
					fliterOpenAll.Add(item2);
				}
			}
			if (array2.Count <= 0)
			{
				continue;
			}
			foreach (string item3 in array2)
			{
				fliterCloseAll.Add(item3);
			}
		}
	}

	private Array<string> SplitFliter(string fliter)
	{
		Array<string> array = new Array<string>();
		if (string.IsNullOrEmpty(fliter))
		{
			return array;
		}
		string[] array2 = fliter.Split("&", StringSplitOptions.RemoveEmptyEntries);
		foreach (string item in array2)
		{
			array.Add(item);
		}
		return array;
	}

	public void ClearDamagePointFliters(AdobeAnimateSprite sprite)
	{
		sprite.SetFlitersRecursive((Godot.Collections.Array?)fliterCloseAll, open: true);
		sprite.SetFlitersRecursive((Godot.Collections.Array?)fliterOpenAll, open: false);
	}

	public void ClearDamagePointAll(AdobeAnimateSprite sprite)
	{
		ClearDamagePointFliters(sprite);
		foreach (CharacterDamagePointConfig damagePoint in damagePointList)
		{
			if (damagePoint != null && !damagePoint.replaceMediaName.IsEmpty)
			{
				sprite.SetAtlasReplace(damagePoint.replaceMediaName, string.Empty);
			}
		}
	}

	public void SetDamagePointFliters(AdobeAnimateSprite sprite, string damagePointName)
	{
		if (!damagePointDictionary.ContainsKey(damagePointName))
		{
			return;
		}
		Dictionary dictionary = (Dictionary)damagePointDictionary[damagePointName];
		if (dictionary.ContainsKey("Config"))
		{
			CharacterDamagePointConfig characterDamagePointConfig = (CharacterDamagePointConfig)(GodotObject)dictionary["Config"];
			sprite.SetFlitersRecursive((Godot.Collections.Array)dictionary["Close"], open: false);
			sprite.SetFlitersRecursive((Godot.Collections.Array)dictionary["Open"], open: true);
			if (!characterDamagePointConfig.replaceMediaName.IsEmpty)
			{
				sprite.SetAtlasReplace(characterDamagePointConfig.replaceMediaName, characterDamagePointConfig.replaceMediaTexturePath);
			}
		}
	}

	public void CreateEffect(AdobeAnimateSprite sprite, string damagePointName, Vector2I gridPos = default(Vector2I))
	{
		if (!damagePointDictionary.ContainsKey(damagePointName))
		{
			return;
		}
		Dictionary dictionary = (Dictionary)damagePointDictionary[damagePointName];
		if (dictionary.ContainsKey("Config"))
		{
			CharacterDamagePointConfig characterDamagePointConfig = (CharacterDamagePointConfig)(GodotObject)dictionary["Config"];
			if (characterDamagePointConfig.animeEffect != null)
			{
				TowerDefenseEffectParticlesOnce towerDefenseEffectParticlesOnce = TowerDefenseManager.CreateEffectParticlesOnce(characterDamagePointConfig.animeEffect, gridPos);
				TowerDefenseManager.GetCharacterNode().AddChild(towerDefenseEffectParticlesOnce, forceReadableName: false, Node.InternalMode.Disabled);
				towerDefenseEffectParticlesOnce.GlobalPosition = ResolveLogicalSpritePosition(sprite) + characterDamagePointConfig.animeEffectOffset;
			}
		}
	}

	private static Vector2 ResolveLogicalSpritePosition(AdobeAnimateSprite sprite)
	{
		Node node = sprite;
		while (GodotObject.IsInstanceValid(node))
		{
			if (node is TowerDefenseCharacter towerDefenseCharacter)
			{
				return towerDefenseCharacter.GetLogicalGlobalPosition(sprite);
			}
			node = node.GetParent();
		}
		if (!GodotObject.IsInstanceValid(sprite))
		{
			return Vector2.Zero;
		}
		return sprite.GlobalPosition;
	}
}
