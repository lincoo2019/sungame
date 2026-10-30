using System;
using System.Collections.Generic;
using Godot;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public sealed class XWVisualSegmentedOption : IDisposable
{
	private readonly OptionButton _source;

	private readonly HFlowContainer _host;

	private readonly Func<int, Texture2D> _iconProvider;

	private readonly List<Button> _segments = new List<Button>();

	private bool _disposed;

	public int SegmentCount => _segments.Count;

	public bool IsVisualSurfaceReady
	{
		get
		{
			if (!_disposed && GodotObject.IsInstanceValid(_source))
			{
				return GodotObject.IsInstanceValid(_host);
			}
			return false;
		}
	}

	public XWVisualSegmentedOption(OptionButton source, HFlowContainer host, Func<int, Texture2D> iconProvider = null)
	{
		_source = source;
		_host = host;
		_iconProvider = iconProvider;
		if (GodotObject.IsInstanceValid(_source) && GodotObject.IsInstanceValid(_host))
		{
			_source.Visible = false;
			_source.ItemSelected += OnSourceSelected;
		}
	}

	public void Rebuild()
	{
		if (_disposed || !GodotObject.IsInstanceValid(_source) || !GodotObject.IsInstanceValid(_host))
		{
			return;
		}
		foreach (Button segment in _segments)
		{
			if (GodotObject.IsInstanceValid(segment))
			{
				_host.RemoveChild(segment);
				segment.QueueFree();
			}
		}
		_segments.Clear();
		ButtonGroup buttonGroup = new ButtonGroup
		{
			AllowUnpress = false
		};
		for (int i = 0; i < _source.ItemCount; i++)
		{
			int capturedIndex = i;
			string itemText = _source.GetItemText(i);
			Button button = new Button
			{
				Name = $"VisualOption{i}",
				Text = itemText,
				TooltipText = itemText,
				ToggleMode = true,
				ButtonGroup = buttonGroup,
				CustomMinimumSize = new Vector2(104f, 38f),
				SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
				TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
				Icon = (_iconProvider?.Invoke(i) ?? _source.GetItemIcon(i) ?? ResolveSemanticIcon(itemText)),
				ExpandIcon = false
			};
			button.AddThemeConstantOverride("icon_max_width", 22);
			button.Pressed += delegate
			{
				Select(capturedIndex);
			};
			_host.AddChild(button, forceReadableName: false, Node.InternalMode.Disabled);
			_segments.Add(button);
		}
		RefreshSelection();
	}

	public void RefreshSelection()
	{
		if (_disposed || !GodotObject.IsInstanceValid(_source))
		{
			return;
		}
		for (int i = 0; i < _segments.Count; i++)
		{
			if (GodotObject.IsInstanceValid(_segments[i]))
			{
				_segments[i].SetPressedNoSignal(i == _source.Selected);
			}
		}
	}

	public void Select(int index)
	{
		if (!_disposed && GodotObject.IsInstanceValid(_source) && index >= 0 && index < _source.ItemCount)
		{
			_source.Select(index);
			RefreshSelection();
			_source.EmitSignal(OptionButton.SignalName.ItemSelected, index);
		}
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}
		_disposed = true;
		if (GodotObject.IsInstanceValid(_source))
		{
			_source.ItemSelected -= OnSourceSelected;
		}
		foreach (Button segment in _segments)
		{
			if (GodotObject.IsInstanceValid(segment))
			{
				if (GodotObject.IsInstanceValid(_host) && segment.GetParent() == _host)
				{
					_host.RemoveChild(segment);
				}
				segment.QueueFree();
			}
		}
		_segments.Clear();
	}

	private void OnSourceSelected(long _)
	{
		RefreshSelection();
	}

	private static Texture2D ResolveSemanticIcon(string label)
	{
		string text = label?.ToLowerInvariant() ?? "";
		string path = ((!text.Contains("npc") && !text.Contains("角色") && !text.Contains("演员")) ? ((!text.Contains("动画") && !text.Contains("anime")) ? ((!text.Contains("费用") && !text.Contains("coin") && !text.Contains("阳光")) ? ((!text.Contains("条件") && !text.Contains("比较")) ? ((!text.Contains("步骤") && !text.Contains("流程")) ? ((!text.Contains("目标") && !text.Contains("铲") && !text.Contains("格")) ? "res://addons/ModEditor/Icons/ResourceCard.svg" : "res://addons/ModEditor/Icons/2D.svg") : "res://addons/ModEditor/Icons/FlowPort.svg") : "res://addons/ModEditor/Icons/Unlock.svg") : "res://addons/ModEditor/Icons/ResourceCollectable.svg") : "res://addons/ModEditor/Icons/MainMovieWrite.svg") : "res://addons/ModEditor/Icons/ResourceCharacter.svg");
		return ResourceLoader.Load<Texture2D>(path, null, ResourceLoader.CacheMode.Reuse);
	}
}
