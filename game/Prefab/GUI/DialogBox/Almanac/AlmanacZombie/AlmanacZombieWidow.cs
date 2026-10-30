using Godot;
using Godot.Collections;
using PVZHE.ModEditor.ModSystem;

[GlobalClass]
public partial class AlmanacZombieWidow : Control
{
	public delegate void PressedEventHandler(TowerDefensePacketConfig config);

	public Control spriteNode;

	public Control previewClip;

	public TowerDefensePacketConfig config;

	public AdobeAnimateSprite sprite;

	private bool _previewHovered;

	private bool _visibilityRefreshQueued;

	public event PressedEventHandler OnPressed;

	public override void _Ready()
	{
		spriteNode = GetNode<Control>("%SpriteNode");
		previewClip = GetNode<Control>("%Mask");
		base.VisibilityChanged += _VisibilityChanged;
		GetNode<BaseButton>("%Button").MouseEntered += _MouseEntered;
		GetNode<BaseButton>("%Button").MouseExited += _MouseExited;
		GetNode<BaseButton>("%Button").Pressed += _ButtonPressed;
	}

	public override void _ExitTree()
	{
		_visibilityRefreshQueued = false;
		TryUnsubscribeCharacterSkinSwitch();
		ReleasePreviewTree(sprite);
		base._ExitTree();
	}

	public void Init(TowerDefensePacketConfig packetConfig)
	{
		TryUnsubscribeCharacterSkinSwitch();
		config = packetConfig;
		_previewHovered = false;
		CreateSprite();
		if (GodotObject.IsInstanceValid(sprite))
		{
			FreezePreviewTree(sprite);
			previewClip.Visible = true;
		}
		else
		{
			previewClip.Visible = false;
		}
		TrySubscribeCharacterSkinSwitch();
	}

	public void ClearEventHandlers()
	{
		this.OnPressed = null;
	}

	public void CreateSprite()
	{
		if (config == null || !GodotObject.IsInstanceValid(config.characterConfig))
		{
			return;
		}
		ReleaseSprite();
		sprite = TowerDefenseManager.GetPacketSprite(config);
		if (GodotObject.IsInstanceValid(sprite))
		{
			sprite.LightMask = 0;
			sprite.forceLocalRender = true;
			sprite.SetRenderClipControl(previewClip);
			spriteNode.AddChild(sprite, forceReadableName: false, InternalMode.Disabled);
			sprite.Position = config.packetAnimeOffset + new Vector2(-2f, 0f);
			sprite.Scale = config.packetAnimeScale;
			if (config.packetFlip)
			{
				sprite.Scale = new Vector2(0f - sprite.Scale.X, sprite.Scale.Y);
			}
			if (config.overrideHypnoses)
			{
				sprite.Modulate = new Color(0.72f, 0.62f, 1f, sprite.Modulate.A);
			}
			TowerDefenseCharacterConfig characterConfig = config.characterConfig;
			ApplyInitialArmor(characterConfig);
			ApplyCurrentCustomFilter(characterConfig);
			sprite.SetAnimation(config.packetAnimeClip);
			sprite.UpdateMediaReplaceData();
			sprite.UpdateChild();
			FreezePreviewTree(sprite);
			AdobeAnimateRenderManager.WarmupRenderMount(sprite);
		}
	}

	private void ApplyInitialArmor(TowerDefenseCharacterConfig characterConfig)
	{
		if (characterConfig.armorData == null || config.initArmor == null)
		{
			return;
		}
		foreach (string item in config.initArmor)
		{
			ArmorSlotConfig slotConfig = characterConfig.armorData.GetSlotConfig(item);
			TowerDefenseArmorTypeData typeData = characterConfig.armorData.GetTypeData(item);
			if (typeData == null)
			{
				continue;
			}
			string replaceMethod = slotConfig.replaceMethod;
			if (!(replaceMethod == "Media"))
			{
				if (replaceMethod == "Sprite")
				{
					CharacterArmorData.CreateArmorPartNode(sprite, slotConfig, typeData);
				}
			}
			else
			{
				characterConfig.armorData.OpenArmorFliters(sprite, item);
				characterConfig.armorData.SetArmorReplace(sprite, item, 0);
			}
		}
	}

