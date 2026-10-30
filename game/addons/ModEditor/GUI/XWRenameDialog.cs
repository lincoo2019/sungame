using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Godot;

namespace PVZHE.ModEditor.GUI;

public partial class XWRenameDialog : ConfirmationDialog
{
	[Signal]
	public delegate void RenameConfirmedEventHandler(string[] oldNames, string[] newNames);

	private LineEdit _searchEdit;

	private LineEdit _replaceEdit;

	private CheckBox _regexCheck;

	private LineEdit _prefixEdit;

	private LineEdit _suffixEdit;

	private SpinBox _counterStart;

	private SpinBox _counterStep;

	private SpinBox _counterPadding;

	private ItemList _previewList;

	private string[] _targets = Array.Empty<string>();

	public override void _Ready()
	{
		_searchEdit = GetNode<LineEdit>("%SearchEdit");
		_replaceEdit = GetNode<LineEdit>("%ReplaceEdit");
		_regexCheck = GetNode<CheckBox>("%RegexCheck");
		_prefixEdit = GetNode<LineEdit>("%PrefixEdit");
		_suffixEdit = GetNode<LineEdit>("%SuffixEdit");
		_counterStart = GetNode<SpinBox>("%CounterStart");
		_counterStep = GetNode<SpinBox>("%CounterStep");
		_counterPadding = GetNode<SpinBox>("%CounterPadding");
		_previewList = GetNode<ItemList>("%PreviewList");
		_searchEdit.TextChanged += delegate
		{
			UpdatePreview();
		};
		_replaceEdit.TextChanged += delegate
		{
			UpdatePreview();
		};
		_regexCheck.Pressed += UpdatePreview;
		_prefixEdit.TextChanged += delegate
		{
			UpdatePreview();
		};
		_suffixEdit.TextChanged += delegate
		{
			UpdatePreview();
		};
		_counterStart.ValueChanged += delegate
		{
			UpdatePreview();
		};
		_counterStep.ValueChanged += delegate
		{
			UpdatePreview();
		};
		_counterPadding.ValueChanged += delegate
		{
			UpdatePreview();
		};
		base.Confirmed += OnConfirmed;
	}

	public void SetTargets(string[] names)
	{
		_targets = names ?? Array.Empty<string>();
		UpdatePreview();
	}

	private void UpdatePreview()
	{
		_previewList.Clear();
		int num = (int)_counterStart.Value;
		int num2 = (int)_counterStep.Value;
		int num3 = (int)_counterPadding.Value;
		string text = _searchEdit.Text;
		string text2 = _replaceEdit.Text;
		string text3 = _prefixEdit.Text;
		string text4 = _suffixEdit.Text;
		string[] targets = _targets;
		foreach (string text5 in targets)
		{
			string text6 = text5;
			if (!string.IsNullOrEmpty(text))
			{
				if (_regexCheck.ButtonPressed)
				{
					try
					{
						text6 = Regex.Replace(text6, text, text2);
					}
					catch
					{
					}
				}
				else
				{
					text6 = text6.Replace(text, text2);
				}
			}
			text6 = text3 + text6 + text4;
			if (num3 > 0)
			{
				text6 += num.ToString("D" + num3);
			}
			_previewList.AddItem(text5 + "  →  " + text6);
			num += num2;
		}
	}

	private void OnConfirmed()
	{
		List<string> list = new List<string>();
		List<string> list2 = new List<string>();
		int num = (int)_counterStart.Value;
		int num2 = (int)_counterStep.Value;
		int num3 = (int)_counterPadding.Value;
		string text = _searchEdit.Text;
		string text2 = _replaceEdit.Text;
		string text3 = _prefixEdit.Text;
		string text4 = _suffixEdit.Text;
		string[] targets = _targets;
		foreach (string text5 in targets)
		{
			string text6 = text5;
			if (!string.IsNullOrEmpty(text))
			{
				if (_regexCheck.ButtonPressed)
				{
					try
					{
						text6 = Regex.Replace(text6, text, text2);
					}
					catch
					{
					}
				}
				else
				{
					text6 = text6.Replace(text, text2);
				}
			}
			text6 = text3 + text6 + text4;
			if (num3 > 0)
			{
				text6 += num.ToString("D" + num3);
			}
			list.Add(text5);
			list2.Add(text6);
			num += num2;
		}
		EmitSignal(SignalName.RenameConfirmed, list.ToArray(), list2.ToArray());
	}
}
