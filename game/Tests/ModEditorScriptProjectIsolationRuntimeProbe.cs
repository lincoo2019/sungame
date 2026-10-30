using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor.ScriptEditor;

public partial class ModEditorScriptProjectIsolationRuntimeProbe : Node
{
	private readonly List<string> _failures = new List<string>();

	public override async void _Ready()
	{
		bool outsideRejected = false;
		bool rootSwitchIsolated = false;
		bool fullRootSwitchSafe = false;
		bool invalidRootCleared = false;
		bool newRootPublished = false;
		bool oldRootAbsent = false;
		string probeRoot = ProjectSettings.GlobalizePath("user://mod_editor_script_project_isolation");
		string rootA = Path.Combine(probeRoot, "ModA");
		string rootB = Path.Combine(probeRoot, "ModB");
		string pathA = Path.Combine(rootA, "A.cs");
		string pathB = Path.Combine(rootB, "B.cs");
		try
		{
			TryDeleteProbeRoot(probeRoot);
			Directory.CreateDirectory(rootA);
			Directory.CreateDirectory(rootB);
			File.WriteAllText(pathA, "namespace ModA;\npublic class InitialA { }\n");
			File.WriteAllText(pathB, "namespace ModB;\npublic class InitialB { }\n");
			for (int i = 0; i < 200; i++)
			{
				File.WriteAllText(Path.Combine(rootA, $"FullRace{i}.cs"), $"namespace ModA; public class FullRaceA{i} {{ }}\n");
			}
			XWCSharpCodeModel.ResetIndexForProbe();
			Task task = XWCSharpCodeModel.RequestBackgroundWarmup(rootA);
			Task newFullIndex = XWCSharpCodeModel.RequestBackgroundWarmup(rootB);
			try
			{
				await task;
			}
			catch (OperationCanceledException)
			{
			}
			await newFullIndex;
			XWCSharpProjectIndex.Snapshot snapshot = XWCSharpProjectIndex.GetSnapshot();
			fullRootSwitchSafe = snapshot.Documents.ContainsKey(Normalize(pathB)) && snapshot.TypeNamespaces.ContainsKey("InitialB") && !snapshot.Documents.ContainsKey(Normalize(pathA)) && string.Equals(Normalize(snapshot.ProjectRoot).TrimEnd('/'), Normalize(rootB).TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
			Require(fullRootSwitchSafe, "Switching roots during an active full index left the new Mod unindexed.");
			string missingRoot = Path.Combine(probeRoot, "MissingMod");
			await XWCSharpCodeModel.RequestBackgroundWarmup(missingRoot);
			XWCSharpProjectIndex.Snapshot snapshot2 = XWCSharpProjectIndex.GetSnapshot();
			invalidRootCleared = snapshot2.Documents.Count == 0 && snapshot2.TypeNamespaces.Count == 0 && string.Equals(Normalize(snapshot2.ProjectRoot).TrimEnd('/'), Normalize(missingRoot).TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
			Require(invalidRootCleared, "An invalid new Mod root retained the previous Mod completion snapshot.");
			XWCSharpCodeModel.ResetIndexForProbe();
			await XWCSharpCodeModel.RequestBackgroundWarmup(rootA);
			int rejectedBefore = XWCSharpCodeModel.GetIndexMetrics().RejectedOutOfRootRequests;
			await XWCSharpCodeModel.RequestSourceUpdate(pathB, "namespace ModB;\npublic class MustNotEnterModA { }\n");
			XWCSharpProjectIndex.Snapshot snapshot3 = XWCSharpProjectIndex.GetSnapshot();
			outsideRejected = XWCSharpCodeModel.GetIndexMetrics().RejectedOutOfRootRequests == rejectedBefore + 1 && !snapshot3.Documents.ContainsKey(Normalize(pathB)) && !snapshot3.TypeNamespaces.ContainsKey("MustNotEnterModA");
			Require(outsideRejected, "A C# source outside the active Mod root entered the completion index.");
			StringBuilder stringBuilder = new StringBuilder("namespace ModA;\n");
			for (int j = 0; j < 30000; j++)
			{
				stringBuilder.Append("public class OldRootStale").Append(j).Append(" { }\n");
			}
			Task task2 = XWCSharpCodeModel.RequestSourceUpdate(pathA, stringBuilder.ToString());
			Task task3 = XWCSharpCodeModel.RequestBackgroundWarmup(rootB);
			global::_003C_003Ey__InlineArray2<Task> buffer = default(global::_003C_003Ey__InlineArray2<Task>);
			buffer[0] = task2;
			buffer[1] = task3;
			await Task.WhenAll(buffer);
			await XWCSharpCodeModel.RequestSourceUpdate(pathB, "namespace ModB;\npublic class NewRootIncremental { }\n");
			XWCSharpProjectIndex.Snapshot snapshot4 = XWCSharpProjectIndex.GetSnapshot();
			string text = Normalize(rootB).TrimEnd('/');
			newRootPublished = snapshot4.TypeNamespaces.ContainsKey("NewRootIncremental") && snapshot4.Documents.ContainsKey(Normalize(pathB));
			oldRootAbsent = !snapshot4.TypeNamespaces.ContainsKey("OldRootStale29999") && !snapshot4.Documents.ContainsKey(Normalize(pathA));
			bool flag = true;
			foreach (string key in snapshot4.Documents.Keys)
			{
				flag &= IsInsideRoot(key, text);
			}
			rootSwitchIsolated = string.Equals(Normalize(snapshot4.ProjectRoot).TrimEnd('/'), text, StringComparison.OrdinalIgnoreCase) && flag && newRootPublished && oldRootAbsent;
			Require(newRootPublished, "The active Mod root did not publish its latest incremental C# source.");
			Require(oldRootAbsent, "A detached incremental worker published the previous Mod root after switching.");
			Require(rootSwitchIsolated, "The final completion snapshot contains mixed Mod project roots.");
		}
		catch (Exception ex2)
		{
			_failures.Add(ex2.ToString());
		}
		finally
		{
			XWCSharpCodeModel.ResetIndexForProbe();
			TryDeleteProbeRoot(probeRoot);
		}
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_EDITOR_SCRIPT_PROJECT_ISOLATION_FAILURE] " + failure);
		}
		GD.Print($"[MOD_EDITOR_SCRIPT_PROJECT_ISOLATION_PROBE] outsideRejected={outsideRejected} fullRootSwitchSafe={fullRootSwitchSafe} invalidRootCleared={invalidRootCleared} rootSwitchIsolated={rootSwitchIsolated} newRootPublished={newRootPublished} oldRootAbsent={oldRootAbsent} failures={_failures.Count}");
		Console.Out.Flush();
		Console.Error.Flush();
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}

	private static string Normalize(string path)
	{
		try
		{
			return Path.GetFullPath(path).Replace('\\', '/');
		}
		catch
		{
			return (path ?? "").Replace('\\', '/');
		}
	}

	private static bool IsInsideRoot(string path, string root)
	{
		string value = Normalize(root).TrimEnd('/') + "/";
		return Normalize(path).StartsWith(value, StringComparison.OrdinalIgnoreCase);
	}

	private static void TryDeleteProbeRoot(string path)
	{
		try
		{
			string text = Normalize(path);
			string value = Normalize(ProjectSettings.GlobalizePath("user://")).TrimEnd('/') + "/";
			if (text.StartsWith(value, StringComparison.OrdinalIgnoreCase) && Directory.Exists(text))
			{
				Directory.Delete(text, recursive: true);
			}
		}
		catch
		{
		}
	}

	private void Require(bool condition, string message)
	{
		if (!condition)
		{
			_failures.Add(message);
		}
	}
}
