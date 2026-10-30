using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.ModSystem;
using PVZHE.ModEditor.Tools;

namespace PVZHE.ModEditor.Localization.GUI;

public partial class XWLocalizationEditorPanel : PanelContainer
{
	private Button _refreshButton;

	private Button _saveButton;

	private Button _addKeyButton;

	private Button _removeKeyButton;

	private LineEdit _addKeyLineEdit;

	private LineEdit _filterLineEdit;

	private Tree _localizationTree;

	private Label _localizationSummary;

	private XWLocalizationTable _localizationTable = new XWLocalizationTable();

	private readonly List<string> _visibleKeys = new List<string>();

	private string _projectPath = "";

	private string _csvPath = "";

	private bool _dirty;

	public void SetupCsvFile(string csvPath)
	{
		_csvPath = csvPath ?? "";
		if (IsInsideTree() && _localizationTree != null)
		{
			RefreshLocalizationTable();
		}
	}

	public override void _Ready()
	{
		_refreshButton = GetNode<Button>("%RefreshLocalizationTable");
		_saveButton = GetNode<Button>("%SaveLocalizationTable");
		_addKeyButton = GetNode<Button>("%AddLocalizationKey");
		_removeKeyButton = GetNode<Button>("%RemoveLocalizationKey");
		_addKeyLineEdit = GetNode<LineEdit>("%AddKeyLineEdit");
		_filterLineEdit = GetNode<LineEdit>("%FilterLineEdit");
		_localizationTree = GetNode<Tree>("%LocalizationTree");
		_localizationSummary = GetNode<Label>("%LocalizationSummary");
		ConfigureTree();
		_refreshButton.Pressed += RefreshLocalizationTable;
		_saveButton.Pressed += SaveLocalizationTable;
		_addKeyButton.Pressed += AddLocalizationKey;
		_removeKeyButton.Pressed += RemoveLocalizationKey;
		_filterLineEdit.TextChanged += delegate
		{
			PopulateLocalizationTree();
		};
		_addKeyLineEdit.TextSubmitted += delegate
		{
			AddLocalizationKey();
		};
		_localizationTree.ItemEdited += EditLocalizationEntry;
		RefreshLocalizationTable();
	}

	private void ConfigureTree()
	{
		_localizationTree.Columns = 4;
		_localizationTree.ColumnTitlesVisible = true;
		_localizationTree.HideRoot = true;
		_localizationTree.SetColumnTitle(0, "Key");
		_localizationTree.SetColumnTitle(1, "简体中文 zh_CN");
		_localizationTree.SetColumnTitle(2, "英语 en_US");
		_localizationTree.SetColumnTitle(3, "状态");
		_localizationTree.SetColumnExpand(0, expand: true);
		_localizationTree.SetColumnExpand(1, expand: true);
		_localizationTree.SetColumnExpand(2, expand: true);
		_localizationTree.SetColumnExpand(3, expand: false);
		_localizationTree.SetColumnCustomMinimumWidth(3, 72);
	}

	private void RefreshLocalizationTable()
	{
		_projectPath = GetCurrentProjectPath();
		if (!string.IsNullOrWhiteSpace(_csvPath))
		{
			_localizationTable = XWLocalizationTable.LoadFromCsvFile(_csvPath);
			if (!string.IsNullOrWhiteSpace(_projectPath))
			{
				_localizationTable.EnsureProjectRows(_projectPath);
			}
			_dirty = false;
			PopulateLocalizationTree();
		}
		else if (string.IsNullOrWhiteSpace(_projectPath))
		{
			_localizationTree.Clear();
			_localizationSummary.Text = "请先打开 Mod 工程。";
		}
		else
		{
			_localizationTable = XWLocalizationTable.LoadFromProject(_projectPath);
			_localizationTable.EnsureProjectRows(_projectPath);
			_dirty = false;
			PopulateLocalizationTree();
		}
	}

	private void SaveLocalizationTable()
	{
		_projectPath = GetCurrentProjectPath();
		if (!string.IsNullOrWhiteSpace(_csvPath))
		{
			_localizationTable.SaveToCsvFile(_csvPath, XWLocalizationTable.DefaultLocales);
			if (!string.IsNullOrWhiteSpace(_projectPath))
			{
				XWModManifestSyncService.RegisterPath(_projectPath, _csvPath);
			}
			int value = (string.IsNullOrWhiteSpace(_projectPath) ? null : XWModManifest.Load(Path.Combine(_projectPath, "mod.json"))).Translations?.Count ?? 0;
			_dirty = false;
			PopulateLocalizationTree();
			List<string> missingTranslations = _localizationTable.GetMissingTranslations(XWLocalizationTable.DefaultLocales);
			_localizationSummary.Text = $"已保存到 Localization/*.csv    Key: {_localizationTable.Entries.Count}    缺失: {missingTranslations.Count}    文件: {value}";
		}
		else if (string.IsNullOrWhiteSpace(_projectPath))
		{
			_localizationSummary.Text = "请先打开 Mod 工程。";
		}
		else
		{
			_localizationTable.SaveToProject(_projectPath, XWLocalizationTable.DefaultLocales);
			int value2 = XWModManifest.Load(Path.Combine(_projectPath, "mod.json")).Translations?.Count ?? 0;
			_dirty = false;
			PopulateLocalizationTree();
			List<string> missingTranslations2 = _localizationTable.GetMissingTranslations(XWLocalizationTable.DefaultLocales);
			_localizationSummary.Text = $"已保存到 Localization/*.csv    Key: {_localizationTable.Entries.Count}    缺失: {missingTranslations2.Count}    文件: {value2}";
		}
	}

