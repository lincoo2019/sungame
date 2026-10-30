using Godot;

[Tool]
[GlobalClass]
public partial class TowerDefenseVase : TowerDefenseItem
{
	public WaterEnvironmentComponent waterEnvironmentComponent;

	public VaseContentComponent vaseContentComponent;

	public EntryAnimationComponent entryAnimationComponent;

	public LightDetectionComponent lightDetectionComponent;

	private MousePressComponent _mousePressComponent;

	private AdobeAnimateSpriteBase _hammer;

	private TowerDefenseInGamePacketShow _packetShow;

	private string _packetName = "";

	private TowerDefensePacketConfig _packetConfig;

	public bool pressed;

	public bool over;

	private bool _showPacket;

	public override bool PreserveDeathTransformation => true;

	[Export(PropertyHint.None, "")]
	public AdobeAnimateSprite backSprite { get; set; }

	[Export(PropertyHint.None, "")]
	public bool packetCanShow { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public AdobeAnimateSpriteBase waterLineSprite { get; set; }

	[Export(PropertyHint.None, "")]
	public PackedScene chunkParticles { get; set; }

	[Export(PropertyHint.None, "")]
	public string packetBank { get; set; } = "Total";

	[Export(PropertyHint.None, "")]
	public string packetName
	{
		get
		{
			return _packetName;
		}
		set
		{
			string text = value ?? "";
			if (!(_packetName == text))
			{
				_packetName = text;
				packetConfig = ((_packetName == "") ? null : TowerDefenseManager.GetPacketConfig(_packetName));
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public TowerDefensePacketConfig packetConfig
	{
		get
		{
			return _packetConfig;
		}
		set
		{
			_packetConfig = value;
			VaseContentComponent obj = vaseContentComponent;
			if (obj != null && !obj.IsReleased)
			{
				vaseContentComponent.NotifyContentConfigAssigned(value);
			}
			RefreshPacketShowFromConfig();
		}
	}

	[Export(PropertyHint.None, "")]
	public bool useEnterAnime { get; set; } = true;

	public bool showPacket
	{
		get
		{
			return _showPacket;
		}
		set
		{
			_showPacket = value;
			if (GodotObject.IsInstanceValid(_packetShow))
			{
				if (!_showPacket)
				{
					_packetShow.Visible = false;
				}
				else
				{
					_packetShow.Visible = true;
				}
			}
		}
	}

	public void SetContentConfig(TowerDefensePacketConfig contentConfig)
	{
		_packetName = (GodotObject.IsInstanceValid(contentConfig) ? contentConfig.saveKey : "");
		packetConfig = contentConfig;
	}

	public bool ShouldAlwaysShowPacket()
	{
		if (Global.IsEditor)
		{
			return SceneManager.CurrentScene == "LevelEditorStage";
		}
		return false;
	}

	private void SetFrontShellRevealed(bool revealed)
	{
		if (GodotObject.IsInstanceValid(base.sprite))
		{
			base.sprite.Visible = true;
			float num = (revealed ? 0f : 1f);
			if (!Mathf.IsEqualApprox(base.sprite.SelfModulate.A, num))
			{
				Color selfModulate = base.sprite.SelfModulate;
				base.sprite.SetRenderSelfModulate(new Color(selfModulate.R, selfModulate.G, selfModulate.B, num));
			}
		}
	}

	private void RefreshPacketShowFromConfig()
	{
		if (!IsNodeReady() || !GodotObject.IsInstanceValid(_packetShow))
		{
			return;
		}
		if (GodotObject.IsInstanceValid(_packetConfig))
		{
			_packetShow.Init(_packetConfig);
			_packetShow.button.Visible = false;
			_packetShow.showCost = false;
			if (ShouldAlwaysShowPacket())
			{
				showPacket = true;
				SetFrontShellRevealed(revealed: true);
				_packetShow.Modulate = new Color(_packetShow.Modulate.R, _packetShow.Modulate.G, _packetShow.Modulate.B);
			}
		}
		else
		{
			_packetShow.Clear();
			_packetShow.button.Visible = false;
			_packetShow.showCost = false;
			if (ShouldAlwaysShowPacket())
			{
				showPacket = false;
				SetFrontShellRevealed(revealed: false);
				_packetShow.Modulate = new Color(_packetShow.Modulate.R, _packetShow.Modulate.G, _packetShow.Modulate.B, 0f);
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (editorPreviewMode)
		{
			_packetShow = GetNode<TowerDefenseInGamePacketShow>("%PacketShow");
			_packetShow.button.Visible = false;
			_packetShow.showCost = false;
			Callable.From(RefreshPacketShowFromConfig).CallDeferred();
		}
		else
		{
			if (Engine.IsEditorHint())
			{
				return;
			}
			if (GodotObject.IsInstanceValid(base.sprite))
			{
				base.sprite.runtimeViewportCullingEnabled = false;
			}
			if (GodotObject.IsInstanceValid(backSprite))
			{
				backSprite.runtimeViewportCullingEnabled = false;
			}
			_hammer = GetNode<AdobeAnimateSpriteBase>("%Hammer");
			_hammer.OnAnimeCompleted += HammerAnimeCompleted;
			_packetShow = GetNode<TowerDefenseInGamePacketShow>("%PacketShow");
			if (GodotObject.IsInstanceValid(componentManager))
			{
				waterEnvironmentComponent = componentManager.GetRuntime<WaterEnvironmentComponent>();
				lightDetectionComponent = componentManager.GetRuntime<LightDetectionComponent>();
				entryAnimationComponent = componentManager.GetRuntime<EntryAnimationComponent>();
				vaseContentComponent = componentManager.GetRuntime<VaseContentComponent>();
				_mousePressComponent = componentManager.GetRuntime<MousePressComponent>();
				MousePressComponent mousePressComponent = _mousePressComponent;
				if (mousePressComponent != null && !mousePressComponent.IsReleased)
				{
					_mousePressComponent.OnPressed += HandleVasePressed;
				}
			}
			targetRegistrationComponent.canProjectileCheck = false;
			showPacket = true;
			showPacket = false;
			_packetShow.button.Visible = false;
			_packetShow.showCost = false;
			RefreshPacketShowFromConfig();
			AddToGroup("Vase", persistent: true);
			if (GodotObject.IsInstanceValid(cell) && cell.isWater)
			{
				base.groundHeight = 0f - waterEnvironmentComponent.waterHeight;
				inGame = false;
				waterEnvironmentComponent.inWater = true;
				inGame = true;
				shadowSprite.Visible = false;
				ShadowComponent obj = shadowComponent;
				if (obj != null && !obj.IsReleased)
				{
					shadowComponent.SetShadowVisible(visible: false);
				}
				Transform2D screenTransform = GetViewport().GetScreenTransform();
				screenTransform = new Transform2D(screenTransform.X, screenTransform.Y, Vector2.Zero);
				Vector2 logicalGlobalPosition = GetLogicalGlobalPosition(spriteGroup);
				float y = (screenTransform * (logicalGlobalPosition + new Vector2(0f, 100f))).Y;
				backSprite.SetDiscardDownPos(y);
				SetSpriteGroupShaderParameter("discardDownPos", y);
				float y2 = (screenTransform * (logicalGlobalPosition + new Vector2(0f, 48f))).Y;
				Tween tween = CreateTween();
				tween.SetParallel();
				tween.SetEase(Tween.EaseType.Out);
				tween.SetTrans(Tween.TransitionType.Cubic);
				tween.TweenMethod(Callable.From(delegate(double v)
				{
					SetSpriteDiscardDownPos(v);
				}), y, y2, 1.0);
				tween.TweenMethod(Callable.From(delegate(double v)
				{
					SetBackSpriteDiscardDownPos(v);
				}), y, y2, 1.0);
				CreateSplash();
				if (GodotObject.IsInstanceValid(waterLineSprite))
				{
					waterLineSprite.Visible = true;
				}
			}
			if (Global.IsEditor && SceneManager.CurrentScene == "LevelEditorStage")
			{
				return;
			}
			bool flag = GodotObject.IsInstanceValid(TowerDefenseManager.CurrentControl) && TowerDefenseManager.CurrentControl.hasProgress;
			if (useEnterAnime && !flag)
			{
				EntryAnimationComponent obj2 = entryAnimationComponent;
				if (obj2 != null && !obj2.IsReleased)
				{
					entryAnimationComponent.PlayConfiguredFallBounce((float)GD.RandRange(0.25, 1.0));
				}
			}
		}
	}

	public override void _ExitTree()
	{
		MousePressComponent mousePressComponent = _mousePressComponent;
		if (mousePressComponent != null && !mousePressComponent.IsReleased)
		{
			_mousePressComponent.OnPressed -= HandleVasePressed;
		}
		_mousePressComponent = null;
		base._ExitTree();
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (TowerDefenseCharacter.CachedEditorHint || !GodotObject.IsInstanceValid(_packetShow) || !GodotObject.IsInstanceValid(spriteGroup) || !GodotObject.IsInstanceValid(base.sprite))
		{
			return;
		}
		if ((ulong)((long)TowerDefenseProcessModeDispatch.CurrentPhysicsFrame + (long)randFreshIndex) % 5uL == 0L)
		{
			ulong currentPhysicsFrame = TowerDefenseProcessModeDispatch.CurrentPhysicsFrame;
			Vector2 globalPositionForPhysicsFrame = GetGlobalPositionForPhysicsFrame(currentPhysicsFrame);
			if ((double)globalPositionForPhysicsFrame.X > groundRight - 30.0)
			{
				globalPositionForPhysicsFrame.X = (float)(groundRight - 30.0);
				SetGlobalPositionForPhysicsFrame(globalPositionForPhysicsFrame, currentPhysicsFrame);
			}
			_packetShow.Position = new Vector2(_packetShow.Position.X, spriteGroup.Position.Y);
		}
		int num;
		if (GodotObject.IsInstanceValid(packetConfig))
		{
			LightDetectionComponent obj = lightDetectionComponent;
			num = (((obj != null && obj.CheckShow()) || ShouldAlwaysShowPacket()) ? 1 : 0);
		}
		else
		{
			num = 0;
		}
		showPacket = (byte)num != 0;
		if (showPacket)
		{
			_packetShow.Modulate = new Color(_packetShow.Modulate.R, _packetShow.Modulate.G, _packetShow.Modulate.B, (float)Mathf.Lerp(_packetShow.Modulate.A, 1.0, delta * 2.0));
			SetFrontShellRevealed(revealed: true);
		}
		else
		{
			_packetShow.Modulate = new Color(_packetShow.Modulate.R, _packetShow.Modulate.G, _packetShow.Modulate.B, (float)Mathf.Lerp(_packetShow.Modulate.A, 0.0, delta * 5.0));
			SetFrontShellRevealed(revealed: false);
		}
	}

	public override void FinalizeProgressRestore()
	{
		base.FinalizeProgressRestore();
		if (useEnterAnime)
		{
			isGround = true;
			base.z = base.groundHeight;
			ySpeed = 0.0;
			if (GodotObject.IsInstanceValid(transformPoint))
			{
				Marker2D marker2D = transformPoint;
				EntryAnimationComponent obj = entryAnimationComponent;
				marker2D.Scale = ((obj != null && !obj.IsReleased) ? entryAnimationComponent.settleScale : Vector2.One);
			}
		}
	}

	public override void SetZ()
	{
		base.SetZ();
		if (GodotObject.IsInstanceValid(backSprite))
		{
			backSprite.NotifyAncestorTransformChangedForRender();
		}
	}

	private void HandleVasePressed(Vector2 pointerPosition)
	{
		if (pressed || !GodotObject.IsInstanceValid(_hammer))
		{
			return;
		}
		TowerDefenseManager towerDefenseManager = TowerDefenseManager.Instance;
		if (GodotObject.IsInstanceValid(towerDefenseManager) && !(towerDefenseManager.GetMapGridPosFromMouse(pointerPosition) != base.gridPos))
		{
			PacketPickControl packetPickControl = towerDefenseManager.GetPacketPickControl();
			if (!GodotObject.IsInstanceValid(packetPickControl) || !GodotObject.IsInstanceValid(packetPickControl.packetPick))
			{
				_hammer.Visible = true;
				AudioManager.Instance.AudioPlay("Swing");
				_hammer.SetAnimation("OpenPot", loop: false);
				pressed = true;
			}
		}
	}

	public override void DestroySet()
	{
		VaseContentComponent obj = vaseContentComponent;
		if (obj != null && !obj.IsReleased)
		{
			vaseContentComponent.DestroySet();
		}
	}

	public override void SmashDestroy()
	{
		if (!isDestroy)
		{
			isDestroy = true;
			DestroySet();
			TowerDefenseManager.CurrentControl?.CleanupCharacterCell(this);
			TowerDefenseManager.Instance.CharacterUnregister(this);
			RemoveFromGroup("Character");
			QueueFree();
		}
	}

	public void MultiplayerBreak()
	{
		if (!over)
		{
			over = true;
			isDestroy = true;
			TowerDefenseManager.CurrentControl?.CleanupCharacterCell(this);
			if (GodotObject.IsInstanceValid(TowerDefenseInGameLevelControl.instance))
			{
				TowerDefenseInGameLevelControl.instance.hasSpawn = true;
			}
			VaseContentComponent obj = vaseContentComponent;
			if (obj != null && !obj.IsReleased)
			{
				vaseContentComponent.PlayBreakVisuals();
			}
			TowerDefenseManager.Instance.CharacterUnregister(this);
			RemoveFromGroup("Character");
			QueueFree();
		}
	}

	public void HammerAnimeCompleted(string clip)
	{
		if (clip == "OpenPot")
		{
			_hammer.Visible = false;
			if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
			{
				MultiPlayerManager.Instance.SendVaseBreakRequest(base.gridPos.X, base.gridPos.Y);
			}
			else
			{
				Destroy();
			}
		}
	}

	public TowerDefenseLevelVaseConfig Export()
	{
		TowerDefenseLevelVaseConfig towerDefenseLevelVaseConfig = new TowerDefenseLevelVaseConfig();
		towerDefenseLevelVaseConfig.gridPos = base.gridPos;
		if (GodotObject.IsInstanceValid(packetConfig))
		{
			towerDefenseLevelVaseConfig.packetName = packetConfig.saveKey;
		}
		switch (base.config.name)
		{
		case "VaseNormal":
			towerDefenseLevelVaseConfig.type = "Normal";
			break;
		case "VasePlant":
			towerDefenseLevelVaseConfig.type = "Plant";
			break;
		case "VaseZombie":
			towerDefenseLevelVaseConfig.type = "Zombie";
			break;
		}
		return towerDefenseLevelVaseConfig;
	}

	public override void BlowBack(double num, double time = 1.0)
	{
	}

	private void SetSpriteDiscardDownPos(double value)
	{
		if (GodotObject.IsInstanceValid(base.sprite))
		{
			base.sprite.SetDiscardDownPos((float)value);
		}
	}

	private void SetBackSpriteDiscardDownPos(double value)
	{
		if (GodotObject.IsInstanceValid(backSprite))
		{
			backSprite.SetDiscardDownPos((float)value);
		}
	}
}
