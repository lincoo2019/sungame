using System;
using Godot;
using PVZHE.ModEditor.Blueprint;
using PVZHE.ModEditor.Core;

namespace PVZHE.ModEditor.GUI;

public partial class XWBlueprintCreateDialog : ConfirmationDialog
{
	[Signal]
	public delegate void BlueprintCreatedEventHandler(string blueprintPath);

	private const string ScenePath = "res://addons/ModEditor/GUI/XWBlueprintCreateDialog.tscn";

	private LineEdit _parentName;

	private LineEdit _filePath;

	private Label _messageLabel;

	private Button _parentSearchButton;

	private Button _pathButton;

	private FileDialog _fileDialog;

	private Button _nodeParentCard;

	private Button _node2DParentCard;

	private Button _controlParentCard;

	private Button _resourceParentCard;

	private string _pendingParentClass = "Node";

	private string _pendingPath = "";

	private bool _isPathValid;

	public static XWBlueprintCreateDialog Create()
	{
		return ResourceLoader.Load<PackedScene>("res://addons/ModEditor/GUI/XWBlueprintCreateDialog.tscn", null, ResourceLoader.CacheMode.Reuse).Instantiate<XWBlueprintCreateDialog>(PackedScene.GenEditState.Disabled);
	}

	public override void _Ready()
	{
		_parentName = GetNode<LineEdit>("%ParentName");
		_filePath = GetNode<LineEdit>("%FilePath");
		_messageLabel = GetNode<Label>("%MsgLabel");
		_parentSearchButton = GetNode<Button>("%ParentSearchBtn");
		_pathButton = GetNode<Button>("%PathBtn");
		_fileDialog = GetNode<FileDialog>("%PathDialog");
		_nodeParentCard = GetNode<Button>("%NodeParentCard");
		_node2DParentCard = GetNode<Button>("%Node2DParentCard");
		_controlParentCard = GetNode<Button>("%ControlParentCard");
		_resourceParentCard = GetNode<Button>("%ResourceParentCard");
		_fileDialog.Title = "选择蓝图保存路径";
		_fileDialog.FileMode = FileDialog.FileModeEnum.SaveFile;
		_fileDialog.Access = FileDialog.AccessEnum.Filesystem;
		_fileDialog.ClearFilters();
		_fileDialog.AddFilter("*.tres", "蓝图资源");
		base.Confirmed += OnConfirmed;
		base.CloseRequested += base.QueueFree;
		_parentName.TextChanged += delegate
		{
			UpdateDialogState();
		};
		_parentName.TextSubmitted += delegate
		{
			TryCreate();
		};
		_parentSearchButton.Pressed += OnParentSearchPressed;
		_filePath.TextChanged += delegate
		{
			UpdateDialogState();
		};
		_filePath.TextSubmitted += delegate
		{
			TryCreate();
		};
		_pathButton.Pressed += OnBrowsePathPressed;
		_fileDialog.FileSelected += OnPathSelected;
		_nodeParentCard.Pressed += delegate
		{
			SelectParentPreset("Node");
		};
		_node2DParentCard.Pressed += delegate
		{
			SelectParentPreset("Node2D");
		};
		_controlParentCard.Pressed += delegate
		{
			SelectParentPreset("Control");
		};
		_resourceParentCard.Pressed += delegate
		{
			SelectParentPreset("Resource");
		};
		ApplyPendingConfig();
		UpdateDialogState();
	}

	public void Config(string parentClass, string path)
	{
		_pendingParentClass = (string.IsNullOrWhiteSpace(parentClass) ? "Node" : parentClass.Trim());
		_pendingPath = path ?? "";
		if (_parentName != null)
		{
			ApplyPendingConfig();
			UpdateDialogState();
		}
	}

	private void ApplyPendingConfig()
	{
		_parentName.Text = _pendingParentClass;
		_filePath.Text = _pendingPath;
		_parentName.Deselect();
		_filePath.Deselect();
	}

