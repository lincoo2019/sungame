using System;
using Godot;

[GlobalClass]
public partial class TowerDefenseMapControl : Node2D
{
	public CanvasModulate canvasModulate;

	public Node2D spriteNode;

	public Node2D mapNode;

	public Node2D mapIceCap;

	public CanvasLayer changeMapLayer;

	[Export(PropertyHint.None, "")]
	public CanvasModulate canvasModulateCharacter;

	public Sprite2D editorSprite;

	public TowerDefenseBattleFeatureMap mapFeature;

	private GameSaveManager _gameSaveManager;

	public override void _Ready()
	{
		canvasModulate = GetNode<CanvasModulate>("%CanvasModulate");
		spriteNode = GetNode<Node2D>("%SpriteNode");
		mapNode = GetNode<Node2D>("%MapNode");
		mapIceCap = GetNode<Node2D>("%MapIceCap");
		changeMapLayer = GetNode<CanvasLayer>("%ChangeMapLayer");
		SubscribeGameConfig();
		RefreshMapEffectVisibility();
		if (Global.Instance.isEditor && SceneManager.Instance.currentScene == "LevelEditorStage")
		{
			spriteNode.Scale = Vector2.One * 0.85f;
		}
		foreach (Node child in mapNode.GetChildren())
		{
			child.QueueFree();
		}
	}

	public override void _PhysicsProcess(double _delta)
	{
		if (GodotObject.IsInstanceValid(mapFeature) && GodotObject.IsInstanceValid(TowerDefenseManager.Instance.currentControl) && !TowerDefenseManager.Instance.currentControl.isGameRunning && !TowerDefenseManager.Instance.currentControl.isGameFail)
		{
			mapFeature.ProcessInput();
		}
	}

	public override void _ExitTree()
	{
		if (GodotObject.IsInstanceValid(_gameSaveManager))
		{
			_gameSaveManager.ConfigValueChanged -= OnConfigValueChanged;
		}
		_gameSaveManager = null;
	}

	public void SetCharacterCanvasModulate(CanvasModulate value)
	{
		canvasModulateCharacter = value;
		SubscribeGameConfig();
		RefreshMapEffectVisibility();
	}

	private void SubscribeGameConfig()
	{
		GameSaveManager instance = GameSaveManager.Instance;
		if (_gameSaveManager != instance)
		{
			if (GodotObject.IsInstanceValid(_gameSaveManager))
			{
				_gameSaveManager.ConfigValueChanged -= OnConfigValueChanged;
			}
			_gameSaveManager = instance;
			if (GodotObject.IsInstanceValid(_gameSaveManager))
			{
				_gameSaveManager.ConfigValueChanged += OnConfigValueChanged;
			}
		}
	}

	private void OnConfigValueChanged(string key, Variant value)
	{
		if (string.Equals(key, "MapEffect", StringComparison.Ordinal))
		{
			ApplyMapEffectVisibility(value.AsBool());
		}
	}

	private void RefreshMapEffectVisibility()
	{
		bool visible = !GodotObject.IsInstanceValid(_gameSaveManager) || _gameSaveManager.GetConfigValue("MapEffect").AsBool();
		ApplyMapEffectVisibility(visible);
	}

	private void ApplyMapEffectVisibility(bool visible)
	{
		if (GodotObject.IsInstanceValid(canvasModulate))
		{
			canvasModulate.Visible = visible;
		}
		if (GodotObject.IsInstanceValid(canvasModulateCharacter))
		{
			canvasModulateCharacter.Visible = visible;
		}
	}

	public void UpdateCanvasModulate(double _delta)
	{
		if (GodotObject.IsInstanceValid(canvasModulate) && GodotObject.IsInstanceValid(mapFeature) && mapFeature.isChange && GodotObject.IsInstanceValid(mapFeature.currentGradient) && GodotObject.IsInstanceValid(mapFeature.currentGradient.Gradient))
		{
			canvasModulate.Color = mapFeature.currentGradient.Gradient.Sample((float)mapFeature.currentGradientPos);
			if (GodotObject.IsInstanceValid(canvasModulateCharacter))
			{
				canvasModulateCharacter.Color = canvasModulate.Color;
			}
		}
	}

	public Vector2 ConvertMapWorldToSpriteNodePosition(Vector2 mapWorldPosition, Vector2 screenOffset)
	{
		if (!GodotObject.IsInstanceValid(spriteNode))
		{
			return Vector2.Zero;
		}
		Vector2 vector = GetCanvasTransform() * mapWorldPosition + screenOffset;
		return spriteNode.GetGlobalTransformWithCanvas().AffineInverse() * vector;
	}

	public void PositionMapOverlayAtWorldPoint(Node2D overlay, Vector2 mapWorldPosition, Vector2 screenOffset)
	{
		if (GodotObject.IsInstanceValid(overlay))
		{
			overlay.Position = ConvertMapWorldToSpriteNodePosition(mapWorldPosition, screenOffset);
		}
	}

	public bool IsConfirmInput()
	{
		if (Global.Instance.isEditor && SceneManager.CurrentScene == "LevelEditorStage" && GodotObject.IsInstanceValid(LevelEditorMapEditor.instance) && LevelEditorMapEditor.instance.ShouldSuppressPlacementConfirm)
		{
			return false;
		}
		if (Global.Instance.isMobile)
		{
			return Input.IsActionJustReleased("Press");
		}
		return Input.IsActionJustPressed("Press");
	}
}
