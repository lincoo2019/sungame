using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

public sealed class ShaderEffectComponent : CharacterComponentRuntime
{
	private static readonly Color HologramRenderModulate = new Color(0.72f, 1f, 1.1f, 0.75f);

	private const string BrightStrengthShaderParameter = "brightStrength";

	private const string WhiteStrengthShaderParameter = "whiteStrength";

	private const string ToolHighlightShaderParameter = "toolHighlightStrength";

	private static readonly StringName SyncAliveKey = new StringName("_alive");

	private static readonly StringName SyncEffectFlagsKey = new StringName("effectFlags");

	public static readonly System.Collections.Generic.Dictionary<string, int> ShaderEffectFlags = new System.Collections.Generic.Dictionary<string, int>(StringComparer.Ordinal)
	{
		{ "ash", 1 },
		{ "iceSpeedDown", 2 },
		{ "cover", 4 },
		{ "hypnoses", 8 },
		{ "imitater", 16 },
		{ "redHeat", 32 },
		{ "puzzle", 64 },
		{ "poisoning", 128 },
		{ "blink", 256 },
		{ "hologram", 512 }
	};

	public TowerDefenseCharacter parent;

	private List<AdobeAnimateSpriteBase> _shaderNodes;

	private HashSet<AdobeAnimateSpriteBase> _shaderNodeSet;

	private Stack<Node2D> _nodeTraversalStack;

	private System.Collections.Generic.Dictionary<string, Variant> _visualParameters;

	private bool _shaderNodesInitialized;

	private int _effectFlags;

	private int _syncPayloadEffectFlags;

	private float _brightStrength;

	private float _whiteStrength;

	private float _toolHighlightStrength;

	private bool _waitingForParentReady;

	private bool _syncPayloadInitialized;

	private Color _baseSpriteGroupModulate = Colors.White;

	private bool _hasBaseSpriteGroupModulate;

	private Dictionary _syncPayload => GetReusableSyncPayload();

	protected override void OnBound()
	{
		parent = base.Owner;
	}

