using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using PVZHE.ModEditor.Blueprint;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.ModSystem.Validation;
using PVZHE.ModEditor.ResourceEditors;
using PVZHE.ModEditor.ScriptEditor;

namespace PVZHE.ModEditor.Issues.GUI;

public partial class XWIssuePanel : PanelContainer
{
	private readonly List<XWValidationIssue> _issues = new List<XWValidationIssue>();

	private Tree _tree;

	private Label _summary;

	public override void _Ready()
	{
		_summary = GetNodeOrNull<Label>("Root/Summary");
		_tree = GetNodeOrNull<Tree>("Root/IssueTree");
		if (_tree != null)
		{
			_tree.Columns = 5;
			_tree.HideRoot = true;
			_tree.SetColumnTitle(0, "级别");
			_tree.SetColumnTitle(1, "类型");
			_tree.SetColumnTitle(2, "资源");
			_tree.SetColumnTitle(3, "位置");
			_tree.SetColumnTitle(4, "说明");
			_tree.ColumnTitlesVisible = true;
			_tree.ItemActivated += OnIssueActivated;
		}
		Render();
	}

	public void LoadIssues(IEnumerable<XWValidationIssue> issues)
	{
		_issues.Clear();
		if (issues != null)
		{
			_issues.AddRange(issues);
		}
		Render();
	}

	public void ClearIssues()
	{
		_issues.Clear();
		Render();
	}

	public void OpenIssue(XWValidationIssue issue)
	{
		if (issue == null)
		{
			return;
		}
		string text = ((!string.IsNullOrWhiteSpace(issue.JumpTarget)) ? issue.JumpTarget : issue.FilePath);
		if (string.IsNullOrWhiteSpace(text))
		{
			XWEditorInterface.Instance?.ShowToast(issue.Message);
			return;
		}
		if (Path.GetExtension(text).ToLowerInvariant() == ".cs")
		{
			XWScriptEditor xWScriptEditor = XWEditorInterface.Instance?.GetScriptEditor();
			if (xWScriptEditor != null)
			{
				if (xWScriptEditor.TryOpenFileAt(text, issue.Line, issue.Column))
				{
					XWEditorInterface.Instance?.FocusPanel("script_editor");
				}
				return;
			}
		}
		string text2 = NormalizeResourcePath(text);
		if (!OpenBlueprintIssue(issue, text2) && !XWResourceEditorRegistry.TryOpenPath(text2))
		{
			if (ResourceLoader.Exists(text2))
			{
				Resource resource = ResourceLoader.Load<Resource>(text2, null, ResourceLoader.CacheMode.Reuse);
				XWEditorInterface.Instance?.EditResource(resource, XWResourceEditContext.ForRoot(resource, text2, "resource_editor"));
				return;
			}
			XWEditorInterface.Instance?.ShowToast($"{issue.Code}: {text}:{issue.Line + 1}:{issue.Column + 1}");
		}
	}

	private void Render()
	{
		if (_summary != null)
		{
			_summary.Text = $"问题: {_issues.Count}";
		}
		if (_tree != null)
		{
			_tree.Clear();
			TreeItem parent = _tree.CreateItem();
			for (int i = 0; i < _issues.Count; i++)
			{
				XWValidationIssue xWValidationIssue = _issues[i];
				TreeItem treeItem = _tree.CreateItem(parent);
				treeItem.SetText(0, xWValidationIssue.Level.ToString());
				treeItem.SetText(1, xWValidationIssue.Code.ToString());
				treeItem.SetText(2, xWValidationIssue.ResourceKey);
				treeItem.SetText(3, BuildLocation(xWValidationIssue));
				treeItem.SetText(4, xWValidationIssue.Message);
				treeItem.SetMetadata(0, i);
			}
		}
	}

	private void OnIssueActivated()
	{
		if (_tree == null)
		{
			return;
		}
		TreeItem selected = _tree.GetSelected();
		if (selected == null)
		{
			return;
		}
		Variant metadata = selected.GetMetadata(0);
		if (metadata.VariantType == Variant.Type.Int)
		{
			int num = metadata.AsInt32();
			if (num >= 0 && num < _issues.Count)
			{
				OpenIssue(_issues[num]);
			}
		}
	}

	private static string BuildLocation(XWValidationIssue issue)
	{
		string text = ((!string.IsNullOrWhiteSpace(issue.FilePath)) ? issue.FilePath : issue.JumpTarget);
		string text2 = (string.IsNullOrWhiteSpace(text) ? "" : text.Replace('\\', '/').GetFile());
		if (issue.Line >= 0)
		{
			return $"{text2}:{issue.Line + 1}:{issue.Column + 1}";
		}
		return text2;
	}

	private static bool OpenBlueprintIssue(XWValidationIssue issue, string target)
	{
		if (string.IsNullOrWhiteSpace(target))
		{
			return false;
		}
		string text = Path.GetExtension(target).ToLowerInvariant();
		if (!(text == ".tres") && !(text == ".res"))
		{
			return false;
		}
		if (!ResourceLoader.Exists(target))
		{
			return false;
		}
		XWBPScript xWBPScript;
		try
		{
			xWBPScript = ResourceLoader.Load<XWBPScript>(target, null, ResourceLoader.CacheMode.Reuse);
		}
		catch
		{
			return false;
		}
		if (!GodotObject.IsInstanceValid(xWBPScript))
		{
			return false;
		}
		if (!(XWEditorInterface.Instance?.GetBlueprintEditor() is XWBPEditor xWBPEditor))
		{
			return false;
		}
		xWBPEditor.Init(xWBPScript);
		int blueprintNodeId = GetBlueprintNodeId(issue.ResourceKey);
		if (blueprintNodeId >= 0)
		{
			xWBPEditor.FocusNode(blueprintNodeId);
		}
		XWEditorInterface.Instance?.FocusPanel("bp_editor");
		return true;
	}

	private static int GetBlueprintNodeId(string resourceKey)
	{
		if (string.IsNullOrWhiteSpace(resourceKey))
		{
			return -1;
		}
		int num = resourceKey.IndexOf("#node:", StringComparison.OrdinalIgnoreCase);
		if (num < 0)
		{
			return -1;
		}
		int num2 = num + "#node:".Length;
		int i;
		for (i = num2; i < resourceKey.Length && char.IsDigit(resourceKey[i]); i++)
		{
		}
		int num3 = num2;
		if (!int.TryParse(resourceKey.Substring(num3, i - num3), out var result))
		{
			return -1;
		}
		return result;
	}

	private static string NormalizeResourcePath(string path)
	{
		if (string.IsNullOrWhiteSpace(path) || path.StartsWith("res://") || path.StartsWith("uid://"))
		{
			return path;
		}
		string text = ProjectSettings.GlobalizePath("res://").TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		string fullPath;
		try
		{
			fullPath = Path.GetFullPath(path);
		}
		catch
		{
			return path;
		}
		if (!fullPath.StartsWith(text))
		{
			return path;
		}
		string text2 = fullPath.Substring(text.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		return "res://" + text2.Replace('\\', '/');
	}
}
