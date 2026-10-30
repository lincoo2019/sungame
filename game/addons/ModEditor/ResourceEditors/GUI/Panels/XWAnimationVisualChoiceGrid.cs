using System;
using System.Collections.Generic;
using Godot;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public partial class XWAnimationVisualChoiceGrid : PanelContainer
{
	private const string CardScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWAnimationVisualChoiceCard.tscn";

	private static PackedScene _cardScene;

	private readonly List<XWAnimationVisualChoiceCard> _cards = new List<XWAnimationVisualChoiceCard>();

	private Label _titleLabel;

	private Label _summaryLabel;

	private Label _emptyLabel;

	private HFlowContainer _choiceHost;

	private int _selectedChoiceId = -1;

	public int ChoiceCount => _cards.Count;

	public int SelectedChoiceId => _selectedChoiceId;

	public event Action<int> ChoiceSelected;

	public override void _Ready()
	{
		_titleLabel = GetNode<Label>("%ChoiceTitle");
		_summaryLabel = GetNode<Label>("%ChoiceSummary");
		_emptyLabel = GetNode<Label>("%EmptyLabel");
		_choiceHost = GetNode<HFlowContainer>("%ChoiceHost");
	}

	public void Configure(string title, IEnumerable<XWAnimationVisualChoice> choices, int selectedId, bool enabled)
	{
		ClearChoices();
		_titleLabel.Text = title ?? "";
		if (_cardScene == null)
		{
			_cardScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWAnimationVisualChoiceCard.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		if (choices != null)
		{
			foreach (XWAnimationVisualChoice choice in choices)
			{
				XWAnimationVisualChoiceCard xWAnimationVisualChoiceCard = _cardScene?.Instantiate<XWAnimationVisualChoiceCard>(PackedScene.GenEditState.Disabled);
				if (GodotObject.IsInstanceValid(xWAnimationVisualChoiceCard))
				{
					_choiceHost.AddChild(xWAnimationVisualChoiceCard, forceReadableName: false, InternalMode.Disabled);
					xWAnimationVisualChoiceCard.Configure(choice.Id, choice.Title, choice.Subtitle, choice.Icon, choice.Accent);
					xWAnimationVisualChoiceCard.Disabled = !enabled;
					xWAnimationVisualChoiceCard.ChoicePressed += OnCardPressed;
					_cards.Add(xWAnimationVisualChoiceCard);
				}
			}
		}
		_summaryLabel.Text = ((_cards.Count == 0) ? "无可用项" : $"{_cards.Count} 个视觉选项");
		_emptyLabel.Visible = _cards.Count == 0;
		SelectChoice(selectedId, emitSignal: false);
	}

	public bool SelectChoice(int choiceId, bool emitSignal)
	{
		bool flag = false;
		foreach (XWAnimationVisualChoiceCard card in _cards)
		{
			bool flag2 = card.ChoiceId == choiceId;
			card.SetSelected(flag2);
			flag = flag || flag2;
		}
		if (!flag)
		{
			return false;
		}
		_selectedChoiceId = choiceId;
		if (emitSignal)
		{
			this.ChoiceSelected?.Invoke(choiceId);
		}
		return true;
	}

	public void SetChoicesEnabled(bool enabled)
	{
		foreach (XWAnimationVisualChoiceCard card in _cards)
		{
			card.Disabled = !enabled;
		}
	}

	public void ClearChoices()
	{
		foreach (XWAnimationVisualChoiceCard card in _cards)
		{
			if (GodotObject.IsInstanceValid(card))
			{
				card.ChoicePressed -= OnCardPressed;
				if (card.GetParent() == _choiceHost)
				{
					_choiceHost.RemoveChild(card);
				}
				card.QueueFree();
			}
		}
		_cards.Clear();
		_selectedChoiceId = -1;
		if (GodotObject.IsInstanceValid(_emptyLabel))
		{
			_emptyLabel.Visible = true;
		}
	}

	private void OnCardPressed(int choiceId)
	{
		SelectChoice(choiceId, emitSignal: true);
	}
}