	private void AddLocalizationKey()
	{
		string text = NormalizeLocalizationKey(_addKeyLineEdit.Text);
		if (string.IsNullOrWhiteSpace(text))
		{
			_localizationSummary.Text = "请输入有效的多语言 key。";
			return;
		}
		string[] defaultLocales = XWLocalizationTable.DefaultLocales;
		foreach (string text2 in defaultLocales)
		{
			if (!_localizationTable.Entries.TryGetValue(text, out var value) || !value.Values.ContainsKey(text2))
			{
				_localizationTable.Set(text, text2, "");
			}
		}
		_addKeyLineEdit.Text = "";
		_dirty = true;
		PopulateLocalizationTree(text);
	}

	private void RemoveLocalizationKey()
	{
		string selectedKey = GetSelectedKey();
		if (!string.IsNullOrWhiteSpace(selectedKey) && _localizationTable.Remove(selectedKey))
		{
			_dirty = true;
			PopulateLocalizationTree();
		}
	}

	private void PopulateLocalizationTree(string selectKey = "")
	{
		_visibleKeys.Clear();
		_localizationTree.Clear();
		TreeItem parent = _localizationTree.CreateItem();
		List<string> list = new List<string>(_localizationTable.Entries.Keys);
		list.Sort(StringComparer.OrdinalIgnoreCase);
		foreach (string item in list)
		{
			if (_localizationTable.Entries.TryGetValue(item, out var value) && MatchesFilter(value))
			{
				_visibleKeys.Add(item);
				TreeItem treeItem = _localizationTree.CreateItem(parent);
				treeItem.SetText(0, item);
				treeItem.SetText(1, value.Values.TryGetValue("zh_CN", out var value2) ? value2 : "");
				treeItem.SetText(2, value.Values.TryGetValue("en_US", out var value3) ? value3 : "");
				treeItem.SetText(3, IsLocalizationEntryMissing(value) ? "缺失" : "完整");
				treeItem.SetEditable(1, enabled: true);
				treeItem.SetEditable(2, enabled: true);
				treeItem.SetMetadata(0, item);
				treeItem.SetMetadata(1, item);
				treeItem.SetMetadata(2, item);
				if (IsLocalizationEntryMissing(value))
				{
					treeItem.SetCustomColor(3, new Color(1f, 0.62f, 0.25f));
				}
				if (!string.IsNullOrEmpty(selectKey) && item == selectKey)
				{
					treeItem.Select(0);
				}
			}
		}
		UpdateSummary();
	}

	private void EditLocalizationEntry()
	{
		TreeItem selected = _localizationTree.GetSelected();
		if (selected == null)
		{
			return;
		}
		int selectedColumn = _localizationTree.GetSelectedColumn();
		string text = selectedColumn switch
		{
			1 => "zh_CN", 
			2 => "en_US", 
			_ => "", 
		};
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		string text2 = selected.GetMetadata(0).AsString();
		if (string.IsNullOrWhiteSpace(text2))
		{
			return;
		}
		_localizationTable.Set(text2, text, selected.GetText(selectedColumn));
		_dirty = true;
		if (_localizationTable.Entries.TryGetValue(text2, out var value))
		{
			bool flag = IsLocalizationEntryMissing(value);
			selected.SetText(3, flag ? "缺失" : "完整");
			if (flag)
			{
				selected.SetCustomColor(3, new Color(1f, 0.62f, 0.25f));
			}
			else
			{
				selected.ClearCustomColor(3);
			}
		}
		UpdateSummary();
	}

	private void UpdateSummary()
	{
		List<string> missingTranslations = _localizationTable.GetMissingTranslations(XWLocalizationTable.DefaultLocales);
		string value = (_dirty ? "    未保存" : "");
		_localizationSummary.Text = $"Localization/*.csv    Key: {_localizationTable.Entries.Count}    显示: {_visibleKeys.Count}    缺失: {missingTranslations.Count}{value}";
	}

	private bool MatchesFilter(XWLocalizationTable.Entry entry)
	{
		string value = _filterLineEdit?.Text?.Trim() ?? "";
		if (string.IsNullOrWhiteSpace(value))
		{
			return true;
		}
		if (entry.Key.Contains(value, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		foreach (string value2 in entry.Values.Values)
		{
			if (!string.IsNullOrEmpty(value2) && value2.Contains(value, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	private string GetSelectedKey()
	{
		TreeItem selected = _localizationTree.GetSelected();
		if (selected == null)
		{
			return "";
		}
		return selected.GetMetadata(0).AsString();
	}

	private static bool IsLocalizationEntryMissing(XWLocalizationTable.Entry entry)
	{
		string[] defaultLocales = XWLocalizationTable.DefaultLocales;
		foreach (string key in defaultLocales)
		{
			if (!entry.Values.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
			{
				return true;
			}
		}
		return false;
	}

	private static string NormalizeLocalizationKey(string rawKey)
	{
		if (string.IsNullOrWhiteSpace(rawKey))
		{
			return "";
		}
		List<char> list = new List<char>(rawKey.Trim().Length);
		string text = rawKey.Trim();
		foreach (char c in text)
		{
			if (char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == '.')
			{
				list.Add(c);
			}
			else if (char.IsWhiteSpace(c) || c == '/' || c == '\\')
			{
				list.Add('.');
			}
		}
		return new string(list.ToArray()).Trim('.');
	}

	private static string GetCurrentProjectPath()
	{
		if (XWEditorInterface.Instance?.GetEditorPanel() is ModEditorPanel modEditorPanel)
		{
			return modEditorPanel.GetCurrentProject()?.ProjectPath ?? "";
		}
		return "";
	}
}
