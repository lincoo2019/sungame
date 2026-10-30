using Godot;
using Godot.Collections;

[GlobalClass]
public partial class StarExchange : DialogBoxBase
{
	private static Json _starExchangeResource;

	private static PackedScene _starExchangeContainner;

	private VBoxContainer chapterContainer;

	private static Json STAR_EXCHANGE_RESOURCE => _starExchangeResource ?? (_starExchangeResource = GD.Load<Json>("res://Asset/Config/StarExchange/StarExchangeResource.json"));

	private static PackedScene STAR_EXCHANGE_CONTAINNER => _starExchangeContainner ?? (_starExchangeContainner = GD.Load<PackedScene>("uid://clhvbc3i3p6tf"));

	public override void _Ready()
	{
		base._Ready();
		chapterContainer = GetNode<VBoxContainer>("%ChapterContainer");
		GetNode<BaseButton>("%BackButton").Pressed += BackButtonPressed;
		GetNode<BaseButton>("%TryLevelButton").Pressed += TryLevelButtonPressed;
		foreach (Variant item in ((Dictionary)STAR_EXCHANGE_RESOURCE.Data)["Exchange"].AsGodotArray())
		{
			AddChapter(item.AsGodotDictionary());
		}
	}

	public void AddChapter(Dictionary data)
	{
		StarExchangeContainner starExchangeContainner = (StarExchangeContainner)STAR_EXCHANGE_CONTAINNER.Instantiate(PackedScene.GenEditState.Disabled);
		chapterContainer.AddChild(starExchangeContainner, forceReadableName: false, InternalMode.Disabled);
		starExchangeContainner.Init(data);
	}

	public void BackButtonPressed()
	{
		CloseDialog();
	}

	public async void TryLevelButtonPressed()
	{
		AudioManager.Instance.AudioPlay("ButtonPress");
		Global.Instance.enterTryLevelGroup = "Star";
		DialogBoxBase dialogBoxBase = DialogManager.Instance.DialogCreate("TryLevel", new Dictionary { { "openedFromStarExchange", true } });
		TowerDefenseManager.Instance.coinBank.CallDeferred("StartHide");
		base.Visible = false;
		await dialogBoxBase.WaitForClose();
		base.Visible = true;
	}
}
