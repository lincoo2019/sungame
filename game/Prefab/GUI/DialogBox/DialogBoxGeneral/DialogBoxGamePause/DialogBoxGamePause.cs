using Godot;

public partial class DialogBoxGamePause : DialogPopup
{
	public Control spriteNode;

	public AdobeAnimateSprite sprite;

	public override void _Ready()
	{
		base._Ready();
		spriteNode = GetNode<Control>("%SpriteNode");
		GetNode<BaseButton>("%TrueButton").Pressed += TrueButtonPressed;
		string randomSpriteName = ResourceManager.Instance.GetRandomSpriteName();
		PackedScene characterSprite = ResourceManager.Instance.GetCharacterSprite(randomSpriteName);
		if (characterSprite != null)
		{
			sprite = characterSprite.Instantiate<AdobeAnimateSprite>(PackedScene.GenEditState.Disabled);
			sprite.LightMask = 0;
			spriteNode.AddChild(sprite, forceReadableName: false, InternalMode.Disabled);
		}
	}

	public void TrueButtonPressed()
	{
		CloseDialog();
	}

	public override void _Input(InputEvent inputEvent)
	{
		if (!IsQueuedForDeletion() && inputEvent.IsActionPressed("Pause"))
		{
			GetViewport().SetInputAsHandled();
			CloseDialog();
		}
	}
}
