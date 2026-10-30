using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor.FileSystem;

namespace PVZHE.ModEditor.Tools;

public static class XWAdobeAnimateImportService
{
	private sealed class PreparedImport
	{
		public bool Success { get; set; }

		public XWAdobeAnimateImportResult Result { get; set; }

		public string BaseName { get; set; } = "";

		public string ResourcePath { get; set; } = "";

		public string DatPath { get; set; } = "";
	}

	public static XWAdobeAnimateImportResult ImportXfl(string xflPath, string targetDirectory, string resourceName = "")
	{
		return ImportAdobeAnimateFile(xflPath, targetDirectory, resourceName);
	}

	public static async Task<XWAdobeAnimateImportResult> ImportAdobeAnimateFileAsync(string sourcePath, string targetDirectory, string resourceName = "", CancellationToken cancellationToken = default(CancellationToken))
	{
		Trace($"Import async start: source={sourcePath}, target={targetDirectory}, resourceName={resourceName}");
		if (cancellationToken.IsCancellationRequested)
		{
			Trace("Import async canceled before prepare.");
			return Failure("Adobe Animate DAT export canceled.");
		}
		PreparedImport prepared = PrepareImport(sourcePath, targetDirectory, resourceName);
		if (!prepared.Success)
		{
			Trace("Import prepare failed: " + prepared.Result?.Error);
			return prepared.Result;
		}
		Trace("Import prepared: resource=" + prepared.ResourcePath + ", dat=" + prepared.DatPath);
		XWAdobeAnimateBatchExportResult xWAdobeAnimateBatchExportResult = await XWAdobeAnimateBatchExporter.ExportAdobeAnimateToDatAsync(sourcePath, prepared.DatPath, cancellationToken);
		Trace($"Import batch completed: success={xWAdobeAnimateBatchExportResult?.Success}, exporter={xWAdobeAnimateBatchExportResult?.ExporterPath}, dat={xWAdobeAnimateBatchExportResult?.DatPath}, error={xWAdobeAnimateBatchExportResult?.Error}");
		if (cancellationToken.IsCancellationRequested)
		{
			Trace("Import async canceled after batch export.");
			return Failure("Adobe Animate DAT export canceled.", xWAdobeAnimateBatchExportResult);
		}
		return CompleteImport(prepared, xWAdobeAnimateBatchExportResult);
	}

	public static XWAdobeAnimateImportResult ImportAdobeAnimateFile(string sourcePath, string targetDirectory, string resourceName = "")
	{
		Trace($"Import sync start: source={sourcePath}, target={targetDirectory}, resourceName={resourceName}");
		PreparedImport preparedImport = PrepareImport(sourcePath, targetDirectory, resourceName);
		if (!preparedImport.Success)
		{
			Trace("Import prepare failed: " + preparedImport.Result?.Error);
			return preparedImport.Result;
		}
		Trace("Import prepared: resource=" + preparedImport.ResourcePath + ", dat=" + preparedImport.DatPath);
		XWAdobeAnimateBatchExportResult xWAdobeAnimateBatchExportResult = XWAdobeAnimateBatchExporter.ExportAdobeAnimateToDat(sourcePath, preparedImport.DatPath);
		Trace($"Import batch completed: success={xWAdobeAnimateBatchExportResult?.Success}, exporter={xWAdobeAnimateBatchExportResult?.ExporterPath}, dat={xWAdobeAnimateBatchExportResult?.DatPath}, error={xWAdobeAnimateBatchExportResult?.Error}");
		return CompleteImport(preparedImport, xWAdobeAnimateBatchExportResult);
	}

	private static PreparedImport PrepareImport(string sourcePath, string targetDirectory, string resourceName)
	{
		targetDirectory = NormalizeDirectory(targetDirectory);
		if (string.IsNullOrWhiteSpace(targetDirectory))
		{
			return PreparedFailure("Target animation directory is empty.");
		}
		string text = Path.GetExtension(ToAbsolutePath(sourcePath)).ToLowerInvariant();
		if (!(text == ".xfl") && !(text == ".fla"))
		{
			return PreparedFailure("Only Adobe Animate .xfl and .fla files can be imported as animation resources.");
		}
		if (!Godot.FileAccess.FileExists(sourcePath) && !File.Exists(ToAbsolutePath(sourcePath)))
		{
			return PreparedFailure("Adobe Animate source file not found: " + sourcePath);
		}
		Directory.CreateDirectory(ToAbsolutePath(targetDirectory));
		string name = (string.IsNullOrWhiteSpace(resourceName) ? Path.GetFileNameWithoutExtension(ToAbsolutePath(sourcePath)) : resourceName);
		name = XWTemplateLibrary.SanitizeName(name);
		name = MakeUniqueAnimationBaseName(targetDirectory, name);
		return new PreparedImport
		{
			Success = true,
			BaseName = name,
			ResourcePath = targetDirectory.PathJoin(name + ".tres"),
			DatPath = targetDirectory.PathJoin(name + ".dat")
		};
	}

