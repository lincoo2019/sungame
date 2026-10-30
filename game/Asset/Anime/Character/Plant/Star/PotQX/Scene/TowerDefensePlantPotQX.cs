using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefensePlantPotQX : TowerDefensePlant
{
	private CharacterTimerComponent _timerComponent;

	private StateHandle _openState;

	private StateHandle _closeState;

	private bool _roleStateSignalsConnected;

	public Array<TowerDefenseCharacter> createCharacterList = new Array<TowerDefenseCharacter>();

	public bool open;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint() && !editorPreviewMode)
		{
			_timerComponent = componentManager.GetRuntime<CharacterTimerComponent>("character.timer");
			_timerComponent.OnTimeout += Timeout;
			base.OnDestroy += HandleOwnerDestroyed;
			_openState = base.StateMachine?.GetStateById("plant.pot_qx.open");
			_closeState = base.StateMachine?.GetStateById("plant.pot_qx.close");
			ConnectRoleStateSignals();
		}
	}

	public override void _ExitTree()
	{
		base.OnDestroy -= HandleOwnerDestroyed;
		DisconnectRoleStateSignals();
		ClearHologramCharacter();
		CharacterTimerComponent timerComponent = _timerComponent;
		if (timerComponent != null && !timerComponent.IsReleased)
		{
			_timerComponent.OnTimeout -= Timeout;
		}
		base._ExitTree();
	}

	private void ConnectRoleStateSignals()
	{
		if (_roleStateSignalsConnected)
		{
			return;
		}
		StateHandle openState = _openState;
		if (openState != null && openState.IsValid)
		{
			StateHandle closeState = _closeState;
			if (closeState != null && closeState.IsValid)
			{
				_openState.Entered += OpenEntered;
				_openState.Exited += OpenExited;
				_openState.PhysicsProcessing += OpenProcessing;
				_closeState.Entered += CloseEntered;
				_closeState.Exited += CloseExited;
				_closeState.PhysicsProcessing += CloseProcessing;
				_roleStateSignalsConnected = true;
			}
		}
	}

	private void DisconnectRoleStateSignals()
	{
		if (_roleStateSignalsConnected)
		{
			_openState.Entered -= OpenEntered;
			_openState.Exited -= OpenExited;
			_openState.PhysicsProcessing -= OpenProcessing;
			_closeState.Entered -= CloseEntered;
			_closeState.Exited -= CloseExited;
			_closeState.PhysicsProcessing -= CloseProcessing;
			_openState = null;
			_closeState = null;
			_roleStateSignalsConnected = false;
		}
	}

	public override void IdleProcessing(double delta)
	{
		base.IdleProcessing(delta);
		if (inGame && TowerDefenseManager.CurrentControl.isGameRunning && !open)
		{
			SendStateEvent("ToOpen");
			open = true;
		}
	}

	public virtual void OpenEntered()
	{
		base.sprite.SetAnimation("Open", loop: false);
	}

	public virtual void OpenProcessing(double delta)
	{
		base.sprite.timeScale = timeScale;
	}

	public virtual void OpenExited()
	{
	}

	public virtual void CloseEntered()
	{
		base.sprite.SetAnimation("Close", loop: false);
	}

	public virtual void CloseProcessing(double delta)
	{
		base.sprite.timeScale = timeScale;
	}

	public virtual void CloseExited()
	{
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (!(clip == "Open"))
		{
			if (clip == "Close")
			{
				ClearHologramCharacter();
				Destroy();
			}
		}
		else
		{
			CreateHologramCharacter();
			Idle();
			_timerComponent.Run("Close", 30.0);
		}
	}

	protected internal override void OnHypnosisStateChanged()
	{
		base.OnHypnosisStateChanged();
		SyncHologramHypnoses();
	}

	private void SyncHologramHypnoses()
	{
		bool hypnoses = instance.hypnoses;
		foreach (TowerDefenseCharacter createCharacter in createCharacterList)
		{
			if (GodotObject.IsInstanceValid(createCharacter) && !createCharacter.isDestroy)
			{
				createCharacter.SyncHologramHypnoses(hypnoses);
			}
		}
	}

	private TowerDefensePacketConfig GetHologramPacket(TowerDefenseCharacter character)
	{
		if (!GodotObject.IsInstanceValid(character))
		{
			return null;
		}
		if (GodotObject.IsInstanceValid(character.packet))
		{
			return character.packet;
		}
		if (!GodotObject.IsInstanceValid(character.config))
		{
			return null;
		}
		TowerDefensePacketConfig packetConfigReadOnlyByCharacterName = TowerDefenseManager.GetPacketConfigReadOnlyByCharacterName(character.config.name);
		if (GodotObject.IsInstanceValid(packetConfigReadOnlyByCharacterName))
		{
			return packetConfigReadOnlyByCharacterName.Duplicate(deep: true) as TowerDefensePacketConfig;
		}
		return TowerDefenseManager.GetPacketConfig(character.config.name);
	}

	public void CreateHologramCharacter()
	{
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
		{
			return;
		}
		Array<TowerDefensePacketConfig> array = new Array<TowerDefensePacketConfig>();
		foreach (TowerDefenseCharacter item in cell.GetCharacterListSave())
		{
			if (item != this && item.config.canCopy && item.camp == base.camp && item.config.plantGridOverrideType == TowerDefenseEnum.PLANTGRIDTYPE.NOONE)
			{
				TowerDefensePacketConfig hologramPacket = GetHologramPacket(item);
				if (GodotObject.IsInstanceValid(hologramPacket))
				{
					array.Add(hologramPacket);
				}
			}
		}
		for (int i = 1; i <= TowerDefenseManager.Instance.GetMapGridNum().Y; i++)
		{
			if (i == base.gridPos.Y)
			{
				continue;
			}
			Vector2I vector2I = new Vector2I(base.gridPos.X, i);
			TowerDefenseCellInstance cellGet = TowerDefenseManager.GetMapCell(vector2I);
			foreach (TowerDefensePacketConfig item2 in array)
			{
				if (!cellGet.CanPacketPlant(item2))
				{
					continue;
				}
				TowerDefenseCharacter character = item2.Plant(vector2I);
				if (!GodotObject.IsInstanceValid(character))
				{
					continue;
				}
				createCharacterList.Add(character);
				bool hypnoses = instance.hypnoses;
				Callable.From(delegate
				{
					if (GodotObject.IsInstanceValid(character))
					{
						character.SetSpriteGroupShaderParameter("hologram", true);
						if (hypnoses)
						{
							character.Hypnoses();
						}
						character.instance.hologram = true;
						character.instance.canBeCollection = false;
						cellGet.ReleaseHologramGridOccupancy(character);
					}
				}).CallDeferred();
				if (Global.IsMultiplayerMode && MultiPlayerManager.IsHost)
				{
					TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
					if (GodotObject.IsInstanceValid(currentControl))
					{
						int nextSyncId = currentControl.GetNextSyncId();
						currentControl.RegisterSyncCharacter(nextSyncId, character);
						MultiPlayerManager.Instance.SendSpawnCharacterAt(item2.saveKey, vector2I.X, vector2I.Y, nextSyncId);
					}
				}
			}
		}
	}

	public void ClearHologramCharacter()
	{
		Array<TowerDefenseCharacter> array = createCharacterList.Duplicate();
		createCharacterList.Clear();
		foreach (TowerDefenseCharacter item in array)
		{
			if (GodotObject.IsInstanceValid(item))
			{
				item.EmitDestroy();
				TowerDefenseManager.Instance.CharacterUnregister(item);
				item.RemoveFromGroup("Character");
				item.QueueFree();
			}
		}
	}

	private void HandleOwnerDestroyed(TowerDefenseCharacter _)
	{
		ClearHologramCharacter();
	}

	public void Timeout(string timerName)
	{
		if (timerName == "Close")
		{
			SendStateEvent("ToClose");
		}
	}

	public override void DestroySet()
	{
		base.DestroySet();
		ClearHologramCharacter();
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
		{
			return;
		}
		TowerDefenseCharacter character = TowerDefenseManager.GetPacketConfig("PlantPot").Plant(base.gridPos);
		if (Global.IsMultiplayerMode && MultiPlayerManager.IsHost)
		{
			TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
			if (GodotObject.IsInstanceValid(currentControl) && GodotObject.IsInstanceValid(character))
			{
				int nextSyncId = currentControl.GetNextSyncId();
				currentControl.RegisterSyncCharacter(nextSyncId, character);
				MultiPlayerManager.Instance.SendSpawnCharacterAt("PlantPot", base.gridPos.X, base.gridPos.Y, nextSyncId);
			}
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary { { "open", open } };
	}

	public override void ImportVariantSave(Dictionary data)
	{
		open = data.GetValueOrDefault("open", Variant.From<bool>(false)).AsBool();
	}
}