	private void ApplyCurrentCustomFilter(TowerDefenseCharacterConfig characterConfig)
	{
		if (characterConfig.customData != null && TryGetPacketSaveValue(config.saveKey, out var packetData))
		{
			string text = packetData.GetValueOrDefault("Key", new Dictionary()).AsGodotDictionary().GetValueOrDefault("Custom", "")
				.AsString();
			if (text != "" && characterConfig.customData.customDictionary.ContainsKey(text))
			{
				characterConfig.customData.SetCustomFliters(sprite, text);
			}
		}
	}

	public void _VisibilityChanged()
	{
		if (IsInsideTree() && !_visibilityRefreshQueued)
		{
			_visibilityRefreshQueued = true;
			CallDeferred("ApplyDeferredVisibilityState");
		}
	}

	public void ApplyDeferredVisibilityState()
	{
		_visibilityRefreshQueued = false;
		if (!IsInsideTree())
		{
			return;
		}
		if (!IsVisibleInTree())
		{
			if (GodotObject.IsInstanceValid(sprite))
			{
				FreezePreviewTree(sprite);
			}
			return;
		}
		if (!GodotObject.IsInstanceValid(sprite))
		{
			CreateSprite();
		}
		if (GodotObject.IsInstanceValid(sprite) && !_previewHovered)
		{
			FreezePreviewTree(sprite);
		}
		previewClip.Visible = GodotObject.IsInstanceValid(sprite);
	}

	private void ReleaseSprite()
	{
		if (GodotObject.IsInstanceValid(sprite))
		{
			ReleasePreviewTree(sprite);
			sprite.QueueFree();
		}
		sprite = null;
	}

	public void ReleaseForPoolDisposal()
	{
		_visibilityRefreshQueued = false;
		_previewHovered = false;
		ClearEventHandlers();
		TryUnsubscribeCharacterSkinSwitch();
		config = null;
		ReleaseSprite();
		if (GodotObject.IsInstanceValid(previewClip))
		{
			previewClip.Visible = false;
		}
	}

	private static void ReleasePreviewTree(Node node)
	{
		if (!GodotObject.IsInstanceValid(node))
		{
			return;
		}
		if (node is AdobeAnimateSprite adobeAnimateSprite)
		{
			adobeAnimateSprite.ReleaseForcedCpuPoseData();
			adobeAnimateSprite.ClearRenderClipControl();
		}
		foreach (Node child in node.GetChildren(includeInternal: true))
		{
			ReleasePreviewTree(child);
		}
	}

	public void _ButtonPressed()
	{
		if (config != null)
		{
			AudioManager.Instance.AudioPlay("PacketPick");
			this.OnPressed?.Invoke(config);
		}
	}

	public void _MouseEntered()
	{
		_previewHovered = true;
		previewClip.Visible = true;
		if (!GodotObject.IsInstanceValid(sprite))
		{
			CreateSprite();
		}
		if (!GodotObject.IsInstanceValid(sprite))
		{
			previewClip.Visible = false;
			return;
		}
		sprite.SetAnimation(config.packetAnimeClip);
		sprite.UpdateMediaReplaceData();
		sprite.Visible = true;
		PlayPreviewTreeFromStart(sprite);
		AdobeAnimateRenderManager.WarmupRenderMount(sprite);
	}

	public void _MouseExited()
	{
		_previewHovered = false;
		if (GodotObject.IsInstanceValid(sprite))
		{
			FreezePreviewTree(sprite);
		}
	}

	public void Reset()
	{
		_previewHovered = false;
		if (GodotObject.IsInstanceValid(sprite))
		{
			FreezePreviewTree(sprite);
		}
	}

	private void ConfigurePreviewSprite(AdobeAnimateSprite animateSprite)
	{
		animateSprite.LightMask = 0;
		animateSprite.keepRenderSubmittedWhenPaused = true;
		animateSprite.forceLocalRender = true;
		animateSprite.SetRenderClipControl(previewClip);
		animateSprite.ProcessMode = ProcessModeEnum.Always;
	}

