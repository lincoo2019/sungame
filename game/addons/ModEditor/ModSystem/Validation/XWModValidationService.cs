using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor.Blueprint;
using PVZHE.ModEditor.ModSystem.References;
using PVZHE.ModEditor.ScriptEditor;

namespace PVZHE.ModEditor.ModSystem.Validation;

public sealed class XWModValidationService
{
	public async Task<List<XWValidationIssue>> ValidateProjectAsync(string projectPath)
	{
		try
		{
			return ValidateExport(await Task.Run(() => new XWModExportSnapshot(projectPath)));
		}
		catch (Exception error)
		{
			return InvalidProject(projectPath, error);
		}
	}

	public List<XWValidationIssue> ValidateProject(string projectPath)
	{
		try
		{
			return ValidateExport(new XWModExportSnapshot(projectPath));
		}
		catch (Exception error)
		{
			return InvalidProject(projectPath, error);
		}
	}

	private static List<XWValidationIssue> InvalidProject(string path, Exception error)
	{
		return new List<XWValidationIssue>
		{
			new XWValidationIssue
			{
				Code = XWValidationIssue.IssueCode.ManifestError,
				Message = error.Message,
				FilePath = Path.Combine(path, "mod.json"),
				JumpTarget = Path.Combine(path, "mod.json")
			}
		};
	}

	public List<XWValidationIssue> ValidateExport(XWModExportSnapshot snapshot)
	{
		List<XWValidationIssue> list = new List<XWValidationIssue>();
		ValidateDuplicateKeys(snapshot.Manifest, list);
		ValidateDependencyConflict(snapshot.Manifest, list);
		if (ResolveExistingScriptFiles(snapshot.Root, snapshot.Manifest).Count > 0)
		{
			ValidateDotNetToolchain(list);
		}
		ValidateBlueprintGeneration(snapshot.Root, snapshot.Manifest, list);
		XWModProjectContentValidation.Validate(snapshot, list);
		return list;
	}

	private static void ValidateResources(string projectPath, XWModManifest manifest, List<XWValidationIssue> issues)
	{
		foreach (string item in EnumerateDeclaredFiles(manifest))
		{
			string text = Path.Combine(projectPath, item.Replace('/', Path.DirectorySeparatorChar));
			if (!File.Exists(text))
			{
				issues.Add(new XWValidationIssue
				{
					Code = XWValidationIssue.IssueCode.MissingResource,
					Message = "MissingResource: " + item,
					FilePath = text,
					JumpTarget = text
				});
			}
		}
		IEnumerable<string> enumerable2;
		if (!Directory.Exists(projectPath))
		{
			IEnumerable<string> enumerable = Array.Empty<string>();
			enumerable2 = enumerable;
		}
		else
		{
			enumerable2 = Directory.EnumerateFiles(projectPath, "*", SearchOption.AllDirectories);
		}
		foreach (string item2 in enumerable2)
		{
			if (!IsTextResource(item2))
			{
				continue;
			}
			string input;
			try
			{
				input = File.ReadAllText(item2);
			}
			catch
			{
				continue;
			}
			foreach (Match item3 in Regex.Matches(input, "uid://[A-Za-z0-9_]+"))
			{
				string value = item3.Value;
				if (!ResourceLoader.Exists(value))
				{
					issues.Add(new XWValidationIssue
					{
						Code = XWValidationIssue.IssueCode.InvalidUid,
						Message = "InvalidUid: " + value,
						FilePath = item2,
						JumpTarget = item2
					});
				}
			}
		}
	}

	private static IEnumerable<string> EnumerateDeclaredFiles(XWModManifest manifest)
	{
		foreach (string resource in manifest.Resources)
		{
			yield return resource;
		}
		foreach (string script in manifest.Scripts)
		{
			yield return script;
		}
		foreach (string blueprint in manifest.Blueprints)
		{
			yield return blueprint;
		}
		foreach (string translation in manifest.Translations)
		{
			yield return translation;
		}
	}

