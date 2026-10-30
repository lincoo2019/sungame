using System.Collections.Generic;
using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantGhostbuster : TowerDefensePlant
{
	public bool over;

	private GravebusterComponent _gravebusterComponent;

	private readonly HashSet<TowerDefenseCharacter> _ghostOverlaps = new HashSet<TowerDefenseCharacter>();

	private readonly HashSet<TowerDefenseCharacter> _ghostScratch = new HashSet<TowerDefenseCharacter>();

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_gravebusterComponent = componentManager.GetRuntime<GravebusterComponent>();
			if (_gravebusterComponent != null)
			{
				_gravebusterComponent.OnOver += GravebusterOver;
			}
			instance.canBeCollection = false;
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		GravebusterComponent gravebusterComponent = _gravebusterComponent;
		if (gravebusterComponent != null && !gravebusterComponent.IsReleased)
		{
			_gravebusterComponent.OnOver -= GravebusterOver;
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (!Engine.IsEditorHint())
		{
			ProcessGhostOverlaps();
		}
	}

	private void ProcessGhostOverlaps()
	{
		_ghostScratch.Clear();
		if (!GodotObject.IsInstanceValid(TowerDefenseManager.Instance.currentControl) || !TowerDefenseManager.Instance.currentControl.isGameRunning || !inGame || nearDie || die || !TryGetActiveWorldHitRect(out var rect))
		{
			return;
		}
		List<TowerDefenseCharacter> charactersIntersectingRectListExcludingCamp = TowerDefenseManager.Instance.characterRegistry.GetCharactersIntersectingRectListExcludingCamp(rect, base.camp);
		for (int i = 0; i < charactersIntersectingRectListExcludingCamp.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = charactersIntersectingRectListExcludingCamp[i];
			if (GodotObject.IsInstanceValid(towerDefenseCharacter) && !(towerDefenseCharacter.config.name != "ZombieGhost") && towerDefenseCharacter.camp != base.camp)
			{
				_ghostScratch.Add(towerDefenseCharacter);
				if (!_ghostOverlaps.Contains(towerDefenseCharacter))
				{
					HandleGhostEntered(towerDefenseCharacter);
				}
			}
		}
		_ghostOverlaps.RemoveWhere((TowerDefenseCharacter character) => !GodotObject.IsInstanceValid(character) || !_ghostScratch.Contains(character));
		foreach (TowerDefenseCharacter item in _ghostScratch)
		{
			_ghostOverlaps.Add(item);
		}
	}

	private void HandleGhostEntered(TowerDefenseCharacter character)
	{
		if (GodotObject.IsInstanceValid(character))
		{
			character.Hurt(100000.0);
		}
	}

	public async void GravebusterOver(TowerDefenseGravestone graveStone)
	{
		if (over)
		{
			return;
		}
		over = true;
		if (Global.Instance.isMultiplayerMode && !MultiPlayerManager.Instance.isHost)
		{
			return;
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("ZombieGhost");
		Vector2 logicalPosition = GetLogicalGlobalPosition();
		TowerDefenseCharacter zombie = packetConfig.Create(logicalPosition, base.gridPos, base.groundHeight);
		TowerDefenseGroundItemBase.characterNode.AddChild(zombie, forceReadableName: false, InternalMode.Disabled);
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		if (GodotObject.IsInstanceValid(zombie))
		{
			((TowerDefenseZombie)zombie).Walk();
			if (!instance.hypnoses)
			{
				zombie.Hypnoses();
			}
			zombie.instance.hitpoints = graveStone.instance.hitpoints;
		}
		if (Global.Instance.isMultiplayerMode && MultiPlayerManager.Instance.isHost)
		{
			TowerDefenseControlNew currentControl = TowerDefenseManager.Instance.currentControl;
			if (GodotObject.IsInstanceValid(currentControl))
			{
				int nextSyncId = currentControl.GetNextSyncId();
				currentControl.RegisterSyncCharacter(nextSyncId, zombie);
				MultiPlayerManager.Instance.SendSpawnCharacterAt("ZombieGhost", base.gridPos.X, base.gridPos.Y, nextSyncId, 1.0, 1.0, hypnoses: true, 0.0, useCreate: true, logicalPosition.X, logicalPosition.Y, walkAfterSpawn: true, base.groundHeight);
			}
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary { ["over"] = over };
	}

	public override void ImportVariantSave(Dictionary data)
	{
		over = data.ContainsKey("over") && data["over"].AsBool();
	}
}
