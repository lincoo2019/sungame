using System;
using System.IO;
using System.Text;
using Godot;

namespace PVZHE.ModEditor.Tools;

public static class XWAdobeAnimateTrace
{
	private const string Prefix = "[ModEditor][AdobeAnimate]";

	private static readonly object LogLock = new object();

	public static string LogPath => NormalizePath(Path.Combine(Path.GetTempPath(), "PVZHE_ModEditor", "adobe_animate_trace.log"));

	public static void ResetLog()
	{
		try
		{
			string text = Path.GetDirectoryName(LogPath) ?? "";
			if (!string.IsNullOrWhiteSpace(text))
			{
				Directory.CreateDirectory(text);
			}
			File.WriteAllText(LogPath, $"{"[ModEditor][AdobeAnimate]"} trace reset {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}{System.Environment.NewLine}", Encoding.UTF8);
		}
		catch (Exception ex)
		{
			GD.PrintErr("[ModEditor][AdobeAnimate] trace reset failed: " + ex.Message);
		}
	}

	public static void Write(string message)
	{
		WriteInternal(message, isError: false);
	}

	public static void WriteError(string message)
	{
		WriteInternal(message, isError: true);
	}

	private static void WriteInternal(string message, bool isError)
	{
		string text = $"{"[ModEditor][AdobeAnimate]"} [{DateTime.Now:HH:mm:ss.fff}] [T{System.Environment.CurrentManagedThreadId}] {message}";
		if (isError)
		{
			GD.PrintErr(text);
		}
		else
		{
			GD.Print(text);
		}
		try
		{
			lock (LogLock)
			{
				string text2 = Path.GetDirectoryName(LogPath) ?? "";
				if (!string.IsNullOrWhiteSpace(text2))
				{
					Directory.CreateDirectory(text2);
				}
				File.AppendAllText(LogPath, text + System.Environment.NewLine, Encoding.UTF8);
			}
		}
		catch (Exception ex)
		{
			GD.PrintErr("[ModEditor][AdobeAnimate] trace file append failed: " + ex.Message);
		}
	}

	private static string NormalizePath(string path)
	{
		return (path ?? "").Replace('\\', '/');
	}
}
