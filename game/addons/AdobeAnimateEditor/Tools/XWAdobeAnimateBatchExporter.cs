using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Godot;

namespace PVZHE.ModEditor.Tools;

public static class XWAdobeAnimateBatchExporter
{
	private enum ExportWaitResult
	{
		ProcessExited,
		DatReady,
		TimedOut,
		Canceled
	}

	private const int ExportTimeoutMilliseconds = 300000;

	private const int DatStabilityPollMilliseconds = 200;

	private const int StableDatPollsRequired = 3;

	private static readonly string[] CandidateBatchPaths = new string[15]
	{
		"BuildTools/AdobeAnimate/export_adobe_animate_to_dat.bat", "BuildTools/AdobeAnimate/ExportAdobeAnimateToDat.bat", "BuildTools/AdobeAnimate/export_fla_to_dat.bat", "BuildTools/AdobeAnimate/ExportFlaToDat.bat", "BuildTools/AdobeAnimate/export_xfl_to_dat.bat", "BuildTools/AdobeAnimate/ExportXflToDat.bat", "Tools/AdobeAnimate/export_adobe_animate_to_dat.bat", "Tools/AdobeAnimate/ExportAdobeAnimateToDat.bat", "Tools/AdobeAnimate/export_fla_to_dat.bat", "Tools/AdobeAnimate/ExportFlaToDat.bat",
		"Tools/AdobeAnimate/export_xfl_to_dat.bat", "Tools/AdobeAnimate/ExportXflToDat.bat", "EditorTools/AdobeAnimate/export_adobe_animate_to_dat.bat", "EditorTools/AdobeAnimate/export_fla_to_dat.bat", "EditorTools/AdobeAnimate/export_xfl_to_dat.bat"
	};

	public static string FindExporterBatch()
	{
		string text = NormalizePath(OS.GetEnvironment("XW_ADOBE_ANIMATE_EXPORTER"));
		if (IsExistingFile(text))
		{
			return text;
		}
		foreach (string searchRoot in GetSearchRoots())
		{
			string[] candidateBatchPaths = CandidateBatchPaths;
			foreach (string path in candidateBatchPaths)
			{
				string text2 = NormalizePath(Path.Combine(searchRoot, path));
				if (IsExistingFile(text2))
				{
					return text2;
				}
			}
		}
		return "";
	}

	public static XWAdobeAnimateBatchExportResult ExportXflToDat(string xflPath, string datPath)
	{
		return ExportAdobeAnimateToDat(xflPath, datPath);
	}

