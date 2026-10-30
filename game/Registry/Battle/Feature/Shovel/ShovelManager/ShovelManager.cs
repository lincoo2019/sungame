using System;
using Godot;

[GlobalClass]
public partial class ShovelManager : Control
{
	private const string ToolHighlightShaderParameter = "toolHighlightStrength";

	private const float ToolHighlightStrength = 0.5f;

	public TextureButton shovelButton;

	public Sprite2D shovelSprite;

	public ShovelConfig shovelConfig;

	public bool shovelShow;

	public bool shovelPressedAwait;

	public TowerDefenseMapControl mapControl;

	public TowerDefenseBattleFeatureMap mapFeature;

	public bool shovelPick;

	public Sprite2D mapShovelSprite;

	public TowerDefenseCharacter lastShovelPlant;

	private SceneTreeTimer _pressDebounceTimer;

	private Action _pressDebounceHandler;

	private GlobalFeatureManager _globalFeatureManager;

	public override void _Ready()
	{
		shovelButton = GetNode<TextureButton>("%ShovelButton");
		shovelSprite = GetNode<Sprite2D>("%ShovelSprite");
		_globalFeatureManager = GlobalFeatureManager.Instance;
		if (_globalFeatureManager != null)
		{
			_globalFeatureManager.FeatureChanged += OnGlobalFeatureChanged;
		}
		if (GodotObject.IsInstanceValid(shovelButton))
		{
			shovelButton.ActionMode = BaseButton.ActionModeEnum.Press;
			shovelButton.Pressed += ShovelButtonPressed;
		}
		string text = GameSaveManager.Instance.GetKeyValue("CurrentShovel").AsString();
		if (text == "")
		{
			text = "ShovelDefault";
		}
		shovelConfig = TowerDefenseManager.GetShovel(text);
		if (GodotObject.IsInstanceValid(shovelSprite))
		{
			shovelSprite.Texture = shovelConfig.texture;
		}
		mapShovelSprite = new Sprite2D();
		mapShovelSprite.Visible = false;
		if (GodotObject.IsInstanceValid(mapControl) && GodotObject.IsInstanceValid(mapControl.spriteNode))
		{
			mapControl.spriteNode.AddChild(mapShovelSprite, forceReadableName: false, InternalMode.Disabled);
		}
		if (GodotObject.IsInstanceValid(shovelConfig))
		{
			mapShovelSprite.Texture = shovelConfig.texture;
		}
		SetPhysicsProcess(enable: false);
		Callable.From(UpdateModeVisibility).CallDeferred();
	}

	private void OnGlobalFeatureChanged(string featureId, int _oldValue, int _newValue)
	{
		if (featureId == "Shovel")
		{
			UpdateModeVisibility();
		}
	}

	private bool CanUseShovel()
	{
		CommandManager instance = CommandManager.Instance;
		if (instance == null || !instance.debugPacketOpenAll)
		{
			GlobalFeatureManager globalFeatureManager = _globalFeatureManager;
			if (globalFeatureManager == null || !globalFeatureManager.IsUnlocked("Shovel"))
			{
				goto IL_0049;
			}
		}
		if (!TowerDefenseManager.Instance.IsIZMMode())
		{
			return !TowerDefenseManager.Instance.IsIZM2Mode();
		}
		goto IL_0049;
		IL_0049:
		return false;
	}

