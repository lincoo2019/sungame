using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

namespace PVZHE.ModEditor.ModSystem;

public sealed class XWModSandboxSession
{
	private readonly List<string> _activeModIds = new List<string>();

	public bool IsRunning { get; private set; }

	public string TestLevelKey { get; private set; } = "";

	public Task StartAsync(string modId, string testLevelKey = "")
	{
		IsRunning = true;
		TestLevelKey = testLevelKey;
		if (!string.IsNullOrWhiteSpace(modId) && !_activeModIds.Contains(modId))
		{
			_activeModIds.Add(modId);
		}
		GD.Print("[XWModSandboxSession] started: " + modId + ", level=" + testLevelKey);
		return Task.CompletedTask;
	}

	public void Stop()
	{
		foreach (string activeModId in _activeModIds)
		{
			XWModRuntimeRegistry.UnregisterOwner(activeModId);
		}
		_activeModIds.Clear();
		IsRunning = false;
		TestLevelKey = "";
		GD.Print("[XWModSandboxSession] stopped");
	}
}
