using System;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public sealed class GravebusterComponent : CharacterComponentRuntime
{
	public delegate void OverEventHandler(TowerDefenseGravestone graveStone);

	public AdobeAnimateSprite sprite;

	public string landAnimeClips = "Land";

	public float landAnimeTimeScale = 1f;

	public string gravebusterAnimeClips = "Idle";

	public float gravebusterTimeScale = 1f;

	public bool requireGravestoneTarget = true;

	public float consumeDuration = 5f;

	public float landStartPositionY = -70f;

	public float consumeEndPositionY = -30f;

	public float discardStartOffset = -30f;

	public float discardEndOffset;

	public float discardResetPosition = -10000f;

	public StringName discardShaderParameter = "discardUpPos";

	public Tween.EaseType consumeEase = Tween.EaseType.InOut;

	public Tween.TransitionType consumeTransition;

	public string consumeAudio = "GraveBusterChomp";

	public bool createDrop = true;

	public string dropFeatureName = "Coins";

	public Vector2 dropVelocityXRange = new Vector2(-50f, 50f);

	public float dropVelocityY = -400f;

	public float dropGravity = 980f;

	public StringName gravebusterStateEvent = "ToGravebuster";

	public StringName idleStateEvent = "ToIdle";

	public TowerDefenseCharacter parent;

	public TowerDefenseGravestone graveStone;

	public Tween graveStoneTween;

	public Vector2 _syncDropVelocity = Vector2.Zero;

	public bool _syncDeserializing;

	private static readonly StringName SyncAliveKey = new StringName("_alive");

	private readonly Dictionary _syncPayload = new Dictionary();

	private bool _syncPayloadInitialized;

	private bool _syncPayloadHasState;

	private string _syncPayloadState;

	private bool _syncPayloadHasGraveStone;

	private int _syncPayloadGraveStoneId = -1;

	private bool _syncPayloadHasDropVelocity;

	private Vector2 _syncPayloadDropVelocity;

	private TaskCompletionSource<bool> _consumeCompletion;

	private Action _consumeFinishedHandler;

	private bool _consumeRunning;

	private bool _over;

	private bool _parentReadyConnected;

	private bool _stateSignalsConnected;

	private ulong _consumeVersion;

	private ulong _consumeStartedAtMsec;

	private float _consumeRunDuration;

	private bool _pendingResumeConsume;

	private float _pendingConsumeRemaining;

	private float _pendingSpritePositionY = 0f / 0f;

	private StateHandle _idleState;

	private StateHandle _consumeState;

	private string _pendingSyncedState;

	private NodePath _spritePath = new NodePath();

	private bool _configured;

	private bool _spriteConnected;

	internal override bool HasOwnerGameplayActivationWork => true;

	private static bool IsRemoteClient
	{
		get
		{
			if (Global.IsMultiplayerMode)
			{
				return !MultiPlayerManager.IsHost;
			}
			return false;
		}
	}

	private bool CanDispatchStateProcessing
	{
		get
		{
			if (base.Alive && GodotObject.IsInstanceValid(parent))
			{
				return GodotObject.IsInstanceValid(sprite);
			}
			return false;
		}
	}

	private GravebusterComponentDefinition Definition => base.ComponentDefinition as GravebusterComponentDefinition;

	public event OverEventHandler OnOver;

	protected override void OnBound()
	{
		parent = base.Owner;
		ConfigureOnce();
		sprite = ((GodotObject.IsInstanceValid(parent) && !_spritePath.IsEmpty) ? parent.GetNodeOrNull<AdobeAnimateSprite>(_spritePath) : null);
		if (GodotObject.IsInstanceValid(sprite) && !_spriteConnected)
		{
			sprite.OnAnimeCompleted += AnimeCompleted;
			_spriteConnected = true;
		}
		PrepareLandingPosition();
		if (GodotObject.IsInstanceValid(parent))
		{
			if (parent.IsNodeReady())
			{
				InitializeAfterParentReady();
				return;
			}
			parent.Ready += InitializeAfterParentReady;
			_parentReadyConnected = true;
		}
	}

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		_consumeVersion++;
		DisconnectParentReady();
		if (_spriteConnected && GodotObject.IsInstanceValid(sprite))
		{
			sprite.OnAnimeCompleted -= AnimeCompleted;
		}
		_spriteConnected = false;
		CancelConsumeTween();
		ResetGravestoneDiscard();
		ClearSyncPayload();
		parent = null;
		graveStone = null;
		sprite = null;
	}

	protected override void OnReleased()
	{
		ClearSyncPayload();
		this.OnOver = null;
		parent = null;
		graveStone = null;
		sprite = null;
		_idleState = null;
		_consumeState = null;
	}

	private void ConfigureOnce()
	{
		if (!_configured && Definition != null)
		{
			GravebusterComponentDefinition definition = Definition;
			_spritePath = definition.spritePath;
			landAnimeClips = definition.landAnimeClips;
			landAnimeTimeScale = definition.landAnimeTimeScale;
			gravebusterAnimeClips = definition.gravebusterAnimeClips;
			gravebusterTimeScale = definition.gravebusterTimeScale;
			requireGravestoneTarget = definition.requireGravestoneTarget;
			consumeDuration = definition.consumeDuration;
			landStartPositionY = definition.landStartPositionY;
			consumeEndPositionY = definition.consumeEndPositionY;
			discardStartOffset = definition.discardStartOffset;
			discardEndOffset = definition.discardEndOffset;
			discardResetPosition = definition.discardResetPosition;
			discardShaderParameter = definition.discardShaderParameter;
			consumeEase = definition.consumeEase;
			consumeTransition = definition.consumeTransition;
			consumeAudio = definition.consumeAudio;
			createDrop = definition.createDrop;
			dropFeatureName = definition.dropFeatureName;
			dropVelocityXRange = definition.dropVelocityXRange;
			dropVelocityY = definition.dropVelocityY;
			dropGravity = definition.dropGravity;
			gravebusterStateEvent = definition.gravebusterStateEvent;
			idleStateEvent = definition.idleStateEvent;
			_configured = true;
		}
	}

	protected override void OnStateRuntimeAttached()
	{
		_idleState = base.StateMachine?.GetStateById("gravebuster.idle");
		_consumeState = base.StateMachine?.GetStateById("gravebuster.consume");
		ConnectStateSignals();
		Callable.From(ApplyPendingSyncedState).CallDeferred();
	}

	protected override void OnStateRuntimeRegistered()
	{
		ApplyPendingSyncedState();
	}

	protected override void OnOwnerGameplayActivated()
	{
		PrepareLandingPosition();
	}

	protected override void OnAuthoritativeStateRestorePreparing(bool remote)
	{
		_pendingSyncedState = null;
		_consumeVersion++;
		CancelConsumeTween();
	}

	protected override void OnAuthoritativeStateRestored(StateMachineSnapshot snapshot, bool remote)
	{
		RestoreAuthoritativeConsumeMechanics(remote);
	}

	private void RestoreAuthoritativeConsumeMechanics(bool remote)
	{
		if ((base.StateMachine?.CurrentStateHandle?.StableId ?? string.Empty) != "gravebuster.consume")
		{
			_pendingResumeConsume = false;
			_pendingConsumeRemaining = 0f;
			_pendingSpritePositionY = 0f / 0f;
			ResetGravestoneDiscard();
		}
		else
		{
			if (!GodotObject.IsInstanceValid(sprite))
			{
				return;
			}
			if (float.IsFinite(_pendingSpritePositionY))
			{
				sprite.Position = new Vector2(sprite.Position.X, _pendingSpritePositionY);
			}
			if (_pendingResumeConsume)
			{
				RestoreDiscardFromSpritePosition();
				if (!string.IsNullOrEmpty(gravebusterAnimeClips) && sprite.clip != gravebusterAnimeClips)
				{
					sprite.SetAnimation(gravebusterAnimeClips);
				}
				if (remote)
				{
					_consumeRunning = true;
				}
				else
				{
					ResumeConsumeWithoutEntryEffects(_pendingConsumeRemaining);
				}
			}
			else if (!string.IsNullOrEmpty(landAnimeClips) && sprite.clip != landAnimeClips)
			{
				sprite.SetAnimation(landAnimeClips, loop: false);
			}
			_pendingResumeConsume = false;
			_pendingConsumeRemaining = 0f;
			_pendingSpritePositionY = 0f / 0f;
		}
	}

	private void ResumeConsumeWithoutEntryEffects(float remainingDuration)
	{
		float num = Mathf.Max(0.001f, remainingDuration);
		_consumeRunning = true;
		_consumeStartedAtMsec = Time.GetTicksMsec();
		_consumeRunDuration = num;
		StartGravebusterAsync(++_consumeVersion, num, playAudio: false, resumeFromCurrentPosition: true);
	}

	protected override void OnStateRuntimeDetaching()
	{
		DisconnectStateSignals();
		_idleState = null;
		_consumeState = null;
	}

	private void InitializeAfterParentReady()
	{
		DisconnectParentReady();
		ResolveGravestoneTarget();
		Callable.From(ResolveGravestoneTarget).CallDeferred();
	}

	private void PrepareLandingPosition()
	{
		if (GodotObject.IsInstanceValid(parent) && GodotObject.IsInstanceValid(sprite) && parent.inGame && !Engine.IsEditorHint() && (!Global.IsEditor || !(SceneManager.CurrentScene == "LevelEditorStage")))
		{
			sprite.Position = new Vector2(sprite.Position.X, landStartPositionY);
		}
	}

	private void ResolveGravestoneTarget()
	{
		if (!GodotObject.IsInstanceValid(graveStone) && TryGetParent(out var owner))
		{
			TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(owner.gridPos);
			if (GodotObject.IsInstanceValid(mapCell))
			{
				graveStone = mapCell.FindSlotParent(owner) as TowerDefenseGravestone;
			}
		}
	}

	private void DisconnectParentReady()
	{
		if (_parentReadyConnected && GodotObject.IsInstanceValid(parent))
		{
			parent.Ready -= InitializeAfterParentReady;
		}
		_parentReadyConnected = false;
	}

	private void ConnectStateSignals()
	{
		if (!_stateSignalsConnected)
		{
			ConnectState(_idleState, IdleEntered, IdleExited, IdleProcessing);
			ConnectState(_consumeState, GravebusterEntered, GravebusterExited, GravebusterProcessing);
			_stateSignalsConnected = true;
		}
	}

	private void DisconnectStateSignals()
	{
		if (_stateSignalsConnected)
		{
			DisconnectState(_idleState, IdleEntered, IdleExited, IdleProcessing);
			DisconnectState(_consumeState, GravebusterEntered, GravebusterExited, GravebusterProcessing);
			_stateSignalsConnected = false;
		}
	}

	private static void ConnectState(StateHandle node, Action entered, Action exited, Action<double> processing)
	{
		if (node != null && node.IsValid)
		{
			node.Entered += entered;
			node.Exited += exited;
			node.PhysicsProcessing += processing;
		}
	}

	private static void DisconnectState(StateHandle node, Action entered, Action exited, Action<double> processing)
	{
		if (node != null)
		{
			node.Entered -= entered;
			node.Exited -= exited;
			node.PhysicsProcessing -= processing;
		}
	}

	public void IdleEntered()
	{
		if (TryGetParent(out var owner) && owner.componentRunning)
		{
			owner.Idle();
		}
	}

	public void IdleProcessing(double delta)
	{
		if (CanDispatchStateProcessing && !IsRemoteClient && !_consumeRunning && CanStart(out var owner) && IsPlacedInMapCell(owner))
		{
			ResolveGravestoneTarget();
			if (!requireGravestoneTarget || GodotObject.IsInstanceValid(graveStone))
			{
				owner.Component();
				SendStateEvent(gravebusterStateEvent);
			}
		}
	}

	public void IdleExited()
	{
	}

	public void GravebusterEntered()
	{
		ResolveGravestoneTarget();
		if (!GodotObject.IsInstanceValid(sprite))
		{
			return;
		}
		sprite.Position = new Vector2(sprite.Position.X, landStartPositionY);
		if (!string.IsNullOrEmpty(landAnimeClips))
		{
			sprite.SetAnimation(landAnimeClips, loop: false, 0.2);
			if (!string.IsNullOrEmpty(gravebusterAnimeClips))
			{
				sprite.AddAnimation(gravebusterAnimeClips, 0.0);
			}
		}
		else
		{
			StartGravebuster();
		}
	}

	public void GravebusterProcessing(double delta)
	{
		if (!CanDispatchStateProcessing)
		{
			return;
		}
		if (!TryGetParent(out var owner) || !owner.componentAlive)
		{
			SendStateEvent(idleStateEvent);
		}
		else if (GodotObject.IsInstanceValid(sprite))
		{
			if (sprite.clip == landAnimeClips)
			{
				sprite.timeScale = owner.timeScale * (double)landAnimeTimeScale;
			}
			else if (sprite.clip == gravebusterAnimeClips)
			{
				sprite.timeScale = owner.timeScale * (double)gravebusterTimeScale;
			}
		}
	}

	public void GravebusterExited()
	{
		if (_consumeRunning && !_over)
		{
			_consumeVersion++;
			CancelConsumeTween();
			ResetGravestoneDiscard();
		}
	}

	public void AnimeCompleted(string clip)
	{
		if (clip == landAnimeClips)
		{
			StartGravebuster();
		}
	}

	public void StartGravebuster()
	{
		if (!_consumeRunning && TryGetParent(out var _) && GodotObject.IsInstanceValid(sprite))
		{
			_consumeRunning = true;
			float num = Mathf.Max(0f, consumeDuration);
			_consumeStartedAtMsec = Time.GetTicksMsec();
			_consumeRunDuration = num;
			StartGravebusterAsync(++_consumeVersion, num, playAudio: true, resumeFromCurrentPosition: false);
		}
	}

	private async Task StartGravebusterAsync(ulong version, float duration, bool playAudio, bool resumeFromCurrentPosition)
	{
		try
		{
			if (playAudio)
			{
				PlayConsumeAudio();
			}
			float num = GetDiscardPosition(discardStartOffset);
			float discardPosition = GetDiscardPosition(discardEndOffset);
			if (resumeFromCurrentPosition && GodotObject.IsInstanceValid(sprite))
			{
				float value = Mathf.InverseLerp(landStartPositionY, consumeEndPositionY, sprite.Position.Y);
				num = Mathf.Lerp(num, discardPosition, Mathf.Clamp(value, 0f, 1f));
			}
			SetGraveDiscardUpPos(num);
			if (duration > 0f)
			{
				if (!GodotObject.IsInstanceValid(parent))
				{
					return;
				}
				graveStoneTween = parent.CreateTween();
				graveStoneTween.SetParallel();
				graveStoneTween.SetEase(consumeEase);
				graveStoneTween.SetTrans(consumeTransition);
				_consumeCompletion = new TaskCompletionSource<bool>();
				_consumeFinishedHandler = delegate
				{
					_consumeCompletion?.TrySetResult(result: true);
				};
				graveStoneTween.Finished += _consumeFinishedHandler;
				graveStoneTween.TweenProperty(sprite, "position:y", consumeEndPositionY, duration);
				if (GodotObject.IsInstanceValid(graveStone))
				{
					graveStoneTween.TweenMethod(Callable.From<double>(SetGraveDiscardUpPos), num, discardPosition, duration);
				}
				if (!(await _consumeCompletion.Task) || version != _consumeVersion)
				{
					return;
				}
				CleanupConsumeTween(kill: false);
			}
			else
			{
				sprite.Position = new Vector2(sprite.Position.X, consumeEndPositionY);
				SetGraveDiscardUpPos(discardPosition);
			}
			if (!IsRemoteClient && version == _consumeVersion && TryGetParent(out var owner))
			{
				CreateDrop(owner);
				if (GodotObject.IsInstanceValid(graveStone))
				{
					graveStone.Destroy();
				}
				Over();
			}
		}
		finally
		{
			if (version == _consumeVersion)
			{
				_consumeRunning = false;
				_consumeStartedAtMsec = 0uL;
				_consumeRunDuration = 0f;
			}
		}
	}

	private void CreateDrop(TowerDefenseCharacter owner)
	{
		if (createDrop && GodotObject.IsInstanceValid(TowerDefenseManager.Instance) && GodotObject.IsInstanceValid(GameSaveManager.Instance) && GameSaveManager.Instance.GetFeatureValue(dropFeatureName) > 0)
		{
			Vector2 velocity;
			if (_syncDeserializing && _syncDropVelocity != Vector2.Zero)
			{
				velocity = _syncDropVelocity;
				_syncDropVelocity = Vector2.Zero;
				_syncDeserializing = false;
			}
			else
			{
				float num = Mathf.Min(dropVelocityXRange.X, dropVelocityXRange.Y);
				float num2 = Mathf.Max(dropVelocityXRange.X, dropVelocityXRange.Y);
				velocity = (_syncDropVelocity = new Vector2((float)GD.RandRange(num, num2), dropVelocityY));
			}
			Vector2 logicalGlobalPosition = owner.GetLogicalGlobalPosition();
			TowerDefenseGroundItemBase towerDefenseGroundItemBase = TowerDefenseManager.Instance.FallingObjectCreate(logicalGlobalPosition, owner.GetGroundHeight(logicalGlobalPosition.Y), velocity, dropGravity);
			if (GodotObject.IsInstanceValid(towerDefenseGroundItemBase))
			{
				towerDefenseGroundItemBase.gridPos = owner.gridPos;
			}
		}
	}

	public void Over()
	{
		if (!_over)
		{
			_over = true;
			this.OnOver?.Invoke(graveStone);
			if (GodotObject.IsInstanceValid(parent))
			{
				parent.Destroy();
			}
		}
	}

	private float GetDiscardPosition(float offset)
	{
		if (!TryGetParent(out var owner) || !GodotObject.IsInstanceValid(owner.spriteGroup) || !GodotObject.IsInstanceValid(owner.GetViewport()))
		{
			return discardResetPosition;
		}
		Transform2D screenTransform = owner.GetViewport().GetScreenTransform();
		screenTransform.Origin = Vector2.Zero;
		return (screenTransform * (owner.GetLogicalGlobalPosition(owner.spriteGroup) + new Vector2(0f, offset))).Y;
	}

	private void SetGraveDiscardUpPos(double value)
	{
		if (GodotObject.IsInstanceValid(graveStone) && !discardShaderParameter.IsEmpty)
		{
			graveStone.SetSpriteGroupShaderParameter(discardShaderParameter.ToString(), (float)value);
		}
	}

	private void ResetGravestoneDiscard()
	{
		SetGraveDiscardUpPos(discardResetPosition);
	}

	private void RestoreDiscardFromSpritePosition()
	{
		if (GodotObject.IsInstanceValid(sprite))
		{
			float value = Mathf.InverseLerp(landStartPositionY, consumeEndPositionY, sprite.Position.Y);
			float discardPosition = GetDiscardPosition(discardStartOffset);
			float discardPosition2 = GetDiscardPosition(discardEndOffset);
			SetGraveDiscardUpPos(Mathf.Lerp(discardPosition, discardPosition2, Mathf.Clamp(value, 0f, 1f)));
		}
	}

	private void PlayConsumeAudio()
	{
		if (!string.IsNullOrEmpty(consumeAudio) && GodotObject.IsInstanceValid(AudioManager.Instance))
		{
			AudioManager.Instance.AudioPlay(consumeAudio);
		}
	}

	private void CancelConsumeTween()
	{
		_consumeCompletion?.TrySetResult(result: false);
		CleanupConsumeTween(kill: true);
		_consumeRunning = false;
		_consumeStartedAtMsec = 0uL;
		_consumeRunDuration = 0f;
	}

	private void CleanupConsumeTween(bool kill)
	{
		if (GodotObject.IsInstanceValid(graveStoneTween) && _consumeFinishedHandler != null)
		{
			graveStoneTween.Finished -= _consumeFinishedHandler;
		}
		if (kill && GodotObject.IsInstanceValid(graveStoneTween))
		{
			graveStoneTween.Kill();
		}
		graveStoneTween = null;
		_consumeFinishedHandler = null;
		_consumeCompletion = null;
	}

	private bool CanStart(out TowerDefenseCharacter owner)
	{
		if (TryGetParent(out owner) && !owner.componentRunning && owner.inGame && owner.componentAlive && GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
		{
			return TowerDefenseManager.Instance.IsGameRunning();
		}
		return false;
	}

	private static bool IsPlacedInMapCell(TowerDefenseCharacter owner)
	{
		if (!GodotObject.IsInstanceValid(owner))
		{
			return false;
		}
		TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(owner.gridPos);
		if (GodotObject.IsInstanceValid(mapCell))
		{
			return mapCell.characterList.Contains(owner);
		}
		return false;
	}

	private bool TryGetParent(out TowerDefenseCharacter owner)
	{
		owner = parent;
		if (base.Alive)
		{
			return GodotObject.IsInstanceValid(owner);
		}
		return false;
	}

	public override void SetAlive(bool value)
	{
		base.SetAlive(value);
		if (!value && _consumeRunning)
		{
			_consumeVersion++;
			CancelConsumeTween();
			ResetGravestoneDiscard();
			SendStateEvent(idleStateEvent, allowWhenInactive: true);
		}
	}

	public override Dictionary ExportComponentSave()
	{
		Dictionary dictionary = SerializeRuntimeState();
		dictionary["consume_running"] = _consumeRunning;
		dictionary["consume_remaining"] = GetConsumeRemainingDuration();
		dictionary["over"] = _over;
		if (GodotObject.IsInstanceValid(sprite))
		{
			dictionary["sprite_position_y"] = sprite.Position.Y;
		}
		if (GodotObject.IsInstanceValid(graveStone))
		{
			dictionary["grave_stone_name"] = graveStone.Name.ToString().ValidateNodeName();
		}
		return dictionary;
	}

	public override void ImportComponentSave(Dictionary data, TowerDefenseLevelSaveConfigCSharp owner)
	{
		if (data == null)
		{
			return;
		}
		_pendingResumeConsume = data.GetValueOrDefault("consume_running", false).AsBool();
		_pendingConsumeRemaining = Mathf.Max(0f, data.GetValueOrDefault("consume_remaining", 0.0).AsSingle());
		_pendingSpritePositionY = (data.ContainsKey("sprite_position_y") ? data["sprite_position_y"].AsSingle() : (0f / 0f));
		_over = data.GetValueOrDefault("over", false).AsBool();
		string graveStoneName = data.GetValueOrDefault("grave_stone_name", "").AsString();
		ResolveSavedGravestone(owner, graveStoneName);
		if (!string.IsNullOrEmpty(graveStoneName))
		{
			Callable.From(delegate
			{
				ResolveSavedGravestone(owner, graveStoneName);
			}).CallDeferred();
		}
		DeserializeRuntimeState(data);
	}

	public override Dictionary SyncSerialize()
	{
		StateHandle stateHandle = base.StateMachine?.CurrentStateHandle;
		bool flag = stateHandle?.IsValid ?? false;
		string text = (flag ? stateHandle.StableId : null);
		bool flag2 = GodotObject.IsInstanceValid(graveStone);
		int num = (flag2 ? graveStone.syncId : (-1));
		bool flag3 = _syncDropVelocity != Vector2.Zero;
		int num2 = (flag ? 1 : 0) + (flag2 ? 1 : 0) + (flag3 ? 2 : 0);
		if (_syncPayloadInitialized)
		{
			if (_syncPayload.Count == num2 + 1)
			{
				_syncPayload.Remove(SyncAliveKey);
			}
			if (_syncPayload.Count == num2 && _syncPayloadHasState == flag && (!flag || string.Equals(_syncPayloadState, text, StringComparison.Ordinal)) && _syncPayloadHasGraveStone == flag2 && (!flag2 || _syncPayloadGraveStoneId == num) && _syncPayloadHasDropVelocity == flag3 && (!flag3 || _syncPayloadDropVelocity == _syncDropVelocity))
			{
				return _syncPayload;
			}
		}
		_syncPayload.Clear();
		if (flag)
		{
			_syncPayload["state"] = text;
		}
		if (flag2)
		{
			_syncPayload["grave_stone_sync_id"] = num;
		}
		if (flag3)
		{
			_syncPayload["drop_velocity_x"] = _syncDropVelocity.X;
			_syncPayload["drop_velocity_y"] = _syncDropVelocity.Y;
		}
		_syncPayloadHasState = flag;
		_syncPayloadState = text;
		_syncPayloadHasGraveStone = flag2;
		_syncPayloadGraveStoneId = num;
		_syncPayloadHasDropVelocity = flag3;
		_syncPayloadDropVelocity = _syncDropVelocity;
		_syncPayloadInitialized = true;
		return _syncPayload;
	}

	private void ClearSyncPayload()
	{
		_syncPayload.Clear();
		_syncPayloadInitialized = false;
		_syncPayloadHasState = false;
		_syncPayloadState = null;
		_syncPayloadHasGraveStone = false;
		_syncPayloadGraveStoneId = -1;
		_syncPayloadHasDropVelocity = false;
		_syncPayloadDropVelocity = Vector2.Zero;
	}

	private Dictionary SerializeRuntimeState()
	{
		Dictionary dictionary = new Dictionary();
		StateHandle stateHandle = base.StateMachine?.CurrentStateHandle;
		if (stateHandle != null && stateHandle.IsValid)
		{
			dictionary["state"] = stateHandle.StableId;
		}
		if (GodotObject.IsInstanceValid(graveStone))
		{
			dictionary["grave_stone_sync_id"] = graveStone.syncId;
		}
		if (_syncDropVelocity != Vector2.Zero)
		{
			dictionary["drop_velocity_x"] = _syncDropVelocity.X;
			dictionary["drop_velocity_y"] = _syncDropVelocity.Y;
		}
		return dictionary;
	}

	public override void SyncDeserialize(Dictionary data)
	{
		DeserializeRuntimeState(data);
	}

	private void DeserializeRuntimeState(Dictionary data)
	{
		if (data == null)
		{
			return;
		}
		if (data.ContainsKey("grave_stone_sync_id"))
		{
			int num = data["grave_stone_sync_id"].AsInt32();
			if (num >= 0 && GodotObject.IsInstanceValid(TowerDefenseManager.CurrentControl) && TowerDefenseManager.CurrentControl._syncCharacters.TryGetValue(num, out var value) && value is TowerDefenseGravestone towerDefenseGravestone)
			{
				graveStone = towerDefenseGravestone;
			}
		}
		if (data.ContainsKey("drop_velocity_x"))
		{
			_syncDropVelocity = new Vector2(data.GetValueOrDefault("drop_velocity_x", 0.0).AsSingle(), data.GetValueOrDefault("drop_velocity_y", 0.0).AsSingle());
			_syncDeserializing = true;
		}
		if (base.ShouldApplyLegacyStateField && data.ContainsKey("state"))
		{
			_pendingSyncedState = data["state"].AsString();
			ApplyPendingSyncedState();
		}
	}

	private float GetConsumeRemainingDuration()
	{
		if (!_consumeRunning)
		{
			return 0f;
		}
		if (_consumeRunDuration <= 0f || _consumeStartedAtMsec == 0L)
		{
			return Mathf.Max(0f, consumeDuration);
		}
		double num = (double)(Time.GetTicksMsec() - _consumeStartedAtMsec) / 1000.0;
		return Mathf.Max(0f, _consumeRunDuration - (float)num);
	}

	private void ResolveSavedGravestone(TowerDefenseLevelSaveConfigCSharp owner, string graveStoneName)
	{
		if (!string.IsNullOrEmpty(graveStoneName) && owner?.charcterDicionary != null && owner.charcterDicionary.TryGetValue(new StringName(graveStoneName), out var value) && GodotObject.IsInstanceValid(value) && value is TowerDefenseGravestone towerDefenseGravestone)
		{
			graveStone = towerDefenseGravestone;
		}
	}

	private void ApplyPendingSyncedState()
	{
		if (GodotObject.IsInstanceValid(parent) && parent.IsInsideTree() && base.IsStateMachineRegistered && !string.IsNullOrEmpty(_pendingSyncedState))
		{
			IStateMachineController stateMachine = base.StateMachine;
			if (stateMachine != null && stateMachine.CurrentStateHandle?.IsValid == true && SyncForceState(base.StateMachine, _pendingSyncedState))
			{
				_pendingSyncedState = null;
			}
		}
	}

	private static bool SyncForceState(IStateMachineController state, string targetState)
	{
		if (state == null || !state.IsInitialized || string.IsNullOrEmpty(targetState))
		{
			return false;
		}
		StateHandle currentStateHandle = state.CurrentStateHandle;
		StateHandle stateHandle = state.ResolveState(targetState, allowDisplayNameFallback: true);
		if (currentStateHandle == null || !currentStateHandle.IsValid || stateHandle == null || !stateHandle.IsValid)
		{
			return false;
		}
		if (currentStateHandle.StableId == stateHandle.StableId)
		{
			return true;
		}
		StateMachineSnapshot stateMachineSnapshot = state.CaptureSnapshot();
		if (stateMachineSnapshot == null)
		{
			return false;
		}
		stateMachineSnapshot.ActiveStateIds.Clear();
		stateMachineSnapshot.ActiveStateIds.Add(stateHandle.StableId);
		stateMachineSnapshot.PendingTransitionIds.Clear();
		stateMachineSnapshot.PendingDelayRemaining.Clear();
		return state.RestoreSnapshot(stateMachineSnapshot);
	}
}
