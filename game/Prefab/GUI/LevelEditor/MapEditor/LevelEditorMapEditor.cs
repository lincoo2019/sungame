using System.Collections.Generic;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class LevelEditorMapEditor : Control
{
	private static readonly Vector2 MapPreviewLogicalSize = new Vector2(1200f, 600f);

	private const float MouseWheelZoomFactor = 1.1f;

	private const float MaximumMapPreviewZoom = 2f;

	private const float MinimumPinchDistance = 12f;

	public static LevelEditorMapEditor instance;

	private Node2D _mapViewport;

	private Node2D _characterNode;

	private TextureButton _shovelButton;

	private Control _packetBank;

	private Control _transformNode;

	private TowerDefenseMapControl _mapControl;

	private TowerDefenseMapConfig _previewMapConfig;

	private readonly System.Collections.Generic.Dictionary<int, Vector2> _touchPositions = new System.Collections.Generic.Dictionary<int, Vector2>();

	private float _minimumMapPreviewZoom = 1f;

	private float _previousPinchDistance;

	private Vector2 _previousPinchCenter;

	private bool _pinchActive;

	private bool _suppressTouchConfirm;

	private ulong _suppressTouchConfirmThroughPhysicsFrame;

	private bool _mousePanActive;

	private MouseButton _mousePanButton;

	private Vector2 _previousMousePanPosition;

	private bool _ownsMapFeature;

	private PacketPickControl _ownedPacketPickControl;

	private ShovelManager _ownedShovelManager;

	private ShovelPickTool _ownedShovelPickTool;

	[Export(PropertyHint.None, "")]
	public TowerDefenseLevelConfig levelConfig;

	[Export(PropertyHint.None, "")]
	public bool isolatedPreviewMode;

	public TowerDefenseBattleFeatureMap mapFeature;

	public bool shovelShow;

	public Node2D characterNode => _characterNode;

	public float MapPreviewZoom
	{
		get
		{
			if (!GodotObject.IsInstanceValid(_mapViewport))
			{
				return 1f;
			}
			return _mapViewport.Scale.X;
		}
	}

	public bool ShouldSuppressPlacementConfirm
	{
		get
		{
			if (!_suppressTouchConfirm)
			{
				return Engine.GetPhysicsFrames() <= _suppressTouchConfirmThroughPhysicsFrame;
			}
			return true;
		}
	}

	public bool IsMapPanActive => _mousePanActive;

	public override void _Ready()
	{
		instance = this;
		_mapViewport = GetNode<Node2D>("%MapViewport");
		_characterNode = GetNode<Node2D>("%CharacterNode");
		_shovelButton = GetNode<TextureButton>("%ShovelButton");
		_packetBank = GetNode<Control>("%LevelEditorPacketBank");
		_transformNode = GetNode<Control>("TransformNode");
		_mapControl = GetNode<TowerDefenseMapControl>("%TowerDefenseMapControl");
		if (!isolatedPreviewMode)
		{
			base.VisibilityChanged += Save;
		}
		base.VisibilityChanged += OnChildPacketBankVisibilityChanged;
		_shovelButton.Pressed += ShovelButtonPressed;
		mapFeature = (isolatedPreviewMode ? null : TowerDefenseManager.GetMapFeature());
		if (!GodotObject.IsInstanceValid(mapFeature) && GodotObject.IsInstanceValid(_mapControl))
		{
			mapFeature = new TowerDefenseBattleFeatureMap();
			mapFeature.mapControl = _mapControl;
			_mapControl.mapFeature = mapFeature;
			_ownsMapFeature = true;
		}
		if (!GodotObject.IsInstanceValid(mapFeature) || !GodotObject.IsInstanceValid(mapFeature.mapControl))
		{
			return;
		}
		if (!GodotObject.IsInstanceValid(mapFeature.packetPickControl))
		{
			mapFeature.packetPickControl = new PacketPickControl();
			mapFeature.packetPickControl.Init(mapFeature.mapControl, mapFeature);
			mapFeature.mapControl.AddChild(mapFeature.packetPickControl, forceReadableName: false, InternalMode.Disabled);
			_ownedPacketPickControl = mapFeature.packetPickControl;
		}
		if (!GodotObject.IsInstanceValid(mapFeature.shovelManager))
		{
			ShovelManager shovelManager = new ShovelManager();
			shovelManager.mapShovelSprite = new Sprite2D();
			shovelManager.mapShovelSprite.Visible = false;
			if (GodotObject.IsInstanceValid(ResourceManager.Instance) && ResourceManager.Instance.SHOVELS.TryGetValue("ShovelDefault", out var value))
			{
				shovelManager.shovelConfig = value as ShovelConfig;
			}
			if (GodotObject.IsInstanceValid(shovelManager.shovelConfig))
			{
				shovelManager.mapShovelSprite.Texture = shovelManager.shovelConfig.texture;
			}
			shovelManager.shovelButton = _shovelButton;
			shovelManager.Init(mapFeature.mapControl, mapFeature);
			mapFeature.shovelManager = shovelManager;
			ShovelPickTool shovelPickTool = new ShovelPickTool();
			shovelPickTool.Init(mapFeature.mapControl);
			shovelPickTool.SetShovelManager(shovelManager);
			mapFeature.packetPickControl.RegisterTool(shovelPickTool);
			_ownedShovelManager = shovelManager;
			_ownedShovelPickTool = shovelPickTool;
		}
	}

	public override void _Input(InputEvent inputEvent)
	{
		if (!Global.Instance.isMobile || (!(inputEvent is InputEventMouseButton) && !(inputEvent is InputEventMouseMotion)))
		{
			if (inputEvent is InputEventMouseButton mouseButton)
			{
				HandleMousePanButton(mouseButton);
				HandleMouseWheelInput(mouseButton);
			}
			else if (inputEvent is InputEventMouseMotion mouseMotion)
			{
				HandleMousePanMotion(mouseMotion);
			}
			else if (inputEvent is InputEventScreenTouch || inputEvent is InputEventScreenDrag)
			{
				HandleTouchInput(inputEvent);
			}
		}
	}

	public override void _ExitTree()
	{
		ResetMousePan();
		ResetTouchGesture();
		if (!isolatedPreviewMode)
		{
			base.VisibilityChanged -= Save;
		}
		base.VisibilityChanged -= OnChildPacketBankVisibilityChanged;
		if (GodotObject.IsInstanceValid(_shovelButton))
		{
			_shovelButton.Pressed -= ShovelButtonPressed;
		}
		PacketPickControl packetPickControl = (GodotObject.IsInstanceValid(mapFeature) ? mapFeature.packetPickControl : _ownedPacketPickControl);
		if (GodotObject.IsInstanceValid(_ownedShovelPickTool))
		{
			if (GodotObject.IsInstanceValid(packetPickControl))
			{
				packetPickControl.UnregisterTool(_ownedShovelPickTool);
			}
			_ownedShovelPickTool.Free();
			_ownedShovelPickTool = null;
		}
		if (GodotObject.IsInstanceValid(_ownedShovelManager))
		{
			if (GodotObject.IsInstanceValid(_ownedShovelManager.mapShovelSprite))
			{
				_ownedShovelManager.mapShovelSprite.QueueFree();
			}
			_ownedShovelManager.mapShovelSprite = null;
			_ownedShovelManager.mapControl = null;
			_ownedShovelManager.mapFeature = null;
			_ownedShovelManager.Free();
			_ownedShovelManager = null;
		}
		if (GodotObject.IsInstanceValid(_ownedPacketPickControl))
		{
			_ownedPacketPickControl.DisposeBattleState();
		}
		if (_ownsMapFeature && GodotObject.IsInstanceValid(mapFeature))
		{
			mapFeature.packetPickControl = null;
			mapFeature.shovelManager = null;
			mapFeature.mapControl = null;
			if (GodotObject.IsInstanceValid(_mapControl))
			{
				_mapControl.mapFeature = null;
			}
		}
		_ownedPacketPickControl = null;
		_previewMapConfig = null;
		_mapViewport = null;
		_packetBank = null;
		_transformNode = null;
		mapFeature = null;
		if (instance == this)
		{
			instance = null;
		}
		base._ExitTree();
	}

	public void ApplyMapPreviewConfig(TowerDefenseMapConfig mapConfig)
	{
		if (!GodotObject.IsInstanceValid(mapConfig) || !GodotObject.IsInstanceValid(_mapViewport) || !GodotObject.IsInstanceValid(_mapControl))
		{
			return;
		}
		_previewMapConfig = mapConfig;
		if (GodotObject.IsInstanceValid(_mapControl.editorSprite))
		{
			Texture2D texture = _mapControl.editorSprite.Texture;
			if (GodotObject.IsInstanceValid(texture))
			{
				_mapControl.editorSprite.Scale = new Vector2((texture.GetWidth() > 0) ? (mapConfig.mapSize.X / (float)texture.GetWidth()) : 1f, (texture.GetHeight() > 0) ? (mapConfig.mapSize.Y / (float)texture.GetHeight()) : 1f);
			}
			else
			{
				_mapControl.editorSprite.Scale = Vector2.One;
			}
		}
		float a = ((mapConfig.mapSize.X > 0f) ? (MapPreviewLogicalSize.X / mapConfig.mapSize.X) : 1f);
		float b = ((mapConfig.mapSize.Y > 0f) ? (MapPreviewLogicalSize.Y / mapConfig.mapSize.Y) : 1f);
		_minimumMapPreviewZoom = Mathf.Clamp(Mathf.Min(1f, Mathf.Min(a, b)), 0.05f, 1f);
		_mapViewport.Scale = Vector2.One * _minimumMapPreviewZoom;
		CenterMapPreview();
		ResetMousePan();
		ResetTouchGesture();
		RefreshMapGridMetrics();
	}

	private void HandleMousePanButton(InputEventMouseButton mouseButton)
	{
		if (!mouseButton.Pressed && _mousePanActive && mouseButton.ButtonIndex == _mousePanButton)
		{
			ResetMousePan();
			GetViewport().SetInputAsHandled();
		}
		else if (mouseButton.Pressed && CanProcessMapPreviewInput(mouseButton.Position) && IsMousePanButtonAvailable(mouseButton.ButtonIndex))
		{
			_mousePanActive = true;
			_mousePanButton = mouseButton.ButtonIndex;
			_previousMousePanPosition = mouseButton.Position;
			GetViewport().SetInputAsHandled();
		}
	}

	private void HandleMousePanMotion(InputEventMouseMotion mouseMotion)
	{
		if (_mousePanActive && CanProcessMapPreviewInput())
		{
			Transform2D globalTransformWithCanvas = _transformNode.GetGlobalTransformWithCanvas();
			Vector2 vector = globalTransformWithCanvas.AffineInverse() * _previousMousePanPosition;
			Vector2 vector2 = globalTransformWithCanvas.AffineInverse() * mouseMotion.Position;
			_mapViewport.Position += vector2 - vector;
			_previousMousePanPosition = mouseMotion.Position;
			ClampMapPreviewPosition();
			RefreshMapGridMetrics();
			GetViewport().SetInputAsHandled();
		}
	}

	private bool IsMousePanButtonAvailable(MouseButton button)
	{
		switch (button)
		{
		case MouseButton.Right:
		case MouseButton.Middle:
			return true;
		default:
			return false;
		case MouseButton.Left:
			if (GodotObject.IsInstanceValid(mapFeature?.packetPickControl))
			{
				return !mapFeature.packetPickControl.IsPicking();
			}
			return true;
		}
	}

	private void ResetMousePan()
	{
		_mousePanActive = false;
		_mousePanButton = MouseButton.None;
		_previousMousePanPosition = Vector2.Zero;
	}

	private void HandleMouseWheelInput(InputEventMouseButton mouseButton)
	{
		if (CanProcessMapPreviewInput(mouseButton.Position) && mouseButton.Pressed && (mouseButton.ButtonIndex == MouseButton.WheelUp || mouseButton.ButtonIndex == MouseButton.WheelDown))
		{
			float y = Mathf.Max(1f, mouseButton.Factor);
			float x = ((mouseButton.ButtonIndex == MouseButton.WheelUp) ? 1.1f : 0.9090909f);
			ApplyMapPreviewZoomAtScreenAnchor(MapPreviewZoom * Mathf.Pow(x, y), mouseButton.Position, mouseButton.Position);
			GetViewport().SetInputAsHandled();
		}
	}

	private void HandleTouchInput(InputEvent inputEvent)
	{
		if (!CanProcessMapPreviewInput())
		{
			ResetTouchGesture();
			return;
		}
		if (inputEvent is InputEventScreenTouch inputEventScreenTouch)
		{
			if (inputEventScreenTouch.Pressed)
			{
				if (CanProcessMapPreviewInput(inputEventScreenTouch.Position))
				{
					_touchPositions[inputEventScreenTouch.Index] = inputEventScreenTouch.Position;
				}
			}
			else
			{
				_touchPositions.Remove(inputEventScreenTouch.Index);
				if (_touchPositions.Count == 0 && _suppressTouchConfirm)
				{
					_suppressTouchConfirm = false;
					_suppressTouchConfirmThroughPhysicsFrame = Engine.GetPhysicsFrames() + 1;
				}
			}
			if (_touchPositions.Count == 2)
			{
				_suppressTouchConfirm = true;
			}
			RefreshPinchBaseline();
			return;
		}
		InputEventScreenDrag inputEventScreenDrag = (InputEventScreenDrag)inputEvent;
		if (!_touchPositions.ContainsKey(inputEventScreenDrag.Index))
		{
			return;
		}
		_touchPositions[inputEventScreenDrag.Index] = inputEventScreenDrag.Position;
		if (_touchPositions.Count != 2)
		{
			RefreshPinchBaseline();
			return;
		}
		_suppressTouchConfirm = true;
		GetPinchGeometry(out var distance, out var center);
		if (!_pinchActive || _previousPinchDistance < 12f || distance < 12f)
		{
			_previousPinchDistance = distance;
			_previousPinchCenter = center;
			_pinchActive = distance >= 12f;
		}
		else
		{
			ApplyMapPreviewZoomAtScreenAnchor(MapPreviewZoom * distance / _previousPinchDistance, center, _previousPinchCenter);
			_previousPinchDistance = distance;
			_previousPinchCenter = center;
			GetViewport().SetInputAsHandled();
		}
	}

	private bool CanProcessMapPreviewInput()
	{
		if (IsVisibleInTree() && GodotObject.IsInstanceValid(_previewMapConfig) && GodotObject.IsInstanceValid(_mapViewport))
		{
			return GodotObject.IsInstanceValid(_transformNode);
		}
		return false;
	}

	private bool CanProcessMapPreviewInput(Vector2 screenPosition)
	{
		if (CanProcessMapPreviewInput() && IsScreenPointInsideMapPreview(screenPosition))
		{
			return !IsScreenPointOverEditorControls(screenPosition);
		}
		return false;
	}

	private bool IsScreenPointInsideMapPreview(Vector2 screenPosition)
	{
		Vector2 point = _transformNode.GetGlobalTransformWithCanvas().AffineInverse() * screenPosition;
		return GetMapPreviewInputRect().HasPoint(point);
	}

	private Rect2 GetMapPreviewInputRect()
	{
		Rect2 result = new Rect2(Vector2.Zero, MapPreviewLogicalSize);
		if (!GodotObject.IsInstanceValid(_packetBank) || !_packetBank.IsVisibleInTree())
		{
			return result;
		}
		Vector2 vector = _transformNode.GetGlobalTransformWithCanvas().AffineInverse() * _packetBank.GetGlobalRect().Position;
		if (vector.X > result.Position.X && vector.X < result.End.X)
		{
			result.Size = new Vector2(vector.X - result.Position.X, result.Size.Y);
		}
		return result;
	}

	private bool IsScreenPointOverEditorControls(Vector2 screenPosition)
	{
		if ((!GodotObject.IsInstanceValid(_packetBank) || !_packetBank.GetGlobalRect().HasPoint(screenPosition)) && (!GodotObject.IsInstanceValid(_shovelButton) || !_shovelButton.GetGlobalRect().HasPoint(screenPosition)))
		{
			return IsPointerOverEditorControls(screenPosition);
		}
		return true;
	}

	private bool IsPointerOverEditorControls(Vector2 screenPosition)
	{
		Control control = GetViewport()?.GuiGetHoveredControl();
		if (!GodotObject.IsInstanceValid(control) || !control.GetGlobalRect().HasPoint(screenPosition))
		{
			return false;
		}
		while (GodotObject.IsInstanceValid(control))
		{
			if (control == _packetBank || control == _shovelButton || control is BaseButton)
			{
				return true;
			}
			if (control == this)
			{
				return false;
			}
			control = control.GetParent() as Control;
		}
		return false;
	}

	private void RefreshPinchBaseline()
	{
		if (_touchPositions.Count != 2)
		{
			_pinchActive = false;
			_previousPinchDistance = 0f;
			_previousPinchCenter = Vector2.Zero;
		}
		else
		{
			GetPinchGeometry(out _previousPinchDistance, out _previousPinchCenter);
			_pinchActive = _previousPinchDistance >= 12f;
		}
	}

	private void GetPinchGeometry(out float distance, out Vector2 center)
	{
		using System.Collections.Generic.Dictionary<int, Vector2>.ValueCollection.Enumerator enumerator = _touchPositions.Values.GetEnumerator();
		enumerator.MoveNext();
		Vector2 current = enumerator.Current;
		enumerator.MoveNext();
		Vector2 current2 = enumerator.Current;
		distance = current.DistanceTo(current2);
		center = (current + current2) * 0.5f;
	}

	private void ApplyMapPreviewZoomAtScreenAnchor(float targetZoom, Vector2 currentScreenAnchor, Vector2 previousScreenAnchor)
	{
		float num = Mathf.Max(MapPreviewZoom, 0.001f);
		float num2 = Mathf.Clamp(targetZoom, _minimumMapPreviewZoom, 2f);
		Transform2D globalTransformWithCanvas = _transformNode.GetGlobalTransformWithCanvas();
		Vector2 vector = globalTransformWithCanvas.AffineInverse() * previousScreenAnchor;
		Vector2 vector2 = globalTransformWithCanvas.AffineInverse() * currentScreenAnchor;
		Vector2 vector3 = (vector - _mapViewport.Position) / num;
		_mapViewport.Scale = Vector2.One * num2;
		_mapViewport.Position = vector2 - vector3 * num2;
		ClampMapPreviewPosition();
		RefreshMapGridMetrics();
	}

	private void CenterMapPreview()
	{
		Vector2 mapPreviewOrigin = GetMapPreviewOrigin();
		Rect2 mapPreviewInputRect = GetMapPreviewInputRect();
		Vector2 vector = _previewMapConfig.mapSize * MapPreviewZoom;
		float x = ((vector.X > mapPreviewInputRect.Size.X) ? (mapPreviewInputRect.End.X - (mapPreviewOrigin.X + _previewMapConfig.mapSize.X) * MapPreviewZoom) : (mapPreviewInputRect.Position.X + (mapPreviewInputRect.Size.X - vector.X) * 0.5f - mapPreviewOrigin.X * MapPreviewZoom));
		float y = (MapPreviewLogicalSize.Y - vector.Y) * 0.5f - mapPreviewOrigin.Y * MapPreviewZoom;
		_mapViewport.Position = new Vector2(x, y);
		ClampMapPreviewPosition();
	}

	private void ClampMapPreviewPosition()
	{
		if (GodotObject.IsInstanceValid(_previewMapConfig) && GodotObject.IsInstanceValid(_mapViewport))
		{
			Vector2 mapPreviewOrigin = GetMapPreviewOrigin();
			Rect2 mapPreviewInputRect = GetMapPreviewInputRect();
			float mapPreviewZoom = MapPreviewZoom;
			_mapViewport.Position = new Vector2(ClampMapPreviewAxis(_mapViewport.Position.X, mapPreviewOrigin.X, _previewMapConfig.mapSize.X, mapPreviewInputRect.Position.X, mapPreviewInputRect.Size.X, mapPreviewZoom), ClampMapPreviewAxis(_mapViewport.Position.Y, mapPreviewOrigin.Y, _previewMapConfig.mapSize.Y, 0f, MapPreviewLogicalSize.Y, mapPreviewZoom));
		}
	}

	private static float ClampMapPreviewAxis(float position, float contentOrigin, float contentLength, float viewportStart, float viewportLength, float zoom)
	{
		float num = contentLength * zoom;
		if (num <= viewportLength)
		{
			return viewportStart + (viewportLength - num) * 0.5f - contentOrigin * zoom;
		}
		float min = viewportStart + viewportLength - (contentOrigin + contentLength) * zoom;
		float max = viewportStart - contentOrigin * zoom;
		return Mathf.Clamp(position, min, max);
	}

	private Vector2 GetMapPreviewOrigin()
	{
		return _mapControl.Position + _previewMapConfig.mapOffset;
	}

	private void RefreshMapGridMetrics()
	{
		if (GodotObject.IsInstanceValid(mapFeature))
		{
			mapFeature.NotifyMapTransformChanged();
		}
	}

	private void ResetTouchGesture()
	{
		_touchPositions.Clear();
		_previousPinchDistance = 0f;
		_previousPinchCenter = Vector2.Zero;
		_pinchActive = false;
		_suppressTouchConfirm = false;
		_suppressTouchConfirmThroughPhysicsFrame = 0uL;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (GodotObject.IsInstanceValid(mapFeature))
		{
			mapFeature.Process((float)delta);
		}
	}

	public void Init(TowerDefenseLevelConfig _levelConfig)
	{
		levelConfig = _levelConfig;
	}

	public void OnChildPacketBankVisibilityChanged()
	{
		if (GodotObject.IsInstanceValid(LevelEditorPacketBank.Instance))
		{
			LevelEditorPacketBank.Instance.RefreshWhenVisible();
		}
	}

	public void Save()
	{
		Save(isSave: false);
	}

	public void Save(bool isSave)
	{
		Release();
		if (!GodotObject.IsInstanceValid(levelConfig))
		{
			return;
		}
		bool flag = false;
		if (levelConfig.finishMethod == TowerDefenseEnum.LEVEL_FINISH_METHOD.VASE)
		{
			flag = true;
		}
		if ((isSave && IsVisibleInTree()) || (!isSave && !IsVisibleInTree()))
		{
			levelConfig.preSpawnList.Clear();
			if (flag)
			{
				levelConfig.vaseManager.vaseList.Clear();
			}
			Array<TowerDefenseCharacter> array = new Array<TowerDefenseCharacter>();
			if (GodotObject.IsInstanceValid(mapFeature))
			{
				for (int i = 1; i < mapFeature.config.gridNum.X + 1; i++)
				{
					for (int j = 1; j < mapFeature.config.gridNum.Y + 1; j++)
					{
						foreach (TowerDefenseCharacter item4 in mapFeature.plantGrid[i][j].As<TowerDefenseCellInstance>().GetCharacterListSave())
						{
							if (!array.Contains(item4))
							{
								if (flag && item4 is TowerDefenseVase)
								{
									TowerDefenseLevelVaseConfig item = ((TowerDefenseVase)item4).Export();
									levelConfig.vaseManager.vaseList.Add(item);
									array.Add(item4);
								}
								else
								{
									TowerDefenseLevelPreSpawnConfig item2 = CreatePreSpawnConfig(item4, new Vector2I(i, j));
									levelConfig.preSpawnList.Add(item2);
									array.Add(item4);
								}
							}
						}
					}
				}
			}
			foreach (Variant item5 in TowerDefenseManager.Instance.GetCharacter())
			{
				TowerDefenseCharacter towerDefenseCharacter = (TowerDefenseCharacter)(GodotObject)item5;
				if (!array.Contains(towerDefenseCharacter))
				{
					TowerDefenseLevelPreSpawnConfig item3 = CreatePreSpawnConfig(towerDefenseCharacter, towerDefenseCharacter.gridPos);
					levelConfig.preSpawnList.Add(item3);
					array.Add(towerDefenseCharacter);
				}
			}
			if (levelConfig.finishMethod == TowerDefenseEnum.LEVEL_FINISH_METHOD.WAVE)
			{
				levelConfig.vaseManager = null;
			}
			levelConfig.MarkPreSpawnDataEditedFromEditor();
			if (!isSave && !IsVisibleInTree())
			{
				ClearCharacter();
			}
		}
		else
		{
			if (!IsVisibleInTree())
			{
				return;
			}
			if (GodotObject.IsInstanceValid(mapFeature))
			{
				for (int k = 1; k < mapFeature.config.gridNum.X + 1; k++)
				{
					for (int l = 1; l < mapFeature.config.gridNum.Y + 1; l++)
					{
						mapFeature.plantGrid[k][l].As<TowerDefenseCellInstance>().Clear();
					}
				}
			}
			foreach (TowerDefenseLevelPreSpawnConfig preSpawn in levelConfig.preSpawnList)
			{
				TowerDefenseCharacter character = preSpawn.SpawnCharacter(default(Vector2I), editorPreviewMode: true);
				if (GodotObject.IsInstanceValid(character) && GodotObject.IsInstanceValid(preSpawn.characterOverride))
				{
					preSpawn.characterOverride.ExecuteCharacter(character);
				}
			}
			if (!GodotObject.IsInstanceValid(levelConfig.vaseManager))
			{
				return;
			}
			foreach (TowerDefenseLevelVaseConfig vase in levelConfig.vaseManager.vaseList)
			{
				TowerDefensePacketConfig towerDefensePacketConfig = null;
				string type = vase.type;
				towerDefensePacketConfig = ((type == "Plant") ? TowerDefenseManager.GetPacketConfig("VasePlant") : ((!(type == "Zombie")) ? TowerDefenseManager.GetPacketConfig("VaseNormal") : TowerDefenseManager.GetPacketConfig("VaseZombie")));
				TowerDefenseCharacter towerDefenseCharacter2 = towerDefensePacketConfig.Plant(vase.gridPos, playAudio: true, noLimit: false, default(EconomyAccountId), skipPlacementCheck: false, editorPreviewMode: true);
				if (GodotObject.IsInstanceValid(towerDefenseCharacter2) && vase.packetName != "")
				{
					((TowerDefenseVase)towerDefenseCharacter2).packetConfig = vase.GetPacket();
				}
			}
		}
	}

	private static TowerDefenseLevelPreSpawnConfig CreatePreSpawnConfig(TowerDefenseCharacter character, Vector2I gridPos)
	{
		TowerDefenseLevelPreSpawnConfig towerDefenseLevelPreSpawnConfig = new TowerDefenseLevelPreSpawnConfig
		{
			gridPos = gridPos,
			packetName = character.packet.saveKey
		};
		if (character is TowerDefenseVase towerDefenseVase && GodotObject.IsInstanceValid(towerDefenseVase.packetConfig))
		{
			AddCharacterOverrideProperty(towerDefenseLevelPreSpawnConfig, "packetName", Variant.From(in towerDefenseVase.packetConfig.saveKey));
		}
		if (character is TowerDefenseItemSheild towerDefenseItemSheild)
		{
			AddCharacterOverrideProperty(towerDefenseLevelPreSpawnConfig, "shieldType", Variant.From<StringName>(towerDefenseItemSheild.shieldType));
			double from = towerDefenseItemSheild.shieldHitpoints;
			if (GodotObject.IsInstanceValid(towerDefenseItemSheild.instance))
			{
				from = towerDefenseItemSheild.instance.hitpoints;
			}
			AddCharacterOverrideProperty(towerDefenseLevelPreSpawnConfig, "shieldHitpoints", Variant.From(in from));
		}
		return towerDefenseLevelPreSpawnConfig;
	}

	private static void AddCharacterOverrideProperty(TowerDefenseLevelPreSpawnConfig preSpawnConfig, string propertyName, Variant value)
	{
		if (!GodotObject.IsInstanceValid(preSpawnConfig.characterOverride))
		{
			preSpawnConfig.characterOverride = new TowerDefenseCharacterOverride();
		}
		TowerDefenseCharacterOverride characterOverride = preSpawnConfig.characterOverride;
		if (characterOverride.propertyChange == null)
		{
			characterOverride.propertyChange = new Array<TowerDefenseCharacterPropertyChangeConfig>();
		}
		TowerDefenseCharacterPropertyChangeConfig item = new TowerDefenseCharacterPropertyChangeConfig
		{
			propertyName = propertyName,
			value = value
		};
		preSpawnConfig.characterOverride.propertyChange.Add(item);
	}

	public void Clear()
	{
		levelConfig = null;
		ClearCharacter();
	}

	public void ClearCharacter()
	{
		foreach (Variant item in TowerDefenseManager.Instance.GetCharacter())
		{
			TowerDefenseCharacter towerDefenseCharacter = (TowerDefenseCharacter)(GodotObject)item;
			if (GodotObject.IsInstanceValid(towerDefenseCharacter))
			{
				towerDefenseCharacter.skipDestroySet = true;
				towerDefenseCharacter.Destroy();
			}
		}
	}

	public void ShovelButtonPressed()
	{
		if (mapFeature == null)
		{
			return;
		}
		if (GodotObject.IsInstanceValid(mapFeature.packetPickControl))
		{
			mapFeature.packetPickControl.PacketPickRelease();
		}
		if (_shovelButton.ButtonPressed)
		{
			AudioManager.Instance.AudioPlay("Shovel");
			if (GodotObject.IsInstanceValid(mapFeature.shovelManager))
			{
				mapFeature.shovelManager.shovelPick = true;
				mapFeature.shovelManager.mapShovelSprite.Position = new Vector2(-100f, -100f);
				mapFeature.shovelManager.mapShovelSprite.Visible = true;
			}
		}
		else
		{
			AudioManager.Instance.AudioPlay("ShovelDeny");
			if (GodotObject.IsInstanceValid(mapFeature.shovelManager))
			{
				mapFeature.shovelManager.shovelPick = false;
				mapFeature.shovelManager.mapShovelSprite.Visible = false;
			}
		}
	}

	public void Release()
	{
		if (mapFeature != null)
		{
			if (GodotObject.IsInstanceValid(mapFeature.packetPickControl))
			{
				mapFeature.packetPickControl.Release();
			}
			_shovelButton.ButtonPressed = false;
		}
	}
}