	private void UpdateModeVisibility()
	{
		if (IsInsideTree() && GodotObject.IsInstanceValid(shovelButton) && GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
		{
			bool flag = CanUseShovel();
			shovelButton.Visible = flag;
			if (!flag)
			{
				ShovelReset();
			}
		}
	}

	public override void _ExitTree()
	{
		if (_globalFeatureManager != null)
		{
			_globalFeatureManager.FeatureChanged -= OnGlobalFeatureChanged;
		}
		_globalFeatureManager = null;
		ClearPressDebounce();
		shovelPressedAwait = false;
		if (GodotObject.IsInstanceValid(shovelButton))
		{
			shovelButton.Pressed -= ShovelButtonPressed;
		}
		ShovelReset();
		if (GodotObject.IsInstanceValid(mapShovelSprite))
		{
			mapShovelSprite.QueueFree();
		}
		mapShovelSprite = null;
		mapControl = null;
		mapFeature = null;
		shovelConfig = null;
		base._ExitTree();
	}

	private void UpdateShovelSlotDisplay()
	{
		if (GodotObject.IsInstanceValid(shovelSprite))
		{
			shovelSprite.Visible = !shovelPick;
			if (shovelSprite.Visible && GodotObject.IsInstanceValid(shovelSprite.Texture))
			{
				shovelSprite.Scale = Vector2.One * 80f / shovelSprite.Texture.GetWidth();
			}
		}
	}

	private void ApplyShovelPickState(bool picked)
	{
		shovelPick = picked;
		if (GodotObject.IsInstanceValid(shovelButton))
		{
			shovelButton.SetPressedNoSignal(picked);
		}
		if (GodotObject.IsInstanceValid(mapShovelSprite))
		{
			mapShovelSprite.Visible = picked;
		}
		UpdateShovelSlotDisplay();
	}

	public override void _Input(InputEvent _event)
	{
		if (GodotObject.IsInstanceValid(shovelButton) && CanUseShovel() && !shovelPressedAwait && Input.IsActionJustPressed("Shovel") && !TowerDefenseManager.Instance.IsIZMMode() && !TowerDefenseManager.Instance.IsIZM2Mode())
		{
			shovelButton.ButtonPressed = !shovelButton.ButtonPressed;
			ShovelButtonPressed();
		}
	}

	public void ShovelButtonPressed()
	{
		if (!CanUseShovel())
		{
			ApplyShovelPickState(picked: false);
		}
		else if (!shovelShow && !TowerDefenseManager.Instance.currentControl.isGameRunning)
		{
			ApplyShovelPickState(picked: false);
		}
		else if (!shovelPressedAwait)
		{
			PickShovel(shovelButton.ButtonPressed);
			shovelPressedAwait = true;
			ClearPressDebounce();
			_pressDebounceTimer = GetTree().CreateTimer(0.1, processAlways: false);
			_pressDebounceHandler = OnPressDebounceTimeout;
			_pressDebounceTimer.Timeout += _pressDebounceHandler;
		}
	}

	private void OnPressDebounceTimeout()
	{
		ClearPressDebounce();
		if (IsInsideTree())
		{
			shovelPressedAwait = false;
		}
	}

	private void ClearPressDebounce()
	{
		if (GodotObject.IsInstanceValid(_pressDebounceTimer) && _pressDebounceHandler != null)
		{
			_pressDebounceTimer.Timeout -= _pressDebounceHandler;
		}
		_pressDebounceTimer = null;
		_pressDebounceHandler = null;
	}

	public void Init(TowerDefenseMapControl _mapControl, TowerDefenseBattleFeatureMap _mapFeature)
	{
		mapControl = _mapControl;
		mapFeature = _mapFeature;
		if (GodotObject.IsInstanceValid(mapControl) && GodotObject.IsInstanceValid(mapControl.spriteNode) && GodotObject.IsInstanceValid(mapShovelSprite) && mapShovelSprite.GetParent() == null)
		{
			mapControl.spriteNode.AddChild(mapShovelSprite, forceReadableName: false, InternalMode.Disabled);
		}
	}

	public void PickShovel(bool open)
	{
		if (GodotObject.IsInstanceValid(mapFeature) && GodotObject.IsInstanceValid(mapFeature.packetPickControl))
		{
			mapFeature.packetPickControl.PacketPickRelease();
		}
		if (open)
		{
			AudioManager.Instance.AudioPlay("Shovel");
			if (GodotObject.IsInstanceValid(mapShovelSprite))
			{
				mapShovelSprite.Position = new Vector2(-100f, -100f);
			}
		}
		else
		{
			AudioManager.Instance.AudioPlay("ShovelDeny");
		}
		ApplyShovelPickState(open);
	}

	public void ProcessShovelPick(TowerDefenseCellInstance cell, Vector2I gridPos, Vector2 mousePos)
	{
		if (mapShovelSprite != null)
		{
			mapShovelSprite.Visible = true;
			if (mapShovelSprite.Texture != null)
			{
				mapShovelSprite.Scale = Vector2.One * 80f / mapShovelSprite.Texture.GetWidth();
			}
			if (GodotObject.IsInstanceValid(mapControl) && GodotObject.IsInstanceValid(mapControl.spriteNode))
			{
				mapControl.PositionMapOverlayAtWorldPoint(mapShovelSprite, mousePos, new Vector2(35f, -35f));
			}
		}
		if (TowerDefenseManager.Instance.CheckMapGridPosIn(gridPos))
		{
			double groundHeight = mapFeature.GetGroundHeight(cell);
			Vector2 mapCellPos = TowerDefenseManager.Instance.GetMapCellPos(gridPos);
			Vector2 mapGridSize = TowerDefenseManager.Instance.GetMapGridSize();
			double percentage = (mousePos - mapCellPos + new Vector2(0f, (float)groundHeight)).Y / mapGridSize.Y;
			TowerDefenseCharacter shovelCharacter = cell.GetShovelCharacter(percentage, shovelConfig);
			if (shovelCharacter != lastShovelPlant)
			{
				SetToolHighlight(lastShovelPlant, 0f);
				lastShovelPlant = shovelCharacter;
				SetToolHighlight(lastShovelPlant, 0.5f);
			}
			if (!cell.CanShovel(percentage, shovelConfig) || !mapControl.IsConfirmInput())
			{
				return;
			}
			AudioManager.Instance.AudioPlay("ShovelDig");
			if (Global.Instance.isMultiplayerMode)
			{
				MultiPlayerManager.Instance.SendRemovePlant(gridPos.X, gridPos.Y);
				cell.Shovel(shovelConfig, percentage);
				mapFeature.packetPickControl.Release();
			}
			else if (!Global.Instance.isEditor || SceneManager.Instance.currentScene != "LevelEditorStage")
			{
				cell.Shovel(shovelConfig, percentage);
				mapFeature.packetPickControl.Release();
			}
			else
			{
				cell.Shovel(TowerDefenseManager.GetShovel("ShovelDefault"), percentage);
				if (GodotObject.IsInstanceValid(LevelEditorMapEditor.instance) && GodotObject.IsInstanceValid(LevelEditorMapEditor.instance.levelConfig))
				{
					LevelEditorMapEditor.instance.levelConfig.canExport = false;
				}
			}
			lastShovelPlant = null;
		}
		else if (GodotObject.IsInstanceValid(lastShovelPlant))
		{
			SetToolHighlight(lastShovelPlant, 0f);
			lastShovelPlant = null;
		}
	}

	private static void SetToolHighlight(TowerDefenseCharacter character, float strength)
	{
		if (GodotObject.IsInstanceValid(character))
		{
			character.SetSpriteGroupShaderParameter("toolHighlightStrength", strength);
		}
	}

	public void ShovelRelease()
	{
		if (shovelPick)
		{
			AudioManager.Instance.AudioPlay("ShovelDeny");
		}
		ApplyShovelPickState(picked: false);
		if (GodotObject.IsInstanceValid(lastShovelPlant))
		{
			SetToolHighlight(lastShovelPlant, 0f);
			lastShovelPlant = null;
		}
	}

	public void ShovelReset()
	{
		ApplyShovelPickState(picked: false);
		if (GodotObject.IsInstanceValid(lastShovelPlant))
		{
			SetToolHighlight(lastShovelPlant, 0f);
			lastShovelPlant = null;
		}
	}

	public void Start()
	{
		if (GodotObject.IsInstanceValid(shovelButton))
		{
			UpdateModeVisibility();
		}
	}
}
