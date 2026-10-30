using System.Collections.Generic;
using Godot;
using PVZHE.ModEditor.Core;

namespace PVZHE.ModEditor.Inspector;

public partial class XWResourcePreview : Node
{
	[Signal]
	public delegate void PreviewReadyEventHandler(string path, Texture2D previewTexture, Texture2D previewSmall);

	private readonly HashSet<string> _queuedPaths = new HashSet<string>();

	private readonly Dictionary<string, Texture2D> _cache = new Dictionary<string, Texture2D>();

	private Timer _timer;

	public override void _Ready()
	{
		_timer = new Timer
		{
			WaitTime = 0.1,
			Autostart = true
		};
		_timer.Timeout += ProcessQueue;
		AddChild(_timer, forceReadableName: false, InternalMode.Disabled);
	}

	public void QueuePreviewResource(string path)
	{
		if (!string.IsNullOrEmpty(path) && !_cache.ContainsKey(path) && !_queuedPaths.Contains(path))
		{
			_queuedPaths.Add(path);
		}
	}

	private void ProcessQueue()
	{
		if (_queuedPaths.Count == 0)
		{
			return;
		}
		List<string> list = new List<string>(_queuedPaths);
		_queuedPaths.Clear();
		foreach (string item in list)
		{
			if (!_cache.ContainsKey(item))
			{
				Texture2D texture2D = GeneratePreview(item);
				if (texture2D != null)
				{
					_cache[item] = texture2D;
					EmitSignal(SignalName.PreviewReady, item, texture2D, texture2D);
				}
			}
		}
	}

	private static Texture2D GeneratePreview(string path)
	{
		if (!ResourceLoader.Exists(path))
		{
			return null;
		}
		if (ResourceLoader.Load(path, "", ResourceLoader.CacheMode.Reuse) is Texture2D texture2D && XWTextureSafety.CanPreview(texture2D))
		{
			return texture2D;
		}
		return null;
	}

	public void ClearCache()
	{
		_cache.Clear();
		_queuedPaths.Clear();
	}
}
