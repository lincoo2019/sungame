using System.Collections.Generic;
using Godot;

public sealed partial class TowerDefenseHealthDisplayBatch : Node2D
{
	private const string DefaultFontPath = "uid://coqskwlqtnypf";

	private static Font _defaultFont;

	private readonly Dictionary<int, TowerDefenseHealthDisplayBatchLayer> _layers = new Dictionary<int, TowerDefenseHealthDisplayBatchLayer>();

	private readonly Dictionary<ShowHealthComponent, TowerDefenseHealthDisplayBatchLayer> _displayLayers = new Dictionary<ShowHealthComponent, TowerDefenseHealthDisplayBatchLayer>(ReferenceEqualityComparer.Instance);

	private HealthTextLayoutCache _textLayoutCache;

	private long _drawRebuildCount;

	private Font _font;

	public static TowerDefenseHealthDisplayBatch Instance { get; private set; }

	public static int ActiveDisplayCount
	{
		get
		{
			if (!GodotObject.IsInstanceValid(Instance))
			{
				return 0;
			}
			return Instance._displayLayers.Count;
		}
	}

	public static int ActiveLayerCount
	{
		get
		{
			if (!GodotObject.IsInstanceValid(Instance))
			{
				return 0;
			}
			return Instance._layers.Count;
		}
	}

	public static int CachedTextLineCount
	{
		get
		{
			if (!GodotObject.IsInstanceValid(Instance))
			{
				return 0;
			}
			return Instance._textLayoutCache?.Count ?? 0;
		}
	}

	public static long TextLayoutBuildCount
	{
		get
		{
			if (!GodotObject.IsInstanceValid(Instance))
			{
				return 0L;
			}
			return Instance._textLayoutCache?.BuildCount ?? 0;
		}
	}

	public static long DrawRebuildCount
	{
		get
		{
			if (!GodotObject.IsInstanceValid(Instance))
			{
				return 0L;
			}
			return Instance._drawRebuildCount;
		}
	}

	public static bool Attach(ShowHealthComponent display)
	{
		if (display == null || !display.CanUseSharedDrawBatch())
		{
			return false;
		}
		if (!EnsureInstance(display.BatchOwner))
		{
			return false;
		}
		return Instance.AttachCore(display);
	}

	public static void Detach(ShowHealthComponent display)
	{
		if (display != null && GodotObject.IsInstanceValid(Instance))
		{
			Instance.DetachCore(display);
		}
	}

	public static void Update(ShowHealthComponent display)
	{
		if (display != null && GodotObject.IsInstanceValid(Instance) && Instance._displayLayers.TryGetValue(display, out var value))
		{
			value.RequestRedraw();
		}
	}

	public static void NotifyZIndexChanged(ShowHealthComponent display)
	{
		if (display != null && GodotObject.IsInstanceValid(Instance))
		{
			Instance.MoveToCurrentLayer(display);
		}
	}

	public static void NotifyPositionChanged(ShowHealthComponent display)
	{
		if (display != null && GodotObject.IsInstanceValid(Instance) && Instance._displayLayers.TryGetValue(display, out var value))
		{
			value.RequestRedraw();
		}
	}

	internal static bool TryGetDisplayZIndex(ShowHealthComponent display, out int zIndex)
	{
		zIndex = 0;
		if (display == null || !GodotObject.IsInstanceValid(Instance))
		{
			return false;
		}
		if (!Instance._displayLayers.TryGetValue(display, out var value))
		{
			return false;
		}
		zIndex = value.ZIndex;
		return true;
	}