	private static void ValidateReferenceGraph(string projectPath, List<XWValidationIssue> issues)
	{
		foreach (KeyValuePair<string, List<string>> missingReference in new XWReferenceGraphService().BuildForProject(projectPath).MissingReferences)
		{
			foreach (string item in missingReference.Value)
			{
				issues.Add(new XWValidationIssue
				{
					Code = (item.StartsWith("uid://") ? XWValidationIssue.IssueCode.InvalidUid : XWValidationIssue.IssueCode.MissingResource),
					Message = (item.StartsWith("uid://") ? ("InvalidUid: " + item) : ("MissingResource: " + item)),
					FilePath = missingReference.Key,
					JumpTarget = missingReference.Key,
					ResourceKey = item
				});
			}
		}
	}

	private static void ValidateDuplicateKeys(XWModManifest manifest, List<XWValidationIssue> issues)
	{
		foreach (KeyValuePair<string, List<string>> provide in manifest.Provides)
		{
			HashSet<string> hashSet = new HashSet<string>();
			foreach (string item in provide.Value)
			{
				if (!hashSet.Add(item))
				{
					issues.Add(new XWValidationIssue
					{
						Code = XWValidationIssue.IssueCode.DuplicateKey,
						Message = "DuplicateKey: " + provide.Key + "/" + item,
						ResourceKey = item
					});
				}
			}
		}
	}

	private static void ValidateDependencyConflict(XWModManifest manifest, List<XWValidationIssue> issues)
	{
		HashSet<string> hashSet = new HashSet<string>();
		foreach (XWModDependency dependency in manifest.Dependencies)
		{
			hashSet.Add(dependency.Id);
		}
		foreach (XWModDependency conflict in manifest.Conflicts)
		{
			if (hashSet.Contains(conflict.Id))
			{
				issues.Add(new XWValidationIssue
				{
					Code = XWValidationIssue.IssueCode.DependencyConflict,
					Message = "DependencyConflict: " + conflict.Id,
					ResourceKey = conflict.Id
				});
			}
		}
	}

	private static void ValidateDotNetToolchain(List<XWValidationIssue> issues)
	{
		if (!XWDotNetToolchainLocator.Locate().Exists)
		{
			issues.Add(new XWValidationIssue
			{
				Code = XWValidationIssue.IssueCode.DotNetToolchainMissing,
				Message = "此工程包含 C#，请安装 .NET SDK 或配套 BuildTools 后导出。"
			});
		}
	}

	private static void ValidateScriptCompilation(string projectPath, XWModManifest manifest, List<XWValidationIssue> issues)
	{
		List<string> list = ResolveExistingScriptFiles(projectPath, manifest);
		if (list.Count == 0)
		{
			return;
		}
		XWInGameDotNetBuildService.BuildResult buildResult = new XWInGameDotNetBuildService().CreateModProject(string.IsNullOrWhiteSpace(manifest.Id) ? Path.GetFileName(projectPath) : manifest.Id, projectPath, list);
		if (buildResult.Success)
		{
			return;
		}
		if (buildResult.Diagnostics.Count == 0)
		{
			issues.Add(new XWValidationIssue
			{
				Code = XWValidationIssue.IssueCode.ScriptCompileFailed,
				Message = "ScriptCompileFailed: C# build failed without compiler diagnostics.",
				FilePath = buildResult.ProjectPath,
				JumpTarget = buildResult.ProjectPath
			});
			return;
		}
		foreach (XWCodeErrorChecker.ErrorData diagnostic in buildResult.Diagnostics)
		{
			issues.Add(new XWValidationIssue
			{
				Level = ((diagnostic.SeverityLevel == XWCodeErrorChecker.Severity.Warning) ? XWValidationIssue.Severity.Warning : XWValidationIssue.Severity.Error),
				Code = XWValidationIssue.IssueCode.ScriptCompileFailed,
				Message = "ScriptCompileFailed: " + diagnostic.Message,
				FilePath = diagnostic.FilePath,
				Line = diagnostic.Line,
				Column = diagnostic.Column,
				JumpTarget = diagnostic.FilePath
			});
		}
	}

