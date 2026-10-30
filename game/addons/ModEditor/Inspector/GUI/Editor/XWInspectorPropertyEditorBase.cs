using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Core;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorPropertyEditorBase : PanelContainer
{
	[Signal]
	public delegate void ValueChangedEventHandler(GodotObject obj, StringName property, StringName field, Variant value);

	public Label PropertyNameLabel;

	public Button ResetButton;

	protected HBoxContainer InformationContainner;

	protected Control PropertyEditor;

	internal const float InspectorPropertyHeight = 28f;

	public XWInspectorProperty Property;

	public StringName Field = "";

	public bool PropertyNameLock;

	public XWUndoRedoManager UndoRedoManager;

	public Variant LastValue;

	private bool _hover;

	private bool _isMixedValue;

	private Button _keyButton;

	private readonly List<Control> _continuousInputControls = new List<Control>();

	private readonly HashSet<ulong> _continuousInputControlIds = new HashSet<ulong>();

	private readonly HashSet<ulong> _spinBoxTextPreviewIds = new HashSet<ulong>();

	private bool _continuousEditActive;

	private GodotObject _continuousEditObject;

	private StringName _continuousEditProperty = "";

	private StringName _continuousEditRootProperty = "";

	private StringName _continuousEditField = "";

	private Variant _continuousEditOldValue;

	private Variant _continuousEditPreviewValue;

	private bool _continuousEditHasPreview;

	private bool _refreshingEditorValue;

	private bool _continuousBindingPassPending;

	private static Texture2D _iconReload;

	private static Texture2D _iconPin;

	private static Texture2D _iconFavorite;

	private static Texture2D _iconCopy;

	private static Texture2D _iconPaste;

	private static Texture2D _iconKey;

	[Export(PropertyHint.None, "")]
	public NodePath PropertyEditorPath { get; set; }

	public bool Select { get; set; }

	public bool IsInner { get; set; }

	public int HistoryTypeOverride { get; set; } = -1;

	public bool IsContinuousEditActive => _continuousEditActive;

	public XWInspectorPropertyEditorBase ContinuousEditOwner { get; set; }

	public int ContinuousInputControlCount => _continuousInputControls.Count;

	public string ContinuousBindingRootDescription
	{
		get
		{
			if (!GodotObject.IsInstanceValid(this))
			{
				return "invalid";
			}
			return $"{Name}:{GetType().Name}:children={GetChildCount()}";
		}
	}

	public float SplitRatio { get; set; } = 0.5f;

	public bool IsResourceEditor { get; protected set; }

	private static void LoadIcons()
	{
		_iconReload = XWTextureSafety.SafeIcon(ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/IconReload.svg", null, ResourceLoader.CacheMode.Reuse));
		_iconPin = XWTextureSafety.SafeIcon(ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/Pin.svg", null, ResourceLoader.CacheMode.Reuse));
		_iconFavorite = XWTextureSafety.SafeIcon(ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/Favorites.svg", null, ResourceLoader.CacheMode.Reuse));
		_iconCopy = XWTextureSafety.SafeIcon(ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/ActionCopy.svg", null, ResourceLoader.CacheMode.Reuse));
		_iconPaste = XWTextureSafety.SafeIcon(ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/ActionPaste.svg", null, ResourceLoader.CacheMode.Reuse));
		_iconKey = XWTextureSafety.SafeIcon(ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/ClassIcon/Key.svg", null, ResourceLoader.CacheMode.Reuse));
	}

	public override void _Ready()
	{
		if (_iconReload == null)
		{
			LoadIcons();
		}
		PropertyNameLabel = GetNode<Label>("%PropertyNameLabel");
		ResetButton = GetNode<Button>("%ResetButton");
		InformationContainner = GetNode<HBoxContainer>("%InformationContainner");
		if (PropertyEditorPath != null && !PropertyEditorPath.IsEmpty)
		{
			PropertyEditor = GetNode<Control>(PropertyEditorPath);
		}
		else
		{
			PropertyEditor = GetNodeOrNull<Control>("%PropertyEditor");
		}
		ResetButton.Pressed += ResetButtonPressed;
		CreateKeyButton();
		NormalizePropertyEditorLayout();
		ApplySplitRatio();
		base.GuiInput += OnGuiInput;
		base.MouseEntered += OnMouseEntered;
		base.MouseExited += OnMouseExited;
		_continuousBindingPassPending = true;
		SetProcess(enable: true);
	}

	public override void _Process(double delta)
	{
		if (_continuousBindingPassPending)
		{
			BindContinuousInputControls();
			_continuousBindingPassPending = false;
			SetProcess(enable: false);
		}
	}

	public override void _ExitTree()
	{
		CancelContinuousEdit();
		base._ExitTree();
	}

	private void CreateKeyButton()
	{
		if (GodotObject.IsInstanceValid(InformationContainner))
		{
			_keyButton = new Button
			{
				Name = "KeyButton",
				Icon = _iconKey,
				CustomMinimumSize = new Vector2(16f, 16f),
				Flat = true,
				TooltipText = "添加关键帧",
				Visible = false
			};
			_keyButton.Pressed += OnKeyButtonPressed;
			InformationContainner.AddChild(_keyButton, forceReadableName: false, InternalMode.Disabled);
		}
	}

	protected void NormalizePropertyEditorLayout()
	{
		base.CustomMinimumSize = WithMinimumHeight(base.CustomMinimumSize, 28f);
		NormalizeInspectorRowFill();
		if (GodotObject.IsInstanceValid(InformationContainner))
		{
			InformationContainner.CustomMinimumSize = WithMinimumHeight(InformationContainner.CustomMinimumSize, 28f);
			InformationContainner.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			InformationContainner.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		}
		if (GodotObject.IsInstanceValid(PropertyNameLabel))
		{
			PropertyNameLabel.VerticalAlignment = VerticalAlignment.Center;
			PropertyNameLabel.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
			PropertyNameLabel.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		}
		NormalizeIconButton(ResetButton, 16f);
		NormalizeIconButton(_keyButton, 16f);
		if (GodotObject.IsInstanceValid(PropertyEditor))
		{
			PropertyEditor.CustomMinimumSize = WithMinimumHeight(PropertyEditor.CustomMinimumSize, 28f);
			PropertyEditor.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			NormalizeEditorRoot(PropertyEditor);
			NormalizeControlTree(PropertyEditor);
		}
	}

	private void NormalizeInspectorRowFill()
	{
		Control nodeOrNull = GetNodeOrNull<Control>("MarginContainer");
		if (GodotObject.IsInstanceValid(nodeOrNull))
		{
			nodeOrNull.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		}
		Control nodeOrNull2 = GetNodeOrNull<Control>("MarginContainer/VBoxContainer");
		if (GodotObject.IsInstanceValid(nodeOrNull2))
		{
			nodeOrNull2.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		}
		Control nodeOrNull3 = GetNodeOrNull<Control>("MarginContainer/VBoxContainer/HBoxContainer");
		if (GodotObject.IsInstanceValid(nodeOrNull3))
		{
			nodeOrNull3.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		}
	}

	private void NormalizeEditorRoot(Control control)
	{
		if (GodotObject.IsInstanceValid(control) && control == PropertyEditor)
		{
			control.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		}
	}

	private static Vector2 WithMinimumHeight(Vector2 size, float height)
	{
		if (size.Y >= height)
		{
			return size;
		}
		return new Vector2(size.X, height);
	}

	private static void NormalizeIconButton(Button button, float size)
	{
		if (GodotObject.IsInstanceValid(button))
		{
			button.CustomMinimumSize = new Vector2(size, size);
			button.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
			button.SizeFlagsVertical = SizeFlags.ShrinkCenter;
			button.Flat = true;
		}
	}

	private static void NormalizeControlTree(Control control)
	{
		if (!GodotObject.IsInstanceValid(control))
		{
			return;
		}
		if (control is Label label)
		{
			label.VerticalAlignment = VerticalAlignment.Center;
			label.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
			label.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		}
		if (control is SpinBox spinBox)
		{
			NormalizeSpinBox(spinBox);
		}
		if (IsSingleLineEditor(control))
		{
			control.CustomMinimumSize = WithMinimumHeight(control.CustomMinimumSize, 28f);
			control.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		}
		if (control is Button button)
		{
			button.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
		}
		foreach (Node child in control.GetChildren())
		{
			if (child is Control control2)
			{
				NormalizeControlTree(control2);
			}
		}
	}

	internal static void NormalizeSpinBox(SpinBox spinBox)
	{
		if (GodotObject.IsInstanceValid(spinBox))
		{
			spinBox.Alignment = HorizontalAlignment.Right;
			spinBox.UpdateOnTextChanged = true;
			spinBox.CustomMinimumSize = WithMinimumHeight(spinBox.CustomMinimumSize, 28f);
			spinBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			spinBox.SizeFlagsVertical = SizeFlags.ShrinkCenter;
			LineEdit lineEdit = spinBox.GetLineEdit();
			if (GodotObject.IsInstanceValid(lineEdit))
			{
				lineEdit.Alignment = HorizontalAlignment.Right;
				lineEdit.CaretBlink = true;
				lineEdit.SelectAllOnFocus = true;
			}
		}
	}

	private static bool IsSingleLineEditor(Control control)
	{
		if (!(control is LineEdit) && !(control is SpinBox) && !(control is Button) && !(control is OptionButton))
		{
			return control is ColorPickerButton;
		}
		return true;
	}

	private void OnKeyButtonPressed()
	{
		if (!GodotObject.IsInstanceValid(Property))
		{
			return;
		}
		XWEditorInterface instance = XWEditorInterface.Instance;
		if (instance != null && instance.HasAnimationEditor())
		{
			XWInspector inspector = GetInspector();
			if (GodotObject.IsInstanceValid(inspector))
			{
				inspector.EmitSignal(XWInspector.SignalName.PropertyKeyed, Property.PropName, GetPropertyValue());
			}
		}
	}

	private void ApplySplitRatio()
	{
		if (GodotObject.IsInstanceValid(InformationContainner))
		{
			InformationContainner.SizeFlagsStretchRatio = SplitRatio;
			if (GodotObject.IsInstanceValid(PropertyEditor))
			{
				PropertyEditor.SizeFlagsStretchRatio = 1f - SplitRatio;
			}
		}
	}

	public void SetSplitRatio(float ratio)
	{
		SplitRatio = Mathf.Clamp(ratio, 0.1f, 0.9f);
		ApplySplitRatio();
	}

	public virtual void ValueChange(Variant value, StringName field = null)
	{
		if (!GodotObject.IsInstanceValid(Property))
		{
			return;
		}
		if (field == null || field.ToString() == "")
		{
			field = Field;
		}
		if (_refreshingEditorValue)
		{
			return;
		}
		if (_continuousInputControls.Count == 0)
		{
			BindContinuousInputControls();
		}
		if (!IsInner && (_continuousEditActive || HasFocusedContinuousInput()))
		{
			if (!_continuousEditActive)
			{
				BeginContinuousEdit(field);
			}
			if (_continuousEditActive)
			{
				PreviewContinuousValue(value, field);
				return;
			}
		}
		if (IsInner)
		{
			EmitSignal(SignalName.ValueChanged, Property.Object, Property.PropName, field, value);
			UpdateResetButton();
			return;
		}
		Variant propertyValue = GetPropertyValue();
		if (propertyValue.VariantType == Variant.Type.Array && field != null && field.ToString() != "")
		{
			Godot.Collections.Array array = propertyValue.As<Godot.Collections.Array>();
			int index = field.ToString().ToInt();
			array[index] = value;
			WriteObjectProperty(Property.Object, Property.PropName, propertyValue);
			EmitSignal(SignalName.ValueChanged, Property.Object, Property.PropName, field, value);
			Property.SetCall();
			return;
		}
		if (propertyValue.VariantType == Variant.Type.Dictionary && field != null && field.ToString() != "")
		{
			Dictionary dictionary = propertyValue.As<Dictionary>();
			string text = field.ToString();
			if (dictionary.ContainsKey(field))
			{
				dictionary[field] = value;
			}
			else if (dictionary.ContainsKey(text))
			{
				dictionary[text] = value;
			}
			else
			{
				dictionary[field] = value;
			}
			WriteObjectProperty(Property.Object, Property.PropName, propertyValue);
			EmitSignal(SignalName.ValueChanged, Property.Object, Property.PropName, field, value);
			Property.SetCall();
			return;
		}
		bool flag = field != null && field.ToString() != "";
		StringName stringName = ((!flag) ? Property.PropName : new StringName($"{Property.PropName}:{field}"));
		if (GodotObject.IsInstanceValid(Property.Object))
		{
			Variant value2 = (flag ? ReadIndexedObjectProperty(Property.Object, stringName) : ReadObjectProperty(Property.Object, stringName));
			if (flag)
			{
				WriteIndexedObjectProperty(Property.Object, stringName, value);
			}
			else
			{
				WriteObjectProperty(Property.Object, stringName, value);
			}
			if (GodotObject.IsInstanceValid(UndoRedoManager))
			{
				UndoRedoManager.CreateAction($"Set Property: {stringName}", mergeMode: false, HistoryTypeOverride);
				UndoRedoManager.AddDoProperty(Property.Object, stringName, value);
				UndoRedoManager.AddUndoProperty(Property.Object, stringName, value2);
				UndoRedoManager.CommitAction();
			}
		}
		UpdateResetButton();
		EmitSignal(SignalName.ValueChanged, Property.Object, Property.PropName, Field, value);
		Property.SetCall();
	}

	public virtual Variant GetValue()
	{
		return default(Variant);
	}

	public virtual void SetEditProperty(XWInspectorProperty property, StringName field = null)
	{
		CancelContinuousEdit();
		Property = property;
		if (!PropertyNameLock)
		{
			if (!IsInner)
			{
				string text = property.PropName.ToString();
				if (property.LabelOverride != "")
				{
					text = property.LabelOverride;
				}
				XWInspector inspector = GetInspector();
				if (GodotObject.IsInstanceValid(inspector) && inspector.PropertyNameStyleMode == XWInspector.PropertyNameStyle.StyleCapitalized)
				{
					PropertyNameLabel.Text = CapitalizePropertyName(text);
				}
				else
				{
					PropertyNameLabel.Text = text;
				}
			}
			else
			{
				PropertyNameLabel.Text = field?.ToString() ?? "";
			}
		}
		Field = field ?? ((StringName)"");
		if (property.ReadOnly)
		{
			HideEditor();
		}
		else
		{
			ShowEditor();
		}
		if (property.Description != "")
		{
			base.TooltipText = property.Description;
		}
		LastValue = GetPropertyValue();
		UpdateResetButton();
		UpdateKeyButton();
		UpdateValueSafely();
		_continuousBindingPassPending = true;
		SetProcess(enable: true);
	}

	public virtual void UpdateValue()
	{
	}

	public void UpdateValueSafely()
	{
		bool refreshingEditorValue = _refreshingEditorValue;
		_refreshingEditorValue = true;
		try
		{
			UpdateValue();
		}
		finally
		{
			_refreshingEditorValue = refreshingEditorValue;
		}
	}

	public void BeginContinuousEdit(StringName field = null)
	{
		if (_refreshingEditorValue || _continuousEditActive || !GodotObject.IsInstanceValid(Property) || !GodotObject.IsInstanceValid(Property.Object))
		{
			return;
		}
		if (IsInner)
		{
			if (GodotObject.IsInstanceValid(ContinuousEditOwner) && ContinuousEditOwner != this)
			{
				ContinuousEditOwner.BeginContinuousEdit();
			}
			return;
		}
		if (field == null || field.ToString() == "")
		{
			field = Field;
		}
		bool flag = field != null && field.ToString() != "";
		StringName stringName = (flag ? new StringName($"{Property.PropName}:{field}") : Property.PropName);
		_continuousEditActive = true;
		_continuousEditObject = Property.Object;
		_continuousEditProperty = stringName;
		_continuousEditRootProperty = Property.PropName;
		_continuousEditField = field ?? ((StringName)"");
		_continuousEditOldValue = ReadObjectProperty(Property.Object, Property.PropName);
		_continuousEditPreviewValue = (flag ? ReadIndexedObjectProperty(Property.Object, stringName) : _continuousEditOldValue);
		_continuousEditHasPreview = false;
	}

	public void CommitContinuousEdit()
	{
		if (!_continuousEditActive)
		{
			if (IsInner && GodotObject.IsInstanceValid(ContinuousEditOwner) && ContinuousEditOwner != this)
			{
				ContinuousEditOwner.CommitContinuousEdit();
			}
			return;
		}
		GodotObject continuousEditObject = _continuousEditObject;
		StringName continuousEditProperty = _continuousEditProperty;
		StringName continuousEditRootProperty = _continuousEditRootProperty;
		StringName continuousEditField = _continuousEditField;
		Variant continuousEditOldValue = _continuousEditOldValue;
		Variant continuousEditPreviewValue = _continuousEditPreviewValue;
		bool continuousEditHasPreview = _continuousEditHasPreview;
		Variant variant = (GodotObject.IsInstanceValid(continuousEditObject) ? ReadObjectProperty(continuousEditObject, continuousEditRootProperty) : continuousEditPreviewValue);
		ClearContinuousEditSession();
		if (continuousEditHasPreview && GodotObject.IsInstanceValid(continuousEditObject) && !continuousEditOldValue.Equals(variant))
		{
			if (GodotObject.IsInstanceValid(UndoRedoManager))
			{
				UndoRedoManager.CreateAction($"Set Property: {continuousEditProperty}", mergeMode: false, HistoryTypeOverride);
				UndoRedoManager.AddDoProperty(continuousEditObject, continuousEditRootProperty, variant);
				UndoRedoManager.AddUndoProperty(continuousEditObject, continuousEditRootProperty, continuousEditOldValue);
				UndoRedoManager.CommitAction();
			}
			else
			{
				WriteObjectProperty(continuousEditObject, continuousEditRootProperty, variant);
			}
			LastValue = GetPropertyValue();
			UpdateResetButton();
			EmitSignal(SignalName.ValueChanged, continuousEditObject, continuousEditRootProperty, continuousEditField, continuousEditPreviewValue);
			Property.SetCall();
		}
	}

	public void CancelContinuousEdit()
	{
		if (!_continuousEditActive)
		{
			if (IsInner && GodotObject.IsInstanceValid(ContinuousEditOwner) && ContinuousEditOwner != this)
			{
				ContinuousEditOwner.CancelContinuousEdit();
			}
			return;
		}
		GodotObject continuousEditObject = _continuousEditObject;
		StringName continuousEditRootProperty = _continuousEditRootProperty;
		Variant continuousEditOldValue = _continuousEditOldValue;
		bool continuousEditHasPreview = _continuousEditHasPreview;
		ClearContinuousEditSession();
		if (continuousEditHasPreview && GodotObject.IsInstanceValid(continuousEditObject))
		{
			WriteObjectProperty(continuousEditObject, continuousEditRootProperty, continuousEditOldValue);
			if (GodotObject.IsInstanceValid(Property))
			{
				Property.SetCall();
			}
		}
	}

	private void PreviewContinuousValue(Variant value, StringName field)
	{
		if (_continuousEditActive && GodotObject.IsInstanceValid(_continuousEditObject))
		{
			StringName stringName = field ?? ((StringName)"");
			if (((stringName.ToString() == "") ? Property.PropName : new StringName($"{Property.PropName}:{stringName}")) != _continuousEditProperty)
			{
				CommitContinuousEdit();
				BeginContinuousEdit(stringName);
			}
			if (_continuousEditActive && !((_continuousEditField != null && _continuousEditField.ToString() != "") ? ReadIndexedObjectProperty(_continuousEditObject, _continuousEditProperty) : ReadObjectProperty(_continuousEditObject, _continuousEditProperty)).Equals(value))
			{
				WriteContinuousProperty(_continuousEditObject, _continuousEditProperty, _continuousEditField, value);
				_continuousEditPreviewValue = value;
				_continuousEditHasPreview = true;
				LastValue = GetPropertyValue();
				UpdateResetButton();
				Property.SetCall();
			}
		}
	}

	private static void WriteContinuousProperty(GodotObject editObject, StringName propertyTrue, StringName field, Variant value)
	{
		if (field != null && field.ToString() != "")
		{
			WriteIndexedObjectProperty(editObject, propertyTrue, value);
		}
		else
		{
			WriteObjectProperty(editObject, propertyTrue, value);
		}
	}

	private void ClearContinuousEditSession()
	{
		_continuousEditActive = false;
		_continuousEditObject = null;
		_continuousEditProperty = "";
		_continuousEditRootProperty = "";
		_continuousEditField = "";
		_continuousEditOldValue = default(Variant);
		_continuousEditPreviewValue = default(Variant);
		_continuousEditHasPreview = false;
	}

	private void BindContinuousInputControls()
	{
		BindContinuousInputControlTree(this);
	}

	private void BindContinuousInputControlTree(Control control)
	{
		if (!GodotObject.IsInstanceValid(control))
		{
			return;
		}
		if (control is LineEdit || control is TextEdit || control is SpinBox || control is Slider || control is ColorPickerButton)
		{
			BindContinuousInputControl(control);
		}
		SpinBox spinBox = control as SpinBox;
		if (spinBox != null)
		{
			spinBox.UpdateOnTextChanged = true;
			LineEdit lineEdit = spinBox.GetLineEdit();
			if (GodotObject.IsInstanceValid(lineEdit))
			{
				BindContinuousInputControl(lineEdit);
				if (_spinBoxTextPreviewIds.Add(spinBox.GetInstanceId()))
				{
					lineEdit.TextChanged += delegate(string text)
					{
						ApplySpinBoxTextPreview(spinBox, text);
					};
				}
			}
		}
		foreach (Node child in control.GetChildren())
		{
			if (child is Control control2)
			{
				BindContinuousInputControlTree(control2);
			}
		}
	}

	private void BindContinuousInputControl(Control control)
	{
		ulong instanceId = control.GetInstanceId();
		if (!_continuousInputControlIds.Add(instanceId))
		{
			return;
		}
		_continuousInputControls.Add(control);
		if (control is ColorPickerButton colorPickerButton)
		{
			colorPickerButton.Pressed += delegate
			{
				BeginContinuousEdit();
			};
			colorPickerButton.PopupClosed += CommitContinuousEdit;
			ColorPicker picker = colorPickerButton.GetPicker();
			if (!GodotObject.IsInstanceValid(picker))
			{
				return;
			}
			picker.ColorChanged += delegate(Color color)
			{
				if (_continuousEditActive)
				{
					ValueChange(color);
				}
			};
			return;
		}
		control.FocusEntered += delegate
		{
			BeginContinuousEdit();
		};
		control.FocusExited += delegate
		{
			Callable.From(CommitContinuousEditIfFocusLeft).CallDeferred();
		};
		if (control is LineEdit lineEdit)
		{
			lineEdit.TextSubmitted += delegate
			{
				CommitContinuousEdit();
			};
		}
	}

	private void CommitContinuousEditIfFocusLeft()
	{
		if (!HasFocusedContinuousInput())
		{
			CommitContinuousEdit();
		}
	}

	private void ApplySpinBoxTextPreview(SpinBox spinBox, string text)
	{
		if (!_refreshingEditorValue && GodotObject.IsInstanceValid(spinBox) && !string.IsNullOrWhiteSpace(text))
		{
			string text2 = text.Trim();
			if (!string.IsNullOrEmpty(spinBox.Prefix) && text2.StartsWith(spinBox.Prefix))
			{
				string text3 = text2;
				int length = spinBox.Prefix.Length;
				text2 = text3.Substring(length, text3.Length - length).TrimStart();
			}
			if (!string.IsNullOrEmpty(spinBox.Suffix) && text2.EndsWith(spinBox.Suffix))
			{
				string text3 = text2;
				int length = spinBox.Suffix.Length;
				text2 = text3.Substring(0, text3.Length - length).TrimEnd();
			}
			if (double.TryParse(text2, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) || double.TryParse(text2, NumberStyles.Float, CultureInfo.CurrentCulture, out result))
			{
				spinBox.Value = result;
			}
		}
	}

	private bool HasFocusedContinuousInput()
	{
		foreach (Control continuousInputControl in _continuousInputControls)
		{
			if (GodotObject.IsInstanceValid(continuousInputControl) && continuousInputControl.HasFocus())
			{
				return true;
			}
		}
		return false;
	}

	protected static void SetLineEditTextPreservingCaret(LineEdit lineEdit, string text)
	{
		if (GodotObject.IsInstanceValid(lineEdit))
		{
			if (text == null)
			{
				text = "";
			}
			if (!(lineEdit.Text == text) && !lineEdit.HasFocus())
			{
				lineEdit.Text = text;
			}
		}
	}

	protected static void SetTextEditTextPreservingCaret(TextEdit textEdit, string text)
	{
		if (GodotObject.IsInstanceValid(textEdit))
		{
			if (text == null)
			{
				text = "";
			}
			if (!(textEdit.Text == text) && !textEdit.HasFocus())
			{
				textEdit.Text = text;
			}
		}
	}

	public void SetMixedValue()
	{
		_isMixedValue = true;
		if (GodotObject.IsInstanceValid(PropertyNameLabel))
		{
			PropertyNameLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.8f));
		}
		if (GodotObject.IsInstanceValid(PropertyEditor))
		{
			PropertyEditor.TooltipText = "多选节点值不一致";
		}
	}

	private void UpdateResetButton()
	{
		if (GodotObject.IsInstanceValid(Property) && GodotObject.IsInstanceValid(Property.Object) && GodotObject.IsInstanceValid(ResetButton))
		{
			ResetButton.Visible = ShouldShowResetButton();
		}
	}

	private bool ShouldShowResetButton()
	{
		if (Property.RestValue.VariantType != Variant.Type.Nil)
		{
			Variant propertyValue = GetPropertyValue();
			if (propertyValue.VariantType == Property.RestValue.VariantType)
			{
				return !propertyValue.Equals(Property.RestValue);
			}
			return false;
		}
		if (TryGetCustomRevertValue(out var revertValue))
		{
			return !GetPropertyValue().Equals(revertValue);
		}
		Variant variant = ClassDB.ClassGetPropertyDefaultValue(new StringName(Property.Object.GetClass()), Property.PropName);
		if (variant.VariantType != Variant.Type.Nil)
		{
			return !GetPropertyValue().Equals(variant);
		}
		return false;
	}

	private bool TryGetCustomRevertValue(out Variant revertValue)
	{
		revertValue = default(Variant);
		if (!GodotObject.IsInstanceValid(Property) || !GodotObject.IsInstanceValid(Property.Object))
		{
			return false;
		}
		if (Property.PropName == (StringName)"")
		{
			return false;
		}
		if (!Property.Object.HasMethod(new StringName("property_can_revert")))
		{
			return false;
		}
		if (!Property.Object.Call("property_can_revert", Property.PropName).AsBool())
		{
			return false;
		}
		revertValue = GetRevertValue();
		return true;
	}

	private void UpdateKeyButton()
	{
		if (GodotObject.IsInstanceValid(_keyButton))
		{
			if (!GodotObject.IsInstanceValid(Property) || !GodotObject.IsInstanceValid(Property.Object))
			{
				_keyButton.Visible = false;
			}
			else if (Property.ReadOnly)
			{
				_keyButton.Visible = false;
			}
			else
			{
				_keyButton.Visible = Property.Object is Node && (XWEditorInterface.Instance?.HasAnimationEditor() ?? false);
			}
		}
	}

	public Variant GetPropertyValue()
	{
		if (!GodotObject.IsInstanceValid(Property) || !GodotObject.IsInstanceValid(Property.Object))
		{
			return default(Variant);
		}
		if (!IsInner && Property.PropName == (StringName)"")
		{
			return Variant.From<GodotObject>(Property.Object);
		}
		Variant variant = ReadObjectProperty(Property.Object, Property.PropName);
		if (!IsInner)
		{
			if (Property.PropName == (StringName)"")
			{
				return Property.Object;
			}
			return variant;
		}
		Variant.Type variantType = variant.VariantType;
		if (variantType >= Variant.Type.Array && variantType <= Variant.Type.PackedVector4Array && Field != null && Field.ToString() != "")
		{
			int idx = Field.ToString().ToInt();
			return GetVariantArrayElement(variant, variantType, idx);
		}
		if (variantType == Variant.Type.Dictionary && Field != null && Field.ToString() != "")
		{
			Dictionary dictionary = variant.As<Dictionary>();
			if (dictionary.ContainsKey(Field))
			{
				return dictionary[Field];
			}
			string text = Field.ToString();
			if (dictionary.ContainsKey(text))
			{
				return dictionary[text];
			}
		}
		return variant;
	}

	public void ResetButtonPressed()
	{
		if (!GodotObject.IsInstanceValid(Property) || !GodotObject.IsInstanceValid(Property.Object))
		{
			return;
		}
		Variant revertValue = GetRevertValue();
		if (revertValue.VariantType != Variant.Type.Nil || Property.RestValue.VariantType != Variant.Type.Nil)
		{
			Variant value = ((Property.RestValue.VariantType != Variant.Type.Nil) ? Property.RestValue : revertValue);
			Variant value2 = ReadObjectProperty(Property.Object, Property.PropName);
			WriteObjectProperty(Property.Object, Property.PropName, value);
			if (GodotObject.IsInstanceValid(UndoRedoManager))
			{
				UndoRedoManager.CreateAction($"Reset Property: {Property.PropName}", mergeMode: false, HistoryTypeOverride);
				UndoRedoManager.AddDoProperty(Property.Object, Property.PropName, value);
				UndoRedoManager.AddUndoProperty(Property.Object, Property.PropName, value2);
				UndoRedoManager.CommitAction();
			}
			RefreshEditProperty();
		}
	}

	private Variant GetRevertValue()
	{
		if (!GodotObject.IsInstanceValid(Property) || !GodotObject.IsInstanceValid(Property.Object))
		{
			return default(Variant);
		}
		if (Property.Object.HasMethod(new StringName("property_get_revert")) && Property.PropName != (StringName)"" && Property.Object.Call("property_can_revert", Property.PropName).AsBool())
		{
			return Property.Object.Call("property_get_revert", Property.PropName);
		}
		return ClassDB.ClassGetPropertyDefaultValue(new StringName(Property.Object.GetClass()), Property.PropName);
	}

	public void RefreshEditProperty()
	{
		SetEditProperty(Property, Field);
	}

	public virtual void SetupRangeHint(string hintString)
	{
	}

	public void DoSelect()
	{
		Select = true;
		QueueRedraw();
	}

	public void Deselect()
	{
		Select = false;
		QueueRedraw();
	}

	public virtual void HideEditor()
	{
		if (GodotObject.IsInstanceValid(PropertyEditor))
		{
			PropertyEditor.Visible = false;
		}
	}

	public virtual void ShowEditor()
	{
		if (GodotObject.IsInstanceValid(PropertyEditor))
		{
			PropertyEditor.Visible = true;
		}
	}

	private void OnGuiInput(InputEvent @event)
	{
		if (!(@event is InputEventMouseButton inputEventMouseButton))
		{
			return;
		}
		if (inputEventMouseButton.ButtonIndex == MouseButton.Right && inputEventMouseButton.Pressed)
		{
			if (GodotObject.IsInstanceValid(Property))
			{
				XWInspector inspector = GetInspector();
				if (GodotObject.IsInstanceValid(inspector))
				{
					inspector.ShowPropertyContextMenu(Property.PropName, GetGlobalMousePosition(), this);
				}
			}
		}
		else if (inputEventMouseButton.ButtonIndex == MouseButton.Left && inputEventMouseButton.Pressed)
		{
			XWInspector inspector2 = GetInspector();
			if (GodotObject.IsInstanceValid(inspector2))
			{
				inspector2.SelectEditor(this);
			}
		}
	}

	private void OnMouseEntered()
	{
		_hover = true;
		QueueRedraw();
	}

	private void OnMouseExited()
	{
		_hover = false;
		QueueRedraw();
	}

	public override void _Draw()
	{
		Vector2 size = base.Size;
		if (Select)
		{
			StyleBoxFlat styleBoxFlat = new StyleBoxFlat
			{
				BgColor = new Color(1f, 1f, 1f, 0.06f)
			};
			styleBoxFlat.SetContentMarginAll(0f);
			DrawStyleBox(styleBoxFlat, new Rect2(Vector2.Zero, size));
		}
		else if (_hover)
		{
			StyleBoxFlat styleBoxFlat2 = new StyleBoxFlat
			{
				BgColor = new Color(1f, 1f, 1f, 0.03f)
			};
			styleBoxFlat2.SetContentMarginAll(0f);
			DrawStyleBox(styleBoxFlat2, new Rect2(Vector2.Zero, size));
		}
	}

	public override bool _CanDropData(Vector2 atPosition, Variant data)
	{
		if (!GodotObject.IsInstanceValid(Property) || Property.ReadOnly)
		{
			return false;
		}
		if (data.VariantType != Variant.Type.Dictionary)
		{
			return false;
		}
		Dictionary dictionary = data.As<Dictionary>();
		if (!dictionary.ContainsKey("files"))
		{
			return false;
		}
		if (Property.Hint != PropertyHint.ResourceType)
		{
			return false;
		}
		string[] array = dictionary["files"].As<string[]>();
		if (array.Length == 0)
		{
			return false;
		}
		Resource resource = ResourceLoader.Load(array[0], "", ResourceLoader.CacheMode.Reuse);
		if (!GodotObject.IsInstanceValid(resource))
		{
			return false;
		}
		return resource.IsClass(Property.HintString);
	}

	public override void _DropData(Vector2 atPosition, Variant data)
	{
		if (data.VariantType != Variant.Type.Dictionary)
		{
			return;
		}
		Dictionary dictionary = data.As<Dictionary>();
		if (!dictionary.ContainsKey("files"))
		{
			return;
		}
		string[] array = dictionary["files"].As<string[]>();
		if (array.Length != 0)
		{
			Resource resource = ResourceLoader.Load(array[0], "", ResourceLoader.CacheMode.Reuse);
			if (GodotObject.IsInstanceValid(resource))
			{
				ValueChange(resource);
			}
		}
	}

	private void CopyValue()
	{
		Variant propertyValue = GetPropertyValue();
		if (propertyValue.VariantType != Variant.Type.Nil)
		{
			DisplayServer.ClipboardSet(propertyValue.ToString());
		}
	}

	private void PasteValue()
	{
		string text = DisplayServer.ClipboardGet();
		if (string.IsNullOrEmpty(text))
		{
			return;
		}
		Variant propertyValue = GetPropertyValue();
		if (propertyValue.VariantType == Variant.Type.Nil)
		{
			return;
		}
		Variant.Type variantType = propertyValue.VariantType;
		Variant.Type num = variantType - 1;
		if ((ulong)num > 8uL)
		{
			return;
		}
		switch ((int)num)
		{
		case 3:
			ValueChange(text);
			break;
		case 1:
			ValueChange(text.ToInt());
			break;
		case 2:
			ValueChange(text.ToFloat());
			break;
		case 0:
			ValueChange(text.ToLower() == "true" || text == "1");
			break;
		case 4:
		{
			string[] array2 = text.Replace("(", "").Replace(")", "").Split(",");
			if (array2.Length >= 2)
			{
				ValueChange(new Vector2(array2[0].ToFloat(), array2[1].ToFloat()));
			}
			break;
		}
		case 8:
		{
			string[] array = text.Replace("(", "").Replace(")", "").Split(",");
			if (array.Length >= 3)
			{
				ValueChange(new Vector3(array[0].ToFloat(), array[1].ToFloat(), array[2].ToFloat()));
			}
			break;
		}
		case 5:
		case 6:
		case 7:
			break;
		}
	}

	private void CopyPropertyPath()
	{
		if (GodotObject.IsInstanceValid(Property))
		{
			DisplayServer.ClipboardSet(Property.PropName.ToString());
		}
	}

	private void DeleteProperty()
	{
		if (GodotObject.IsInstanceValid(Property) && GodotObject.IsInstanceValid(Property.Object))
		{
			if (Property.Object.HasMethod(new StringName("remove_property")))
			{
				Property.Object.Call("remove_property", Property.PropName);
			}
			else if (Property.Object is Node node)
			{
				node.SetMeta(Property.PropName.ToString(), default(Variant));
			}
		}
	}

	protected XWInspector GetInspector()
	{
		for (Node parent = GetParent(); parent != null; parent = parent.GetParent())
		{
			if (parent is XWInspector result)
			{
				return result;
			}
		}
		return null;
	}

	internal static Variant ReadObjectProperty(GodotObject obj, StringName propertyName)
	{
		if (!GodotObject.IsInstanceValid(obj) || propertyName == null || propertyName == (StringName)"")
		{
			return default(Variant);
		}
		return obj.Get(propertyName);
	}

	internal static void WriteObjectProperty(GodotObject obj, StringName propertyName, Variant value)
	{
		if (GodotObject.IsInstanceValid(obj) && !(propertyName == null) && !(propertyName == (StringName)""))
		{
			obj.Set(propertyName, value);
		}
	}

	internal static Variant ReadIndexedObjectProperty(GodotObject obj, StringName propertyPath)
	{
		if (!GodotObject.IsInstanceValid(obj) || propertyPath == null || propertyPath == (StringName)"")
		{
			return default(Variant);
		}
		return obj.GetIndexed(new NodePath(propertyPath.ToString()));
	}

	internal static void WriteIndexedObjectProperty(GodotObject obj, StringName propertyPath, Variant value)
	{
		if (GodotObject.IsInstanceValid(obj) && !(propertyPath == null) && !(propertyPath == (StringName)""))
		{
			obj.SetIndexed(new NodePath(propertyPath.ToString()), value);
		}
	}

	private static Variant GetVariantArrayElement(Variant value, Variant.Type vt, int idx)
	{
		Variant.Type num = vt - 28;
		if ((ulong)num <= 10uL)
		{
			switch ((int)num)
			{
			case 0:
			{
				Godot.Collections.Array array5 = value.As<Godot.Collections.Array>();
				if (idx < 0 || idx >= array5.Count)
				{
					return default(Variant);
				}
				return array5[idx];
			}
			case 1:
			{
				byte[] array7 = value.As<byte[]>();
				return (byte)((idx >= 0 && idx < array7.Length) ? array7[idx] : 0);
			}
			case 2:
			{
				int[] array3 = value.As<int[]>();
				return (idx >= 0 && idx < array3.Length) ? array3[idx] : 0;
			}
			case 3:
			{
				long[] array8 = value.As<long[]>();
				return (idx >= 0 && idx < array8.Length) ? array8[idx] : 0;
			}
			case 4:
			{
				float[] array4 = value.As<float[]>();
				return (idx >= 0 && idx < array4.Length) ? array4[idx] : 0f;
			}
			case 5:
			{
				double[] array9 = value.As<double[]>();
				return (idx >= 0 && idx < array9.Length) ? array9[idx] : 0.0;
			}
			case 6:
			{
				string[] array2 = value.As<string[]>();
				return (idx >= 0 && idx < array2.Length) ? array2[idx] : null;
			}
			case 7:
			{
				Vector2[] array10 = value.As<Vector2[]>();
				return (idx >= 0 && idx < array10.Length) ? array10[idx] : default(Vector2);
			}
			case 8:
			{
				Vector3[] array11 = value.As<Vector3[]>();
				return (idx >= 0 && idx < array11.Length) ? array11[idx] : default(Vector3);
			}
			case 9:
			{
				Color[] array6 = value.As<Color[]>();
				return (idx >= 0 && idx < array6.Length) ? array6[idx] : default(Color);
			}
			case 10:
			{
				Vector4[] array = value.As<Vector4[]>();
				return (idx >= 0 && idx < array.Length) ? array[idx] : default(Vector4);
			}
			}
		}
		return default(Variant);
	}

	private static string CapitalizePropertyName(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return name;
		}
		if (name.Length == 1)
		{
			return name.ToUpper();
		}
		bool flag = false;
		string text2;
		for (int i = 0; i < name.Length; i++)
		{
			if (name[i] == '_')
			{
				string text = name.Substring(0, i);
				text2 = name;
				int num = i + 1;
				name = text + " " + text2.Substring(num, text2.Length - num);
				flag = true;
			}
		}
		if (!flag)
		{
			StringBuilder stringBuilder = new StringBuilder();
			for (int j = 0; j < name.Length; j++)
			{
				if (j > 0 && char.IsUpper(name[j]) && !char.IsUpper(name[j - 1]))
				{
					stringBuilder.Append(' ');
				}
				stringBuilder.Append(name[j]);
			}
			name = stringBuilder.ToString();
		}
		ReadOnlySpan<char> readOnlySpan = new ReadOnlySpan<char>(char.ToUpper(name[0]));
		text2 = name;
		return string.Concat(readOnlySpan, text2.Substring(1, text2.Length - 1));
	}
}
