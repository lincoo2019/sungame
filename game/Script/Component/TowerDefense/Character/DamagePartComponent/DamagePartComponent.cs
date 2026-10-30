using System;
using System.Runtime.CompilerServices;
using Godot;
using Godot.Collections;

public sealed class DamagePartComponent : CharacterComponentRuntime
{
	private sealed class FilterCacheEntry
	{
		public string Source;

		public Array<StringName> Filters;
	}

	public Vector2 _syncPartVelocity = Vector2.Zero;

	public bool _syncDeserializing;

	private StringName _pendingSyncPartName;

	private Vector2 _pendingSyncPosition;

	private Vector2 _pendingSyncVelocity;

	private long _pendingSyncSequence;

	private long _lastAppliedSyncSequence;

	private bool _hasPendingSyncPart;

	private static readonly ConditionalWeakTable<GodotObject, FilterCacheEntry> _filterCache = new ConditionalWeakTable<GodotObject, FilterCacheEntry>();

	private Dictionary _syncPayload => GetReusableSyncPayload();

	private DamagePartComponentDefinition Definition => base.ComponentDefinition as DamagePartComponentDefinition;

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		_syncPartVelocity = Vector2.Zero;
		_syncDeserializing = false;
		ClearReusableSyncPayload();
		_pendingSyncPartName = null;
		_pendingSyncPosition = Vector2.Zero;
		_pendingSyncVelocity = Vector2.Zero;
		_pendingSyncSequence = 0L;
		_lastAppliedSyncSequence = 0L;
		_hasPendingSyncPart = false;
	}

	protected override void OnActivated()
	{
		TryApplyPendingSynchronizedDamagePart();
	}

	public void DamagePartCreate(StringName damagePointName, Node2D node, Vector2 velocity = default(Vector2), bool keepSlotScale = true, Vector2 offset = default(Vector2), bool fromSync = false, Vector2? synchronizedPosition = null, long synchronizedSequence = 0L)
	{
		if (fromSync && synchronizedPosition.HasValue && !GodotObject.IsInstanceValid(node))
		{
			ApplySynchronizedDamagePart(damagePointName, synchronizedPosition.Value, velocity, synchronizedSequence);
		}
		else
		{
			SpawnDamagePart(damagePointName, node, velocity, keepSlotScale, offset, fromSync, synchronizedPosition, synchronizedSequence);
		}
	}

	public void SpawnDamagePart(StringName damagePointName, Node2D node, Vector2 velocity = default(Vector2), bool keepSlotScale = true, Vector2 offset = default(Vector2), bool fromSync = false, Vector2? synchronizedPosition = null, long synchronizedSequence = 0L)
	{
		SpawnDamagePartCore(damagePointName, node, velocity, keepSlotScale, offset, fromSync, synchronizedPosition, synchronizedSequence);
	}

	public bool ApplySynchronizedDamagePart(StringName damagePointName, Vector2 worldPosition, Vector2 velocity, long sequence)
	{
		if (damagePointName == null || damagePointName.IsEmpty || (sequence > 0 && sequence <= _lastAppliedSyncSequence))
		{
			return false;
		}
		if (_hasPendingSyncPart && _pendingSyncSequence > 0 && sequence > 0 && sequence < _pendingSyncSequence)
		{
			return false;
		}
		_pendingSyncPartName = damagePointName;
		_pendingSyncPosition = worldPosition;
		_pendingSyncVelocity = velocity;
		_pendingSyncSequence = sequence;
		_hasPendingSyncPart = true;
		return TryApplyPendingSynchronizedDamagePart();
	}

	private bool TryApplyPendingSynchronizedDamagePart()
	{
		if (!_hasPendingSyncPart || !TryGetRuntime(out var _))
		{
			return false;
		}
		StringName pendingSyncPartName = _pendingSyncPartName;
		Vector2 pendingSyncPosition = _pendingSyncPosition;
		Vector2 pendingSyncVelocity = _pendingSyncVelocity;
		long pendingSyncSequence = _pendingSyncSequence;
		if (!SpawnDamagePartCore(pendingSyncPartName, null, pendingSyncVelocity, keepSlotScale: true, Vector2.Zero, fromSync: true, pendingSyncPosition, pendingSyncSequence))
		{
			return false;
		}
		if (pendingSyncSequence > _lastAppliedSyncSequence)
		{
			_lastAppliedSyncSequence = pendingSyncSequence;
		}
		_pendingSyncPartName = null;
		_pendingSyncPosition = Vector2.Zero;
		_pendingSyncVelocity = Vector2.Zero;
		_pendingSyncSequence = 0L;
		_hasPendingSyncPart = false;
		return true;
	}

	private bool SpawnDamagePartCore(StringName damagePointName, Node2D node, Vector2 velocity, bool keepSlotScale, Vector2 offset, bool fromSync, Vector2? synchronizedPosition, long synchronizedSequence)
	{
		if (!TryGetRuntime(out var owner) || damagePointName == null || damagePointName.IsEmpty)
		{
			return false;
		}
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost && !fromSync)
		{
			return false;
		}
		bool flag = fromSync && synchronizedPosition.HasValue;
		Vector2 syncPartVelocity = _syncPartVelocity;
		bool syncDeserializing = _syncDeserializing;
		if (!fromSync)
		{
			velocity = ResolveVelocity(velocity);
			velocity = ConsumeSyncVelocity(velocity);
		}
		if (owner.damagePart.TryGetValue(damagePointName, out var value) && value.AsGodotObject() is ArmorSlotConfig)
		{
			bool num = TryArmorDamagePart(damagePointName, node, velocity, fromSync, synchronizedPosition, flag, synchronizedSequence);
			if (!num && !fromSync)
			{
				RestorePreparedSyncVelocity(syncPartVelocity, syncDeserializing);
			}
			return num;
		}
		if (!owner.damagePartList.Contains(damagePointName) || !owner.damagePartSlot.ContainsKey(damagePointName))
		{
			return false;
		}
		if (!TryGetDamagePartSlot(damagePointName, out var slot))
		{
			return false;
		}
		Vector2 vector = (flag ? synchronizedPosition.Value : owner.GetLogicalGlobalPosition(slot));
		bool num2 = TryCreateDamagePart(damagePointName, node, velocity, keepSlotScale, offset, slot, vector);
		if (num2)
		{
			SendDamagePartIfHost(damagePointName, vector, velocity, fromSync);
			return num2;
		}
		if (!fromSync)
		{
			RestorePreparedSyncVelocity(syncPartVelocity, syncDeserializing);
		}
		return num2;
	}

	public bool TryArmorDamagePart(StringName damagePointName, Node2D node, Vector2 velocity, bool fromSync)
	{
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost && !fromSync)
		{
			return true;
		}
		Vector2 syncPartVelocity = _syncPartVelocity;
		bool syncDeserializing = _syncDeserializing;
		if (!fromSync)
		{
			velocity = ResolveVelocity(velocity);
			velocity = ConsumeSyncVelocity(velocity);
		}
		bool num = TryArmorDamagePart(damagePointName, node, velocity, fromSync, null, hasAuthoritativePosition: false, 0L);
		if (!num && !fromSync)
		{
			RestorePreparedSyncVelocity(syncPartVelocity, syncDeserializing);
		}
		return num;
	}

	private void RestorePreparedSyncVelocity(Vector2 velocity, bool deserializing)
	{
		_syncPartVelocity = velocity;
		_syncDeserializing = deserializing;
	}

	private bool TryArmorDamagePart(StringName damagePointName, Node2D node, Vector2 velocity, bool fromSync, Vector2? synchronizedPosition, bool hasAuthoritativePosition, long synchronizedSequence)
	{
		if (!TryGetRuntime(out var owner) || !owner.damagePart.TryGetValue(damagePointName, out var value) || !(value.AsGodotObject() is ArmorSlotConfig))
		{
			return false;
		}
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost && !fromSync)
		{
			return true;
		}
		TowerDefenseArmorInstance armorInstance = GetArmorInstance(damagePointName);
		if (!GodotObject.IsInstanceValid(armorInstance) || (armorInstance.isRemove && !fromSync))
		{
			return false;
		}
		bool flag = false;
		Node2D node2D = node;
		if (!GodotObject.IsInstanceValid(node2D))
		{
			node2D = armorInstance.sprite;
		}
		if (!GodotObject.IsInstanceValid(node2D))
		{
			node2D = CreateArmorStagePart(armorInstance);
			flag = true;
		}
		if (!GodotObject.IsInstanceValid(node2D))
		{
			return false;
		}
		AdobeAnimateSlot slot;
		bool flag2 = TryGetDamagePartSlot(damagePointName, out slot);
		Vector2 vector = (hasAuthoritativePosition ? synchronizedPosition.Value : (flag2 ? owner.GetLogicalGlobalPosition(slot) : GetArmorDropPosition(damagePointName, node2D)));
		Transform2D transform2D = ResolveSourceGlobalTransform(owner, node2D, flag2 ? slot : null);
		Node2D node2D2 = (flag ? node2D : (node2D.Duplicate() as Node2D));
		if (!GodotObject.IsInstanceValid(node2D2) || !TryPopDamagePart(out var damagePart))
		{
			if (GodotObject.IsInstanceValid(node2D2))
			{
				node2D2.QueueFree();
			}
			return false;
		}
		Transform2D value2 = BuildDropRootTransform(owner, vector);
		node2D2.Transform = ((node2D.IsInsideTree() || flag2) ? (value2.AffineInverse() * transform2D) : node2D.Transform);
		if (!ApplyDamagePartTransform(damagePart, node2D2, vector, Definition?.armorHeightOffset ?? 24f, velocity, value2))
		{
			damagePart.Over();
			return false;
		}
		armorInstance.damagePartDropped = true;
		SendDamagePartIfHost(damagePointName, vector, velocity, fromSync);
		return true;
	}

	public void CreateDamagePart(StringName damagePointName, Node2D node, Vector2 velocity, bool keepSlotScale, Vector2 offset, AdobeAnimateSlot slot)
	{
		TryCreateDamagePart(damagePointName, node, velocity, keepSlotScale, offset, slot, GodotObject.IsInstanceValid(slot) ? base.Owner.GetLogicalGlobalPosition(slot) : Vector2.Zero);
	}

	private bool TryCreateDamagePart(StringName damagePointName, Node2D node, Vector2 velocity, bool keepSlotScale, Vector2 offset, AdobeAnimateSlot slot, Vector2 worldPosition)
	{
		if (!TryGetRuntime(out var owner) || !GodotObject.IsInstanceValid(slot))
		{
			return false;
		}
		bool flag = GodotObject.IsInstanceValid(node);
		Transform2D transform2D = (flag ? owner.GetLogicalGlobalTransform(node) : Transform2D.Identity);
		Node2D node2D = (flag ? node : CreateConfiguredPartNode(damagePointName, slot));
		if (!GodotObject.IsInstanceValid(node2D) || !TryPopDamagePart(out var damagePart))
		{
			if (!flag && GodotObject.IsInstanceValid(node2D))
			{
				node2D.QueueFree();
			}
			return false;
		}
		Variant value;
		bool flag2 = owner.damagePart.TryGetValue(damagePointName, out value) && value.AsGodotObject() is ArmorSlotConfig;
		if (GodotObject.IsInstanceValid(node2D.GetParent()))
		{
			node2D.GetParent().RemoveChild(node2D);
		}
		Transform2D value2 = BuildDropRootTransform(owner, worldPosition);
		if (flag)
		{
			node2D.Transform = value2.AffineInverse() * transform2D;
			if (!flag2 && offset != Vector2.Zero)
			{
				node2D.Position = offset;
			}
		}
		else if (flag2 && node2D is AdobeAnimatePart)
		{
			Transform2D transform2D2 = owner.GetLogicalGlobalTransform(slot) * node2D.Transform;
			node2D.Transform = value2.AffineInverse() * transform2D2;
		}
		if (!ApplyDamagePartTransform(damagePart, node2D, worldPosition, Definition?.normalHeightOffset ?? 0f, velocity, value2))
		{
			damagePart.Over();
			return false;
		}
		TowerDefenseArmorInstance armorInstance = GetArmorInstance(damagePointName);
		if (GodotObject.IsInstanceValid(armorInstance))
		{
			armorInstance.damagePartDropped = true;
		}
		return true;
	}

	private Node2D CreateConfiguredPartNode(StringName damagePointName, AdobeAnimateSlot slot)
	{
		if (!TryGetRuntime(out var owner) || !owner.damagePart.TryGetValue(damagePointName, out var value))
		{
			return null;
		}
		GodotObject godotObject = value.AsGodotObject();
		if (godotObject is CharacterDamagePointConfig characterDamagePointConfig)
		{
			return slot.CreatePart(GetFilterArray(characterDamagePointConfig, characterDamagePointConfig.animeFliterClose));
		}
		if (godotObject is ArmorSlotConfig config)
		{
			return CreateArmorDamagePartNode(damagePointName, config, slot);
		}
		return null;
	}

	private bool ApplyDamagePartTransform(DamagePartDrop damagePart, Node2D partNode, Vector2 worldPosition, float heightOffset, Vector2 velocity, Transform2D? dropRootTransform = null)
	{
		if (!GodotObject.IsInstanceValid(damagePart) || !GodotObject.IsInstanceValid(partNode) || !velocity.IsFinite() || !TryGetRuntime(out var owner))
		{
			return false;
		}
		AdobeAnimateManagedSprite2D.RestoreDetachedNode(partNode);
		double height = owner.GetGroundHeight(worldPosition.Y) - owner.groundHeight + (double)GetShadowOffsetY() + (double)heightOffset;
		damagePart.Init(partNode, height, velocity, preserveSpritePosition: true);
		if (damagePart.sprite != partNode)
		{
			return false;
		}
		if (dropRootTransform.HasValue)
		{
			damagePart.GlobalTransform = dropRootTransform.Value;
		}
		else
		{
			DamagePartComponentDefinition definition = Definition;
			if (definition == null || definition.inheritCharacterScale)
			{
				damagePart.Scale *= GetCharacterVisualScale(owner);
			}
		}
		if (!dropRootTransform.HasValue)
		{
			damagePart.GlobalPosition = worldPosition;
		}
		damagePart.InitializeRenderBucket(owner.gridPos, worldPosition);
		damagePart.Visible = true;
		return true;
	}

	private static bool TryPopDamagePart(out DamagePartDrop damagePart)
	{
		damagePart = null;
		Node2D characterNode = TowerDefenseManager.GetCharacterNode();
		if (!GodotObject.IsInstanceValid(characterNode) || !characterNode.IsInsideTree())
		{
			return false;
		}
		damagePart = ObjectManager.PoolPop(ObjectManagerConfig.OBJECT.damagePart, characterNode) as DamagePartDrop;
		if (GodotObject.IsInstanceValid(damagePart))
		{
			return damagePart.IsInsideTree();
		}
		return false;
	}

	private static Transform2D BuildDropRootTransform(TowerDefenseCharacter owner, Vector2 worldPosition)
	{
		Transform2D result = (GodotObject.IsInstanceValid(owner?.sprite) ? owner.GetLogicalGlobalTransform(owner.sprite) : Transform2D.Identity);
		result.Origin = worldPosition;
		return result;
	}

	private static Transform2D ResolveSourceGlobalTransform(TowerDefenseCharacter owner, Node2D source, AdobeAnimateSlot slot)
	{
		if (!GodotObject.IsInstanceValid(source))
		{
			return Transform2D.Identity;
		}
		if (source.IsInsideTree())
		{
			return owner.GetLogicalGlobalTransform(source);
		}
		if (!GodotObject.IsInstanceValid(slot))
		{
			return source.Transform;
		}
		return owner.GetLogicalGlobalTransform(slot) * source.Transform;
	}

	private static Vector2 GetCharacterVisualScale(TowerDefenseCharacter owner)
	{
		Vector2 scale = owner.Scale;
		if (GodotObject.IsInstanceValid(owner.transformPoint))
		{
			scale *= owner.transformPoint.Scale;
		}
		if (GodotObject.IsInstanceValid(owner.sprite))
		{
			scale *= owner.sprite.Scale;
		}
		return scale;
	}

	private float GetShadowOffsetY()
	{
		TowerDefenseCharacter owner = base.Owner;
		if (!GodotObject.IsInstanceValid(owner?.shadowSprite))
		{
			return 0f;
		}
		return owner.shadowSprite.Position.Y;
	}

	private TowerDefenseArmorInstance GetArmorInstance(StringName damagePointName)
	{
		if (!TryGetRuntime(out var owner))
		{
			return null;
		}
		TowerDefenseArmorInstance armorFromName = owner.GetArmorFromName(damagePointName.ToString());
		if (!GodotObject.IsInstanceValid(armorFromName))
		{
			return null;
		}
		return armorFromName;
	}

	private bool TryGetDamagePartSlot(StringName damagePointName, out AdobeAnimateSlot slot)
	{
		slot = null;
		if (!TryGetRuntime(out var owner) || !owner.damagePartSlot.TryGetValue(damagePointName, out var value))
		{
			return false;
		}
		slot = owner.GetNodeOrNull<AdobeAnimateSlot>(value.AsString());
		if (!GodotObject.IsInstanceValid(slot))
		{
			return false;
		}
		slot.Update();
		return true;
	}

	private Vector2 GetArmorDropPosition(StringName damagePointName, Node2D armorPart)
	{
		TowerDefenseCharacter owner = base.Owner;
		if (GodotObject.IsInstanceValid(armorPart) && armorPart.IsInsideTree())
		{
			return owner.GetLogicalGlobalPosition(armorPart);
		}
		if (TryGetDamagePartSlot(damagePointName, out var slot))
		{
			return owner.GetLogicalGlobalPosition(slot);
		}
		if (!GodotObject.IsInstanceValid(owner))
		{
			return Vector2.Zero;
		}
		return owner.GetLogicalGlobalPosition();
	}

	private static string GetArmorStageTexturePath(TowerDefenseArmorInstance armor)
	{
		if (!GodotObject.IsInstanceValid(armor) || armor.typeData?.stageAnimeTexturePaths == null || armor.typeData.stageAnimeTexturePaths.Count == 0)
		{
			return string.Empty;
		}
		int index = Mathf.Clamp(armor.stageIndex, 0, armor.typeData.stageAnimeTexturePaths.Count - 1);
		return armor.typeData.stageAnimeTexturePaths[index];
	}

	private static AdobeAnimatePart CreateArmorStagePart(TowerDefenseArmorInstance armor)
	{
		string armorStageTexturePath = GetArmorStageTexturePath(armor);
		if (string.IsNullOrWhiteSpace(armorStageTexturePath) || armor.slotConfig == null)
		{
			return null;
		}
		AdobeAnimatePart adobeAnimatePart = AdobeAnimatePart.CreateAtlasTexturePart(armorStageTexturePath);
		adobeAnimatePart.Transform = BuildArmorStageTransform(armor.slotConfig);
		return adobeAnimatePart;
	}

	private static Transform2D BuildArmorStageTransform(ArmorSlotConfig config)
	{
		if (config != null)
		{
			return new Transform2D((float)config.rotation, config.scale, 0f, config.offset);
		}
		return Transform2D.Identity;
	}

	private Node2D CreateArmorDamagePartNode(StringName name, ArmorSlotConfig config, AdobeAnimateSlot slot)
	{
		TowerDefenseArmorInstance armorInstance = GetArmorInstance(name);
		if (GodotObject.IsInstanceValid(armorInstance?.sprite))
		{
			return armorInstance.sprite.Duplicate() as Node2D;
		}
		AdobeAnimatePart adobeAnimatePart = CreateArmorStagePart(armorInstance);
		if (!GodotObject.IsInstanceValid(adobeAnimatePart))
		{
			return slot.CreatePart(GetFilterArray(config, config.destroyFliter));
		}
		return adobeAnimatePart;
	}

	private static Node2D DuplicateOrReusePartNode(Node2D node)
	{
		if (!GodotObject.IsInstanceValid(node))
		{
			return null;
		}
		if (!GodotObject.IsInstanceValid(node.GetParent()))
		{
			return node;
		}
		return node.Duplicate() as Node2D;
	}

	private static Array<StringName> GetFilterArray(GodotObject config, string value)
	{
		if (value == null)
		{
			value = string.Empty;
		}
		if (!GodotObject.IsInstanceValid(config))
		{
			return ParseFilterArray(value);
		}
		FilterCacheEntry value2 = _filterCache.GetValue(config, (GodotObject _) => new FilterCacheEntry());
		if (value2.Filters != null && string.Equals(value2.Source, value, StringComparison.Ordinal))
		{
			return value2.Filters;
		}
		value2.Source = value;
		value2.Filters = ParseFilterArray(value);
		return value2.Filters;
	}

	private static Array<StringName> ParseFilterArray(string value)
	{
		Array<StringName> array = new Array<StringName>();
		if (value.Length > 0)
		{
			string[] array2 = value.Split('&', StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i < array2.Length; i++)
			{
				array.Add(new StringName(array2[i]));
			}
		}
		return array;
	}

	public TowerDefenseMagnet MagnetCreate(TowerDefenseArmorInstance armor, Node2D node)
	{
		if (!TryGetRuntime(out var owner) || !GodotObject.IsInstanceValid(armor) || armor.slotConfig == null || !TryGetDamagePartSlot(new StringName(armor.slotConfig.armorName), out var slot))
		{
			return null;
		}
		TowerDefenseMagnet towerDefenseMagnet = TowerDefenseMagnet.Create(armor);
		if (!GodotObject.IsInstanceValid(towerDefenseMagnet))
		{
			return null;
		}
		Node2D characterNode = TowerDefenseManager.GetCharacterNode();
		if (!GodotObject.IsInstanceValid(characterNode))
		{
			towerDefenseMagnet.Free();
			return null;
		}
		characterNode.AddChild(towerDefenseMagnet, forceReadableName: false, Node.InternalMode.Disabled);
		Node2D node2D = node;
		if (!GodotObject.IsInstanceValid(node2D))
		{
			node2D = CreateConfiguredPartNode(new StringName(armor.slotConfig.armorName), slot);
		}
		else if (GodotObject.IsInstanceValid(node2D.GetParent()))
		{
			Vector2 globalScale = node2D.GlobalScale;
			node2D.GetParent().RemoveChild(node2D);
			node2D.Scale = globalScale;
		}
		if (!GodotObject.IsInstanceValid(node2D))
		{
			towerDefenseMagnet.QueueFree();
			return null;
		}
		AdobeAnimateManagedSprite2D.RestoreDetachedNode(node2D);
		towerDefenseMagnet.Init(node2D);
		towerDefenseMagnet.GlobalPosition = owner.GetLogicalGlobalPosition(slot);
		node2D.Rotation = slot.Rotation;
		towerDefenseMagnet.gridPos = owner.gridPos;
		return towerDefenseMagnet;
	}

	public TowerDefenseMagnet ArmorDraw(TowerDefenseArmorInstance armor)
	{
		if (!TryGetRuntime(out var owner) || !GodotObject.IsInstanceValid(owner.instance))
		{
			return null;
		}
		return owner.instance.ArmorDraw(armor);
	}

	private Vector2 ResolveVelocity(Vector2 velocity)
	{
		if (velocity != Vector2.Zero)
		{
			return velocity;
		}
		Vector2 vector = Definition?.randomVelocityXRange ?? DamagePartComponentDefinition.DefaultRandomVelocityXRange;
		float num = Mathf.Min(vector.X, vector.Y);
		float num2 = Mathf.Max(vector.X, vector.Y);
		return new Vector2(y: Definition?.defaultVelocityY ?? (-300f), x: (float)GD.RandRange(num, num2));
	}

	private Vector2 ConsumeSyncVelocity(Vector2 velocity)
	{
		if (_syncDeserializing && _syncPartVelocity != Vector2.Zero)
		{
			velocity = _syncPartVelocity;
			_syncPartVelocity = Vector2.Zero;
			_syncDeserializing = false;
		}
		else
		{
			_syncPartVelocity = velocity;
		}
		return velocity;
	}

	private void SendDamagePartIfHost(StringName name, Vector2 position, Vector2 velocity, bool fromSync)
	{
		if (!(!Global.IsMultiplayerMode || !MultiPlayerManager.IsHost || fromSync) && GodotObject.IsInstanceValid(MultiPlayerManager.Instance) && TryGetRuntime(out var owner))
		{
			MultiPlayerManager.Instance.SendDamagePart(owner.syncId, name.ToString(), position.X, position.Y, velocity.X, velocity.Y, 0L);
		}
	}

	private bool TryGetRuntime(out TowerDefenseCharacter owner)
	{
		owner = base.Owner;
		if (base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active)
		{
			return GodotObject.IsInstanceValid(owner);
		}
		return false;
	}

	public override Dictionary SyncSerialize()
	{
		_syncPayload.Clear();
		if (_syncPartVelocity != Vector2.Zero)
		{
			_syncPayload["part_velocity_x"] = _syncPartVelocity.X;
			_syncPayload["part_velocity_y"] = _syncPartVelocity.Y;
		}
		return _syncPayload;
	}

	public override void SyncDeserialize(Dictionary data)
	{
		if (data != null && data.ContainsKey("part_velocity_x"))
		{
			_syncPartVelocity = new Vector2(data.GetValueOrDefault("part_velocity_x", 0.0).AsSingle(), data.GetValueOrDefault("part_velocity_y", 0.0).AsSingle());
			_syncDeserializing = true;
		}
	}
}
