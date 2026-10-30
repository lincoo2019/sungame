using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseBattleFeatureScreenEffect : TowerDefenseBattleFeature
{
	private static PackedScene _screenEffectControlScene;

	private static PackedScene _rainEffectScene;

	private static PackedScene _stormEffectScene;

	public Control screenEffectControl;

	private readonly System.Collections.Generic.Dictionary<string, PackedScene> _effectScenes = new System.Collections.Generic.Dictionary<string, PackedScene>(StringComparer.OrdinalIgnoreCase);

	private readonly System.Collections.Generic.Dictionary<string, Node> _currentEffects = new System.Collections.Generic.Dictionary<string, Node>(StringComparer.OrdinalIgnoreCase);

	private static PackedScene ScreenEffectControlScene => _screenEffectControlScene ?? (_screenEffectControlScene = GD.Load<PackedScene>("uid://cn2idxrgpsjc5"));

	private static PackedScene RainEffectScene => _rainEffectScene ?? (_rainEffectScene = GD.Load<PackedScene>("uid://brduo5wtglu0"));

	private static PackedScene StormEffectScene => _stormEffectScene ?? (_stormEffectScene = GD.Load<PackedScene>("uid://qha0env3257g"));

	public override void Init(Dictionary _data)
	{
		base.Init(_data);
		RegisterScreenEffect("Rain", RainEffectScene);
		RegisterScreenEffect("Storm", StormEffectScene);
		if (!GodotObject.IsInstanceValid(ScreenEffectControlScene) || !GodotObject.IsInstanceValid(control))
		{
			GD.PushError("[ScreenEffect] Cannot create the screen-effect container.");
			return;
		}
		screenEffectControl = ScreenEffectControlScene.Instantiate<Control>(PackedScene.GenEditState.Disabled);
		control.AddUI(screenEffectControl, 10);
		bool num = data.GetValueOrDefault("StormOpen", false).AsBool();
		int num2 = data.GetValueOrDefault("PacketBankMethod", -1).AsInt32();
		if (num)
		{
			AddScreenEffect("Rain");
			AddScreenEffect("Storm");
		}
		if (num2 == 4)
		{
			AddScreenEffect("Rain");
		}
	}

	public bool RegisterScreenEffect(string effectName, PackedScene effectScene)
	{
		if (string.IsNullOrWhiteSpace(effectName) || !GodotObject.IsInstanceValid(effectScene))
		{
			return false;
		}
		_effectScenes[effectName.Trim()] = effectScene;
		return true;
	}

	public bool UnregisterScreenEffect(string effectName)
	{
		if (!string.IsNullOrWhiteSpace(effectName) && !HasScreenEffect(effectName))
		{
			return _effectScenes.Remove(effectName.Trim());
		}
		return false;
	}

	public bool AddScreenEffect(string effectName)
	{
		if (string.IsNullOrWhiteSpace(effectName) || !GodotObject.IsInstanceValid(screenEffectControl))
		{
			return false;
		}
		effectName = effectName.Trim();
		if (_currentEffects.TryGetValue(effectName, out var value))
		{
			if (GodotObject.IsInstanceValid(value))
			{
				return false;
			}
			_currentEffects.Remove(effectName);
		}
		if (!_effectScenes.TryGetValue(effectName, out var value2) || !GodotObject.IsInstanceValid(value2))
		{
			GD.PushWarning("[ScreenEffect] Unknown effect '" + effectName + "'.");
			return false;
		}
		Node node = value2.Instantiate(PackedScene.GenEditState.Disabled);
		if (!GodotObject.IsInstanceValid(node))
		{
			return false;
		}
		_currentEffects[effectName] = node;
		screenEffectControl.AddChild(node, forceReadableName: false, Node.InternalMode.Disabled);
		return true;
	}

	public void DeleteScreenEffect(string effectName)
	{
		if (!string.IsNullOrWhiteSpace(effectName) && _currentEffects.Remove(effectName.Trim(), out var value) && GodotObject.IsInstanceValid(value))
		{
			value.QueueFree();
		}
	}

	public Node GetScreenEffect(string effectName)
	{
		if (string.IsNullOrWhiteSpace(effectName) || !_currentEffects.TryGetValue(effectName.Trim(), out var value))
		{
			return null;
		}
		if (GodotObject.IsInstanceValid(value))
		{
			return value;
		}
		_currentEffects.Remove(effectName.Trim());
		return null;
	}

	public bool HasScreenEffect(string effectName)
	{
		return GetScreenEffect(effectName) != null;
	}

	public override void Destroy()
	{
		ClearScreenEffects();
		_effectScenes.Clear();
		if (GodotObject.IsInstanceValid(screenEffectControl))
		{
			screenEffectControl.QueueFree();
		}
		screenEffectControl = null;
		base.Destroy();
	}

	public override Dictionary SaveFeature()
	{
		return SerializeEffects();
	}

	public override Dictionary SyncSerialize()
	{
		return SerializeEffects();
	}

	public override void SyncDeserialize(Dictionary _data)
	{
		RestoreEffects(_data, force: false);
	}

	public override void LoadFeature(Dictionary _data, TowerDefenseLevelSaveConfigCSharp _owner)
	{
		RestoreEffects(_data, force: true);
	}

	private Dictionary SerializeEffects()
	{
		Array<string> array = new Array<string>();
		foreach (KeyValuePair<string, Node> currentEffect in _currentEffects)
		{
			if (GodotObject.IsInstanceValid(currentEffect.Value))
			{
				array.Add(currentEffect.Key);
			}
		}
		return new Dictionary { ["effects"] = array };
	}

	private void RestoreEffects(Dictionary state, bool force)
	{
		if (!state.TryGetValue("effects", out var value) || value.VariantType != Variant.Type.Array)
		{
			return;
		}
		Godot.Collections.Array array = value.AsGodotArray();
		if (!force && EffectsMatch(array))
		{
			return;
		}
		ClearScreenEffects();
		foreach (Variant item in array)
		{
			AddScreenEffect(item.AsString());
		}
	}

	private bool EffectsMatch(Godot.Collections.Array effects)
	{
		int num = 0;
		foreach (KeyValuePair<string, Node> currentEffect in _currentEffects)
		{
			if (!GodotObject.IsInstanceValid(currentEffect.Value))
			{
				continue;
			}
			num++;
			bool flag = false;
			foreach (Variant effect in effects)
			{
				if (string.Equals(currentEffect.Key, effect.AsString(), StringComparison.OrdinalIgnoreCase))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				return false;
			}
		}
		return num == effects.Count;
	}

	private void ClearScreenEffects()
	{
		foreach (Node value in _currentEffects.Values)
		{
			if (GodotObject.IsInstanceValid(value))
			{
				value.QueueFree();
			}
		}
		_currentEffects.Clear();
	}
}
