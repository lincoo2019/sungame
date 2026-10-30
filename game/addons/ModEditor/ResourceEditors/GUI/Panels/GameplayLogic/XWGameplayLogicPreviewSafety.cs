using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;
using PVZHE.AdobeAnimateEditor.Inspector;

namespace PVZHE.ModEditor.ResourceEditors.GUI.GameplayLogic;

public sealed class XWGameplayLogicPreviewSafety : IDisposable
{
	private sealed record SignalConnection(GodotObject Source, StringName Signal, Callable Callable);

	private readonly List<SignalConnection> _signals = new List<SignalConnection>();

	private readonly List<Timer> _timers = new List<Timer>();

	private readonly List<Node> _previewRoots = new List<Node>();

	private bool _disposed;

	public void PrepareCharacter(Node node)
	{
		PrepareCharacterBeforeTree(node);
	}

	public static void PrepareCharacterBeforeTree(Node node)
	{
		if (!GodotObject.IsInstanceValid(node))
		{
			return;
		}
		if (node is StateChart stateChart)
		{
			stateChart.InitializationDisabled = true;
		}
		if (node is TowerDefenseCharacter towerDefenseCharacter)
		{
			towerDefenseCharacter.inGame = false;
			towerDefenseCharacter.editorPreviewMode = true;
		}
		else
		{
			TrySet(node, "inGame", false);
			TrySet(node, "editorPreviewMode", true);
		}
		if (PreserveAdobeAnimateRendering(node))
		{
			return;
		}
		node.SetProcess(enable: false);
		node.SetPhysicsProcess(enable: false);
		node.SetProcessInput(enable: false);
		node.SetProcessUnhandledInput(enable: false);
		node.SetProcessUnhandledKeyInput(enable: false);
		foreach (Node child in node.GetChildren())
		{
			PrepareCharacterBeforeTree(child);
		}
	}

	public static void SetCharacterPreviewAnimationActive(Node node, bool active)
	{
		if (!GodotObject.IsInstanceValid(node))
		{
			return;
		}
		if (node is AdobeAnimateSprite adobeAnimateSprite)
		{
			adobeAnimateSprite.ProcessMode = (Node.ProcessModeEnum)(active ? 0 : 4);
			adobeAnimateSprite.RefreshProcessScheduling();
			if (active)
			{
				adobeAnimateSprite.QueueRedraw();
			}
		}
		foreach (Node child in node.GetChildren())
		{
			SetCharacterPreviewAnimationActive(child, active);
		}
	}

	public bool TrackSignal(GodotObject source, StringName signal, Callable callable)
	{
		if (_disposed || !GodotObject.IsInstanceValid(source) || callable.Equals(default(Callable)) || !source.HasSignal(signal))
		{
			return false;
		}
		try
		{
			if (!source.IsConnected(signal, callable))
			{
				source.Connect(signal, callable);
			}
			_signals.Add(new SignalConnection(source, signal, callable));
			return true;
		}
		catch (Exception ex)
		{
			GD.PushWarning($"[ModEditor Preview] 无法跟踪信号 {signal}: {ex.Message}");
			return false;
		}
	}

	public Timer TrackTimer(Timer timer)
	{
		if (!_disposed && GodotObject.IsInstanceValid(timer) && !_timers.Contains(timer))
		{
			_timers.Add(timer);
		}
		return timer;
	}

	public T TrackPreviewRoot<T>(T root) where T : Node
	{
		if (!_disposed && GodotObject.IsInstanceValid(root) && !_previewRoots.Contains(root))
		{
			_previewRoots.Add(root);
		}
		return root;
	}

	public void DisposePreviewTree(Node root)
	{
		if (GodotObject.IsInstanceValid(root))
		{
			StopTimersRecursive(root);
			root.SetProcess(enable: false);
			root.SetPhysicsProcess(enable: false);
			Node parent = root.GetParent();
			if (GodotObject.IsInstanceValid(parent))
			{
				parent.RemoveChild(root);
			}
			if (!root.IsQueuedForDeletion())
			{
				root.QueueFree();
			}
		}
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}
		_disposed = true;
		foreach (SignalConnection signal in _signals)
		{
			try
			{
				if (GodotObject.IsInstanceValid(signal.Source) && signal.Source.IsConnected(signal.Signal, signal.Callable))
				{
					signal.Source.Disconnect(signal.Signal, signal.Callable);
				}
			}
			catch (ObjectDisposedException)
			{
			}
		}
		foreach (Timer timer in _timers)
		{
			if (GodotObject.IsInstanceValid(timer))
			{
				timer.Stop();
			}
		}
		for (int num = _previewRoots.Count - 1; num >= 0; num--)
		{
			DisposePreviewTree(_previewRoots[num]);
		}
		_signals.Clear();
		_timers.Clear();
		_previewRoots.Clear();
	}

	private static bool PreserveAdobeAnimateRendering(Node node)
	{
		if ((!(node is AdobeAnimateSprite) && !(node is AdobeAnimateCpuPreviewCanvas)) || 1 == 0)
		{
			return node.GetType().Name.Contains("AdobeAnimate", StringComparison.Ordinal);
		}
		return true;
	}

	private static void TrySet(Node node, StringName property, Variant value)
	{
		foreach (Dictionary property2 in node.GetPropertyList())
		{
			if (!(property2.GetValueOrDefault("name", "").AsString() != property.ToString()))
			{
				node.Set(property, value);
				break;
			}
		}
	}

	private static void StopTimersRecursive(Node root)
	{
		if (root is Timer timer)
		{
			timer.Stop();
		}
		foreach (Node child in root.GetChildren())
		{
			StopTimersRecursive(child);
		}
	}
}
