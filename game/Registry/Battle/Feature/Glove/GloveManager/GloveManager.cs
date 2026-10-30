using Godot;

[GlobalClass]
public partial class GloveManager : Control
{
	public TextureButton gloveButton;

	public Sprite2D gloveSprite;

	public bool gloveShow;

	public bool glovePressedAwait;

	public TowerDefenseMapControl mapControl;

	public TowerDefenseBattleFeatureMap mapFeature;

	public Sprite2D mapGloveSprite;

	public AdobeAnimateSprite previewSprite;

	public override void _Ready()
	{
		gloveButton = GetNode<TextureButton>("%GloveButton");
		gloveSprite = GetNode<Sprite2D>("%GloveSprite");
		if (GodotObject.IsInstanceValid(gloveButton))
		{
			gloveButton.ActionMode = BaseButton.ActionModeEnum.Press;
			gloveButton.Pressed += GloveButtonPressed;
			gloveButton.Toggled += OnGloveButtonToggled;
		}
		mapGloveSprite = new Sprite2D();
		mapGloveSprite.Visible = false;
		if (GodotObject.IsInstanceValid(mapControl) && GodotObject.IsInstanceValid(mapControl.spriteNode))
		{
			mapControl.spriteNode.AddChild(mapGloveSprite, forceReadableName: false, InternalMode.Disabled);
		}
		if (GodotObject.IsInstanceValid(gloveSprite) && GodotObject.IsInstanceValid(gloveSprite.Texture))
		{
			mapGloveSprite.Texture = gloveSprite.Texture;
			ApplyMapGloveSpriteScale();
		}
		UpdateDisplay();
	}

	public override void _ExitTree()
	{
		if (GodotObject.IsInstanceValid(gloveButton))
		{
			gloveButton.Pressed -= GloveButtonPressed;
			gloveButton.Toggled -= OnGloveButtonToggled;
		}
		if (GodotObject.IsInstanceValid(mapGloveSprite))
		{
			mapGloveSprite.QueueFree();
		}
		FreePreviewSprite();
	}

	private void OnGloveButtonToggled(bool _pressed)
	{
		UpdateDisplay();
	}

	private void ApplyMapGloveSpriteScale()
	{
		if (GodotObject.IsInstanceValid(mapGloveSprite) && GodotObject.IsInstanceValid(mapGloveSprite.Texture))
		{
			mapGloveSprite.Scale = Vector2.One * 80f / mapGloveSprite.Texture.GetWidth();
		}
	}

	public override void _Input(InputEvent _event)
	{
		if (GodotObject.IsInstanceValid(gloveButton) && !glovePressedAwait && Input.IsActionJustPressed("Glove") && !TowerDefenseManager.Instance.IsIZMMode() && !TowerDefenseManager.Instance.IsIZM2Mode())
		{
			gloveButton.ButtonPressed = !gloveButton.ButtonPressed;
			TowerDefenseBattleFeatureGlove gloveFeature = TowerDefenseManager.Instance.GetGloveFeature();
			if (GodotObject.IsInstanceValid(gloveFeature))
			{
				gloveFeature.GloveButtonPressed();
			}
		}
	}

	public void Init(TowerDefenseMapControl _mapControl, TowerDefenseBattleFeatureMap _mapFeature)
	{
		mapControl = _mapControl;
		mapFeature = _mapFeature;
		if (GodotObject.IsInstanceValid(mapControl) && GodotObject.IsInstanceValid(mapControl.spriteNode) && GodotObject.IsInstanceValid(mapGloveSprite) && mapGloveSprite.GetParent() == null)
		{
			mapControl.spriteNode.AddChild(mapGloveSprite, forceReadableName: false, InternalMode.Disabled);
		}
	}

	public void GloveButtonPressed()
	{
		TowerDefenseBattleFeatureGlove gloveFeature = TowerDefenseManager.Instance.GetGloveFeature();
		if (GodotObject.IsInstanceValid(gloveFeature))
		{
			gloveFeature.GloveButtonPressed();
		}
	}

	public void UpdateDisplay()
	{
		if (GodotObject.IsInstanceValid(gloveButton) && GodotObject.IsInstanceValid(gloveSprite))
		{
			gloveSprite.Visible = !gloveButton.ButtonPressed;
			if (gloveSprite.Visible && GodotObject.IsInstanceValid(gloveSprite.Texture))
			{
				gloveSprite.Scale = Vector2.One * 80f / gloveSprite.Texture.GetWidth();
			}
		}
	}

	public void UpdateGloveSprite(TowerDefenseMapControl _mapControl)
	{
		if (mapGloveSprite != null)
		{
			mapGloveSprite.Visible = true;
			if (GodotObject.IsInstanceValid(_mapControl) && GodotObject.IsInstanceValid(_mapControl.spriteNode))
			{
				mapGloveSprite.Position = _mapControl.spriteNode.GetLocalMousePosition() - new Vector2(-35f, 35f);
			}
		}
	}

	public void ShowMapGloveSprite(bool show)
	{
		if (GodotObject.IsInstanceValid(mapGloveSprite))
		{
			mapGloveSprite.Visible = show;
		}
	}

	public void SetMapGloveSpritePos(Vector2 pos)
	{
		if (GodotObject.IsInstanceValid(mapGloveSprite))
		{
			mapGloveSprite.Position = pos;
		}
	}

	public void SetGloveButtonPressed(bool pressed)
	{
		if (GodotObject.IsInstanceValid(gloveButton))
		{
			gloveButton.ButtonPressed = pressed;
		}
	}

	public bool IsGloveButtonPressed()
	{
		if (GodotObject.IsInstanceValid(gloveButton))
		{
			return gloveButton.ButtonPressed;
		}
		return false;
	}

	public void CreatePreviewSprite(TowerDefenseCharacter character)
	{
		FreePreviewSprite();
		if (GodotObject.IsInstanceValid(character))
		{
			TowerDefenseCharacterConfig config = character.config;
			previewSprite = TowerDefenseManager.GetCharacterSprite(config.name);
			previewSprite.LightMask = 0;
			Color modulate = previewSprite.Modulate;
			modulate.A = 0.5f;
			previewSprite.Modulate = modulate;
			previewSprite.ZIndex = 1000;
			previewSprite.Position = new Vector2(-100f, -100f);
			Color meshColor = previewSprite.meshColor;
			meshColor.A = 0.5f;
			previewSprite.meshColor = meshColor;
			if (GodotObject.IsInstanceValid(mapControl) && GodotObject.IsInstanceValid(mapControl.spriteNode))
			{
				mapControl.spriteNode.AddChild(previewSprite, forceReadableName: false, InternalMode.Disabled);
			}
		}
	}

	public void FreePreviewSprite()
	{
		if (GodotObject.IsInstanceValid(previewSprite))
		{
			previewSprite.QueueFree();
		}
		previewSprite = null;
	}
}