	private static XWAdobeAnimateImportResult CompleteImport(PreparedImport prepared, XWAdobeAnimateBatchExportResult batchResult)
	{
		if (!batchResult.Success)
		{
			Trace("Import complete failed before saving resource: " + batchResult.Error);
			return Failure(batchResult.Error, batchResult);
		}
		string baseName = prepared.BaseName;
		string resourcePath = prepared.ResourcePath;
		string datPath = prepared.DatPath;
		Trace($"Import complete start: base={baseName}, resource={resourcePath}, dat={datPath}");
		AdobeAnimateData adobeAnimateData = new AdobeAnimateData();
		adobeAnimateData.ResourceName = baseName;
		adobeAnimateData.SetAnimeFileForModImport(datPath.GetFile());
		Error error = ResourceSaver.Save(adobeAnimateData, resourcePath, ResourceSaver.SaverFlags.None);
		Trace($"Import save resource: path={resourcePath}, result={error}");
		if (error != Error.Ok)
		{
			return Failure($"Animation resource save failed: {error}", batchResult);
		}
		XWFileSystem instance = XWFileSystem.Instance;
		if (instance != null && !string.IsNullOrWhiteSpace(instance.ProjectFolderPath))
		{
			instance.RegisterManifestPath(resourcePath);
			instance.RegisterManifestPath(datPath);
			Trace("Import scan Mod filesystem changes.");
			instance.ScanChanges();
		}
		Trace("Import complete success: resource=" + resourcePath + ", dat=" + datPath);
		return new XWAdobeAnimateImportResult
		{
			Success = true,
			ResourcePath = resourcePath,
			DatPath = datPath,
			BatchResult = batchResult
		};
	}

	private static PreparedImport PreparedFailure(string error)
	{
		return new PreparedImport
		{
			Success = false,
			Result = Failure(error)
		};
	}

	private static string MakeUniqueAnimationBaseName(string targetDirectory, string requestedBaseName)
	{
		string text = (string.IsNullOrWhiteSpace(requestedBaseName) ? "NewAnimation" : requestedBaseName);
		string text2 = text;
		int num = 1;
		while (AnimationPairExists(targetDirectory, text2))
		{
			text2 = text + num;
			num++;
		}
		return text2;
	}

	private static bool AnimationPairExists(string targetDirectory, string baseName)
	{
		string path = targetDirectory.PathJoin(baseName + ".tres");
		string path2 = targetDirectory.PathJoin(baseName + ".dat");
		if (!Godot.FileAccess.FileExists(path) && !Godot.FileAccess.FileExists(path2) && !File.Exists(ToAbsolutePath(path)))
		{
			return File.Exists(ToAbsolutePath(path2));
		}
		return true;
	}

	private static void Trace(string message)
	{
		XWAdobeAnimateTrace.Write(message);
	}

	private static XWAdobeAnimateImportResult Failure(string error, XWAdobeAnimateBatchExportResult batchResult = null)
	{
		return new XWAdobeAnimateImportResult
		{
			Success = false,
			Error = error,
			BatchResult = batchResult
		};
	}

	private static string NormalizeDirectory(string path)
	{
		path = (path ?? "").Replace('\\', '/');
		if (!string.IsNullOrEmpty(path) && !path.EndsWith("/"))
		{
			path += "/";
		}
		return path;
	}

	private static string ToAbsolutePath(string path)
	{
		path = (path ?? "").Replace('\\', '/');
		if (path.StartsWith("res://", StringComparison.OrdinalIgnoreCase) || path.StartsWith("user://", StringComparison.OrdinalIgnoreCase))
		{
			return ProjectSettings.GlobalizePath(path).Replace('\\', '/');
		}
		return path;
	}
}
