using System.Collections.Generic;
using Godot;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorPropertyEditorLayers : XWInspectorPropertyEditorBase
{
	private const string ScenePath = "res://addons/ModEditor/Inspector/GUI/Editor/Layers/XWInspectorPropertyEditorLayers.tscn";

	private Button _button;

	private PopupPanel _popup;

	private GridContainer _grid;

	private readonly List<Button> _layerButtons = new List<Button>();

	public static XWInspectorPropertyEditorLayers Create()
	{
		return ResourceLoader.Load<PackedScene>("res://addons/ModEditor/Inspector/GUI/Editor/Layers/XWInspectorPropertyEditorLayers.tscn", null, ResourceLoader.CacheMode.Reuse).Instantiate<XWInspectorPropertyEditorLayers>(PackedScene.GenEditState.Disabled);
	}

	public override void _Ready()
	{
		base._Ready();
		_button = GetNode<Button>("%LayersButton");
		_popup = GetNode<PopupPanel>("%LayersPopup");
		_grid = GetNode<GridContainer>("%GridContainer");
		_button.Pressed += OnButtonPressed;
		for (int i = 0; i < 32; i++)
		{
			Button button = new Button
			{
				Text = (i + 1).ToString(),
				CustomMinimumSize = new Vector2(18f, 18f),
				ToggleMode = true,
				TooltipText = $"层 {i + 1}"
			};
			int layerIndex = i;
			button.Toggled += delegate
			{
				OnLayerToggled(layerIndex);
			};
			_layerButtons.Add(button);
			_grid.AddChild(button, forceReadableName: false, InternalMode.Disabled);
		}
	}

	public override void UpdateValue()
	{
		Variant propertyValue = GetPropertyValue();
		if (propertyValue.VariantType != Variant.Type.Nil)
		{
			long mask = propertyValue.AsInt64();
			UpdateButtonsFromMask(mask);
			UpdateButtonText(mask);
		}
	}

	public override Variant GetValue()
	{
		long num = 0L;
		for (int i = 0; i < 32; i++)
		{
			if (GodotObject.IsInstanceValid(_layerButtons[i]) && _layerButtons[i].ButtonPressed)
			{
				num |= 1L << i;
			}
		}
		return num;
	}

	private void OnButtonPressed()
	{
		_popup.Position = new Vector2I((int)GetGlobalMousePosition().X, (int)GetGlobalMousePosition().Y);
		_popup.Popup();
	}

	private void OnLayerToggled(int layerIndex)
	{
		long num = 0L;
		for (int i = 0; i < 32; i++)
		{
			if (GodotObject.IsInstanceValid(_layerButtons[i]) && _layerButtons[i].ButtonPressed)
			{
				num |= 1L << i;
			}
		}
		ValueChange(num);
		UpdateButtonText(num);
	}

	private void UpdateButtonsFromMask(long mask)
	{
		for (int i = 0; i < 32; i++)
		{
			if (GodotObject.IsInstanceValid(_layerButtons[i]))
			{
				_layerButtons[i].SetPressedNoSignal((mask & (1L << i)) != 0);
			}
		}
	}

	private void UpdateButtonText(long mask)
	{
		int num = 0;
		for (int i = 0; i < 32; i++)
		{
			if ((mask & (1L << i)) != 0L)
			{
				num++;
			}
		}
		_button.Text = $"{num} 层";
	}
}
