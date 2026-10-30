using Godot;
using Godot.Collections;

public partial class DailyChallengeAwardItem : Control
{
	public Control spriteNode;

	public Label accessLabel;

	public NinePatchButtonBase getButton;

	public Dictionary data;

	public int finish;

	public override void _Ready()
	{
		spriteNode = GetNode<Control>("%SpriteNode");
		accessLabel = GetNode<Label>("%AccessLabel");
		getButton = GetNode<NinePatchButtonBase>("%GetButton");
		getButton.OnPressed += GetButtonPressed;
	}

	public void Init(Dictionary _data, int _finish)
	{
		data = _data;
		finish = _finish;
		TowerDefenseCharacterConfig characterConfig = TowerDefenseManager.GetPacketConfig((string)data["ShowCharacter"]).characterConfig;
		AdobeAnimateSprite characterSprite = TowerDefenseManager.GetCharacterSprite((string)data["ShowCharacter"]);
		characterConfig.customData.SetCustomFliters(characterSprite, data["ShowCustom"].AsString());
		ScrollContainer scrollContainer = GetParent()?.GetParent<ScrollContainer>();
		if (GodotObject.IsInstanceValid(scrollContainer))
		{
			scrollContainer.ClipContents = true;
			ConfigurePreviewRender(characterSprite, scrollContainer);
		}
		spriteNode.AddChild(characterSprite, forceReadableName: false, InternalMode.Disabled);
		Array array = (Array)data["ConditionArg"];
		accessLabel.Text = $"累计完成{(int)array[0]}天挑战获得";
		if ((int)array[0] > finish)
		{
			getButton.disable = true;
			getButton.text = "无法领取";
		}
		else if (GameSaveManager.Instance.GetFeatureValue((string)data["Key"]) != 0)
		{
			getButton.disable = true;
			getButton.text = "已领取";
		}
	}

	private static void ConfigurePreviewRender(Node node, Control previewClip)
	{
		if (!GodotObject.IsInstanceValid(node))
		{
			return;
		}
		if (node is AdobeAnimateSprite adobeAnimateSprite)
		{
			adobeAnimateSprite.LightMask = 0;
			adobeAnimateSprite.forceLocalRender = true;
			adobeAnimateSprite.SetRenderClipControl(previewClip);
			adobeAnimateSprite.ProcessMode = ProcessModeEnum.Always;
		}
		foreach (Node child in node.GetChildren(includeInternal: true))
		{
			ConfigurePreviewRender(child, previewClip);
		}
	}

	public void GetButtonPressed()
	{
		GameSaveManager.Instance.SetFeatureValue((string)data["Key"], true);
		GameSaveManager.Instance.Save();
		getButton.disable = true;
		getButton.text = "已领取";
	}
}
