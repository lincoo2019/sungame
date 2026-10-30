using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantPotatoSquash : TowerDefensePlant
{
	private PotatoComponent _potatoComponent;

	private SquashComponent _squashComponent;

	private AttackComponent _attackComponent;

	private double _readyTime = 7.5;

	[Export(PropertyHint.None, "")]
	public double readyTime
	{
		get
		{
			return _readyTime;
		}
		set
		{
			_readyTime = value;
			if (!IsNodeReady())
			{
				return;
			}
			PotatoComponent potatoComponent = _potatoComponent;
			if (potatoComponent != null && !potatoComponent.IsReleased)
			{
				PotatoComponent potatoComponent2 = _potatoComponent;
				if (potatoComponent2 != null && !potatoComponent2.IsReleased)
				{
					_potatoComponent.readyTime = (float)value;
				}
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_potatoComponent = componentManager.GetRuntime<PotatoComponent>();
			PotatoComponent potatoComponent = _potatoComponent;
			if (potatoComponent != null && !potatoComponent.IsReleased)
			{
				_potatoComponent.readyTime = (float)readyTime;
			}
			_squashComponent = componentManager.GetRuntime<SquashComponent>();
			_attackComponent = componentManager.GetRuntime<AttackComponent>("character.attack.0");
			Vector2 vector = TowerDefenseManager.Instance.GetMapGridSize() * new Vector2(5f, 5f);
			_attackComponent?.SetCheckAreaRectangleSize(0, vector);
			_squashComponent?.SetRuntimeCheckRectangleSize(vector);
			_potatoComponent.OnReadyRise += OnReadyRise;
			_squashComponent.OnJumpDownSmash += OnJumpDownSmash;
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		SquashComponent squashComponent = _squashComponent;
		if (squashComponent != null && !squashComponent.IsReleased)
		{
			_squashComponent.OnJumpDownSmash -= OnJumpDownSmash;
		}
		PotatoComponent potatoComponent = _potatoComponent;
		if (potatoComponent != null && !potatoComponent.IsReleased)
		{
			_potatoComponent.OnReadyRise -= OnReadyRise;
		}
	}

	public void ReadyRise()
	{
		PotatoComponent potatoComponent = _potatoComponent;
		if (potatoComponent != null && !potatoComponent.IsReleased)
		{
			_potatoComponent.ReadyRise();
		}
	}

	private void OnReadyRise()
	{
		_squashComponent.SetAlive(alive: true);
		instance.biteHurt = 0.0;
		Idle();
		base.sprite.SetAnimation(_potatoComponent.chargeAnimeClips);
	}

	private void OnJumpDownSmash()
	{
		AudioManager.Instance.AudioPlay("MineExplosion");
		base.sprite.SetAnimation("Mashed", loop: false);
		if (TowerDefenseManager.HasGameplayAuthority && GodotObject.IsInstanceValid(packet))
		{
			TowerDefenseCharacter towerDefenseCharacter = (base.HasEconomyOwner ? packet.Plant(base.EconomyOwnerAccountId, base.gridPos, playAudio: false) : packet.Plant(base.gridPos, playAudio: false));
			if (GodotObject.IsInstanceValid(towerDefenseCharacter) && instance.hypnoses)
			{
				towerDefenseCharacter.Hypnoses();
			}
			if (GodotObject.IsInstanceValid(towerDefenseCharacter))
			{
				Dictionary spawnState = new Dictionary { ["plant_play_audio"] = false };
				TowerDefenseManager.PublishSpawnedCharacter(packet.saveKey, towerDefenseCharacter, useCreate: false, 0.0, walkAfterSpawn: false, "", spawnState);
			}
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary { ["readyTime"] = readyTime };
	}

	public override void ImportVariantSave(Dictionary data)
	{
		readyTime = (data.ContainsKey("readyTime") ? data["readyTime"].AsDouble() : 7.5);
	}
}
