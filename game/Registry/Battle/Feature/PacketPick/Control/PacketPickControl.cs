using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class PacketPickControl : Node2D
{
	private sealed class PendingPlantRequest
	{
		public TowerDefenseInGamePacketShow Packet;

		public int PacketSyncId;

		public int ExpectedCount;

		public int RejectedCount;

		public ulong DeadlineMsec;

		public SunSpendReceipt SpendReceipt;

		public bool WasAlive;

		public bool WasColdDownOpen;

		public double ColdDown;

		public double ColdDownTimer;
	}

	private const int PlacementPreviewRowTopLayer = 14;

	internal const int FollowPreviewTopZIndex = 4096;

	public TowerDefenseMapControl mapControl;

	public TowerDefenseBattleFeatureMap mapFeature;

	private TowerDefenseBattleFeaturePacketPickConfig _config;

	public TowerDefenseInGamePacketShow packetPick;

	public AdobeAnimateSprite followSprite;

	public List<AdobeAnimateSprite> plantSpriteList = new List<AdobeAnimateSprite>();

	public int selectPacketTimer;

	public List<PacketPickTool> tools = new List<PacketPickTool>();

	private bool _wasPicking;

	private int _toolActivateGrace;

	private bool _suppressSelectionGestureConfirm;

	private readonly System.Collections.Generic.Dictionary<string, PendingPlantRequest> _pendingPlantRequests = new System.Collections.Generic.Dictionary<string, PendingPlantRequest>();

	private readonly List<string> _expiredPlantRequestIds = new List<string>();

	private int _plantRequestSerial;

	private bool _multiplayerEventsSubscribed;

	private double _pendingRequestSweepTimer;

	private Control _placementPreviewRenderMount;

	private Control _followPreviewRenderMount;

	internal List<TowerDefenseInGamePacketShow.ColumnPlacement> CollectColumnPlacements(int column, int? limitPlantGridNum = null)
	{
		List<TowerDefenseInGamePacketShow.ColumnPlacement> list = new List<TowerDefenseInGamePacketShow.ColumnPlacement>();
		if (column < 1 || column > mapFeature.config.gridNum.X)
		{
			return list;
		}
		TowerDefensePacketConfig config = packetPick.config;
		ColumnPlantGridBudget columnPlantGridBudget = new ColumnPlantGridBudget(config, limitPlantGridNum);
		bool flag = config.characterConfig.plantGridType.Contains(TowerDefenseEnum.PLANTGRIDTYPE.PLANT);
		bool flag2 = false;
		foreach (TowerDefenseEnum.PLANTGRIDTYPE item in config.characterConfig.plantGridType)
		{
			flag2 = flag2 || item != TowerDefenseEnum.PLANTGRIDTYPE.PLANT;
		}
		bool hypnoses = config.GetHypnoses();
		TowerDefenseEnum.CHARACTER_CAMP targetCamp = ((!hypnoses) ? TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE : TowerDefenseEnum.CHARACTER_CAMP.PLANT);
		Godot.Collections.Array array = (config.canPlaceOnZombie ? TowerDefenseManager.Instance.GetZombie() : null);
		HashSet<TowerDefenseCharacter> hashSet = new HashSet<TowerDefenseCharacter>();
		for (int i = 1; i <= mapFeature.config.gridNum.Y; i++)
		{
			Vector2I vector2I = new Vector2I(column, i);
			TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(vector2I);
			if (!GodotObject.IsInstanceValid(mapCell) || GodotObject.IsInstanceValid(mapCell.FindPlantingBlocker(config)))
			{
				continue;
			}
			bool flag3 = CanPlaceColumnTargetInCell(mapCell);
			bool flag4 = ShouldPreferCraterRepair(mapCell);
			if (flag3 && flag4 && columnPlantGridBudget.CanPlace(vector2I))
			{
				list.Add(new TowerDefenseInGamePacketShow.ColumnPlacement(vector2I, null, "grid"));
				columnPlantGridBudget.Reserve(vector2I);
				continue;
			}
			if (array != null && !flag4)
			{
				TowerDefenseCharacter towerDefenseCharacter = FindColumnZombie(array, vector2I, targetCamp);
				if (GodotObject.IsInstanceValid(towerDefenseCharacter) && hashSet.Add(towerDefenseCharacter))
				{
					list.Add(new TowerDefenseInGamePacketShow.ColumnPlacement(vector2I, towerDefenseCharacter, "zombie"));
					continue;
				}
			}
			if (!flag3)
			{
				continue;
			}
			TowerDefensePlantJalaVase towerDefensePlantJalaVase = FindColumnJalaVase(mapCell, config);
			if (GodotObject.IsInstanceValid(towerDefensePlantJalaVase))
			{
				if (hashSet.Add(towerDefensePlantJalaVase))
				{
					list.Add(new TowerDefenseInGamePacketShow.ColumnPlacement(vector2I, towerDefensePlantJalaVase, "jala_vase"));
				}
			}
			else if (flag)
			{
				if (!flag2 || mapCell.CanPacketPlant(config))
				{
					TowerDefenseCharacter towerDefenseCharacter2 = FindPlantInCell(mapCell, hypnoses);
					if (GodotObject.IsInstanceValid(towerDefenseCharacter2) && hashSet.Add(towerDefenseCharacter2))
					{
						list.Add(new TowerDefenseInGamePacketShow.ColumnPlacement(vector2I, towerDefenseCharacter2, "plant"));
					}
				}
			}
			else if (mapCell.CanPacketPlant(config) && columnPlantGridBudget.CanPlace(vector2I))
			{
				list.Add(new TowerDefenseInGamePacketShow.ColumnPlacement(vector2I, null, "grid"));
				columnPlantGridBudget.Reserve(vector2I);
			}
		}
		return list;
	}

	private static TowerDefensePlantJalaVase FindColumnJalaVase(TowerDefenseCellInstance cell, TowerDefensePacketConfig config)
	{
		foreach (TowerDefenseCharacter character in cell.characterList)
		{
			if (character is TowerDefensePlantJalaVase towerDefensePlantJalaVase && GodotObject.IsInstanceValid(towerDefensePlantJalaVase) && towerDefensePlantJalaVase.CanAddJala(config))
			{
				return towerDefensePlantJalaVase;
			}
		}
		return null;
	}

	private bool CanPlaceColumnTargetInCell(TowerDefenseCellInstance cell)
	{
		TowerDefensePacketConfig config = packetPick.config;
		if (TowerDefenseManager.Instance.IsIZMMode() && config.izmPlantAllCell)
		{
			return true;
		}
		if (mapFeature.stripeRow == -1)
		{
			return true;
		}
		if ((config.characterConfig is TowerDefensePlantConfig || config.izmPlantLeft) && cell.gridPos.X > mapFeature.stripeRow)
		{
			return false;
		}
		if (config.characterConfig is TowerDefenseZombieConfig && !config.izmPlantLeft)
		{
			return cell.gridPos.X > mapFeature.stripeRow;
		}
		return true;
	}

	private static TowerDefenseCharacter FindColumnZombie(Godot.Collections.Array zombies, Vector2I gridPosition, TowerDefenseEnum.CHARACTER_CAMP targetCamp)
	{
		TowerDefenseCharacter result = null;
		double num = 1.0 / 0.0;
		Vector2 mapCellPlantPos = TowerDefenseManager.GetMapCellPlantPos(gridPosition);
		foreach (Variant zombie in zombies)
		{
			TowerDefenseCharacter towerDefenseCharacter = (TowerDefenseCharacter)(GodotObject)zombie;
			if (!GodotObject.IsInstanceValid(towerDefenseCharacter) || towerDefenseCharacter.isDestroy || towerDefenseCharacter.die || towerDefenseCharacter.nearDie || !GodotObject.IsInstanceValid(towerDefenseCharacter.instance) || towerDefenseCharacter.instance.invincible || !towerDefenseCharacter.instance.canBeCollection || towerDefenseCharacter.camp != targetCamp || towerDefenseCharacter.gridPos.Y != gridPosition.Y)
			{
				continue;
			}
			Vector2 logicalGlobalPosition = towerDefenseCharacter.GetLogicalGlobalPosition();
			if (TowerDefenseManager.Instance.GetMapGridPos(logicalGlobalPosition).X == gridPosition.X)
			{
				double num2 = logicalGlobalPosition.DistanceSquaredTo(mapCellPlantPos);
				if (num2 < num)
				{
					num = num2;
					result = towerDefenseCharacter;
				}
			}
		}
		return result;
	}

	private bool ProcessColumnTargetPlacement(Vector2I gridPosition, bool suppressPlacementConfirm, int limitPlantGridNum)
	{
		if (!TowerDefenseManager.Instance.CheckMapGridPosIn(gridPosition))
		{
			return false;
		}
		List<TowerDefenseInGamePacketShow.ColumnPlacement> list = CollectColumnPlacements(gridPosition.X, limitPlantGridNum);
		foreach (TowerDefenseInGamePacketShow.ColumnPlacement item in list)
		{
			int num = item.GridPos.Y - 1;
			if (num < plantSpriteList.Count)
			{
				Vector2 mapWorldPosition = (GodotObject.IsInstanceValid(item.Target) ? item.Target.GetLogicalGlobalPosition() : (TowerDefenseManager.GetMapCellPlantPos(item.GridPos) - new Vector2(0f, (float)mapFeature.GetGroundHeight(TowerDefenseManager.GetMapCell(item.GridPos)))));
				plantSpriteList[num].Visible = true;
				PositionPlacementPreview(plantSpriteList[num], mapWorldPosition, item.GridPos.Y);
			}
		}
		if (suppressPlacementConfirm || !mapControl.IsConfirmInput())
		{
			return false;
		}
		if (list.Count == 0 || !ProcessColumnPlacements(list))
		{
			TowerDefenseManager.GetMapCell(gridPosition)?.NotifyBlockedPlanting(packetPick.config);
			return false;
		}
		if (!Global.IsEditor || SceneManager.CurrentScene != "LevelEditorStage")
		{
			Release();
		}
		else
		{
			LevelEditorMapEditor.instance.levelConfig.canExport = false;
		}
		return true;
	}

	internal bool ProcessColumnPlacements(IReadOnlyList<TowerDefenseInGamePacketShow.ColumnPlacement> placements)
	{
		TowerDefenseInGamePacketShow packet = packetPick;
		if (!Global.IsMultiplayerMode)
		{
			return packet.PlantColumnTargets(placements).Count > 0;
		}
		if (MultiPlayerManager.Instance == null)
		{
			return false;
		}
		List<TowerDefenseInGamePacketShow.ColumnPlacement> list = new List<TowerDefenseInGamePacketShow.ColumnPlacement>();
		foreach (TowerDefenseInGamePacketShow.ColumnPlacement placement in placements)
		{
			TowerDefenseCharacter target = placement.Target;
			if (placement.Kind == "grid" || (GodotObject.IsInstanceValid(target) && target.syncId >= 0))
			{
				list.Add(placement);
			}
		}
		if (list.Count == 0)
		{
			return false;
		}
		string overrideData = (GodotObject.IsInstanceValid(packet.config._override) ? Json.Stringify(packet.config._override.Export()) : "");
		bool hypnoses = packet.config.GetHypnoses();
		if (!MultiPlayerManager.Instance.isHost)
		{
			if (!TryBeginPendingPlantRequest(packet, list.Count, out var requestId))
			{
				return false;
			}
			if (packet.HasMeta("packet_sync_id"))
			{
				packet.SetMeta("packet_pending_plant", requestId);
			}
			foreach (TowerDefenseInGamePacketShow.ColumnPlacement item in list)
			{
				MultiPlayerManager.Instance.SendPlacePlant(packet.config.saveKey, item.GridPos.X, item.GridPos.Y, -1, overrideData, requestId, "", item.Kind, item.Target?.syncId ?? (-1), hypnoses);
			}
			return true;
		}
		return packet.PlantColumnTargets(list, delegate(TowerDefenseInGamePacketShow.ColumnPlacement placement, TowerDefenseCharacter character)
		{
			int num = character.syncId;
			if (placement.Kind != "jala_vase")
			{
				num = TowerDefenseManager.Instance.currentControl.GetNextSyncId();
				TowerDefenseManager.Instance.currentControl.RegisterSyncCharacter(num, character);
			}
			MultiPlayerManager.Instance.SendPlacePlant(packet.config.saveKey, placement.GridPos.X, placement.GridPos.Y, num, overrideData, "", "", placement.Kind, placement.Target?.syncId ?? (-1), hypnoses);
		}).Count > 0;
	}

	public override void _Ready()
	{
		SetProcess(_pendingPlantRequests.Count > 0);
		SubscribeMultiplayerEvents();
	}

	public override void _ExitTree()
	{
		DisposeBattleState();
		base._ExitTree();
	}

	private void RollbackPendingPlantRequests()
	{
		foreach (PendingPlantRequest value in _pendingPlantRequests.Values)
		{
			value.SpendReceipt?.TryRollback();
			if (GodotObject.IsInstanceValid(value.Packet) && value.Packet.HasMeta("packet_pending_plant"))
			{
				value.Packet.RemoveMeta("packet_pending_plant");
			}
		}
		_pendingPlantRequests.Clear();
		_expiredPlantRequestIds.Clear();
		_pendingRequestSweepTimer = 0.0;
		SetProcess(enable: false);
	}

	public override void _Process(double delta)
	{
		if (_pendingPlantRequests.Count == 0)
		{
			_pendingRequestSweepTimer = 0.0;
			return;
		}
		_pendingRequestSweepTimer += delta;
		if (!(_pendingRequestSweepTimer < (_config?.pendingRequestSweepIntervalSeconds ?? 0.5)))
		{
			_pendingRequestSweepTimer = 0.0;
			SweepExpiredPlantRequests(Time.GetTicksMsec());
		}
	}

	public void Init(TowerDefenseMapControl _mapControl, TowerDefenseBattleFeatureMap _mapFeature, TowerDefenseBattleFeaturePacketPickConfig config = null)
	{
		mapControl = _mapControl;
		mapFeature = _mapFeature;
		_config = config;
		EnsurePreviewRenderMounts();
		SubscribeMultiplayerEvents();
	}

	private void EnsurePreviewRenderMounts()
	{
		Node2D characterNode = TowerDefenseManager.GetCharacterNode();
		if (!GodotObject.IsInstanceValid(characterNode) || !GodotObject.IsInstanceValid(mapControl?.spriteNode))
		{
			GD.PushError("PacketPick preview render mounts require both the battle character node and map overlay node.");
			return;
		}
		if (!GodotObject.IsInstanceValid(_placementPreviewRenderMount))
		{
			_placementPreviewRenderMount = CreatePreviewRenderMount("PacketPickPlacementPreviewRenderMount");
			characterNode.AddChild(_placementPreviewRenderMount, forceReadableName: false, InternalMode.Disabled);
		}
		if (!GodotObject.IsInstanceValid(_followPreviewRenderMount))
		{
			_followPreviewRenderMount = CreatePreviewRenderMount("PacketPickFollowPreviewRenderMount");
			mapControl.spriteNode.AddChild(_followPreviewRenderMount, forceReadableName: false, InternalMode.Disabled);
		}
	}

	private static Control CreatePreviewRenderMount(string nodeName)
	{
		return new Control
		{
			Name = nodeName,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
	}

	public void RegisterTool(PacketPickTool tool)
	{
		PruneInvalidTools();
		if (GodotObject.IsInstanceValid(tool) && !tools.Contains(tool))
		{
			tools.Add(tool);
		}
	}

	public void UnregisterTool(PacketPickTool tool)
	{
		if (GodotObject.IsInstanceValid(tool))
		{
			tool.ToolReset();
		}
		tools.Remove(tool);
		PruneInvalidTools();
	}

	private void PruneInvalidTools()
	{
		tools.RemoveAll((PacketPickTool tool) => !GodotObject.IsInstanceValid(tool));
	}

	private void SubscribeMultiplayerEvents()
	{
		if (!_multiplayerEventsSubscribed && MultiPlayerManager.Instance != null)
		{
			MultiPlayerManager.Instance.OnMatchStateReceived += OnMultiplayerMatchStateReceived;
			_multiplayerEventsSubscribed = true;
		}
	}

	private bool TryBeginPendingPlantRequest(TowerDefenseInGamePacketShow packet, int expectedCount, out string requestId)
	{
		requestId = "";
		SubscribeMultiplayerEvents();
		if (MultiPlayerManager.Instance == null || !GodotObject.IsInstanceValid(packet))
		{
			return false;
		}
		string[] obj = new string[5]
		{
			MultiPlayerManager.Instance.peerId,
			":",
			Time.GetTicksMsec().ToString(),
			":",
			null
		};
		obj[4] = (++_plantRequestSerial).ToString();
		requestId = string.Concat(obj);
		PendingPlantRequest pendingPlantRequest = new PendingPlantRequest
		{
			Packet = packet,
			PacketSyncId = (packet.HasMeta("packet_sync_id") ? packet.GetMeta("packet_sync_id").AsInt32() : (-1)),
			ExpectedCount = Math.Max(1, expectedCount),
			DeadlineMsec = Time.GetTicksMsec() + GetPendingRequestTimeoutMsec(),
			WasAlive = packet.alive,
			WasColdDownOpen = packet.coldDownOpen,
			ColdDown = packet.coldDown,
			ColdDownTimer = packet.coldDownTimer
		};
		if (!packet.TryBeginPendingUse(out pendingPlantRequest.SpendReceipt))
		{
			requestId = "";
			return false;
		}
		_pendingPlantRequests[requestId] = pendingPlantRequest;
		UpdatePendingRequestProcessing();
		return true;
	}

	private ulong GetPendingRequestTimeoutMsec()
	{
		return (ulong)Math.Ceiling(Math.Clamp(_config?.pendingRequestTimeoutSeconds ?? 15.0, 1.0, 120.0) * 1000.0);
	}

	private void UpdatePendingRequestProcessing()
	{
		SetProcess(_pendingPlantRequests.Count > 0);
	}

	private void SweepExpiredPlantRequests(ulong nowMsec)
	{
		_expiredPlantRequestIds.Clear();
		foreach (KeyValuePair<string, PendingPlantRequest> pendingPlantRequest in _pendingPlantRequests)
		{
			if (nowMsec >= pendingPlantRequest.Value.DeadlineMsec)
			{
				_expiredPlantRequestIds.Add(pendingPlantRequest.Key);
			}
		}
		foreach (string expiredPlantRequestId in _expiredPlantRequestIds)
		{
			if (_pendingPlantRequests.TryGetValue(expiredPlantRequestId, out var value))
			{
				FinalizePendingPlantRejected(expiredPlantRequestId, value);
			}
		}
		_expiredPlantRequestIds.Clear();
	}

	private void OnMultiplayerMatchStateReceived(string opCode, string data, string senderId)
	{
		if ((opCode != "place_plant" && opCode != "command_rejected") || MultiPlayerManager.Instance == null || MultiPlayerManager.Instance.isHost || senderId != "1")
		{
			return;
		}
		Variant variant = Json.ParseString(data);
		if (variant.VariantType != Variant.Type.Dictionary)
		{
			return;
		}
		Dictionary dictionary = variant.AsGodotDictionary();
		string text = dictionary.GetValueOrDefault("request_id", "").AsString();
		if (text == "" || !_pendingPlantRequests.ContainsKey(text))
		{
			return;
		}
		if (opCode == "place_plant")
		{
			HandlePendingPlantAccepted(text);
		}
		else if (!(dictionary.GetValueOrDefault("op_code", "").AsString() != "place_plant"))
		{
			string text2 = dictionary.GetValueOrDefault("target_user_id", "").AsString();
			if (MultiPlayerManager.Instance == null || !(text2 != "") || !(text2 != MultiPlayerManager.Instance.peerId))
			{
				HandlePendingPlantRejected(text);
			}
		}
	}

	private void HandlePendingPlantAccepted(string requestId)
	{
		if (_pendingPlantRequests.TryGetValue(requestId, out var value))
		{
			_pendingPlantRequests.Remove(requestId);
			UpdatePendingRequestProcessing();
			if (GodotObject.IsInstanceValid(value.Packet))
			{
				value.Packet.TryCommitPendingUse(value.SpendReceipt);
			}
			else
			{
				value.SpendReceipt?.TryCommit();
			}
			if (value.PacketSyncId >= 0)
			{
				MultiPlayerManager.Instance?.SendPacketPick(value.PacketSyncId);
			}
			if (GodotObject.IsInstanceValid(value.Packet) && value.Packet.HasMeta("packet_pending_plant"))
			{
				value.Packet.RemoveMeta("packet_pending_plant");
			}
		}
	}

	private void HandlePendingPlantRejected(string requestId)
	{
		if (_pendingPlantRequests.TryGetValue(requestId, out var value))
		{
			value.RejectedCount++;
			if (value.RejectedCount >= value.ExpectedCount)
			{
				FinalizePendingPlantRejected(requestId, value);
			}
		}
	}

	private void FinalizePendingPlantRejected(string requestId, PendingPlantRequest pending)
	{
		_pendingPlantRequests.Remove(requestId);
		UpdatePendingRequestProcessing();
		if (pending.PacketSyncId >= 0)
		{
			MultiPlayerManager.Instance?.SendPacketPick(pending.PacketSyncId, "unlock");
		}
		pending.SpendReceipt?.TryRollback();
		TowerDefenseInGamePacketShow packet = pending.Packet;
		if (GodotObject.IsInstanceValid(packet) && !packet.IsQueuedForDeletion())
		{
			if (packet.HasMeta("packet_pending_plant"))
			{
				packet.RemoveMeta("packet_pending_plant");
			}
			packet.alive = pending.WasAlive;
			packet.select = false;
			packet.coldDown = pending.ColdDown;
			packet.coldDownOpen = pending.WasColdDownOpen;
			packet.coldDownTimer = pending.ColdDownTimer;
			if (packet.HasMeta("packet_planted"))
			{
				packet.RemoveMeta("packet_planted");
			}
		}
	}

	public bool IsPicking()
	{
		PruneInvalidTools();
		foreach (PacketPickTool tool in tools)
		{
			if (tool.IsPicking())
			{
				return true;
			}
		}
		return GodotObject.IsInstanceValid(packetPick);
	}

	public bool NeedsInputProcessing()
	{
		if (!IsPicking() && !_wasPicking)
		{
			return _toolActivateGrace > 0;
		}
		return true;
	}

	public AdobeAnimateSprite CreatePreviewSprite(TowerDefensePacketConfig packetConfig)
	{
		AdobeAnimateSprite packetSprite = TowerDefenseManager.GetPacketSprite(packetConfig);
		if (!GodotObject.IsInstanceValid(packetSprite))
		{
			return null;
		}
		ConfigurePreviewRenderTree(packetSprite, _placementPreviewRenderMount);
		Color modulate = packetSprite.Modulate;
		modulate.A = 0.5f;
		packetSprite.Modulate = modulate;
		packetSprite.ZIndex = ResolvePlacementPreviewZIndex(0);
		packetSprite.Position = new Vector2(-100f, -100f);
		if (packetConfig.packetFlip)
		{
			packetSprite.Scale = new Vector2(0f - packetSprite.Scale.X, packetSprite.Scale.Y);
		}
		Color meshColor = packetSprite.meshColor;
		meshColor.A = 0.5f;
		packetSprite.meshColor = meshColor;
		return packetSprite;
	}

	private static void ConfigurePreviewRenderTree(Node node, Control renderMount)
	{
		if (node is AdobeAnimateSprite adobeAnimateSprite)
		{
			adobeAnimateSprite.LightMask = 0;
			adobeAnimateSprite.forceLocalRender = true;
			adobeAnimateSprite.forceCpuPoseRender = true;
			adobeAnimateSprite.SetRenderClipControl(renderMount);
		}
		for (int i = 0; i < node.GetChildCount(); i++)
		{
			ConfigurePreviewRenderTree(node.GetChild(i), renderMount);
		}
	}

	private static void ReleasePreviewRenderTree(Node node)
	{
		if (node is AdobeAnimateSprite adobeAnimateSprite)
		{
			adobeAnimateSprite.ClearRenderClipControl();
			adobeAnimateSprite.ReleaseForcedCpuPoseData();
		}
		for (int i = 0; i < node.GetChildCount(); i++)
		{
			ReleasePreviewRenderTree(node.GetChild(i));
		}
	}

	internal static int ResolvePlacementPreviewZIndex(int gridRow)
	{
		return (int)Math.Clamp((long)gridRow * 15L + 14, -4096L, 4096L);
	}

	internal static void SetPlacementPreviewRenderRow(AdobeAnimateSprite previewSprite, int gridRow)
	{
		if (GodotObject.IsInstanceValid(previewSprite))
		{
			previewSprite.ZIndex = ResolvePlacementPreviewZIndex(gridRow);
		}
	}

	private void PositionPlacementPreview(AdobeAnimateSprite previewSprite, Vector2 mapWorldPosition, int gridRow)
	{
		if (GodotObject.IsInstanceValid(previewSprite) && GodotObject.IsInstanceValid(mapControl))
		{
			SetPlacementPreviewRenderRow(previewSprite, gridRow);
			mapControl.PositionMapOverlayAtWorldPoint(previewSprite, mapWorldPosition, Vector2.Zero);
		}
	}

	public void FreePreviewSprites()
	{
		if (GodotObject.IsInstanceValid(followSprite))
		{
			ReleasePreviewRenderTree(followSprite);
			followSprite.QueueFree();
			followSprite = null;
		}
		foreach (AdobeAnimateSprite plantSprite in plantSpriteList)
		{
			if (GodotObject.IsInstanceValid(plantSprite))
			{
				ReleasePreviewRenderTree(plantSprite);
				plantSprite.QueueFree();
			}
		}
		plantSpriteList.Clear();
	}

	public void ApplyArmorSprite(AdobeAnimateSprite sprite, ArmorSlotConfig slotConfig, TowerDefenseArmorTypeData typeData)
	{
		CharacterArmorData.CreateArmorPartNode(sprite, slotConfig, typeData);
	}

	public TowerDefenseCharacter FindZombieAtPosition(Vector2 mousePos, TowerDefenseEnum.CHARACTER_CAMP targetCamp = TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE)
	{
		Godot.Collections.Array zombie = TowerDefenseManager.Instance.GetZombie();
		TowerDefenseCharacter result = null;
		Vector2 mapGridSize = TowerDefenseManager.Instance.GetMapGridSize();
		double num = (double)Mathf.Min(mapGridSize.X, mapGridSize.Y) * (_config?.characterTargetRadiusScale ?? 0.6);
		num *= num;
		foreach (Variant item in zombie)
		{
			TowerDefenseCharacter towerDefenseCharacter = (TowerDefenseCharacter)(GodotObject)item;
			if (GodotObject.IsInstanceValid(towerDefenseCharacter) && !towerDefenseCharacter.die && !towerDefenseCharacter.nearDie && !towerDefenseCharacter.instance.invincible && towerDefenseCharacter.instance.canBeCollection && towerDefenseCharacter.camp == targetCamp)
			{
				double num2 = towerDefenseCharacter.GetLogicalGlobalPosition().DistanceSquaredTo(mousePos);
				if (num2 < num)
				{
					num = num2;
					result = towerDefenseCharacter;
				}
			}
		}
		return result;
	}

	public TowerDefenseCharacter FindPlantAtPosition(Vector2 mousePos, TowerDefenseEnum.CHARACTER_CAMP targetCamp = TowerDefenseEnum.CHARACTER_CAMP.PLANT)
	{
		Godot.Collections.Array plant = TowerDefenseManager.Instance.GetPlant();
		TowerDefenseCharacter result = null;
		Vector2 mapGridSize = TowerDefenseManager.Instance.GetMapGridSize();
		double num = (double)Mathf.Min(mapGridSize.X, mapGridSize.Y) * (_config?.characterTargetRadiusScale ?? 0.6);
		num *= num;
		foreach (Variant item in plant)
		{
			TowerDefenseCharacter towerDefenseCharacter = (TowerDefenseCharacter)(GodotObject)item;
			if (GodotObject.IsInstanceValid(towerDefenseCharacter) && !towerDefenseCharacter.die && !towerDefenseCharacter.nearDie && !towerDefenseCharacter.instance.invincible && towerDefenseCharacter.instance.canBeCollection && towerDefenseCharacter.camp == targetCamp)
			{
				double num2 = towerDefenseCharacter.GetLogicalGlobalPosition().DistanceSquaredTo(mousePos);
				if (num2 < num)
				{
					num = num2;
					result = towerDefenseCharacter;
				}
			}
		}
		return result;
	}

	public TowerDefenseCharacter FindPlantInCell(TowerDefenseCellInstance cell, bool hypnoses)
	{
		if (!GodotObject.IsInstanceValid(cell))
		{
			return null;
		}
		cell.ClearEmpty();
		foreach (TowerDefenseCharacter character in cell.characterList)
		{
			if (GodotObject.IsInstanceValid(character) && character is TowerDefensePlant && !character.die && !character.nearDie && GodotObject.IsInstanceValid(character.instance) && !character.instance.invincible && !character.instance.hologram && character.instance.canBeCollection)
			{
				return character;
			}
		}
		return null;
	}

	private bool ProcessMultiplayerCharacterPlacement(IReadOnlyList<TowerDefenseCharacter> targets, string placementKind, bool hypnoses)
	{
		if (MultiPlayerManager.Instance == null || !GodotObject.IsInstanceValid(packetPick) || targets == null)
		{
			return false;
		}
		string overrideData = "";
		if (GodotObject.IsInstanceValid(packetPick.config._override))
		{
			overrideData = Json.Stringify(packetPick.config._override.Export());
		}
		List<TowerDefenseCharacter> list = new List<TowerDefenseCharacter>(targets.Count);
		foreach (TowerDefenseCharacter target in targets)
		{
			if (GodotObject.IsInstanceValid(target) && target.syncId >= 0)
			{
				list.Add(target);
			}
		}
		if (list.Count == 0)
		{
			GD.PushWarning($"Packet {packetPick.config.saveKey} cannot use {placementKind} placement without a synchronized target.");
			return false;
		}
		if (!MultiPlayerManager.Instance.isHost)
		{
			if (!TryBeginPendingPlantRequest(packetPick, list.Count, out var requestId))
			{
				return false;
			}
			if (packetPick.HasMeta("packet_sync_id"))
			{
				packetPick.SetMeta("packet_pending_plant", requestId);
			}
			foreach (TowerDefenseCharacter item in list)
			{
				MultiPlayerManager.Instance.SendPlacePlant(packetPick.config.saveKey, item.gridPos.X, item.gridPos.Y, -1, overrideData, requestId, "", placementKind, item.syncId, hypnoses);
			}
			return true;
		}
		int num = 0;
		foreach (TowerDefenseCharacter item2 in list)
		{
			int syncId = item2.syncId;
			int nextSyncId = TowerDefenseManager.Instance.currentControl.GetNextSyncId();
			bool useSun = num == 0;
			TowerDefenseCharacter towerDefenseCharacter = ((placementKind == "plant") ? packetPick.PlantOnPlant(item2, useSun, executeEvent: false) : packetPick.PlantOnZombie(item2, hypnoses, useSun, executeEvent: false));
			if (GodotObject.IsInstanceValid(towerDefenseCharacter))
			{
				TowerDefenseManager.Instance.currentControl.RegisterSyncCharacter(nextSyncId, towerDefenseCharacter);
				MultiPlayerManager.Instance.SendPlacePlant(packetPick.config.saveKey, item2.gridPos.X, item2.gridPos.Y, nextSyncId, overrideData, "", "", placementKind, syncId, hypnoses);
				num++;
			}
		}
		if (num > 0)
		{
			packetPick.NotifyUseBehaviorSucceeded();
		}
		return num > 0;
	}

	public bool ProcessPacketPick(TowerDefenseCellInstance cell, Vector2I gridPos, Vector2 mousePos)
	{
		bool flag = UpdateSelectionGestureConfirmSuppression();
		int num = -1;
		if (GodotObject.IsInstanceValid(TowerDefenseManager.Instance.currentControl) && TowerDefenseManager.Instance.currentControl.levelConfig is TowerDefenseLevelConfig towerDefenseLevelConfig)
		{
			num = towerDefenseLevelConfig.limitGridPlantNum;
		}
		if (selectPacketTimer > 0)
		{
			selectPacketTimer--;
			return true;
		}
		if (followSprite != null)
		{
			followSprite.Visible = true;
			if (GodotObject.IsInstanceValid(mapControl) && GodotObject.IsInstanceValid(mapControl.spriteNode))
			{
				mapControl.PositionMapOverlayAtWorldPoint(followSprite, mousePos, new Vector2(0f, -20f));
			}
		}
		foreach (AdobeAnimateSprite plantSprite in plantSpriteList)
		{
			plantSprite.Visible = false;
		}
		if (mapFeature.isPlantColumn)
		{
			return ProcessColumnTargetPlacement(gridPos, flag, num);
		}
		if (GodotObject.IsInstanceValid(cell) && GodotObject.IsInstanceValid(cell.FindPlantingBlocker(packetPick.config)))
		{
			if (!flag && mapControl.IsConfirmInput())
			{
				cell.NotifyBlockedPlanting(packetPick.config);
			}
			return false;
		}
		bool flag2 = false;
		bool flag3 = ShouldPreferCraterRepair(cell);
		if (packetPick.config.canPlaceOnZombie && !flag3)
		{
			bool hypnoses = packetPick.config.GetHypnoses();
			TowerDefenseEnum.CHARACTER_CAMP targetCamp = ((!hypnoses) ? TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE : TowerDefenseEnum.CHARACTER_CAMP.PLANT);
			TowerDefenseCharacter towerDefenseCharacter = FindZombieAtPosition(mousePos, targetCamp);
			if (GodotObject.IsInstanceValid(towerDefenseCharacter))
			{
				flag2 = true;
				if (plantSpriteList.Count > 0)
				{
					plantSpriteList[0].Visible = true;
					PositionPlacementPreview(plantSpriteList[0], towerDefenseCharacter.GetLogicalGlobalPosition(), towerDefenseCharacter.gridPos.Y);
				}
				if (!flag && mapControl.IsConfirmInput())
				{
					if (Global.Instance.isMultiplayerMode)
					{
						ProcessMultiplayerCharacterPlacement(new TowerDefenseCharacter[1] { towerDefenseCharacter }, "zombie", hypnoses);
					}
					else
					{
						packetPick.PlantOnZombie(towerDefenseCharacter, hypnoses);
					}
					if (!Global.Instance.isEditor || SceneManager.Instance.currentScene != "LevelEditorStage")
					{
						Release();
					}
					else
					{
						LevelEditorMapEditor.instance.levelConfig.canExport = false;
					}
					return true;
				}
			}
		}
		if (!flag2 && GodotObject.IsInstanceValid(cell))
		{
			bool flag4 = true;
			bool flag5 = packetPick.config.characterConfig.plantGridType.Contains(TowerDefenseEnum.PLANTGRIDTYPE.PLANT);
			bool flag6 = false;
			foreach (TowerDefenseEnum.PLANTGRIDTYPE item in packetPick.config.characterConfig.plantGridType)
			{
				if (item != TowerDefenseEnum.PLANTGRIDTYPE.PLANT)
				{
					flag6 = true;
					break;
				}
			}
			if (!TowerDefenseManager.Instance.CheckMapGridPosIn(gridPos))
			{
				flag4 = false;
			}
			if (packetPick.config.IsLimitGridNum() && num != -1 && num <= mapFeature.GetCellPlantNum() && !cell.HasPlant())
			{
				flag4 = false;
			}
			if (packetPick.config.characterConfig is TowerDefenseZombieConfig)
			{
				flag4 = true;
			}
			if (!flag5 && !cell.CanPacketPlant(packetPick.config))
			{
				flag4 = false;
			}
			if ((packetPick.config.characterConfig is TowerDefensePlantConfig || packetPick.config.characterConfig is TowerDefenseZombieConfig) && mapFeature.stripeRow != -1)
			{
				if ((packetPick.config.characterConfig is TowerDefensePlantConfig || packetPick.config.izmPlantLeft) && mapFeature.stripeRow < gridPos.X)
				{
					flag4 = false;
				}
				if (packetPick.config.characterConfig is TowerDefenseZombieConfig && !packetPick.config.izmPlantLeft && mapFeature.stripeRow >= gridPos.X)
				{
					flag4 = false;
				}
			}
			if (TowerDefenseManager.Instance.IsIZMMode() && packetPick.config.izmPlantAllCell)
			{
				flag4 = true;
			}
			if (flag4 && flag5)
			{
				bool hypnoses2 = packetPick.config.GetHypnoses();
				if (!flag6 || cell.CanPacketPlant(packetPick.config))
				{
					TowerDefenseCharacter towerDefenseCharacter2 = FindPlantInCell(cell, hypnoses2);
					if (GodotObject.IsInstanceValid(towerDefenseCharacter2))
					{
						if (plantSpriteList.Count > 0)
						{
							plantSpriteList[0].Visible = true;
							PositionPlacementPreview(plantSpriteList[0], towerDefenseCharacter2.GetLogicalGlobalPosition(), towerDefenseCharacter2.gridPos.Y);
						}
						if (!flag && mapControl.IsConfirmInput())
						{
							if (Global.Instance.isMultiplayerMode)
							{
								ProcessMultiplayerCharacterPlacement(new TowerDefenseCharacter[1] { towerDefenseCharacter2 }, "plant", hypnoses2);
							}
							else
							{
								packetPick.PlantOnPlant(towerDefenseCharacter2);
							}
							if (!Global.Instance.isEditor || SceneManager.Instance.currentScene != "LevelEditorStage")
							{
								Release();
							}
							else
							{
								LevelEditorMapEditor.instance.levelConfig.canExport = false;
							}
							return true;
						}
					}
				}
			}
			else if (flag4)
			{
				Vector2 mapCellPlantPos = TowerDefenseManager.GetMapCellPlantPos(gridPos);
				double groundHeight = mapFeature.GetGroundHeight(cell);
				if (plantSpriteList.Count > 0)
				{
					plantSpriteList[0].Visible = true;
					PositionPlacementPreview(plantSpriteList[0], mapCellPlantPos - new Vector2(0f, (float)groundHeight), gridPos.Y);
				}
				if (!flag && mapControl.IsConfirmInput())
				{
					if (Global.Instance.isMultiplayerMode)
					{
						string overrideData = "";
						if (GodotObject.IsInstanceValid(packetPick.config._override))
						{
							overrideData = Json.Stringify(packetPick.config._override.Export());
						}
						if (MultiPlayerManager.Instance.isHost)
						{
							int nextSyncId = TowerDefenseManager.Instance.currentControl.GetNextSyncId();
							TowerDefenseCharacter towerDefenseCharacter3 = packetPick.Plant(gridPos);
							if (GodotObject.IsInstanceValid(towerDefenseCharacter3))
							{
								TowerDefenseManager.Instance.currentControl.RegisterSyncCharacter(nextSyncId, towerDefenseCharacter3);
								MultiPlayerManager.Instance.SendPlacePlant(packetPick.config.saveKey, gridPos.X, gridPos.Y, nextSyncId, overrideData);
							}
						}
						else
						{
							if (!TryBeginPendingPlantRequest(packetPick, 1, out var requestId))
							{
								Release();
								return true;
							}
							if (packetPick.HasMeta("packet_sync_id"))
							{
								packetPick.SetMeta("packet_pending_plant", requestId);
							}
							MultiPlayerManager.Instance.SendPlacePlant(packetPick.config.saveKey, gridPos.X, gridPos.Y, -1, overrideData, requestId);
						}
						if (!Global.Instance.isEditor || SceneManager.Instance.currentScene != "LevelEditorStage")
						{
							Release();
						}
						else
						{
							LevelEditorMapEditor.instance.levelConfig.canExport = false;
						}
					}
					else
					{
						packetPick.Plant(gridPos);
						if (!Global.Instance.isEditor || SceneManager.Instance.currentScene != "LevelEditorStage")
						{
							Release();
						}
						else
						{
							LevelEditorMapEditor.instance.levelConfig.canExport = false;
						}
					}
				}
			}
		}
		return false;
	}

	private bool ShouldPreferCraterRepair(TowerDefenseCellInstance cell)
	{
		if (!GodotObject.IsInstanceValid(cell))
		{
			return false;
		}
		if (!GodotObject.IsInstanceValid(packetPick) || !GodotObject.IsInstanceValid(packetPick.config) || !GodotObject.IsInstanceValid(packetPick.config.characterConfig))
		{
			return false;
		}
		if (!packetPick.config.characterConfig.plantGridType.Contains(TowerDefenseEnum.PLANTGRIDTYPE.CRATER))
		{
			return false;
		}
		if (!GodotObject.IsInstanceValid(cell.GetRepairableCrater()))
		{
			return false;
		}
		return cell.CanPacketPlant(packetPick.config);
	}

	public void ProcessTools(TowerDefenseCellInstance cell, Vector2I gridPos, Vector2 mousePos)
	{
		foreach (PacketPickTool tool in tools)
		{
			if (tool.IsPicking())
			{
				tool.ProcessPick(cell, gridPos, mousePos);
			}
		}
		bool flag = IsPicking();
		if (flag && !_wasPicking)
		{
			_toolActivateGrace = _config?.toolActivationGraceFrames ?? 5;
		}
		_wasPicking = flag;
	}

	public void PickPacket(TowerDefenseInGamePacketShow _packet)
	{
		TowerDefenseCharacterConfig characterConfig = _packet.config.characterConfig;
		selectPacketTimer = _config?.packetSelectionDebounceFrames ?? 5;
		foreach (PacketPickTool tool in tools)
		{
			tool.ToolReset();
		}
		if (_packet.select)
		{
			EnsurePreviewRenderMounts();
			if (!GodotObject.IsInstanceValid(_placementPreviewRenderMount) || !GodotObject.IsInstanceValid(_followPreviewRenderMount))
			{
				GD.PushError("PacketPick cannot create previews before both render mounts are ready.");
				_packet.Reset();
				return;
			}
			if (GodotObject.IsInstanceValid(packetPick) && packetPick != _packet)
			{
				PacketPickRelease();
			}
			packetPick = _packet;
			_suppressSelectionGestureConfirm = Input.IsActionPressed("Press");
			if (Global.Instance.isMultiplayerMode && _packet.HasMeta("packet_sync_id"))
			{
				int syncId = _packet.GetMeta("packet_sync_id").AsInt32();
				MultiPlayerManager.Instance.SendPacketPick(syncId, "lock");
			}
			Godot.Collections.Array character = TowerDefenseManager.Instance.GetCharacter();
			foreach (Variant item in character)
			{
				((TowerDefenseCharacter)(GodotObject)item).SetSpriteGroupShaderParameter("cover", false);
			}
			foreach (Variant item2 in character)
			{
				TowerDefenseCharacter towerDefenseCharacter = (TowerDefenseCharacter)(GodotObject)item2;
				bool flag = true;
				if (towerDefenseCharacter is TowerDefensePlant && GodotObject.IsInstanceValid(towerDefenseCharacter.cell) && !towerDefenseCharacter.cell.CanPacketPlant(packetPick.config))
				{
					flag = false;
				}
				if (flag && towerDefenseCharacter is TowerDefensePlant && GodotObject.IsInstanceValid(towerDefenseCharacter.cell) && characterConfig is TowerDefensePlantConfig towerDefensePlantConfig && towerDefensePlantConfig.extendCoverDictionary.Count > 0)
				{
					foreach (Vector2I key in towerDefensePlantConfig.extendCoverDictionary.Keys)
					{
						Vector2I vector2I = key;
						TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(towerDefenseCharacter.cell.gridPos + vector2I);
						if (!GodotObject.IsInstanceValid(mapCell))
						{
							continue;
						}
						foreach (TowerDefenseCharacter character2 in mapCell.GetCharacterList())
						{
							if (character2.config.name == towerDefensePlantConfig.extendCoverDictionary[key])
							{
								character2.SetSpriteGroupShaderParameter("cover", true);
							}
						}
					}
				}
				if (flag)
				{
					Array<string> plantCover = packetPick.config.GetPlantCover();
					bool flag2 = plantCover.Count > 0 && plantCover.Contains(towerDefenseCharacter.config.name);
					if (!flag2 && characterConfig.plantCoverSelf)
					{
						flag2 = characterConfig.name == towerDefenseCharacter.config.name;
					}
					if (flag2)
					{
						towerDefenseCharacter.SetSpriteGroupShaderParameter("cover", true);
					}
				}
			}
			FreePreviewSprites();
			followSprite = TowerDefenseManager.GetPacketSprite(_packet.config);
			if (GodotObject.IsInstanceValid(followSprite))
			{
				ConfigurePreviewRenderTree(followSprite, _followPreviewRenderMount);
				followSprite.ZIndex = 4096;
				followSprite.Position = new Vector2(-100f, -100f);
				if (_packet.config.packetFlip)
				{
					followSprite.Scale = new Vector2(0f - followSprite.Scale.X, followSprite.Scale.Y);
				}
				int num = ((!mapFeature.isPlantColumn) ? 1 : mapFeature.config.gridNum.Y);
				for (int i = 0; i < num; i++)
				{
					AdobeAnimateSprite adobeAnimateSprite = CreatePreviewSprite(_packet.config);
					if (GodotObject.IsInstanceValid(adobeAnimateSprite))
					{
						plantSpriteList.Add(adobeAnimateSprite);
					}
				}
				if (characterConfig.armorData != null && _packet.config.initArmor.Count > 0)
				{
					foreach (string item3 in _packet.config.initArmor)
					{
						ArmorSlotConfig slotConfig = characterConfig.armorData.GetSlotConfig(item3);
						TowerDefenseArmorTypeData typeData = characterConfig.armorData.GetTypeData(item3);
						if (typeData == null)
						{
							continue;
						}
						string replaceMethod = slotConfig.replaceMethod;
						if (!(replaceMethod == "Media"))
						{
							if (!(replaceMethod == "Sprite"))
							{
								continue;
							}
							ApplyArmorSprite(followSprite, slotConfig, typeData);
							foreach (AdobeAnimateSprite plantSprite in plantSpriteList)
							{
								ApplyArmorSprite(plantSprite, slotConfig, typeData);
							}
							continue;
						}
						characterConfig.armorData.OpenArmorFliters(followSprite, item3);
						characterConfig.armorData.SetArmorReplace(followSprite, item3, 0);
						foreach (AdobeAnimateSprite plantSprite2 in plantSpriteList)
						{
							characterConfig.armorData.OpenArmorFliters(plantSprite2, item3);
							characterConfig.armorData.SetArmorReplace(plantSprite2, item3, 0);
						}
					}
				}
				if (characterConfig.customData != null)
				{
					Dictionary dictionary = GameSaveManager.Instance.GetTowerDefensePacketValue(_packet.config.saveKey).GetValueOrDefault("Key", new Dictionary()).AsGodotDictionary();
					if (dictionary.GetValueOrDefault("Custom", "").AsString() != "")
					{
						characterConfig.customData.SetCustomFliters(followSprite, dictionary["Custom"].AsString());
						foreach (AdobeAnimateSprite plantSprite3 in plantSpriteList)
						{
							characterConfig.customData.SetCustomFliters(plantSprite3, dictionary["Custom"].AsString());
						}
					}
				}
				ConfigurePreviewRenderTree(followSprite, _followPreviewRenderMount);
				foreach (AdobeAnimateSprite plantSprite4 in plantSpriteList)
				{
					ConfigurePreviewRenderTree(plantSprite4, _placementPreviewRenderMount);
				}
				mapControl.spriteNode.AddChild(followSprite, forceReadableName: false, InternalMode.Disabled);
				{
					foreach (AdobeAnimateSprite plantSprite5 in plantSpriteList)
					{
						mapControl.spriteNode.AddChild(plantSprite5, forceReadableName: false, InternalMode.Disabled);
					}
					return;
				}
			}
			GD.PushWarning("Packet " + _packet.config.saveKey + " has no preview sprite and cannot be picked.");
			PacketPickRelease();
		}
		else if (_packet.canPressPutBack)
		{
			Release();
		}
	}

	private bool UpdateSelectionGestureConfirmSuppression()
	{
		if (!_suppressSelectionGestureConfirm)
		{
			return false;
		}
		bool num = Input.IsActionPressed("Press");
		bool flag = Input.IsActionJustReleased("Press");
		if (!num)
		{
			_suppressSelectionGestureConfirm = false;
		}
		return num || flag;
	}

	public void Release()
	{
		foreach (PacketPickTool tool in tools)
		{
			tool.ToolRelease();
		}
		PacketPickRelease();
	}

	public void ProcessReleaseInput(Vector2 mousePos)
	{
		if (_toolActivateGrace > 0)
		{
			_toolActivateGrace--;
		}
		else if (!Global.Instance.isEditor || SceneManager.Instance.currentScene != "LevelEditorStage")
		{
			if (Global.Instance.isMobile)
			{
				if (Input.IsActionJustPressed("Press") && IsPicking() && GodotObject.IsInstanceValid(mapControl) && !IsPlacementSurfacePoint(mousePos))
				{
					Release();
				}
				if (!Input.IsActionJustReleased("Press") || IsPicking())
				{
					return;
				}
				foreach (AdobeAnimateSprite plantSprite in plantSpriteList)
				{
					if (GodotObject.IsInstanceValid(plantSprite))
					{
						plantSprite.Visible = false;
					}
				}
				if (GodotObject.IsInstanceValid(followSprite))
				{
					followSprite.Visible = false;
				}
				{
					foreach (PacketPickTool tool in tools)
					{
						Node2D mapSprite = tool.GetMapSprite();
						if (GodotObject.IsInstanceValid(mapSprite))
						{
							mapSprite.Visible = false;
						}
					}
					return;
				}
			}
			if (Input.IsActionJustPressed("Release"))
			{
				Release();
			}
		}
		else if (Input.IsActionJustPressed("Release"))
		{
			Release();
		}
	}

	private bool IsPlacementSurfacePoint(Vector2 mousePos)
	{
		if (!GodotObject.IsInstanceValid(mapFeature))
		{
			return false;
		}
		if (mapFeature.groundRect.HasPoint(mousePos))
		{
			return true;
		}
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(instance))
		{
			return false;
		}
		Vector2I mapGridPosFromMouse = instance.GetMapGridPosFromMouse(mousePos);
		if (instance.CheckMapGridPosIn(mapGridPosFromMouse))
		{
			return GodotObject.IsInstanceValid(TowerDefenseManager.GetMapCell(mapGridPosFromMouse));
		}
		return false;
	}

	public void PacketPickRelease()
	{
		if (GodotObject.IsInstanceValid(packetPick))
		{
			if (Global.Instance.isMultiplayerMode && packetPick.HasMeta("packet_sync_id"))
			{
				int syncId = packetPick.GetMeta("packet_sync_id").AsInt32();
				if (!packetPick.HasMeta("packet_pending_plant"))
				{
					if (packetPick.HasMeta("packet_planted"))
					{
						MultiPlayerManager.Instance.SendPacketPick(syncId);
					}
					else
					{
						MultiPlayerManager.Instance.SendPacketPick(syncId, "unlock");
					}
				}
			}
			foreach (Variant item in TowerDefenseManager.Instance.GetCharacter())
			{
				((TowerDefenseCharacter)(GodotObject)item).SetSpriteGroupShaderParameter("cover", false);
			}
			packetPick.Reset();
			packetPick = null;
		}
		_suppressSelectionGestureConfirm = false;
		FreePreviewSprites();
	}

	public void DisposeBattleState()
	{
		if (_multiplayerEventsSubscribed && MultiPlayerManager.Instance != null)
		{
			MultiPlayerManager.Instance.OnMatchStateReceived -= OnMultiplayerMatchStateReceived;
		}
		_multiplayerEventsSubscribed = false;
		RollbackPendingPlantRequests();
		foreach (PacketPickTool tool in tools)
		{
			if (GodotObject.IsInstanceValid(tool))
			{
				tool.ToolReset();
			}
		}
		tools.Clear();
		if (GodotObject.IsInstanceValid(packetPick))
		{
			packetPick.Reset();
		}
		packetPick = null;
		FreePreviewSprites();
		if (GodotObject.IsInstanceValid(_placementPreviewRenderMount))
		{
			_placementPreviewRenderMount.QueueFree();
			_placementPreviewRenderMount = null;
		}
		if (GodotObject.IsInstanceValid(_followPreviewRenderMount))
		{
			_followPreviewRenderMount.QueueFree();
			_followPreviewRenderMount = null;
		}
		mapControl = null;
		mapFeature = null;
		_config = null;
		_wasPicking = false;
		_toolActivateGrace = 0;
		_suppressSelectionGestureConfirm = false;
	}
}