	private void OnParentSearchPressed()
	{
		XWWindowExtendsClassSelector xWWindowExtendsClassSelector = XWWindowExtendsClassSelector.Create();
		if (!GodotObject.IsInstanceValid(xWWindowExtendsClassSelector))
		{
			XWEditorInterface.Instance?.ShowToast("类型选择器未就绪");
			return;
		}
		xWWindowExtendsClassSelector.Title = "选择蓝图父类";
		xWWindowExtendsClassSelector.OkButtonText = "继承";
		xWWindowExtendsClassSelector.ClassSelected += delegate(StringName typeName)
		{
			_parentName.Text = typeName.ToString();
			UpdateDialogState();
		};
		AddChild(xWWindowExtendsClassSelector, forceReadableName: false, InternalMode.Disabled);
		xWWindowExtendsClassSelector.PopupCentered();
	}

	private void SelectParentPreset(string parentClass)
	{
		_parentName.Text = parentClass;
		UpdateDialogState();
	}

	private void OnBrowsePathPressed()
	{
		string text = NormalizePath(_filePath.Text);
		if (!string.IsNullOrEmpty(text))
		{
			_fileDialog.CurrentPath = text;
		}
		_fileDialog.PopupCentered();
	}

	private void OnPathSelected(string path)
	{
		_filePath.Text = path;
		UpdateDialogState();
	}

	private void UpdateDialogState()
	{
		string text = Validate(out _isPathValid);
		bool flag = !string.IsNullOrWhiteSpace(_parentName.Text);
		GetOkButton().Disabled = !flag || !_isPathValid;
		_messageLabel.Text = text;
		_messageLabel.Visible = !string.IsNullOrEmpty(text);
		UpdateParentPresetSelection();
	}

	private void UpdateParentPresetSelection()
	{
		if (GodotObject.IsInstanceValid(_nodeParentCard))
		{
			string a = _parentName.Text.Trim();
			_nodeParentCard.ButtonPressed = string.Equals(a, "Node", StringComparison.Ordinal);
			_node2DParentCard.ButtonPressed = string.Equals(a, "Node2D", StringComparison.Ordinal);
			_controlParentCard.ButtonPressed = string.Equals(a, "Control", StringComparison.Ordinal);
			_resourceParentCard.ButtonPressed = string.Equals(a, "Resource", StringComparison.Ordinal);
		}
	}

	private string Validate(out bool validPath)
	{
		validPath = false;
		if (string.IsNullOrWhiteSpace(_parentName?.Text))
		{
			return "请输入继承的父类。";
		}
		string text = NormalizePath(_filePath?.Text ?? "");
		if (string.IsNullOrWhiteSpace(text))
		{
			return "请输入蓝图文件路径。";
		}
		string file = text.GetFile();
		if (string.IsNullOrWhiteSpace(file))
		{
			return "路径缺少文件名。";
		}
		string baseName = file.GetBaseName();
		if (string.IsNullOrWhiteSpace(baseName) || !IsValidFileName(baseName))
		{
			return "文件名无效。";
		}
		validPath = true;
		return "";
	}

	private void OnConfirmed()
	{
		TryCreate();
	}

	private void TryCreate()
	{
		UpdateDialogState();
		if (_isPathValid && !string.IsNullOrWhiteSpace(_parentName.Text))
		{
			string text = NormalizePath(_filePath.Text);
			XWBlueprintCreationService.Result result = XWBlueprintCreationService.Create(text, _parentName.Text.Trim(), text.GetFile().GetBaseName());
			if (!result.Success)
			{
				ShowError(result.Error);
				return;
			}
			EmitSignal(SignalName.BlueprintCreated, result.CreatedPath);
			QueueFree();
		}
	}

	private void ShowError(string message)
	{
		_messageLabel.Text = message;
		_messageLabel.Visible = true;
		XWEditorInterface.Instance?.ShowToast(message);
		PopupCentered();
	}

	private static string NormalizePath(string path)
	{
		path = (path ?? "").Trim().Replace('\\', '/');
		if (string.IsNullOrEmpty(path))
		{
			return "";
		}
		if (!path.EndsWith(".tres", StringComparison.OrdinalIgnoreCase))
		{
			path += ".tres";
		}
		return path;
	}

	private static bool IsValidFileName(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return false;
		}
		foreach (char c in value)
		{
			bool flag = c < ' ';
			if (!flag)
			{
				bool flag2;
				switch (c)
				{
				case '"':
				case '*':
				case '/':
				case ':':
				case '<':
				case '>':
				case '?':
				case '\\':
				case '|':
					flag2 = true;
					break;
				default:
					flag2 = false;
					break;
				}
				flag = flag2;
			}
			if (flag)
			{
				return false;
			}
		}
		return true;
	}
}