	protected override void OnActivated()
	{
		CaptureBaseSpriteGroupModulate();
	}

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		ClearSyncPayload();
		DisconnectParentReady();
		RestoreBaseSpriteGroupModulate();
		ResetRenderColorMultipliers();
		ClearShaderNodeCache();
		parent = null;
	}

	protected override void OnReleased()
	{
		ClearSyncPayload();
		_visualParameters?.Clear();
		_effectFlags = 0;
		_brightStrength = 0f;
		_whiteStrength = 0f;
		_toolHighlightStrength = 0f;
		_baseSpriteGroupModulate = Colors.White;
		_hasBaseSpriteGroupModulate = false;
	}

	private void CaptureBaseSpriteGroupModulate()
	{
		if (!_hasBaseSpriteGroupModulate && GodotObject.IsInstanceValid(parent?.spriteGroup))
		{
			_baseSpriteGroupModulate = parent.spriteGroup.Modulate;
			_hasBaseSpriteGroupModulate = true;
		}
	}

	private void RestoreBaseSpriteGroupModulate()
	{
		if (_hasBaseSpriteGroupModulate && GodotObject.IsInstanceValid(parent?.spriteGroup))
		{
			parent.spriteGroup.Modulate = _baseSpriteGroupModulate;
		}
	}

	private void InitializeShaderNodes()
	{
		if (GodotObject.IsInstanceValid(parent))
		{
			if (!parent.IsNodeReady())
			{
				ConnectParentReady();
			}
			else
			{
				RefreshShaderNodes();
			}
		}
	}

	private void ClearShaderNodeCache()
	{
		_shaderNodes?.Clear();
		_shaderNodeSet?.Clear();
		_nodeTraversalStack?.Clear();
		_shaderNodesInitialized = false;
	}

	private void ResetRenderColorMultipliers()
	{
		if (_shaderNodes == null)
		{
			return;
		}
		for (int i = 0; i < _shaderNodes.Count; i++)
		{
			AdobeAnimateSpriteBase adobeAnimateSpriteBase = _shaderNodes[i];
			if (GodotObject.IsInstanceValid(adobeAnimateSpriteBase))
			{
				adobeAnimateSpriteBase.SetRenderColorMultiplier(Colors.White);
				adobeAnimateSpriteBase.SetRenderGrayscale(enabled: false);
			}
		}
	}

	private void ConnectParentReady()
	{
		if (!_waitingForParentReady && GodotObject.IsInstanceValid(parent))
		{
			parent.Ready += OnParentReady;
			_waitingForParentReady = true;
		}
	}

	private void DisconnectParentReady()
	{
		if (_waitingForParentReady && GodotObject.IsInstanceValid(parent))
		{
			parent.Ready -= OnParentReady;
		}
		_waitingForParentReady = false;
	}

	private void OnParentReady()
	{
		DisconnectParentReady();
		RefreshShaderNodes();
	}

	public void Init()
	{
		EnsureShaderNodesInitialized();
	}

	private void EnsureShaderNodesInitialized()
	{
		if (!_shaderNodesInitialized)
		{
			InitializeShaderNodes();
		}
	}

	public void RefreshShaderNodes()
	{
		ClearShaderNodeCache();
		if (!GodotObject.IsInstanceValid(parent) || !GodotObject.IsInstanceValid(parent.sprite))
		{
			return;
		}
		if (_shaderNodes == null)
		{
			_shaderNodes = new List<AdobeAnimateSpriteBase>();
		}
		if (_shaderNodeSet == null)
		{
			_shaderNodeSet = new HashSet<AdobeAnimateSpriteBase>();
		}
		if (_nodeTraversalStack == null)
		{
			_nodeTraversalStack = new Stack<Node2D>();
		}
		_nodeTraversalStack.Push(parent.sprite);
		while (_nodeTraversalStack.Count > 0)
		{
			Node2D node2D = _nodeTraversalStack.Pop();
			if (!GodotObject.IsInstanceValid(node2D))
			{
				continue;
			}
			if (node2D is AdobeAnimateSpriteBase item && _shaderNodeSet.Add(item))
			{
				_shaderNodes.Add(item);
			}
			for (int num = node2D.GetChildCount() - 1; num >= 0; num--)
			{
				if (node2D.GetChild(num) is Node2D item2)
				{
					_nodeTraversalStack.Push(item2);
				}
			}
		}
		for (int i = 0; i < _shaderNodes.Count; i++)
		{
			AdobeAnimateSpriteBase adobeAnimateSpriteBase = _shaderNodes[i];
			if (_visualParameters != null)
			{
				foreach (KeyValuePair<string, Variant> visualParameter in _visualParameters)
				{
					if (!IsBrightnessProperty(visualParameter.Key))
					{
						ApplyAdobeAnimateVisualParameter(adobeAnimateSpriteBase, visualParameter.Key, visualParameter.Value);
					}
				}
			}
			ApplyBrightness(adobeAnimateSpriteBase, GetCompositeBrightness());
			adobeAnimateSpriteBase.SetRenderGrayscale(IsImitaterEnabled());
			adobeAnimateSpriteBase.QueueRedraw();
		}
		_shaderNodesInitialized = true;
		ApplyHologramPresentation();
	}

	public void SetSpriteGroupShaderParameter(string property, Variant value)
	{
		if (string.IsNullOrEmpty(property))
		{
			return;
		}
		EnsureShaderNodesInitialized();
		if (ShaderEffectFlags.TryGetValue(property, out var value2))
		{
			SetEffectFlag(value2, value.AsBool(), redrawAll: true);
			return;
		}
		bool flag = IsBrightnessProperty(property);
		if (flag)
		{
			parent?.hitFlashComponent?.EnsureCpuHitFlashCompatibility();
			SetBrightnessProperty(property, (float)value.AsDouble());
		}
		else
		{
			(_visualParameters ?? (_visualParameters = new System.Collections.Generic.Dictionary<string, Variant>(StringComparer.Ordinal)))[property] = value;
		}
		float strength = (flag ? GetCompositeBrightness() : 0f);
		for (int i = 0; i < (_shaderNodes?.Count ?? 0); i++)
		{
			AdobeAnimateSpriteBase adobeAnimateSpriteBase = _shaderNodes[i];
			if (GodotObject.IsInstanceValid(adobeAnimateSpriteBase))
			{
				if (flag)
				{
					ApplyBrightness(adobeAnimateSpriteBase, strength);
				}
				else
				{
					ApplyAdobeAnimateVisualParameter(adobeAnimateSpriteBase, property, value);
				}
			}
		}
	}

	public void SetHitFlashStrengths(float brightStrength, float whiteStrength)
	{
		EnsureShaderNodesInitialized();
		_brightStrength = Mathf.Max(0f, brightStrength);
		_whiteStrength = Mathf.Max(0f, whiteStrength);
		float compositeBrightness = GetCompositeBrightness();
		for (int i = 0; i < (_shaderNodes?.Count ?? 0); i++)
		{
			AdobeAnimateSpriteBase adobeAnimateSpriteBase = _shaderNodes[i];
			if (GodotObject.IsInstanceValid(adobeAnimateSpriteBase))
			{
				ApplyBrightness(adobeAnimateSpriteBase, compositeBrightness);
			}
		}
	}

	public bool TryStartGpuHitFlashEnvelope(bool white, float strength, float duration, out float startTime)
	{
		startTime = 0f;
		EnsureShaderNodesInitialized();
		if (_shaderNodes == null || _shaderNodes.Count == 0 || strength <= 0f || duration <= 0f || !Mathf.IsZeroApprox(_brightStrength) || !Mathf.IsZeroApprox(_whiteStrength) || !Mathf.IsZeroApprox(_toolHighlightStrength))
		{
			return false;
		}
		for (int i = 0; i < _shaderNodes.Count; i++)
		{
			AdobeAnimateSprite adobeAnimateSprite = _shaderNodes[i];
			if (adobeAnimateSprite == null || !GodotObject.IsInstanceValid(adobeAnimateSprite) || !adobeAnimateSprite.CanUseGpuHitFlashEnvelope())
			{
				return false;
			}
		}
		startTime = (float)Math.Max(0.0, AdobeAnimateRuntimeManager.AnimationClockSeconds);
		for (int j = 0; j < _shaderNodes.Count; j++)
		{
			_shaderNodes[j].StartGpuHitFlashEnvelope(white, startTime, strength, duration);
		}
		return true;
	}

	public void ClearGpuHitFlashEnvelopes(bool clearBright = true, bool clearWhite = true)
	{
		if (_shaderNodes == null)
		{
			return;
		}
		for (int i = 0; i < _shaderNodes.Count; i++)
		{
			AdobeAnimateSprite adobeAnimateSprite = _shaderNodes[i];
			if (adobeAnimateSprite != null && GodotObject.IsInstanceValid(adobeAnimateSprite))
			{
				adobeAnimateSprite.ClearGpuHitFlashEnvelopes(clearBright, clearWhite);
			}
		}
	}

	private static bool IsBrightnessProperty(string property)
	{
		if (!(property == "brightStrength") && !(property == "whiteStrength"))
		{
			return property == "toolHighlightStrength";
		}
		return true;
	}

	private void SetBrightnessProperty(string property, float value)
	{
		float num = Mathf.Max(0f, value);
		switch (property)
		{
		case "brightStrength":
			_brightStrength = num;
			break;
		case "whiteStrength":
			_whiteStrength = num;
			break;
		case "toolHighlightStrength":
			_toolHighlightStrength = num;
			break;
		}
	}

	private float GetCompositeBrightness()
	{
		return Mathf.Max(_toolHighlightStrength, Mathf.Max(_brightStrength, _whiteStrength));
	}

	public int GetEffectFlags()
	{
		return _effectFlags;
	}

	public void SetEffectFlags(int flags)
	{
		if (_effectFlags != flags)
		{
			_effectFlags = flags;
			EnsureShaderNodesInitialized();
			ApplyEffectFlags();
		}
	}

	private bool SetEffectFlag(int flag, bool enabled, bool redrawAll)
	{
		int num = (enabled ? (_effectFlags | flag) : (_effectFlags & ~flag));
		if (num == _effectFlags)
		{
			return false;
		}
		_effectFlags = num;
		if (redrawAll)
		{
			ApplyEffectFlags();
		}
		return true;
	}

	public void SetChildShaderParameter(Node2D parentNode, string property, Variant value)
	{
		if (!GodotObject.IsInstanceValid(parentNode) || string.IsNullOrEmpty(property))
		{
			return;
		}
		if (ShaderEffectFlags.TryGetValue(property, out var value2))
		{
			bool enabled = value.AsBool();
			if (SetEffectFlag(value2, enabled, redrawAll: false))
			{
				if (value2 == ShaderEffectFlags["imitater"])
				{
					ApplyImitaterPresentationRecursive(parentNode, enabled);
				}
				QueueRedrawRecursive(parentNode);
			}
		}
		else
		{
			ApplyVisualParameterRecursive(parentNode, property, value);
		}
	}

	private void ApplyVisualParameterRecursive(Node2D root, string property, Variant value)
	{
		(_nodeTraversalStack ?? (_nodeTraversalStack = new Stack<Node2D>())).Clear();
		_nodeTraversalStack.Push(root);
		while (_nodeTraversalStack.Count > 0)
		{
			Node2D node2D = _nodeTraversalStack.Pop();
			if (!GodotObject.IsInstanceValid(node2D))
			{
				continue;
			}
			if (node2D is AdobeAnimateSpriteBase node)
			{
				ApplyAdobeAnimateVisualParameter(node, property, value);
			}
			for (int num = node2D.GetChildCount() - 1; num >= 0; num--)
			{
				if (node2D.GetChild(num) is Node2D item)
				{
					_nodeTraversalStack.Push(item);
				}
			}
		}
	}

	private void QueueRedrawRecursive(Node2D root)
	{
		(_nodeTraversalStack ?? (_nodeTraversalStack = new Stack<Node2D>())).Clear();
		_nodeTraversalStack.Push(root);
		while (_nodeTraversalStack.Count > 0)
		{
			Node2D node2D = _nodeTraversalStack.Pop();
			if (!GodotObject.IsInstanceValid(node2D))
			{
				continue;
			}
			if (node2D is AdobeAnimateSpriteBase adobeAnimateSpriteBase)
			{
				adobeAnimateSpriteBase.QueueRedraw();
			}
			for (int num = node2D.GetChildCount() - 1; num >= 0; num--)
			{
				if (node2D.GetChild(num) is Node2D item)
				{
					_nodeTraversalStack.Push(item);
				}
			}
		}
	}

	private static void ApplyAdobeAnimateVisualParameter(AdobeAnimateSpriteBase node, string property, Variant value)
	{
		switch (property)
		{
		case "discardUpPos":
			node.SetDiscardUpPos((float)value.AsDouble());
			break;
		case "discardDownPos":
			node.SetDiscardDownPos((float)value.AsDouble());
			break;
		case "brightStrength":
		case "whiteStrength":
			ApplyBrightness(node, (float)value.AsDouble());
			break;
		}
	}

	private static void ApplyBrightness(AdobeAnimateSpriteBase node, float strength)
	{
		float num = Mathf.Max(0f, strength);
		node.SetRenderColorMultiplier(new Color(1f + num, 1f + num, 1f + num));
	}

	private void ApplyEffectFlags()
	{
		EnsureShaderNodesInitialized();
		ApplyHologramPresentation();
		bool renderGrayscale = IsImitaterEnabled();
		if (_shaderNodes == null)
		{
			return;
		}
		for (int i = 0; i < _shaderNodes.Count; i++)
		{
			AdobeAnimateSpriteBase adobeAnimateSpriteBase = _shaderNodes[i];
			if (GodotObject.IsInstanceValid(adobeAnimateSpriteBase))
			{
				adobeAnimateSpriteBase.SetRenderGrayscale(renderGrayscale);
				adobeAnimateSpriteBase.QueueRedraw();
			}
		}
	}

	private bool IsImitaterEnabled()
	{
		return (_effectFlags & ShaderEffectFlags["imitater"]) != 0;
	}

	private void ApplyImitaterPresentationRecursive(Node2D root, bool enabled)
	{
		(_nodeTraversalStack ?? (_nodeTraversalStack = new Stack<Node2D>())).Clear();
		_nodeTraversalStack.Push(root);
		while (_nodeTraversalStack.Count > 0)
		{
			Node2D node2D = _nodeTraversalStack.Pop();
			if (!GodotObject.IsInstanceValid(node2D))
			{
				continue;
			}
			if (node2D is AdobeAnimateSpriteBase adobeAnimateSpriteBase)
			{
				adobeAnimateSpriteBase.SetRenderGrayscale(enabled);
			}
			for (int num = node2D.GetChildCount() - 1; num >= 0; num--)
			{
				if (node2D.GetChild(num) is Node2D item)
				{
					_nodeTraversalStack.Push(item);
				}
			}
		}
	}

	private void ApplyHologramPresentation()
	{
		if (GodotObject.IsInstanceValid(parent?.spriteGroup))
		{
			CaptureBaseSpriteGroupModulate();
			if (_hasBaseSpriteGroupModulate)
			{
				int num = ShaderEffectFlags["hologram"];
				parent.spriteGroup.Modulate = (((_effectFlags & num) != 0) ? (_baseSpriteGroupModulate * HologramRenderModulate) : _baseSpriteGroupModulate);
			}
		}
	}

	public override Dictionary ExportComponentSave()
	{
		return new Dictionary { { "effectFlags", _effectFlags } };
	}

	public override void ImportComponentSave(Dictionary data, TowerDefenseLevelSaveConfigCSharp owner)
	{
		SetEffectFlags(data.GetValueOrDefault("effectFlags", _effectFlags).AsInt32());
	}

	public override Dictionary SyncSerialize()
	{
		int effectFlags = _effectFlags;
		if (_syncPayloadInitialized)
		{
			if (_syncPayload.Count == 2)
			{
				_syncPayload.Remove(SyncAliveKey);
			}
			if (_syncPayload.Count == 1 && _syncPayloadEffectFlags == effectFlags)
			{
				return _syncPayload;
			}
		}
		_syncPayload.Clear();
		_syncPayload[SyncEffectFlagsKey] = effectFlags;
		_syncPayloadEffectFlags = effectFlags;
		_syncPayloadInitialized = true;
		return _syncPayload;
	}

	public override void SyncDeserialize(Dictionary data)
	{
		SetEffectFlags(data.GetValueOrDefault("effectFlags", _effectFlags).AsInt32());
	}

	private void ClearSyncPayload()
	{
		ClearReusableSyncPayload();
		_syncPayloadEffectFlags = 0;
		_syncPayloadInitialized = false;
	}
}
