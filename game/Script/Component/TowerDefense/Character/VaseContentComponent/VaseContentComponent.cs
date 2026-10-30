using System;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public sealed class VaseContentComponent : CharacterComponentRuntime
{
	private enum ContentMode
	{
		Random,
		Empty,
		Fixed
	}

	private sealed class BreakResult
	{
		public string ContentType = "none";

		public string ContentName = "";

		public int ZombieSyncId = -1;

		public Dictionary PacketShowData;

		public TowerDefensePacketConfig ContentConfig;
	}

	private sealed class BreakOperationContext
	{
		public TowerDefenseControlNew Owner;

		public int OperationId = -1;

		public SceneTree Tree;

		public Vector2I GridPos;

		public Vector2 VaseGlobalPosition;

		public Vector2 EffectGlobalPosition;

		public int ZIndex;

		public double GroundHeight;

		public bool Hypnoses;

		public bool HadCell;

		public bool AllowRandomContent;

		public string PacketBank = "Total";

		public TowerDefensePacketConfig FixedPacket;

		public PackedScene ChunkParticles;

		public bool PlayBreakAudio;

		public string BreakAudio = "VaseBreaking";

		public Vector2 PacketHorizontalSpeedRange = new Vector2(30f, 50f);

		public float PacketVerticalSpeed = -300f;

		public float PacketGravity = 980f;

		public float PacketAliveTime = 15f;

		public int ReadyWaitFrameLimit = 2;

		public TowerDefenseCharacter CreatedZombie;

		public TowerDefenseInGamePacketShow CreatedPacket;

		public int CreatedPacketSyncId = -1;

		public readonly BreakResult Result = new BreakResult();
	}

	public TowerDefenseVase parent;

	public bool playBreakAudio = true;

	public string breakAudio = "VaseBreaking";

	public Vector2 breakEffectOffset = new Vector2(0f, -30f);

	public Vector2 packetHorizontalSpeedRange = new Vector2(30f, 50f);

	public float packetVerticalSpeed = -300f;

	public float packetGravity = 980f;

	public float packetAliveTime = 15f;

	public int readyWaitFrameLimit = 2;

	private bool _configured;

	private ContentMode _contentMode;

	private VaseContentComponentDefinition Definition => base.ComponentDefinition as VaseContentComponentDefinition;

	protected override void OnBound()
	{
		parent = base.Owner as TowerDefenseVase;
		_contentMode = ((GodotObject.IsInstanceValid(parent?.packetConfig) || !string.IsNullOrEmpty(parent?.packetName)) ? ContentMode.Fixed : ContentMode.Random);
		if (!_configured)
		{
			VaseContentComponentDefinition definition = Definition;
			playBreakAudio = definition?.playBreakAudio ?? true;
			breakAudio = definition?.breakAudio ?? "VaseBreaking";
			breakEffectOffset = definition?.breakEffectOffset ?? new Vector2(0f, -30f);
			packetHorizontalSpeedRange = definition?.packetHorizontalSpeedRange ?? new Vector2(30f, 50f);
			packetVerticalSpeed = definition?.packetVerticalSpeed ?? (-300f);
			packetGravity = definition?.packetGravity ?? 980f;
			packetAliveTime = Mathf.Max(0f, definition?.packetAliveTime ?? 15f);
			readyWaitFrameLimit = Math.Clamp(definition?.readyWaitFrameLimit ?? 2, 1, 30);
			_configured = true;
		}
	}

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		parent = null;
	}

	protected override void OnReleased()
	{
		parent = null;
	}

	public void SetContent(string contentName)
	{
		if (base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && GodotObject.IsInstanceValid(parent))
		{
			if (string.IsNullOrEmpty(contentName))
			{
				parent.SetContentConfig(null);
				_contentMode = ContentMode.Empty;
			}
			else
			{
				parent.packetName = contentName;
				_contentMode = ContentMode.Fixed;
			}
		}
	}

	public void SetRandomContent()
	{
		if (base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && GodotObject.IsInstanceValid(parent))
		{
			parent.SetContentConfig(null);
			_contentMode = ContentMode.Random;
		}
	}

	public void NotifyContentConfigAssigned(TowerDefensePacketConfig contentConfig)
	{
		_contentMode = ((!GodotObject.IsInstanceValid(contentConfig)) ? ContentMode.Empty : ContentMode.Fixed);
	}

	public void DestroySet()
	{
		TowerDefenseVase towerDefenseVase = parent;
		if (base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && GodotObject.IsInstanceValid(towerDefenseVase) && !towerDefenseVase.over)
		{
			towerDefenseVase.over = true;
			BreakOperationContext breakOperationContext = CaptureContext(towerDefenseVase);
			if (GodotObject.IsInstanceValid(breakOperationContext.Owner))
			{
				breakOperationContext.OperationId = breakOperationContext.Owner.BeginPendingBattleOperation();
			}
			if (GodotObject.IsInstanceValid(breakOperationContext.Owner) && GodotObject.IsInstanceValid(breakOperationContext.Owner.levelControl))
			{
				breakOperationContext.Owner.levelControl.hasSpawn = true;
			}
			CreateBreakVisuals(breakOperationContext);
			ResolveBreakAsync(breakOperationContext);
		}
	}

	public void PlayBreakVisuals()
	{
		TowerDefenseVase towerDefenseVase = parent;
		if (base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && GodotObject.IsInstanceValid(towerDefenseVase))
		{
			CreateBreakVisuals(new BreakOperationContext
			{
				GridPos = towerDefenseVase.gridPos,
				EffectGlobalPosition = (GodotObject.IsInstanceValid(towerDefenseVase.transformPoint) ? (towerDefenseVase.GetLogicalGlobalPosition(towerDefenseVase.transformPoint) + breakEffectOffset) : (towerDefenseVase.GetLogicalGlobalPosition() + breakEffectOffset)),
				ChunkParticles = towerDefenseVase.chunkParticles,
				PlayBreakAudio = playBreakAudio,
				BreakAudio = breakAudio,
				PacketHorizontalSpeedRange = packetHorizontalSpeedRange,
				PacketVerticalSpeed = packetVerticalSpeed,
				PacketGravity = packetGravity,
				PacketAliveTime = packetAliveTime,
				ReadyWaitFrameLimit = Math.Clamp(readyWaitFrameLimit, 1, 30)
			});
		}
	}

	private BreakOperationContext CaptureContext(TowerDefenseVase vase)
	{
		TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
		TowerDefensePacketConfig fixedPacket = null;
		if (GodotObject.IsInstanceValid(vase.packetConfig))
		{
			fixedPacket = vase.packetConfig.CreateRuntimeStateCopy();
		}
		return new BreakOperationContext
		{
			Owner = currentControl,
			Tree = (GodotObject.IsInstanceValid(currentControl) ? currentControl.GetTree() : vase.GetTree()),
			GridPos = vase.gridPos,
			VaseGlobalPosition = vase.GetLogicalGlobalPosition(),
			EffectGlobalPosition = (GodotObject.IsInstanceValid(vase.transformPoint) ? (vase.GetLogicalGlobalPosition(vase.transformPoint) + breakEffectOffset) : (vase.GetLogicalGlobalPosition() + breakEffectOffset)),
			ZIndex = vase.ZIndex,
			GroundHeight = vase.groundHeight,
			Hypnoses = (GodotObject.IsInstanceValid(vase.instance) && vase.instance.hypnoses),
			HadCell = GodotObject.IsInstanceValid(vase.cell),
			AllowRandomContent = (_contentMode == ContentMode.Random),
			PacketBank = vase.packetBank,
			FixedPacket = fixedPacket,
			ChunkParticles = vase.chunkParticles,
			PlayBreakAudio = playBreakAudio,
			BreakAudio = breakAudio,
			PacketHorizontalSpeedRange = packetHorizontalSpeedRange,
			PacketVerticalSpeed = packetVerticalSpeed,
			PacketGravity = packetGravity,
			PacketAliveTime = packetAliveTime,
			ReadyWaitFrameLimit = Math.Clamp(readyWaitFrameLimit, 1, 30)
		};
	}

	private static void CreateBreakVisuals(BreakOperationContext context)
	{
		try
		{
			if (context.PlayBreakAudio && !string.IsNullOrEmpty(context.BreakAudio) && GodotObject.IsInstanceValid(AudioManager.Instance))
			{
				AudioManager.Instance.AudioPlay(context.BreakAudio);
			}
			if (GodotObject.IsInstanceValid(context.ChunkParticles) && GodotObject.IsInstanceValid(TowerDefenseGroundItemBase.characterNode))
			{
				TowerDefenseEffectParticlesOnce towerDefenseEffectParticlesOnce = TowerDefenseManager.CreateEffectParticlesOnce(context.ChunkParticles, context.GridPos);
				if (GodotObject.IsInstanceValid(towerDefenseEffectParticlesOnce))
				{
					towerDefenseEffectParticlesOnce.GlobalPosition = context.EffectGlobalPosition;
					TowerDefenseGroundItemBase.characterNode.AddChild(towerDefenseEffectParticlesOnce, forceReadableName: false, Node.InternalMode.Disabled);
				}
			}
		}
		catch (Exception value)
		{
			GD.PushError($"[VaseContent] Failed to create break effect at {context.GridPos}: {value}");
		}
	}

	private static async Task ResolveBreakAsync(BreakOperationContext context)
	{
		_ = 1;
		try
		{
			if (!(await AwaitNextPhysicsFrameAsync(context)) || !IsOperationCurrent(context))
			{
				return;
			}
			TowerDefensePacketConfig towerDefensePacketConfig = (GodotObject.IsInstanceValid(context.FixedPacket) ? context.FixedPacket : (context.AllowRandomContent ? PickRandomContent(context) : null));
			if (GodotObject.IsInstanceValid(towerDefensePacketConfig) && GodotObject.IsInstanceValid(towerDefensePacketConfig.characterConfig))
			{
				if (IsCharacterContent(towerDefensePacketConfig))
				{
					await CreateCharacterContentAsync(context, towerDefensePacketConfig);
				}
				else
				{
					CreatePacketContent(context, towerDefensePacketConfig);
				}
			}
		}
		catch (Exception value)
		{
			GD.PushError($"[VaseContent] Failed to resolve vase at {context.GridPos}: {value}");
		}
		finally
		{
			try
			{
				CleanupUncommittedContent(context);
			}
			catch (Exception value2)
			{
				GD.PushError($"[VaseContent] Failed to clean partial content at {context.GridPos}: {value2}");
			}
			try
			{
				if (IsOperationCurrent(context))
				{
					PublishBreakResult(context);
				}
			}
			catch (Exception value3)
			{
				GD.PushError($"[VaseContent] Failed to publish vase result at {context.GridPos}: {value3}");
			}
			finally
			{
				if (context.OperationId > 0 && GodotObject.IsInstanceValid(context.Owner))
				{
					context.Owner.CompletePendingBattleOperation(context.OperationId);
				}
			}
		}
	}

	private static async Task<bool> AwaitNextPhysicsFrameAsync(BreakOperationContext context)
	{
		if (!GodotObject.IsInstanceValid(context.Tree) || !IsOperationCurrent(context))
		{
			return false;
		}
		await context.Tree.ToSignal(context.Tree, SceneTree.SignalName.PhysicsFrame);
		return GodotObject.IsInstanceValid(context.Tree) && IsOperationCurrent(context);
	}

	private static bool IsOperationCurrent(BreakOperationContext context)
	{
		if (context.OperationId <= 0)
		{
			return GodotObject.IsInstanceValid(context.Tree);
		}
		if (GodotObject.IsInstanceValid(context.Owner))
		{
			return context.Owner.IsPendingBattleOperationCurrent(context.OperationId);
		}
		return false;
	}

	private static TowerDefensePacketConfig PickRandomContent(BreakOperationContext context)
	{
		TowerDefensePacketBankData packetBankData = TowerDefenseManager.GetPacketBankData(context.PacketBank);
		if (!GodotObject.IsInstanceValid(packetBankData))
		{
			return null;
		}
		Godot.Collections.Array packetList = packetBankData.GetPacketList();
		if (packetList == null || packetList.Count == 0)
		{
			return null;
		}
		TowerDefenseCellInstance towerDefenseCellInstance = (context.HadCell ? TowerDefenseManager.GetMapCell(context.GridPos) : null);
		string text = null;
		int num = 0;
		foreach (Variant item in packetList)
		{
			string text2 = item.AsString();
			TowerDefensePacketConfig packetConfigReadOnly = TowerDefenseManager.GetPacketConfigReadOnly(text2);
			if (GodotObject.IsInstanceValid(packetConfigReadOnly) && GodotObject.IsInstanceValid(packetConfigReadOnly.characterConfig) && (context.HadCell || packetConfigReadOnly.characterConfig is TowerDefensePlantConfig) && (!context.HadCell || !IsCharacterContent(packetConfigReadOnly) || (GodotObject.IsInstanceValid(towerDefenseCellInstance) && towerDefenseCellInstance.CanPacketPlant(packetConfigReadOnly))))
			{
				num++;
				if (GD.RandRange(0, num - 1) == 0)
				{
					text = text2;
				}
			}
		}
		if (text == null)
		{
			return null;
		}
		TowerDefensePacketConfig packetConfigReadOnly2 = TowerDefenseManager.GetPacketConfigReadOnly(text);
		if (!GodotObject.IsInstanceValid(packetConfigReadOnly2))
		{
			return null;
		}
		return packetConfigReadOnly2.CreateRuntimeStateCopy();
	}

	private static bool IsCharacterContent(TowerDefensePacketConfig packetConfig)
	{
		TowerDefenseCharacterConfig characterConfig = packetConfig.characterConfig;
		if (characterConfig is TowerDefenseZombieConfig || characterConfig is TowerDefenseGravestoneConfig || characterConfig is TowerDefenseCraterConfig)
		{
			return true;
		}
		return false;
	}

	private static async Task CreateCharacterContentAsync(BreakOperationContext context, TowerDefensePacketConfig packetConfig)
	{
		if (!IsOperationCurrent(context))
		{
			return;
		}
		TowerDefenseCharacter character = (context.CreatedZombie = packetConfig.Plant(context.GridPos));
		bool flag = !GodotObject.IsInstanceValid(character);
		if (!flag)
		{
			flag = !(await AwaitNodeReadyAsync(context, character));
		}
		if (!flag && GodotObject.IsInstanceValid(character.instance) && IsOperationCurrent(context))
		{
			character.instance.wakeUp = true;
			character.groundHeight = context.GroundHeight;
			if (context.Hypnoses)
			{
				character.Hypnoses();
			}
			int num = -1;
			if (Global.IsMultiplayerMode && MultiPlayerManager.IsHost && GodotObject.IsInstanceValid(context.Owner))
			{
				num = context.Owner.GetNextSyncId();
				context.Owner.RegisterSyncCharacter(num, character);
			}
			if (GodotObject.IsInstanceValid(character) && !character.isDestroy)
			{
				context.Result.ContentType = "zombie";
				context.Result.ContentName = packetConfig.saveKey;
				context.Result.ContentConfig = packetConfig;
				context.Result.ZombieSyncId = num;
				context.CreatedZombie = null;
			}
		}
	}

	private static async Task<bool> AwaitNodeReadyAsync(BreakOperationContext context, Node node)
	{
		int frameLimit = Math.Clamp(context.ReadyWaitFrameLimit, 1, 30);
		for (int frame = 0; frame < frameLimit; frame++)
		{
			if (!GodotObject.IsInstanceValid(node) || node.IsQueuedForDeletion() || !IsOperationCurrent(context))
			{
				return false;
			}
			if (node.IsNodeReady())
			{
				return true;
			}
			if (!GodotObject.IsInstanceValid(context.Tree))
			{
				return false;
			}
			await context.Tree.ToSignal(context.Tree, SceneTree.SignalName.ProcessFrame);
		}
		return GodotObject.IsInstanceValid(node) && !node.IsQueuedForDeletion() && node.IsNodeReady() && IsOperationCurrent(context);
	}

	private static void CreatePacketContent(BreakOperationContext context, TowerDefensePacketConfig packetConfig)
	{
		if (!IsOperationCurrent(context) || !GodotObject.IsInstanceValid(TowerDefenseGroundItemBase.characterNode))
		{
			return;
		}
		float num = Mathf.Min(Mathf.Abs(context.PacketHorizontalSpeedRange.X), Mathf.Abs(context.PacketHorizontalSpeedRange.Y));
		float num2 = Mathf.Max(Mathf.Abs(context.PacketHorizontalSpeedRange.X), Mathf.Abs(context.PacketHorizontalSpeedRange.Y));
		float num3 = (float)GD.RandRange(num, num2);
		if (GD.Randf() < 0.5f)
		{
			num3 = 0f - num3;
		}
		float num4 = context.PacketVerticalSpeed;
		TowerDefenseInGamePacketShow towerDefenseInGamePacketShow = (context.CreatedPacket = TowerDefenseManager.CreatePacketShow());
		if (GodotObject.IsInstanceValid(towerDefenseInGamePacketShow))
		{
			towerDefenseInGamePacketShow.ZIndex = context.ZIndex;
			towerDefenseInGamePacketShow.GlobalPosition = context.VaseGlobalPosition + new Vector2(0f, 0f - (float)context.GroundHeight);
			TowerDefenseGroundItemBase.characterNode.AddChild(towerDefenseInGamePacketShow, forceReadableName: false, Node.InternalMode.Disabled);
			towerDefenseInGamePacketShow.Init(packetConfig);
			towerDefenseInGamePacketShow.onlyDraw = false;
			towerDefenseInGamePacketShow.showCost = false;
			towerDefenseInGamePacketShow.useCost = false;
			towerDefenseInGamePacketShow.plantOnce = true;
			towerDefenseInGamePacketShow.canPressPutBack = false;
			towerDefenseInGamePacketShow.StartInit();
			towerDefenseInGamePacketShow.alive = true;
			towerDefenseInGamePacketShow.aliveTime = Mathf.Max(0f, context.PacketAliveTime);
			towerDefenseInGamePacketShow.height = 0.0;
			if (!GodotObject.IsInstanceValid(towerDefenseInGamePacketShow.moveComponent))
			{
				throw new InvalidOperationException("Vase packet is missing MoveComponent.");
			}
			towerDefenseInGamePacketShow.moveComponent.gravity = context.PacketGravity;
			towerDefenseInGamePacketShow.moveComponent.velocity = new Vector2(num3, num4);
			PacketPickControl packetPickControl = TowerDefenseManager.Instance?.GetPacketPickControl();
			if (GodotObject.IsInstanceValid(packetPickControl))
			{
				towerDefenseInGamePacketShow.OnPressed += packetPickControl.PickPacket;
			}
			towerDefenseInGamePacketShow.AddToGroup("VasePacketShow");
			int num5 = -1;
			if (Global.IsMultiplayerMode && MultiPlayerManager.IsHost && GodotObject.IsInstanceValid(context.Owner))
			{
				num5 = (context.CreatedPacketSyncId = context.Owner.GetNextPacketSyncId());
				towerDefenseInGamePacketShow.SetMeta("packet_sync_id", num5);
				context.Owner.RegisterSyncPacket(num5, towerDefenseInGamePacketShow);
			}
			Dictionary dictionary = new Dictionary
			{
				["packet_name"] = packetConfig.saveKey,
				["pos_x"] = towerDefenseInGamePacketShow.GlobalPosition.X,
				["pos_y"] = towerDefenseInGamePacketShow.GlobalPosition.Y,
				["z_index"] = towerDefenseInGamePacketShow.ZIndex,
				["velocity_x"] = num3,
				["velocity_y"] = num4,
				["gravity"] = context.PacketGravity,
				["alive_time"] = towerDefenseInGamePacketShow.aliveTime,
				["sync_id"] = num5
			};
			TowerDefensePacketRuntimeState.Write(packetConfig, dictionary, "packet_override", "can_change_cost", "change_cost_list");
			context.Result.ContentType = "plant";
			context.Result.ContentName = packetConfig.saveKey;
			context.Result.ContentConfig = packetConfig;
			context.Result.PacketShowData = dictionary;
			context.CreatedPacket = null;
			context.CreatedPacketSyncId = -1;
		}
	}

	private static void CleanupUncommittedContent(BreakOperationContext context)
	{
		if (GodotObject.IsInstanceValid(context.CreatedZombie))
		{
			if (context.CreatedZombie.syncId >= 0 && GodotObject.IsInstanceValid(context.Owner))
			{
				context.Owner.DetachSyncCharacter(context.CreatedZombie);
			}
			if (GodotObject.IsInstanceValid(context.Owner))
			{
				context.Owner.CleanupCharacterCell(context.CreatedZombie);
			}
			if (GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
			{
				TowerDefenseManager.Instance.CharacterUnregister(context.CreatedZombie);
			}
			context.CreatedZombie.RemoveFromGroup("Character");
			if (!context.CreatedZombie.IsQueuedForDeletion())
			{
				context.CreatedZombie.QueueFree();
			}
			context.CreatedZombie = null;
		}
		if (GodotObject.IsInstanceValid(context.CreatedPacket))
		{
			if (context.CreatedPacketSyncId >= 0 && GodotObject.IsInstanceValid(context.Owner))
			{
				context.Owner.UnregisterSyncPacket(context.CreatedPacketSyncId);
			}
			context.CreatedPacket.RemoveMeta("packet_sync_id");
			context.CreatedPacket.RemoveFromGroup("VasePacketShow");
			context.CreatedPacket.ClearEventHandlers();
			if (!context.CreatedPacket.IsQueuedForDeletion())
			{
				context.CreatedPacket.QueueFree();
			}
			context.CreatedPacket = null;
			context.CreatedPacketSyncId = -1;
		}
	}

	private static void PublishBreakResult(BreakOperationContext context)
	{
		if (Global.IsMultiplayerMode && MultiPlayerManager.IsHost && GodotObject.IsInstanceValid(MultiPlayerManager.Instance))
		{
			Dictionary dictionary = new Dictionary
			{
				["break_id"] = context.OperationId,
				["grid_x"] = context.GridPos.X,
				["grid_y"] = context.GridPos.Y,
				["content_type"] = context.Result.ContentType,
				["content_name"] = context.Result.ContentName,
				["ground_height"] = context.GroundHeight,
				["hypnoses"] = context.Hypnoses
			};
			if (GodotObject.IsInstanceValid(context.Result.ContentConfig))
			{
				TowerDefensePacketRuntimeState.Write(context.Result.ContentConfig, dictionary, "content_override", "content_can_change_cost", "content_change_cost_list");
			}
			if (context.Result.ContentType == "zombie" && context.Result.ZombieSyncId >= 0)
			{
				dictionary["sync_id"] = context.Result.ZombieSyncId;
			}
			if (context.Result.ContentType == "plant" && context.Result.PacketShowData != null)
			{
				dictionary["packet_show"] = context.Result.PacketShowData;
			}
			MultiPlayerManager.Instance.SendVaseBreakResult(dictionary);
		}
	}

	public override Dictionary ExportComponentSave()
	{
		Dictionary dictionary = new Dictionary();
		Variant variant = "contentMode";
		Dictionary dictionary2 = dictionary;
		Variant key = variant;
		dictionary2[key] = _contentMode switch
		{
			ContentMode.Empty => "empty", 
			ContentMode.Fixed => "fixed", 
			_ => "random", 
		};
		Dictionary dictionary3 = dictionary;
		if (_contentMode != ContentMode.Fixed || !GodotObject.IsInstanceValid(parent))
		{
			return dictionary3;
		}
		TowerDefensePacketConfig packetConfig = parent.packetConfig;
		dictionary3["packetName"] = (GodotObject.IsInstanceValid(packetConfig) ? packetConfig.saveKey : (parent.packetName ?? ""));
		if (GodotObject.IsInstanceValid(packetConfig))
		{
			dictionary3["packetOverrideHypnoses"] = packetConfig.overrideHypnoses;
			TowerDefensePacketRuntimeState.Write(packetConfig, dictionary3, "packetOverride", "packetCanChangeCost", "packetChangeCostList");
		}
		return dictionary3;
	}

	public override void ImportComponentSave(Dictionary data, TowerDefenseLevelSaveConfigCSharp owner)
	{
		if (!GodotObject.IsInstanceValid(parent))
		{
			return;
		}
		if (data == null || !data.ContainsKey("contentMode"))
		{
			_contentMode = ((GodotObject.IsInstanceValid(parent.packetConfig) || !string.IsNullOrEmpty(parent.packetName)) ? ContentMode.Fixed : ContentMode.Random);
			return;
		}
		switch (data.GetValueOrDefault("contentMode", "random").AsString().ToLowerInvariant())
		{
		case "empty":
			_contentMode = ContentMode.Empty;
			parent.SetContentConfig(null);
			_contentMode = ContentMode.Empty;
			break;
		case "fixed":
		{
			string text = data.GetValueOrDefault("packetName", "").AsString();
			TowerDefensePacketRuntimeState.TryCreate(text, data, "packetOverride", "packetCanChangeCost", "packetChangeCostList", out var packetConfig);
			if (GodotObject.IsInstanceValid(packetConfig))
			{
				packetConfig.overrideHypnoses = data["packetOverrideHypnoses"].AsBool();
				parent.SetContentConfig(packetConfig);
			}
			else
			{
				parent.packetName = text;
			}
			_contentMode = ContentMode.Fixed;
			break;
		}
		case "random":
			_contentMode = ContentMode.Random;
			parent.SetContentConfig(null);
			_contentMode = ContentMode.Random;
			break;
		default:
			_contentMode = ((GodotObject.IsInstanceValid(parent.packetConfig) || !string.IsNullOrEmpty(parent.packetName)) ? ContentMode.Fixed : ContentMode.Random);
			break;
		}
	}
}