	public static async Task<XWAdobeAnimateBatchExportResult> ExportAdobeAnimateToDatAsync(string sourcePath, string datPath, CancellationToken cancellationToken = default(CancellationToken))
	{
		string absoluteSourcePath = ToAbsolutePath(sourcePath);
		string absoluteDatPath = ToAbsolutePath(datPath);
		string exporterPath = FindExporterBatch();
		Trace($"Batch export async queued: source={sourcePath}, dat={datPath}, absoluteSource={absoluteSourcePath}, absoluteDat={absoluteDatPath}, exporter={DescribeExporter(exporterPath)}");
		try
		{
			XWAdobeAnimateBatchExportResult xWAdobeAnimateBatchExportResult = await Task.Run(() => ExportAdobeAnimateToDatPrepared(sourcePath, datPath, absoluteSourcePath, absoluteDatPath, exporterPath, cancellationToken), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			Trace($"Batch export async completed: success={xWAdobeAnimateBatchExportResult?.Success}, exporter={xWAdobeAnimateBatchExportResult?.ExporterPath}, dat={xWAdobeAnimateBatchExportResult?.DatPath}, error={xWAdobeAnimateBatchExportResult?.Error}");
			return xWAdobeAnimateBatchExportResult;
		}
		catch (OperationCanceledException)
		{
			return new XWAdobeAnimateBatchExportResult
			{
				Success = false,
				ExporterPath = exporterPath,
				DatPath = datPath,
				Error = "Adobe Animate DAT export canceled."
			};
		}
	}

	public static XWAdobeAnimateBatchExportResult ExportAdobeAnimateToDat(string sourcePath, string datPath)
	{
		string text = ToAbsolutePath(sourcePath);
		string text2 = ToAbsolutePath(datPath);
		string exporterPath = FindExporterBatch();
		Trace($"Batch export sync start: source={sourcePath}, dat={datPath}, absoluteSource={text}, absoluteDat={text2}, exporter={DescribeExporter(exporterPath)}");
		return ExportAdobeAnimateToDatPrepared(sourcePath, datPath, text, text2, exporterPath, CancellationToken.None);
	}

	private static XWAdobeAnimateBatchExportResult ExportAdobeAnimateToDatPrepared(string sourcePath, string datPath, string absoluteSourcePath, string absoluteDatPath, string exporterPath, CancellationToken cancellationToken)
	{
		Trace($"Batch export start: source={absoluteSourcePath}, dat={absoluteDatPath}, exporter={DescribeExporter(exporterPath)}");
		if (cancellationToken.IsCancellationRequested)
		{
			Trace("Batch export canceled before source validation.");
			return Canceled(datPath, exporterPath);
		}
		if (!File.Exists(absoluteSourcePath))
		{
			Trace("Batch export source missing: " + absoluteSourcePath);
			return new XWAdobeAnimateBatchExportResult
			{
				Success = false,
				DatPath = datPath,
				Error = "Adobe Animate source file not found: " + sourcePath
			};
		}
		Trace("Pre-exported Adobe Animate DAT files are ignored; forcing XFL/FLA parsing.");
		Trace("Built-in exporter: forced start source=" + absoluteSourcePath + ", dat=" + absoluteDatPath);
		if (XWAdobeAnimateXflDatExporter.TryExport(absoluteSourcePath, absoluteDatPath, out var output, out var error, cancellationToken))
		{
			Trace("Built-in exporter: success dat=" + absoluteDatPath);
			return new XWAdobeAnimateBatchExportResult
			{
				Success = true,
				ExporterPath = "BuiltInXflDatExporter",
				DatPath = datPath,
				Output = output
			};
		}
		Trace("Built-in exporter: failed error=" + error);
		if (cancellationToken.IsCancellationRequested)
		{
			Trace("Batch export canceled after built-in exporter.");
			return Canceled(datPath, "BuiltInXflDatExporter", error);
		}
		bool flag = IsFlaFile(absoluteSourcePath);
		if (string.IsNullOrEmpty(exporterPath))
		{
			Trace("Batch export failed: forced built-in conversion failed and no external exporter is configured. builtInError=" + error);
			return new XWAdobeAnimateBatchExportResult
			{
				Success = false,
				DatPath = datPath,
				Error = (string.IsNullOrEmpty(error) ? "Forced Adobe Animate XFL/FLA DAT conversion failed, and no external exporter batch was configured." : error)
			};
		}
		string text = absoluteSourcePath;
		string value = absoluteSourcePath;
		string tempDirectory = "";
		string output2 = "";
		if (flag)
		{
			bool flag2 = IsFlaSpecificExporter(exporterPath);
			Trace($"Batch export FLA prepare: useOriginalFla={flag2}, exporter={DescribeExporter(exporterPath)}");
			if (TryExtractFlaToTemporaryXfl(absoluteSourcePath, out var exportSourcePath, out tempDirectory, out output2, out var error2))
			{
				value = exportSourcePath;
				if (!flag2)
				{
					text = exportSourcePath;
				}
				Trace($"Batch export FLA prepare success: exportSource={text}, xfl={value}, temp={tempDirectory}");
			}
			else if (!flag2)
			{
				Trace("Batch export FLA prepare failed: " + error2);
				return new XWAdobeAnimateBatchExportResult
				{
					Success = false,
					ExporterPath = exporterPath,
					DatPath = datPath,
					Error = error2
				};
			}
		}
		string text2 = NormalizePath(Path.GetDirectoryName(absoluteDatPath) ?? "");
		if (!string.IsNullOrEmpty(text2))
		{
			Directory.CreateDirectory(text2);
		}
		StringBuilder output3 = new StringBuilder();
		if (!string.IsNullOrEmpty(error))
		{
			output3.AppendLine(error);
		}
		if (!string.IsNullOrEmpty(output2))
		{
			output3.AppendLine(output2);
		}
		int num = -1;
		try
		{
			ProcessStartInfo processStartInfo = new ProcessStartInfo
			{
				FileName = "cmd.exe",
				WorkingDirectory = NormalizePath(Path.GetDirectoryName(text) ?? Directory.GetCurrentDirectory()),
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true,
				StandardOutputEncoding = Encoding.UTF8,
				StandardErrorEncoding = Encoding.UTF8
			};
			processStartInfo.ArgumentList.Add("/c");
			processStartInfo.ArgumentList.Add(exporterPath);
			processStartInfo.ArgumentList.Add(text);
			processStartInfo.ArgumentList.Add(absoluteDatPath);
			processStartInfo.Environment["XW_ADOBE_ANIMATE_SOURCE_PATH"] = text;
			processStartInfo.Environment["XW_XFL_PATH"] = value;
			processStartInfo.Environment["XW_FLA_PATH"] = absoluteSourcePath;
			processStartInfo.Environment["XW_DAT_PATH"] = absoluteDatPath;
			using Process process = new Process
			{
				StartInfo = processStartInfo
			};
			process.OutputDataReceived += delegate(object _, DataReceivedEventArgs args)
			{
				if (args.Data != null)
				{
					output3.AppendLine(args.Data);
				}
			};
			process.ErrorDataReceived += delegate(object _, DataReceivedEventArgs args)
			{
				if (args.Data != null)
				{
					output3.AppendLine(args.Data);
				}
			};
			Trace($"Batch export external process start: file={processStartInfo.FileName}, exporter={exporterPath}, source={text}, dat={absoluteDatPath}");
			process.Start();
			process.BeginOutputReadLine();
			process.BeginErrorReadLine();
			ExportWaitResult exportWaitResult = WaitForDatOrProcessExit(process, absoluteDatPath, output3, cancellationToken);
			Trace($"Batch export external process wait result: {exportWaitResult}");
			switch (exportWaitResult)
			{
			case ExportWaitResult.Canceled:
				return Canceled(datPath, exporterPath, output3.ToString());
			case ExportWaitResult.TimedOut:
				KillProcessTree(process);
				Trace("Batch export external process timed out and was killed.");
				return new XWAdobeAnimateBatchExportResult
				{
					Success = false,
					ExporterPath = exporterPath,
					DatPath = datPath,
					Output = output3.ToString(),
					Error = "Adobe Animate DAT export timed out."
				};
			default:
				num = (process.HasExited ? process.ExitCode : 0);
				break;
			}
		}
		catch (Exception ex)
		{
			Trace("Batch export external process exception: " + ex.GetType().Name + ": " + ex.Message);
			return new XWAdobeAnimateBatchExportResult
			{
				Success = false,
				ExporterPath = exporterPath,
				DatPath = datPath,
				ExitCode = num,
				Output = output3.ToString(),
				Error = ex.Message
			};
		}
		finally
		{
			CleanupTemporaryDirectory(tempDirectory);
		}
		bool flag3 = File.Exists(absoluteDatPath);
		Trace($"Batch export final check: exitCode={num}, datExists={flag3}, dat={absoluteDatPath}");
		if (num != 0 || !flag3)
		{
			return new XWAdobeAnimateBatchExportResult
			{
				Success = false,
				ExporterPath = exporterPath,
				DatPath = datPath,
				ExitCode = num,
				Output = output3.ToString(),
				Error = ((num != 0) ? $"Adobe Animate DAT export failed with exit code {num}." : ("Adobe Animate DAT export did not create: " + datPath))
			};
		}
		return new XWAdobeAnimateBatchExportResult
		{
			Success = true,
			ExporterPath = exporterPath,
			DatPath = datPath,
			ExitCode = num,
			Output = output3.ToString()
		};
	}

	private static ExportWaitResult WaitForDatOrProcessExit(Process process, string absoluteDatPath, StringBuilder output, CancellationToken cancellationToken)
	{
		DateTime dateTime = DateTime.UtcNow.AddMilliseconds(300000.0);
		long lastDatSize = -1L;
		int stablePolls = 0;
		do
		{
			if (cancellationToken.IsCancellationRequested)
			{
				KillProcessTree(process);
				return ExportWaitResult.Canceled;
			}
			if (IsDatFileStable(absoluteDatPath, ref lastDatSize, ref stablePolls))
			{
				output.AppendLine("Adobe Animate DAT file was created and is stable; continuing without waiting for the exporter process to close.");
				return ExportWaitResult.DatReady;
			}
			if (process.WaitForExit(200))
			{
				return ExportWaitResult.ProcessExited;
			}
		}
		while (!(DateTime.UtcNow >= dateTime));
		return ExportWaitResult.TimedOut;
	}

	private static bool IsDatFileStable(string absoluteDatPath, ref long lastDatSize, ref int stablePolls)
	{
		if (!File.Exists(absoluteDatPath))
		{
			lastDatSize = -1L;
			stablePolls = 0;
			return false;
		}
		long length;
		try
		{
			length = new FileInfo(absoluteDatPath).Length;
		}
		catch (IOException)
		{
			stablePolls = 0;
			return false;
		}
		catch (UnauthorizedAccessException)
		{
			stablePolls = 0;
			return false;
		}
		if (length <= 0)
		{
			lastDatSize = length;
			stablePolls = 0;
			return false;
		}
		if (length == lastDatSize)
		{
			stablePolls++;
		}
		else
		{
			lastDatSize = length;
			stablePolls = 1;
		}
		return stablePolls >= 3;
	}

	private static XWAdobeAnimateBatchExportResult Canceled(string datPath, string exporterPath, string output = "")
	{
		Trace("Batch export canceled: dat=" + datPath + ", exporter=" + DescribeExporter(exporterPath));
		return new XWAdobeAnimateBatchExportResult
		{
			Success = false,
			ExporterPath = exporterPath,
			DatPath = datPath,
			Output = output,
			Error = "Adobe Animate DAT export canceled."
		};
	}

	private static void Trace(string message)
	{
		XWAdobeAnimateTrace.Write(message);
	}

	private static string DescribeExporter(string exporterPath)
	{
		if (!string.IsNullOrWhiteSpace(exporterPath))
		{
			return exporterPath;
		}
		return "<none>";
	}

	private static void KillProcessTree(Process process)
	{
		try
		{
			if (!process.HasExited)
			{
				process.Kill(entireProcessTree: true);
			}
		}
		catch
		{
		}
	}

	private static IEnumerable<string> GetSearchRoots()
	{
		string text = NormalizePath(OS.GetExecutablePath().GetBaseDir());
		if (!string.IsNullOrEmpty(text))
		{
			yield return text;
		}
		string text2 = NormalizePath(ProjectSettings.GlobalizePath("res://"));
		if (!string.IsNullOrEmpty(text2))
		{
			yield return text2;
		}
		string text3 = NormalizePath(Directory.GetCurrentDirectory());
		if (!string.IsNullOrEmpty(text3))
		{
			yield return text3;
		}
	}

	private static bool TryExtractFlaToTemporaryXfl(string absoluteFlaPath, out string exportSourcePath, out string tempDirectory, out string output, out string error)
	{
		exportSourcePath = "";
		tempDirectory = "";
		output = "";
		error = "";
		try
		{
			string path = NormalizePath(Path.Combine(Path.GetTempPath(), "PVZHE_ModEditor", "FlaXfl"));
			tempDirectory = NormalizePath(Path.Combine(path, Path.GetFileNameWithoutExtension(absoluteFlaPath) + "_" + Guid.NewGuid().ToString("N")));
			Directory.CreateDirectory(tempDirectory);
			ZipFile.ExtractToDirectory(absoluteFlaPath, tempDirectory, overwriteFiles: true);
			string text = Directory.EnumerateFiles(tempDirectory, "DOMDocument.xml", SearchOption.AllDirectories).FirstOrDefault();
			string text2 = "";
			text2 = (string.IsNullOrEmpty(text) ? Directory.EnumerateFiles(tempDirectory, "*.xfl", SearchOption.AllDirectories).FirstOrDefault() : Directory.EnumerateFiles(NormalizePath(Path.GetDirectoryName(text) ?? tempDirectory), "*.xfl", SearchOption.TopDirectoryOnly).FirstOrDefault());
			exportSourcePath = NormalizePath(string.IsNullOrEmpty(text2) ? text : text2);
			if (string.IsNullOrEmpty(exportSourcePath))
			{
				error = "Adobe Animate FLA package does not contain DOMDocument.xml or an XFL document.";
				CleanupTemporaryDirectory(tempDirectory);
				tempDirectory = "";
				return false;
			}
			output = "Extracted Adobe Animate FLA to temporary XFL package: " + tempDirectory;
			return true;
		}
		catch (Exception ex)
		{
			error = "Adobe Animate FLA extraction failed: " + ex.Message;
			CleanupTemporaryDirectory(tempDirectory);
			tempDirectory = "";
			return false;
		}
	}

	private static bool IsFlaSpecificExporter(string exporterPath)
	{
		string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(exporterPath ?? "");
		if (fileNameWithoutExtension.Contains("fla", StringComparison.OrdinalIgnoreCase))
		{
			return !fileNameWithoutExtension.Contains("xfl", StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	private static bool IsFlaFile(string path)
	{
		return string.Equals(Path.GetExtension(path), ".fla", StringComparison.OrdinalIgnoreCase);
	}

	private static void CleanupTemporaryDirectory(string tempDirectory)
	{
		if (string.IsNullOrWhiteSpace(tempDirectory))
		{
			return;
		}
		string text = NormalizePath(tempDirectory);
		string value = NormalizePath(Path.Combine(Path.GetTempPath(), "PVZHE_ModEditor", "FlaXfl"));
		if (!text.StartsWith(value, StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		try
		{
			if (Directory.Exists(text))
			{
				Directory.Delete(text, recursive: true);
			}
		}
		catch
		{
		}
	}

	private static bool IsExistingFile(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return false;
		}
		if (!Godot.FileAccess.FileExists(path))
		{
			return File.Exists(path);
		}
		return true;
	}

	private static string ToAbsolutePath(string path)
	{
		path = NormalizePath(path);
		if (path.StartsWith("res://", StringComparison.OrdinalIgnoreCase) || path.StartsWith("user://", StringComparison.OrdinalIgnoreCase))
		{
			return NormalizePath(ProjectSettings.GlobalizePath(path));
		}
		return path;
	}

	private static string NormalizePath(string path)
	{
		return (path ?? "").Replace('\\', '/');
	}
}
