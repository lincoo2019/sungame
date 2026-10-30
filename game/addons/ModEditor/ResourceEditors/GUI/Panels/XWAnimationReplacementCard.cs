using System;
using Godot;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public partial class XWAnimationReplacementCard : Control
{
	private Control _slotPanel;

	private TextureRect _slotThumbnail;

	private Label _slotName;

	private Label _slotMediaId;

	private Label _slotLayers;

	private Button _slotRemoveButton;

	private Control _texturePanel;

	private TextureRect _texturePreview;

	private Label _textureName;

	private Button _textureReplaceButton;

	private Button _textureRemoveButton;

	private Control _emptyPanel;

	private Label _emptyGlyph;

	private Label _emptyTitle;

	private Label _emptySubtitle;

	private Action<string> _removeSlot;

	private Action<int> _replaceTexture;

	private Action<int> _removeTexture;

	private string _mediaName = "";

	private int _textureIndex = -1;

	public override void _Ready()
	{
		_slotPanel = GetNode<Control>("%SlotPanel");
		_slotThumbnail = GetNode<TextureRect>("%SlotThumbnail");
		_slotName = GetNode<Label>("%SlotName");
		_slotMediaId = GetNode<Label>("%SlotMediaId");
		_slotLayers = GetNode<Label>("%SlotLayers");
		_slotRemoveButton = GetNode<Button>("%SlotRemoveButton");
		_texturePanel = GetNode<Control>("%TexturePanel");
		_texturePreview = GetNode<TextureRect>("%TexturePreview");
		_textureName = GetNode<Label>("%TextureName");
		_textureReplaceButton = GetNode<Button>("%TextureReplaceButton");
		_textureRemoveButton = GetNode<Button>("%TextureRemoveButton");
		_emptyPanel = GetNode<Control>("%EmptyPanel");
		_emptyGlyph = GetNode<Label>("%EmptyGlyph");
		_emptyTitle = GetNode<Label>("%EmptyTitle");
		_emptySubtitle = GetNode<Label>("%EmptySubtitle");
		_slotRemoveButton.Pressed += delegate
		{
			_removeSlot?.Invoke(_mediaName);
		};
		_textureReplaceButton.Pressed += delegate
		{
			_replaceTexture?.Invoke(_textureIndex);
		};
		_textureRemoveButton.Pressed += delegate
		{
			_removeTexture?.Invoke(_textureIndex);
		};
	}

	public void BindSlot(string mediaName, int mediaId, Texture2D thumbnail, string layerSummary, bool mediaExists, Action<string> removeSlot)
	{
		ShowMode(_slotPanel, new Vector2(280f, 116f));
		_mediaName = mediaName ?? "";
		_removeSlot = removeSlot;
		_slotThumbnail.Texture = thumbnail;
		_slotThumbnail.TooltipText = (mediaExists ? $"原始媒体 #{mediaId} · {_mediaName}" : "原始媒体已不存在");
		_slotName.Text = _mediaName;
		_slotName.TooltipText = _mediaName;
		_slotName.Modulate = (mediaExists ? new Color(0.9f, 1f, 0.72f) : new Color(1f, 0.42f, 0.34f));
		_slotMediaId.Text = (mediaExists ? $"媒体 #{mediaId + 1}" : "媒体已不存在");
		_slotLayers.Text = (mediaExists ? layerSummary : "可删除此失效槽位");
		_slotLayers.TooltipText = (mediaExists ? layerSummary : "");
		_slotRemoveButton.TooltipText = "删除 " + _mediaName + " 替换槽位";
	}

	public void BindTexture(Texture2D texture, int index, string displayName, string resourcePath, Action<int> replaceTexture, Action<int> removeTexture)
	{
		ShowMode(_texturePanel, new Vector2(218f, 176f));
		_textureIndex = index;
		_replaceTexture = replaceTexture;
		_removeTexture = removeTexture;
		_texturePreview.Texture = texture;
		_texturePreview.TooltipText = (string.IsNullOrWhiteSpace(resourcePath) ? "内嵌纹理" : resourcePath);
		_textureName.Text = $"#{index + 1}  {displayName}";
		_textureName.TooltipText = _texturePreview.TooltipText;
	}

	public void BindEmpty(string glyph, string title, string subtitle)
	{
		ShowMode(_emptyPanel, new Vector2(270f, 108f));
		_emptyGlyph.Text = glyph ?? "";
		_emptyTitle.Text = title ?? "";
		_emptySubtitle.Text = subtitle ?? "";
	}

	private void ShowMode(Control requestedPanel, Vector2 minimumSize)
	{
		base.CustomMinimumSize = minimumSize;
		_slotPanel.Visible = requestedPanel == _slotPanel;
		_texturePanel.Visible = requestedPanel == _texturePanel;
		_emptyPanel.Visible = requestedPanel == _emptyPanel;
	}
}
