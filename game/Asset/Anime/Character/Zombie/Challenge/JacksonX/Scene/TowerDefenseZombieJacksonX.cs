using System;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombieJacksonX : TowerDefenseZombie, IJackson, INetworkDancerOwner
{
	public DancingComponent dancingComponent;

	private readonly TowerDefenseCharacter[] _pendingNetworkDancers = new TowerDefenseCharacter[4];

	public TowerDefenseCharacter disco;

	public bool isSpawnrackup;

	private string _pendingDiscoName = "";

	private string _dancerPacketName = "";

	private bool HasDancingRuntime
	{
		get
		{
			DancingComponent obj = dancingComponent;
			if (obj == null)
			{
				return false;
			}
			return !obj.IsReleased;
		}
	}

	private bool HasDancingComponent
	{
		get
		{
			if (HasDancingRuntime)
			{
				return dancingComponent.Lifecycle == ComponentRuntimeLifecycle.Active;
			}
			return false;
		}
	}

	[Export(PropertyHint.None, "")]
	public string dancerPacketName
	{
		get
		{
			return _dancerPacketName;
		}
		set
		{
			_dancerPacketName = value;
			if (!string.IsNullOrEmpty(value) && HasDancingComponent)
			{
				dancingComponent.dancerPacketName = value;
			}
		}
	}

	public void RemoveDancer(TowerDefenseCharacter dancer)
	{
		if (HasDancingRuntime)
		{
			dancingComponent.RemoveDancer(dancer);
		}
		for (int i = 0; i < 4; i++)
		{
			if (IsSameDancer(_pendingNetworkDancers[i], dancer))
			{
				_pendingNetworkDancers[i] = null;
			}
		}
	}

	private static bool IsSameDancer(TowerDefenseCharacter candidate, TowerDefenseCharacter dancer)
	{
		if (candidate != dancer)
		{
			if (GodotObject.IsInstanceValid(candidate) && GodotObject.IsInstanceValid(dancer) && candidate.syncId >= 0)
			{
				return candidate.syncId == dancer.syncId;
			}
			return false;
		}
		return true;
	}

	public void SetNetworkDancer(int slot, TowerDefenseCharacter dancer)
	{
		if (slot >= 0 && slot < 4)
		{
			if (!HasDancingRuntime)
			{
				_pendingNetworkDancers[slot] = dancer;
				return;
			}
			dancingComponent.SetNetworkDancer(slot, dancer);
			_pendingNetworkDancers[slot] = null;
		}
	}

	private void ApplyPendingNetworkDancers()
	{
		if (!HasDancingRuntime)
		{
			return;
		}
		for (int i = 0; i < 4; i++)
		{
			TowerDefenseCharacter dancer = _pendingNetworkDancers[i];
			if (GodotObject.IsInstanceValid(dancer))
			{
				SetNetworkDancer(i, dancer);
			}
			else
			{
				_pendingNetworkDancers[i] = null;
			}
		}
	}

	private void ReleaseDancerOwnership()
	{
		if (HasDancingRuntime)
		{
			for (int i = 0; i < 4; i++)
			{
				dancingComponent.SetNetworkDancer(i, null);
			}
		}
		System.Array.Fill(_pendingNetworkDancers, null);
	}

	public override void _ExitTree()
	{
		if (IsQueuedForDeletion())
		{
			ReleaseDancerOwnership();
		}
		base._ExitTree();
	}

	public override void _Ready()
	{
		base._Ready();
		if (Engine.IsEditorHint())
		{
			return;
		}
		dancingComponent = componentManager.GetRuntime<DancingComponent>();
		if (HasDancingComponent)
		{
			base.sprite.OnAnimeCompleted -= dancingComponent.AnimeCompleted;
			base.sprite.OnAnimeEvent -= dancingComponent.AnimeEvent;
			if (!string.IsNullOrEmpty(dancerPacketName))
			{
				dancingComponent.dancerPacketName = dancerPacketName;
			}
		}
		ApplyPendingNetworkDancers();
	}

	public override void WalkEntered()
	{
		base.WalkEntered();
		if (HasDancingComponent)
		{
			dancingComponent.OnWalkEntered();
		}
	}

	public override void WalkProcessing(double delta)
	{
		if (HasDancingComponent)
		{
			groundMoveComponent.SetAlive(dancingComponent.CanWalk());
		}
		base.WalkProcessing(delta);
	}

	public override void Walk()
	{
		if (die)
		{
			SendStateEvent("ToDie");
		}
		else if (!HasDancingComponent || !dancingComponent.OnWalk())
		{
			SendStateEvent("ToWalk");
		}
	}

	public override void AttackProcessing(double delta)
	{
		base.AttackProcessing(delta);
		if (HasDancingComponent)
		{
			dancingComponent.OnAttackProcessing(delta);
		}
	}

	public override void DieProcessing(double delta)
	{
		base.DieProcessing(delta);
		if (HasDancingComponent)
		{
			dancingComponent.OnDieProcessing();
		}
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (!inGame || !TowerDefenseManager.Instance.IsGameRunning() || !HasDancingComponent)
		{
			return;
		}
		if (clip == dancingComponent.pointDownAnimeClip)
		{
			if (isSpawnrackup)
			{
				dancingComponent.isPointing = false;
				dancingComponent.HideSpotlights();
				foreach (TowerDefenseCharacter dancer in dancingComponent.dancerList)
				{
					if (GodotObject.IsInstanceValid(dancer) && !dancer.die && !dancer.nearDie)
					{
						((TowerDefenseZombie)dancer).Walk();
					}
				}
				Walk();
				dancingComponent.RequestIdle();
				isSpawnrackup = false;
			}
			else
			{
				JacksonXPointDownAsync();
			}
		}
		else if (clip == dancingComponent.walkAnimeClip)
		{
			dancingComponent.walkTime--;
			if (!die && !nearDie)
			{
				if (dancingComponent.walkTime > 0)
				{
					return;
				}
				if (dancingComponent.CanSpawnDancer())
				{
					Component();
					dancingComponent.RequestPoint();
					return;
				}
				foreach (TowerDefenseCharacter dancer2 in dancingComponent.dancerList)
				{
					if (GodotObject.IsInstanceValid(dancer2) && !dancer2.die && !dancer2.nearDie && dancer2.sprite.clip == dancingComponent.walkAnimeClip)
					{
						dancer2.SendStateEvent("ToDance");
					}
				}
				Component();
				dancingComponent.RequestDance();
			}
			else
			{
				Die();
			}
		}
		else
		{
			if (!(clip == dancingComponent.armRiseAnimeClip))
			{
				return;
			}
			if (dancingComponent.armRiseFlipSprite)
			{
				base.sprite.Scale = new Vector2(0f - base.sprite.Scale.X, base.sprite.Scale.Y);
			}
			dancingComponent.danceTime--;
			if (!die && !nearDie)
			{
				if (dancingComponent.danceTime > 0)
				{
					return;
				}
				if (dancingComponent.CanSpawnDancer())
				{
					Component();
					dancingComponent.RequestPoint();
					return;
				}
				foreach (TowerDefenseCharacter dancer3 in dancingComponent.dancerList)
				{
					if (GodotObject.IsInstanceValid(dancer3) && !dancer3.die && !dancer3.nearDie && dancer3.sprite.clip == dancingComponent.armRiseAnimeClip)
					{
						((TowerDefenseZombie)dancer3).Walk();
					}
				}
				Walk();
				dancingComponent.RequestIdle();
			}
			else
			{
				Die();
			}
		}
	}

	private async Task JacksonXPointDownAsync()
	{
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		if (GodotObject.IsInstanceValid(disco))
		{
			if (disco is TowerDefenseZombieDisco towerDefenseZombieDisco)
			{
				towerDefenseZombieDisco.moonWalkOver = true;
			}
			disco.SendStateEvent("ToPoint");
		}
		dancingComponent.RequestPoint();
		isSpawnrackup = true;
	}

	public override void AnimeEvent(string command, Variant argument)
	{
		base.AnimeEvent(command, argument);
		if (!HasDancingComponent || !(command == "spawn") || die || nearDie)
		{
			return;
		}
		if (isSpawnrackup)
		{
			dancingComponent.SpawnDancer();
		}
		else
		{
			SpawnDisco();
		}
		if (GodotObject.IsInstanceValid(dancingComponent.spotlight))
		{
			dancingComponent.spotlight.Visible = true;
		}
		if (GodotObject.IsInstanceValid(dancingComponent.spotlight2))
		{
			dancingComponent.spotlight2.Visible = true;
		}
		dancingComponent.ChangeSpotlightColor();
		if (!dancingComponent.firstSpawn)
		{
			dancingComponent.firstSpawn = true;
			if (dancingComponent.spotlightAudioName != "")
			{
				AudioManager.Instance.AudioPlay(dancingComponent.spotlightAudioName);
			}
		}
	}

	public void SpawnDisco()
	{
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
		{
			return;
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("ZombieDisco");
		Vector2I mapGridNum = TowerDefenseManager.Instance.GetMapGridNum();
		Vector2 mapGridSize = TowerDefenseManager.Instance.GetMapGridSize();
		Array<Vector2I> array = new Array<Vector2I>
		{
			new Vector2I(1, 0),
			new Vector2I(-1, 0)
		};
		if (base.gridPos.Y > 1)
		{
			array.Add(new Vector2I(0, -1));
		}
		if (base.gridPos.Y < mapGridNum.Y)
		{
			array.Add(new Vector2I(0, 1));
		}
		Vector2I vector2I = array[GD.RandRange(0, array.Count - 1)];
		double hitpointScale = instance.hitpointScale;
		Vector2 scale = transformPoint.Scale;
		Vector2I vector2I2 = base.gridPos + vector2I;
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		Vector2 pos = ((vector2I.X == 0) ? new Vector2(logicalGlobalPosition.X, (float)TowerDefenseManager.GetMapLineY(base.gridPos.Y + vector2I.Y)) : (logicalGlobalPosition + new Vector2(mapGridSize.X * (float)vector2I.X * 1.25f, 0f)));
		disco = packetConfig.Create(pos, vector2I2);
		TowerDefenseManager.GetCharacterNode().CallDeferred("add_child", disco);
		disco.CallDeferred("SetHitpointAndScale", hitpointScale, scale);
		Callable.From(delegate
		{
			disco.Rise(1.0);
		}).CallDeferred();
		disco.invisible = base.invisible;
		if (instance.hypnoses)
		{
			Callable.From(delegate
			{
				if (GodotObject.IsInstanceValid(disco))
				{
					disco.Hypnoses();
				}
			}).CallDeferred();
		}
		if (Global.IsMultiplayerMode && MultiPlayerManager.IsHost)
		{
			TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
			if (GodotObject.IsInstanceValid(currentControl))
			{
				int nextSyncId = currentControl.GetNextSyncId();
				currentControl.RegisterSyncCharacter(nextSyncId, disco);
				MultiPlayerManager.Instance.SendSpawnCharacterAt("ZombieDisco", vector2I2.X, vector2I2.Y, nextSyncId, hitpointScale, scale.X, instance.hypnoses, 1.0, useCreate: true, pos.X, pos.Y);
			}
		}
	}

	public override void Hypnoses(double time = -1.0, bool canFliter = true, TowerDefenseCharacterBuffHypnoses hypnosesConfig = null)
	{
		base.Hypnoses(time, canFliter, hypnosesConfig);
		ReleaseDancerOwnership();
	}

	public override Dictionary ExportVariantSave()
	{
		Dictionary dictionary = new Dictionary { ["isSpawnBackup"] = isSpawnrackup };
		if (GodotObject.IsInstanceValid(disco))
		{
			dictionary["discoNodeName"] = disco.Name;
		}
		return dictionary;
	}

	public override void ImportVariantSave(Dictionary data)
	{
		isSpawnrackup = data.GetValueOrDefault("isSpawnBackup", false).AsBool();
		if (data.ContainsKey("discoNodeName"))
		{
			_pendingDiscoName = data["discoNodeName"].AsString();
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (!(_pendingDiscoName != ""))
		{
			return;
		}
		Node2D node2D = TowerDefenseManager.GetCharacterNode();
		if (GodotObject.IsInstanceValid(node2D))
		{
			Node nodeOrNull = node2D.GetNodeOrNull(_pendingDiscoName);
			if (GodotObject.IsInstanceValid(nodeOrNull))
			{
				disco = nodeOrNull as TowerDefenseCharacter;
			}
		}
		_pendingDiscoName = "";
	}
}