	private static bool EnsureInstance(TowerDefenseCharacter owner)
	{
		if (GodotObject.IsInstanceValid(Instance))
		{
			return GodotObject.IsInstanceValid(Instance._font);
		}
		if (!GodotObject.IsInstanceValid(owner) || !owner.IsInsideTree())
		{
			return false;
		}
		if (_defaultFont == null)
		{
			_defaultFont = GD.Load<Font>("uid://coqskwlqtnypf");
		}
		if (!GodotObject.IsInstanceValid(_defaultFont))
		{
			return false;
		}
		Node node = TowerDefenseProcessModeDispatch.ResolveBatchParent(owner);
		if (!GodotObject.IsInstanceValid(node))
		{
			return false;
		}
		TowerDefenseHealthDisplayBatch towerDefenseHealthDisplayBatch = (Instance = new TowerDefenseHealthDisplayBatch
		{
			Name = "TowerDefenseHealthDisplayBatch",
			_font = _defaultFont,
			_textLayoutCache = new HealthTextLayoutCache(_defaultFont),
			ProcessMode = ProcessModeEnum.Inherit,
			LightMask = 0,
			TopLevel = true,
			ZAsRelative = false
		});
		node.CallDeferred(Node.MethodName.AddChild, towerDefenseHealthDisplayBatch);
		return true;
	}

	public override void _Ready()
	{
		Instance = this;
		base.TopLevel = true;
		base.GlobalTransform = Transform2D.Identity;
		SetProcess(enable: false);
	}

	public override void _ExitTree()
	{
		_displayLayers.Clear();
		_layers.Clear();
		_textLayoutCache?.Dispose();
		_textLayoutCache = null;
		if (Instance == this)
		{
			Instance = null;
		}
	}

	private bool AttachCore(ShowHealthComponent display)
	{
		if (_displayLayers.ContainsKey(display))
		{
			MoveToCurrentLayer(display);
			return true;
		}
		int zIndex = display.ResolveSharedDrawZIndex();
		TowerDefenseHealthDisplayBatchLayer orCreateLayer = GetOrCreateLayer(zIndex);
		orCreateLayer.Add(display);
		_displayLayers.Add(display, orCreateLayer);
		orCreateLayer.RequestRedraw();
		return true;
	}

	private void DetachCore(ShowHealthComponent display)
	{
		if (_displayLayers.Remove(display, out var value))
		{
			value.Remove(display);
			RemoveLayerIfEmpty(value);
		}
	}

	private void MoveToCurrentLayer(ShowHealthComponent display)
	{
		if (_displayLayers.TryGetValue(display, out var value))
		{
			int num = display.ResolveSharedDrawZIndex();
			if (value.ZIndex == num)
			{
				value.RequestRedraw();
				return;
			}
			value.Remove(display);
			RemoveLayerIfEmpty(value);
			TowerDefenseHealthDisplayBatchLayer orCreateLayer = GetOrCreateLayer(num);
			orCreateLayer.Add(display);
			_displayLayers[display] = orCreateLayer;
			orCreateLayer.RequestRedraw();
		}
	}

	private TowerDefenseHealthDisplayBatchLayer GetOrCreateLayer(int zIndex)
	{
		if (_layers.TryGetValue(zIndex, out var value))
		{
			return value;
		}
		value = new TowerDefenseHealthDisplayBatchLayer
		{
			Name = $"HealthZ{zIndex}",
			ZIndex = zIndex,
			ZAsRelative = false,
			TopLevel = true,
			LightMask = 0,
			Font = _font,
			TextLayoutCache = _textLayoutCache,
			OwnerBatch = this
		};
		_layers.Add(zIndex, value);
		AddChild(value, forceReadableName: false, InternalMode.Disabled);
		return value;
	}

	private void RemoveLayerIfEmpty(TowerDefenseHealthDisplayBatchLayer layer)
	{
		if (layer != null && layer.DisplayCount == 0)
		{
			_layers.Remove(layer.ZIndex);
			if (layer.IsInsideTree())
			{
				layer.QueueFree();
			}
			else
			{
				layer.Free();
			}
		}
	}

	internal void ReportDrawRebuild()
	{
		_drawRebuildCount++;
	}
}
