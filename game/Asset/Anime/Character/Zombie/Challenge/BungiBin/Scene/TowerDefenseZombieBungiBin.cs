using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombieBungiBin : TowerDefenseZombie
{
	private StateHandle _dropStateHandle;

	private StateHandle _grabStateHandle;

	private StateHandle _riseStateHandle;

	private StateHandle _downStateHandle;

	private StateHandle _downIdleStateHandle;

	private bool _stateSignalsConnected;

	public bool waitGrab;

	public bool waitDown;

	public bool hasPlant;

	private Sprite2D _bungeeTarget;

	public double waitGrabTimer;

	public double waitDownTimer;

	public bool canBlock = true;

	public Tween dropTween;

	public bool grabOver;

	public bool downOver;

	private Tween _targetDropTween;

	private Tween _riseTween;

	private Node2D _capturedPayloadNode;

	private bool _capturedPayloadIsVisualClone;

	private string _capturedPayloadPacketName = "";

	private string _capturedPayloadClip = "";

	private int _capturedPayloadFrame;

	private Vector2 _capturedPayloadScale = Vector2.One;

	private Array<string> _capturedPayloadCustom = new Array<string>();

	private void ConnectStateSignals()
	{
		if (_stateSignalsConnected)
		{
			return;
		}
		IStateMachineController stateMachine = base.StateMachine;
		if (stateMachine == null || !stateMachine.IsInitialized)
		{
			return;
		}
		_dropStateHandle = base.StateMachine?.GetStateById("zombie.bungi_bin.drop");
		_grabStateHandle = base.StateMachine?.GetStateById("zombie.bungi_bin.grab");
		_riseStateHandle = base.StateMachine?.GetStateById("zombie.bungi_bin.rise");
		_downStateHandle = base.StateMachine?.GetStateById("zombie.bungi_bin.down");
		_downIdleStateHandle = base.StateMachine?.GetStateById("zombie.bungi_bin.down_idle");
		StateHandle dropStateHandle = _dropStateHandle;
		if (dropStateHandle == null || !dropStateHandle.IsValid)
		{
			return;
		}
		StateHandle grabStateHandle = _grabStateHandle;
		if (grabStateHandle == null || !grabStateHandle.IsValid)
		{
			return;
		}
		StateHandle riseStateHandle = _riseStateHandle;
		if (riseStateHandle == null || !riseStateHandle.IsValid)
		{
			return;
		}
		StateHandle downStateHandle = _downStateHandle;
		if (downStateHandle != null && downStateHandle.IsValid)
		{
			StateHandle downIdleStateHandle = _downIdleStateHandle;
			if (downIdleStateHandle != null && downIdleStateHandle.IsValid)
			{
				_dropStateHandle.Entered += DropEntered;
				_dropStateHandle.Exited += DropExited;
				_dropStateHandle.PhysicsProcessing += DropProcessing;
				_grabStateHandle.Entered += GrabEntered;
				_grabStateHandle.Exited += GrabExited;
				_grabStateHandle.PhysicsProcessing += GrabProcessing;
				_riseStateHandle.Entered += RiseEntered;
				_riseStateHandle.Exited += RiseExited;
				_riseStateHandle.PhysicsProcessing += RiseProcessing;
				_downStateHandle.Entered += DownEntered;
				_downStateHandle.Exited += DownExited;
				_downStateHandle.PhysicsProcessing += DownProcessing;
				_downIdleStateHandle.Entered += DownIdleEntered;
				_downIdleStateHandle.Exited += DownIdleExited;
				_downIdleStateHandle.PhysicsProcessing += DownIdleProcessing;
				_stateSignalsConnected = true;
			}
		}
	}

	private void DisconnectStateSignals()
	{
		if (_stateSignalsConnected)
		{
			if (_dropStateHandle != null)
			{
				_dropStateHandle.Entered -= DropEntered;
				_dropStateHandle.Exited -= DropExited;
				_dropStateHandle.PhysicsProcessing -= DropProcessing;
			}
			if (_grabStateHandle != null)
			{
				_grabStateHandle.Entered -= GrabEntered;
				_grabStateHandle.Exited -= GrabExited;
				_grabStateHandle.PhysicsProcessing -= GrabProcessing;
			}
			if (_riseStateHandle != null)
			{
				_riseStateHandle.Entered -= RiseEntered;
				_riseStateHandle.Exited -= RiseExited;
				_riseStateHandle.PhysicsProcessing -= RiseProcessing;
			}
			if (_downStateHandle != null)
			{
				_downStateHandle.Entered -= DownEntered;
				_downStateHandle.Exited -= DownExited;
				_downStateHandle.PhysicsProcessing -= DownProcessing;
			}
			if (_downIdleStateHandle != null)
			{
				_downIdleStateHandle.Entered -= DownIdleEntered;
				_downIdleStateHandle.Exited -= DownIdleExited;
				_downIdleStateHandle.PhysicsProcessing -= DownIdleProcessing;
			}
			_dropStateHandle = null;
			_grabStateHandle = null;
			_riseStateHandle = null;
			_downStateHandle = null;
			_downIdleStateHandle = null;
			_stateSignalsConnected = false;
		}
	}

	public override void _ExitTree()
	{
		CancelDropPresentation();
		CancelRisePresentation();
		DisconnectStateSignals();
		base._ExitTree();
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_bungeeTarget = GetNode<Sprite2D>("%BungeeTarget");
			targetRegistrationComponent.canProjectileCheck = false;
			targetRegistrationComponent.canCarry = false;
			instance.maskFlags = 0;
			AddToGroup("Bungi", persistent: true);
			if (TowerDefenseManager.Instance.IsGameRunning())
			{
				base.z = 600.0;
				isGround = false;
			}
			ConnectStateSignals();
			EnsureCapturedPayloadVisual();
		}
	}

	public override void HitpointsNearDie()
	{
		base.HitpointsNearDie();
		Destroy();
	}

	public override void HitpointsEmpty()
	{
		base.HitpointsEmpty();
		Destroy();
	}

	public override void Walk()
	{
		ActivateGameplayProcessing();
		SendStateEvent("ToDrop");
	}

	public override void IdleEntered()
	{
		base.IdleEntered();
	}

	public override void IdleProcessing(double delta)
	{
		base.IdleProcessing(delta);
		if (!waitDown)
		{
			return;
		}
		if (waitDownTimer < 3.0)
		{
			if (!base.sprite.pause)
			{
				waitDownTimer += delta * timeScale;
			}
			return;
		}
		waitDown = false;
		foreach (string item in new List<string>(buff.buffDictionary.Keys))
		{
			if (item != "Hypnoses")
			{
				buff.DeleteBuff(item);
			}
		}
		SendStateEvent("ToDown");
	}

	public override void Spawn()
	{
		base.z = 600.0;
		isGround = false;
		if (base.IsRemoteNetworkReplica)
		{
			return;
		}
		List<Vector2I> list = new List<Vector2I>();
		foreach (Node item in GetTree().GetNodesInGroup("Bungi"))
		{
			if (item != this && ((TowerDefenseCharacter)item).gridPos != new Vector2I(-1, -1))
			{
				list.Add(((TowerDefenseCharacter)item).gridPos);
			}
		}
		List<TowerDefenseCharacter> cleanCharactersList = TowerDefenseManager.Instance.characterRegistry.GetCleanCharactersList();
		List<TowerDefenseCharacter> list2 = new List<TowerDefenseCharacter>();
		foreach (TowerDefenseCharacter item2 in cleanCharactersList)
		{
			if (!(item2 is TowerDefensePlant) || item2.isDestroy || item2.die || !CanTarget(item2) || item2 is TowerDefensePlantBowlingBase || !item2.instance.canBeCollection || list.Contains(item2.gridPos))
			{
				continue;
			}
			TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(item2.gridPos);
			bool flag = false;
			foreach (TowerDefenseCharacter character in mapCell.GetCharacterList())
			{
				if (character.config.name == "TrashBin")
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				list2.Add(item2);
			}
		}
		if (list2.Count <= 0)
		{
			Destroy();
			base.gridPos = new Vector2I(GD.RandRange(0, TowerDefenseManager.Instance.GetMapGridNum().X), GD.RandRange(0, TowerDefenseManager.Instance.GetMapGridNum().Y));
		}
		else
		{
			TowerDefenseCharacter towerDefenseCharacter = list2[GD.RandRange(0, list2.Count - 1)];
			SetLogicalGlobalPosition(TowerDefenseManager.GetMapCellPlantPos(towerDefenseCharacter.gridPos));
			base.gridPos = towerDefenseCharacter.gridPos;
		}
	}

	public void DropEntered()
	{
		if (!GodotObject.IsInstanceValid(cell))
		{
			Destroy();
			return;
		}
		instance.canBeCollection = false;
		instance.invincible = true;
		base.z = 600.0;
		isGround = false;
		_bungeeTarget.Visible = true;
		_targetDropTween = CreateTween();
		_targetDropTween.SetEase(Tween.EaseType.Out);
		_targetDropTween.SetTrans(Tween.TransitionType.Cubic);
		_targetDropTween.TweenProperty(_bungeeTarget, "position", new Vector2(0f, (float)(15.0 - cell.GetGroundHeight())), 0.5).From(new Vector2(0f, -585f));
		CompleteDropPresentationAsync(_dropStateHandle.ActivationGeneration);
	}

	private async Task CompleteDropPresentationAsync(ulong activationGeneration)
	{
		if (!(await WaitForStateDelayAsync(_dropStateHandle, 1.0)))
		{
			return;
		}
		AudioManager.Instance.AudioPlay("BungeeScream");
		if (await WaitForStateDelayAsync(_dropStateHandle, 1.0))
		{
			dropTween = CreateTween();
			dropTween.SetEase(Tween.EaseType.Out);
			dropTween.SetTrans(Tween.TransitionType.Cubic);
			dropTween.TweenProperty(this, "z", cell.GetGroundHeight(), 1.0);
			base.sprite.SetAnimation("Drop", loop: true, 0.2);
			if (await WaitForStateDelayAsync(_dropStateHandle, 1.0) && IsStateActivationCurrent(_dropStateHandle, activationGeneration))
			{
				isGround = true;
				base.groundHeight = cell.GetGroundHeight();
				base.z = base.groundHeight;
				waitGrab = false;
				waitDown = true;
				instance.invincible = false;
				instance.canBeCollection = true;
				targetRegistrationComponent.canProjectileCheck = true;
				instance.maskFlags = 1;
				Idle();
			}
		}
	}

	public void DropProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 2.0;
	}

	public void DropExited()
	{
		CancelDropPresentation();
	}

	private void CancelDropPresentation()
	{
		if (GodotObject.IsInstanceValid(_targetDropTween))
		{
			_targetDropTween.Kill();
		}
		if (GodotObject.IsInstanceValid(dropTween))
		{
			dropTween.Kill();
		}
		_targetDropTween = null;
		dropTween = null;
	}

	public void DownEntered()
	{
		waitGrab = true;
		instance.unUseBuffFlags = -9;
		targetRegistrationComponent.canProjectileCheck = false;
		instance.invincible = true;
		base.sprite.SetAnimation("Down", loop: false, 0.2);
	}

	public void DownProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 2.0;
	}

	public void DownExited()
	{
	}

	public void DownIdleEntered()
	{
		instance.invincible = false;
		instance.canBeCollection = true;
		targetRegistrationComponent.canProjectileCheck = true;
		instance.maskFlags = 1;
		base.sprite.timeScale = timeScale;
		base.sprite.SetAnimation("DownIdle", loop: true, 0.2);
	}

	public void DownIdleProcessing(double delta)
	{
		base.sprite.timeScale = timeScale;
		if (!waitGrab)
		{
			return;
		}
		if (waitGrabTimer < 5.0)
		{
			if (!base.sprite.pause)
			{
				waitGrabTimer += delta * timeScale;
			}
			return;
		}
		waitGrab = false;
		foreach (string item in new List<string>(buff.buffDictionary.Keys))
		{
			if (item != "Hypnoses")
			{
				buff.DeleteBuff(item);
			}
		}
		SendStateEvent("ToGrab");
	}

	public void DownIdleExited()
	{
	}

	public void GrabEntered()
	{
		instance.unUseBuffFlags = -9;
		targetRegistrationComponent.canProjectileCheck = false;
		instance.invincible = true;
		base.sprite.SetAnimation("Grab", loop: false, 0.2);
	}

	public void GrabProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 2.0;
	}

	public void GrabExited()
	{
	}

	public void RiseEntered()
	{
		base.sprite.SetAnimation("Rise", loop: true, 0.2);
		isGround = false;
		double duration = 1.5;
		if (!canBlock)
		{
			duration = 0.75;
		}
		_riseTween = CreateTween();
		_riseTween.SetParallel();
		_riseTween.SetEase(Tween.EaseType.Out);
		_riseTween.SetTrans(Tween.TransitionType.Cubic);
		_riseTween.TweenProperty(_bungeeTarget, "position", new Vector2(0f, -585f), duration);
		_riseTween.TweenProperty(this, "z", 600, duration);
		CompleteRisePresentationAsync(_riseStateHandle.ActivationGeneration, duration);
	}

	private async Task CompleteRisePresentationAsync(ulong activationGeneration, double duration)
	{
		if (await WaitForStateDelayAsync(_riseStateHandle, duration) && IsStateActivationCurrent(_riseStateHandle, activationGeneration))
		{
			Destroy();
		}
	}

	public void RiseProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 1.0;
	}

	public void RiseExited()
	{
		CancelRisePresentation();
	}

	private void CancelRisePresentation()
	{
		if (GodotObject.IsInstanceValid(_riseTween))
		{
			_riseTween.Kill();
		}
		_riseTween = null;
	}

	public override void AnimeEvent(string command, Variant argument)
	{
		if (!(command == "down"))
		{
			if (!(command == "grab") || grabOver)
			{
				return;
			}
			grabOver = true;
			AudioManager.Instance.AudioPlay("Floop");
			if (!TowerDefenseManager.HasGameplayAuthority)
			{
				return;
			}
			cell = TowerDefenseManager.GetMapCell(base.gridPos);
			if (!GodotObject.IsInstanceValid(cell))
			{
				return;
			}
			List<TowerDefenseCharacter> list = new List<TowerDefenseCharacter>();
			foreach (TowerDefenseCharacter item in new List<TowerDefenseCharacter>(cell.GetCharacterList()))
			{
				if (item.config.name == "TrashBin")
				{
					CapturePayloadDescriptor(item);
					AttachCapturedPayload(item);
				}
				else if (item.camp != base.camp && item is TowerDefensePlant && !(item is TowerDefensePlantBowlingBase))
				{
					list.Add(item);
				}
			}
			if (!hasPlant)
			{
				return;
			}
			{
				foreach (TowerDefenseCharacter item2 in list)
				{
					item2.Destroy();
				}
				return;
			}
		}
		if (downOver)
		{
			return;
		}
		downOver = true;
		if (!TowerDefenseManager.HasGameplayAuthority)
		{
			return;
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("TrashBin");
		if (GodotObject.IsInstanceValid(packetConfig) && cell.CanPacketPlant(packetConfig))
		{
			TowerDefenseCharacter towerDefenseCharacter = (base.EconomyOwnerAccountId.IsValid ? packetConfig.Plant(base.EconomyOwnerAccountId, base.gridPos, playAudio: false) : packetConfig.Plant(base.gridPos, playAudio: false));
			if (GodotObject.IsInstanceValid(towerDefenseCharacter))
			{
				towerDefenseCharacter.camp = base.camp;
				TowerDefenseManager.PublishSpawnedCharacter("TrashBin", towerDefenseCharacter, useCreate: false, 0.0, walkAfterSpawn: false, "", new Dictionary { ["plant_play_audio"] = false });
			}
		}
	}

	public override void AnimeCompleted(string clip)
	{
		if (!(clip == "Down"))
		{
			if (clip == "Grab")
			{
				AnimeEvent("grab", default(Variant));
				SendStateEvent("ToRise");
			}
		}
		else
		{
			AnimeEvent("down", default(Variant));
			SendStateEvent("ToDownIdle");
		}
	}

	public override bool CanBlock()
	{
		if (canBlock)
		{
			return base.z <= 50.0;
		}
		return false;
	}

	public override Dictionary ExportVariantSave()
	{
		Dictionary dictionary = new Dictionary
		{
			["waitGrab"] = waitGrab,
			["hasPlant"] = hasPlant,
			["waitDownTimer"] = waitDownTimer,
			["waitGrabTimer"] = waitGrabTimer,
			["canBlock"] = canBlock,
			["grabOver"] = grabOver,
			["waitDown"] = waitDown,
			["downOver"] = downOver
		};
		WriteCapturedPayloadState(dictionary);
		return dictionary;
	}

	public override void ImportVariantSave(Dictionary data)
	{
		waitGrab = data.GetValueOrDefault("waitGrab", false).AsBool();
		hasPlant = data.GetValueOrDefault("hasPlant", false).AsBool();
		waitDownTimer = data.GetValueOrDefault("waitDownTimer", 0.0).AsDouble();
		waitGrabTimer = data.GetValueOrDefault("waitGrabTimer", 0.0).AsDouble();
		canBlock = data.GetValueOrDefault("canBlock", true).AsBool();
		grabOver = data.GetValueOrDefault("grabOver", false).AsBool();
		waitDown = data.GetValueOrDefault("waitDown", false).AsBool();
		downOver = data.GetValueOrDefault("downOver", false).AsBool();
		ReadCapturedPayloadState(data);
	}

	public override Dictionary ExportNetworkSpecialState()
	{
		Dictionary dictionary = new Dictionary
		{
			["rev"] = GetNetworkSpecialStateRevision(),
			["hasPlant"] = hasPlant
		};
		WriteCapturedPayloadState(dictionary);
		return dictionary;
	}

	public override void ImportNetworkSpecialState(Dictionary data)
	{
		if (data != null)
		{
			hasPlant = data.GetValueOrDefault("hasPlant", hasPlant).AsBool();
			ReadCapturedPayloadState(data);
		}
	}

	public override int GetNetworkSpecialStateRevision()
	{
		return hasPlant ? 1 : 0;
	}

	private void CapturePayloadDescriptor(TowerDefenseCharacter target)
	{
		if (GodotObject.IsInstanceValid(target))
		{
			RemoveCapturedPayloadVisualClone();
			_capturedPayloadNode = target;
			_capturedPayloadIsVisualClone = false;
			hasPlant = true;
			_capturedPayloadPacketName = (GodotObject.IsInstanceValid(target.packet) ? target.packet.saveKey : "");
			_capturedPayloadClip = (GodotObject.IsInstanceValid(target.sprite) ? target.sprite.clip : "");
			_capturedPayloadFrame = (GodotObject.IsInstanceValid(target.sprite) ? target.sprite.frameIndex : 0);
			_capturedPayloadScale = target.Scale;
			_capturedPayloadCustom = ((target.currentCustom != null) ? target.currentCustom.Duplicate() : new Array<string>());
		}
	}

	private void AttachCapturedPayload(TowerDefenseCharacter target)
	{
		if (GodotObject.IsInstanceValid(target) && GodotObject.IsInstanceValid(spriteGroup))
		{
			target.die = true;
			target.Destroy(freeInstance: false);
			if (GodotObject.IsInstanceValid(target.sprite))
			{
				target.sprite.pause = true;
			}
			if (GodotObject.IsInstanceValid(target.shadowSprite))
			{
				target.shadowSprite.Visible = false;
				target.shadowSprite.Texture = null;
			}
			target.DisableGameplayForPermanentEmbeddedVisual();
			target.Reparent(spriteGroup);
		}
	}

	private void WriteCapturedPayloadState(Dictionary data)
	{
		data["capturedPayloadPacket"] = _capturedPayloadPacketName;
		data["capturedPayloadClip"] = _capturedPayloadClip;
		data["capturedPayloadFrame"] = _capturedPayloadFrame;
		data["capturedPayloadScale"] = _capturedPayloadScale;
		data["capturedPayloadCustom"] = _capturedPayloadCustom;
	}

	private void ReadCapturedPayloadState(Dictionary data)
	{
		_capturedPayloadPacketName = data.GetValueOrDefault("capturedPayloadPacket", _capturedPayloadPacketName).AsString();
		_capturedPayloadClip = data.GetValueOrDefault("capturedPayloadClip", _capturedPayloadClip).AsString();
		_capturedPayloadFrame = data.GetValueOrDefault("capturedPayloadFrame", _capturedPayloadFrame).AsInt32();
		_capturedPayloadScale = data.GetValueOrDefault("capturedPayloadScale", _capturedPayloadScale).AsVector2();
		_capturedPayloadCustom = data.GetValueOrDefault("capturedPayloadCustom", new Array<string>()).AsGodotArray<string>();
		EnsureCapturedPayloadVisual();
	}

	private void EnsureCapturedPayloadVisual()
	{
		if (!hasPlant)
		{
			RemoveCapturedPayloadVisualClone();
		}
		else if (!GodotObject.IsInstanceValid(_capturedPayloadNode) && IsNodeReady() && GodotObject.IsInstanceValid(spriteGroup) && !string.IsNullOrEmpty(_capturedPayloadPacketName))
		{
			CreateCapturedPayloadVisual();
		}
	}

	private void CreateCapturedPayloadVisual()
	{
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(_capturedPayloadPacketName);
		if (!GodotObject.IsInstanceValid(packetConfig) || !GodotObject.IsInstanceValid(packetConfig.characterConfig) || string.IsNullOrEmpty(packetConfig.characterConfig.name))
		{
			return;
		}
		PackedScene chacraterScene = TowerDefenseManager.GetChacraterScene(packetConfig.characterConfig.name);
		if (!GodotObject.IsInstanceValid(chacraterScene))
		{
			return;
		}
		TowerDefenseCharacter towerDefenseCharacter = chacraterScene.Instantiate<TowerDefenseCharacter>(PackedScene.GenEditState.Disabled);
		if (!GodotObject.IsInstanceValid(towerDefenseCharacter))
		{
			return;
		}
		towerDefenseCharacter.packet = packetConfig;
		towerDefenseCharacter.config = packetConfig.characterConfig;
		if (_capturedPayloadCustom.Count > 0)
		{
			towerDefenseCharacter.SetCustoms(_capturedPayloadCustom);
		}
		Node2D nodeOrNull = towerDefenseCharacter.GetNodeOrNull<Node2D>("%SpriteGroup");
		AdobeAnimateSprite adobeAnimateSprite = towerDefenseCharacter.sprite;
		if (!GodotObject.IsInstanceValid(nodeOrNull))
		{
			towerDefenseCharacter.Free();
			return;
		}
		towerDefenseCharacter.RemoveChild(nodeOrNull);
		towerDefenseCharacter.Free();
		nodeOrNull.Name = "CapturedPayloadVisual";
		nodeOrNull.Position = Vector2.Zero;
		nodeOrNull.Scale = new Vector2(nodeOrNull.Scale.X * _capturedPayloadScale.X, nodeOrNull.Scale.Y * _capturedPayloadScale.Y);
		spriteGroup.AddChild(nodeOrNull, forceReadableName: false, InternalMode.Disabled);
		if (GodotObject.IsInstanceValid(adobeAnimateSprite))
		{
			if (!string.IsNullOrEmpty(_capturedPayloadClip))
			{
				adobeAnimateSprite.SetAnimation(_capturedPayloadClip);
			}
			adobeAnimateSprite.frameIndex = Math.Max(0, _capturedPayloadFrame);
			adobeAnimateSprite.pause = true;
		}
		_capturedPayloadNode = nodeOrNull;
		_capturedPayloadIsVisualClone = true;
	}

	private void RemoveCapturedPayloadVisualClone()
	{
		if (_capturedPayloadIsVisualClone && GodotObject.IsInstanceValid(_capturedPayloadNode))
		{
			_capturedPayloadNode.QueueFree();
		}
		_capturedPayloadNode = null;
		_capturedPayloadIsVisualClone = false;
	}

	public override void Block(TowerDefenseCharacter target)
	{
		if (!TowerDefenseManager.HasGameplayAuthority)
		{
			return;
		}
		if (GodotObject.IsInstanceValid(dropTween))
		{
			dropTween.Kill();
		}
		canBlock = false;
		instance.invincible = true;
		if (target.gridPos != base.gridPos)
		{
			base.sprite.SetFliter("bin", open: false);
			if (TowerDefenseManager.HasGameplayAuthority)
			{
				TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("TrashBin");
				if (!GodotObject.IsInstanceValid(packetConfig))
				{
					SendStateEvent("ToRise");
					return;
				}
				if (cell.CanPacketPlant(packetConfig))
				{
					TowerDefenseCharacter character = (base.EconomyOwnerAccountId.IsValid ? packetConfig.Plant(base.EconomyOwnerAccountId, base.gridPos) : packetConfig.Plant(base.gridPos));
					if (GodotObject.IsInstanceValid(character))
					{
						TowerDefenseManager.PublishSpawnedCharacter("TrashBin", character, useCreate: false);
					}
				}
			}
		}
		SendStateEvent("ToRise");
	}
}
