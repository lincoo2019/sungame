using Godot;
using PVZHE.ModEditor.Core;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorPropertyEditorShowTexture2D : XWInspectorPropertyEditorBase
{
	private const string ScenePath = "res://addons/ModEditor/Inspector/ResourceInspectorExtend/Texture2D/XWInspectorPropertyEditorShowTexture2D.tscn";

	private TextureRect _textureRect;

	public static XWInspectorPropertyEditorShowTexture2D Create()
	{
		return ResourceLoader.Load<PackedScene>("res://addons/ModEditor/Inspector/ResourceInspectorExtend/Texture2D/XWInspectorPropertyEditorShowTexture2D.tscn", null, ResourceLoader.CacheMode.Reuse).Instantiate<XWInspectorPropertyEditorShowTexture2D>(PackedScene.GenEditState.Disabled);
	}

	public override void _Ready()
	{
		base._Ready();
		_textureRect = GetNode<TextureRect>("%TextureRect");
	}

	public override void UpdateValue()
	{
		Variant propertyValue = GetPropertyValue();
		if (propertyValue.VariantType == Variant.Type.Object && propertyValue.As<GodotObject>() is Texture2D texture && XWTextureSafety.CanPreview(texture))
		{
			_textureRect.Texture = texture;
		}
		else
		{
			_textureRect.Texture = null;
		}
	}
}