	private void FreezePreviewTree(Node node, bool forcePoseRefresh = false)
	{
		if (!GodotObject.IsInstanceValid(node))
		{
			return;
		}
		if (node is AdobeAnimateSprite adobeAnimateSprite)
		{
			ConfigurePreviewSprite(adobeAnimateSprite);
			if (forcePoseRefresh || !adobeAnimateSprite.IsFrozenPreview)
			{
				adobeAnimateSprite.ResetAnimation();
				adobeAnimateSprite.UpdateMediaReplaceData();
				adobeAnimateSprite.UpdateChild();
				adobeAnimateSprite.RefreshManagedSlotSpriteCacheForRender();
				adobeAnimateSprite.QueueRedraw();
			}
			adobeAnimateSprite.SetFrozenPreview(frozen: true);
		}
		foreach (Node child in node.GetChildren(includeInternal: true))
		{
			FreezePreviewTree(child, forcePoseRefresh);
		}
	}

	private void PlayPreviewTreeFromStart(Node node)
	{
		if (!GodotObject.IsInstanceValid(node))
		{
			return;
		}
		if (node is AdobeAnimateSprite adobeAnimateSprite)
		{
			ConfigurePreviewSprite(adobeAnimateSprite);
			adobeAnimateSprite.ResetAnimation();
			adobeAnimateSprite.UpdateMediaReplaceData();
			adobeAnimateSprite.UpdateChild();
			adobeAnimateSprite.RefreshManagedSlotSpriteCacheForRender();
			adobeAnimateSprite.SetFrozenPreview(frozen: false);
			adobeAnimateSprite.RefreshProcessScheduling();
			adobeAnimateSprite.QueueRedraw();
		}
		foreach (Node child in node.GetChildren(includeInternal: true))
		{
			PlayPreviewTreeFromStart(child);
		}
	}

	private void OnCharacterSkinSwitched(string packetSaveKey, string customKey)
	{
		if (!GodotObject.IsInstanceValid(config) || config.saveKey != packetSaveKey || !GodotObject.IsInstanceValid(sprite))
		{
			return;
		}
		TowerDefenseCharacterConfig characterConfig = config.characterConfig;
		if (GodotObject.IsInstanceValid(characterConfig) && characterConfig.customData != null && (!(customKey != "") || characterConfig.customData.customDictionary.ContainsKey(customKey)))
		{
			characterConfig.customData.ClearCustomFliters(sprite);
			if (customKey != "")
			{
				characterConfig.customData.SetCustomFliters(sprite, customKey);
			}
			sprite.UpdateMediaReplaceData();
			sprite.UpdateChild();
			sprite.RefreshManagedSlotSpriteCacheForRender();
			if (_previewHovered)
			{
				PlayPreviewTreeFromStart(sprite);
			}
			else
			{
				FreezePreviewTree(sprite, forcePoseRefresh: true);
			}
		}
	}

	private void TrySubscribeCharacterSkinSwitch()
	{
		if (BattleEventBus.Instance != null)
		{
			BattleEventBus.Instance.OnCharacterSkinSwitched -= OnCharacterSkinSwitched;
			BattleEventBus.Instance.OnCharacterSkinSwitched += OnCharacterSkinSwitched;
		}
	}

	private void TryUnsubscribeCharacterSkinSwitch()
	{
		if (BattleEventBus.Instance != null)
		{
			BattleEventBus.Instance.OnCharacterSkinSwitched -= OnCharacterSkinSwitched;
		}
	}

	private static bool TryGetPacketSaveValue(string saveKey, out Dictionary packetData)
	{
		packetData = new Dictionary();
		if (string.IsNullOrWhiteSpace(saveKey) || GameSaveManager.Instance == null)
		{
			return false;
		}
		packetData = XWModPlayerProgressService.GetPacketState(saveKey);
		return packetData != null;
	}
}
