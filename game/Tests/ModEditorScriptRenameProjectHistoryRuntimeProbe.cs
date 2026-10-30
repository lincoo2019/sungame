using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using PVZHE.ModEditor.ScriptEditor;

public partial class ModEditorScriptRenameProjectHistoryRuntimeProbe : Node
{
	private readonly List<string> _failures = new List<string>();

	public override async void _Ready()
	{
		bool historyScoped = false;
		bool foreignUndoDenied = false;
		bool ownerUndoSucceeded = false;
		bool foreignRedoDenied = false;
		bool ownerRedoSucceeded = false;
		string probeRoot = ProjectSettings.GlobalizePath("user://mod_editor_script_rename_project_history");
		string rootA = Path.Combine(probeRoot, "ModA");
		string rootB = Path.Combine(probeRoot, "ModB");
		string pathA = Path.Combine(rootA, "A.cs");
		string pathB = Path.Combine(rootB, "B.cs");
		try
		{
			TryDeleteProbeRoot(probeRoot);
			Directory.CreateDirectory(rootA);
			Directory.CreateDirectory(rootB);
			File.WriteAllText(pathA, "namespace ModA;\npublic class Alpha { public Alpha Next; }\n");
			File.WriteAllText(pathB, "namespace ModB;\npublic class Beta { }\n");
			int offset = "namespace ModA;\npublic class Alpha { public Alpha Next; }\n".IndexOf("Alpha", StringComparison.Ordinal);
			XWCSharpIdeService.RenamePreview renamePreview = await XWCSharpIdeService.PreviewRenameAsync(rootA, pathA, "namespace ModA;\npublic class Alpha { public Alpha Next; }\n", offset, "RenamedAlpha");
			Require(renamePreview?.IsValid ?? false, "Could not prepare the Mod A rename transaction.");
			bool flag = renamePreview?.IsValid ?? false;
			if (flag)
			{
				flag = await XWCSharpIdeService.ApplyRenameAsync(renamePreview);
			}
			bool flag2 = flag;
			Require(flag2 && File.ReadAllText(pathA) == "namespace ModA;\npublic class RenamedAlpha { public RenamedAlpha Next; }\n", "Could not apply the Mod A rename transaction.");
			XWCSharpIdeService.RenameHistoryAvailability renameHistoryAvailability = XWCSharpIdeService.GetRenameHistoryAvailability(rootA);
			XWCSharpIdeService.RenameHistoryAvailability renameHistoryAvailability2 = XWCSharpIdeService.GetRenameHistoryAvailability(rootB);
			historyScoped = renameHistoryAvailability.CanUndo && !renameHistoryAvailability.CanRedo && !renameHistoryAvailability2.CanUndo && !renameHistoryAvailability2.CanRedo;
			Require(historyScoped, "Rename history availability leaked from Mod A into Mod B.");
			foreignUndoDenied = !(await XWCSharpIdeService.UndoLastRenameAsync(rootB)) && File.ReadAllText(pathA) == "namespace ModA;\npublic class RenamedAlpha { public RenamedAlpha Next; }\n" && File.ReadAllText(pathB) == "namespace ModB;\npublic class Beta { }\n";
			Require(foreignUndoDenied, "Mod B undo modified the previous Mod A transaction.");
			bool flag3 = await XWCSharpIdeService.UndoLastRenameAsync(rootA);
			XWCSharpIdeService.RenameHistoryAvailability renameHistoryAvailability3 = XWCSharpIdeService.GetRenameHistoryAvailability(rootA);
			ownerUndoSucceeded = flag3 && File.ReadAllText(pathA) == "namespace ModA;\npublic class Alpha { public Alpha Next; }\n" && !renameHistoryAvailability3.CanUndo && renameHistoryAvailability3.CanRedo;
			Require(ownerUndoSucceeded, "The owning Mod could not undo its rename transaction.");
			foreignRedoDenied = !(await XWCSharpIdeService.RedoLastRenameAsync(rootB)) && File.ReadAllText(pathA) == "namespace ModA;\npublic class Alpha { public Alpha Next; }\n" && File.ReadAllText(pathB) == "namespace ModB;\npublic class Beta { }\n";
			Require(foreignRedoDenied, "Mod B redo modified the previous Mod A transaction.");
			ownerRedoSucceeded = await XWCSharpIdeService.RedoLastRenameAsync(rootA) && File.ReadAllText(pathA) == "namespace ModA;\npublic class RenamedAlpha { public RenamedAlpha Next; }\n" && File.ReadAllText(pathB) == "namespace ModB;\npublic class Beta { }\n";
			Require(ownerRedoSucceeded, "The owning Mod could not redo its rename transaction.");
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		finally
		{
			XWCSharpCodeModel.ResetIndexForProbe();
			TryDeleteProbeRoot(probeRoot);
		}
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_EDITOR_SCRIPT_RENAME_HISTORY_FAILURE] " + failure);
		}
		GD.Print($"[MOD_EDITOR_SCRIPT_RENAME_HISTORY_PROBE] historyScoped={historyScoped} foreignUndoDenied={foreignUndoDenied} ownerUndoSucceeded={ownerUndoSucceeded} foreignRedoDenied={foreignRedoDenied} ownerRedoSucceeded={ownerRedoSucceeded} failures={_failures.Count}");
		Console.Out.Flush();
		Console.Error.Flush();
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}

	private static void TryDeleteProbeRoot(string path)
	{
		try
		{
			string fullPath = Path.GetFullPath(path);
			string value = Path.GetFullPath(ProjectSettings.GlobalizePath("user://")).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
			if (fullPath.StartsWith(value, StringComparison.OrdinalIgnoreCase) && Directory.Exists(fullPath))
			{
				Directory.Delete(fullPath, recursive: true);
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