	private static List<string> ResolveExistingScriptFiles(string projectPath, XWModManifest manifest)
	{
		List<string> list = new List<string>();
		foreach (string script in manifest.Scripts)
		{
			if (!string.IsNullOrWhiteSpace(script) && script.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
			{
				string text = Path.Combine(projectPath, script.Replace('/', Path.DirectorySeparatorChar));
				if (File.Exists(text))
				{
					list.Add(text);
				}
			}
		}
		return list;
	}

	private static void ValidateBlueprintGeneration(string projectPath, XWModManifest manifest, List<XWValidationIssue> issues)
	{
		foreach (string blueprint in manifest.Blueprints)
		{
			if (string.IsNullOrWhiteSpace(blueprint))
			{
				issues.Add(new XWValidationIssue
				{
					Code = XWValidationIssue.IssueCode.BlueprintGenerationFailed,
					Message = "BlueprintGenerationFailed: empty blueprint path."
				});
				continue;
			}
			string text = Path.Combine(projectPath, blueprint.Replace('/', Path.DirectorySeparatorChar));
			if (!File.Exists(text))
			{
				continue;
			}
			XWBPScript xWBPScript;
			try
			{
				xWBPScript = ResourceLoader.Load<XWBPScript>(text, null, ResourceLoader.CacheMode.IgnoreDeep);
			}
			catch (Exception ex)
			{
				issues.Add(new XWValidationIssue
				{
					Code = XWValidationIssue.IssueCode.BlueprintGenerationFailed,
					Message = "BlueprintGenerationFailed: failed to load " + blueprint + ": " + ex.Message,
					FilePath = text,
					JumpTarget = text,
					ResourceKey = blueprint
				});
				continue;
			}
			if (!GodotObject.IsInstanceValid(xWBPScript))
			{
				issues.Add(new XWValidationIssue
				{
					Code = XWValidationIssue.IssueCode.BlueprintGenerationFailed,
					Message = "BlueprintGenerationFailed: " + blueprint + " is not a valid XWBPScript resource.",
					FilePath = text,
					JumpTarget = text,
					ResourceKey = blueprint
				});
				continue;
			}
			try
			{
				foreach (XWBPValidationResult item in new XWBPValidator(xWBPScript.Deserialize()).Validate())
				{
					issues.Add(ConvertBlueprintValidationResult(blueprint, text, item));
				}
			}
			catch (Exception ex2)
			{
				issues.Add(new XWValidationIssue
				{
					Code = XWValidationIssue.IssueCode.BlueprintGenerationFailed,
					Message = "BlueprintGenerationFailed: " + blueprint + ": " + ex2.Message,
					FilePath = text,
					JumpTarget = text,
					ResourceKey = blueprint
				});
			}
		}
	}

	private static XWValidationIssue ConvertBlueprintValidationResult(string blueprintPath, string fullPath, XWBPValidationResult blueprintResult)
	{
		string text = blueprintResult.GraphData?.Name ?? "";
		string text2 = ((blueprintResult.NodeId >= 0) ? $"{blueprintPath}#node:{blueprintResult.NodeId}" : blueprintPath);
		if (!string.IsNullOrWhiteSpace(text))
		{
			text2 = text2 + "@" + text;
		}
		return new XWValidationIssue
		{
			Level = ConvertBlueprintSeverity(blueprintResult.ResultSeverity),
			Code = XWValidationIssue.IssueCode.BlueprintGenerationFailed,
			Message = $"BlueprintGenerationFailed: {blueprintResult.TypeError}: {blueprintResult.Message}",
			FilePath = fullPath,
			JumpTarget = fullPath,
			ResourceKey = text2
		};
	}

	private static XWValidationIssue.Severity ConvertBlueprintSeverity(XWBPValidationResult.Severity severity)
	{
		return severity switch
		{
			XWBPValidationResult.Severity.Warning => XWValidationIssue.Severity.Warning, 
			XWBPValidationResult.Severity.Info => XWValidationIssue.Severity.Info, 
			_ => XWValidationIssue.Severity.Error, 
		};
	}

	private static bool IsTextResource(string file)
	{
		switch (Path.GetExtension(file).ToLowerInvariant())
		{
		case ".tscn":
		case ".tres":
		case ".json":
		case ".cs":
		case ".gd":
		case ".cfg":
		case ".txt":
			return true;
		default:
			return false;
		}
	}
}
