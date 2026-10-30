using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;

public partial class CrashLogger : Node
{
	private const string StartupLogExportDirName = "PVZHE_Logs";

	private const int MaxStartupLogExports = 10;

	private string _startupLogExportDir = "";

	public static CrashLogger Instance;

	public override void _Ready()
	{
		Instance = this;
		if (!Engine.IsEditorHint())
		{
			_InitPaths();
			_CopyPreviousGodotLogToExportDir();
		}
	}

	public override void _ExitTree()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	private void _InitPaths()
	{
		if (OS.GetName() == "Android")
		{
			string systemDir = OS.GetSystemDir(OS.SystemDir.Documents);
			_startupLogExportDir = systemDir + "/PVZHE_Logs";
		}
		else
		{
			_startupLogExportDir = "user://PVZHE_Logs";
		}
	}

	private void _CopyPreviousGodotLogToExportDir()
	{
		string text = _FindPreviousGodotLogPath();
		if (string.IsNullOrEmpty(text))
		{
			return;
		}
		_EnsureDir(_startupLogExportDir);
		using FileAccess fileAccess = FileAccess.Open(text, FileAccess.ModeFlags.Read);
		if (fileAccess == null)
		{
			GD.PushWarning("[CrashLogger] Failed to open previous Godot log: " + text);
			return;
		}
		string asText = fileAccess.GetAsText();
		string text2 = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture);
		string text3 = _startupLogExportDir + "/godot_startup_" + text2 + ".log";
		using FileAccess fileAccess2 = FileAccess.Open(text3, FileAccess.ModeFlags.Write);
		if (fileAccess2 == null)
		{
			GD.PushWarning("[CrashLogger] Failed to export previous Godot log: " + text3);
			return;
		}
		fileAccess2.StoreString(asText);
		_CleanupOldStartupLogExports();
		GD.Print("[CrashLogger] Previous Godot log exported: " + text3);
	}

	private static string _FindPreviousGodotLogPath()
	{
		string text = OS.GetUserDataDir() + "/logs";
		using DirAccess dirAccess = DirAccess.Open(text);
		if (dirAccess == null)
		{
			return "";
		}
		string result = "";
		ulong num = 0uL;
		dirAccess.ListDirBegin();
		string next = dirAccess.GetNext();
		while (next != "")
		{
			if (dirAccess.CurrentIsDir() || next == "godot.log" || !next.EndsWith(".log", StringComparison.OrdinalIgnoreCase))
			{
				next = dirAccess.GetNext();
				continue;
			}
			string text2 = text + "/" + next;
			ulong modifiedTime = FileAccess.GetModifiedTime(text2);
			if (modifiedTime >= num)
			{
				num = modifiedTime;
				result = text2;
			}
			next = dirAccess.GetNext();
		}
		dirAccess.ListDirEnd();
		return result;
	}

	private void _CleanupOldStartupLogExports()
	{
		using DirAccess dirAccess = DirAccess.Open(_startupLogExportDir);
		if (dirAccess == null)
		{
			return;
		}
		List<string> list = new List<string>();
		dirAccess.ListDirBegin();
		string next = dirAccess.GetNext();
		while (next != "")
		{
			if (next.StartsWith("godot_startup_", StringComparison.Ordinal) && next.EndsWith(".log", StringComparison.OrdinalIgnoreCase))
			{
				list.Add(next);
			}
			next = dirAccess.GetNext();
		}
		dirAccess.ListDirEnd();
		list.Sort(StringComparer.Ordinal);
		while (list.Count > 10)
		{
			string text = list[0];
			list.RemoveAt(0);
			DirAccess.RemoveAbsolute(_startupLogExportDir + "/" + text);
		}
	}

	private static void _EnsureDir(string path)
	{
		if (!DirAccess.DirExistsAbsolute(path))
		{
			DirAccess.MakeDirRecursiveAbsolute(path);
		}
	}

	public CrashLogger()
	{
		StartupLoadDiagnostics.Mark("autoload.constructed/CrashLogger");
	}
}
