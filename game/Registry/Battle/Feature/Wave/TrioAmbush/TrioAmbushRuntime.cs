using System;
using System.Collections.Generic;
using Godot;

public partial class TrioAmbushRuntime : Node
{
	private sealed class Ticket
	{
		public int Operation;

		public double Due;

		public bool Coral;
	}

	public TowerDefenseBattleFeatureWave Wave;

	private BattleEventBus _bus;

	private readonly List<Ticket> _tickets = new List<Ticket>();

	private readonly List<TrioAmbushMember> _members = new List<TrioAmbushMember>();

	public TrioAmbushClock Clock { get; } = new TrioAmbushClock();

	public override void _Ready()
	{
		base.ProcessPhysicsPriority = 10000;
		base.ProcessMode = ProcessModeEnum.Pausable;
		_bus = BattleEventBus.Instance;
		if (GodotObject.IsInstanceValid(_bus))
		{
			_bus.OnColdEffectEmit += OnCold;
		}
	}

	private void OnCold()
	{
		TowerDefenseBattleFeatureWave wave = Wave;
		if (wave != null && wave.IsLifetimeActive)
		{
			Clock.Freeze();
		}
	}

	public void Schedule(bool coral)
	{
		TowerDefenseBattleFeatureWave wave = Wave;
		if (wave != null && wave.IsLifetimeActive && (!Global.IsMultiplayerMode || MultiPlayerManager.IsHost))
		{
			_tickets.Add(new Ticket
			{
				Operation = Wave.BeginPendingSpawnOperation(),
				Due = Clock.Time + 2.0,
				Coral = coral
			});
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		TowerDefenseBattleFeatureWave wave = Wave;
		if (wave == null || !wave.IsLifetimeActive || !GodotObject.IsInstanceValid(Wave.control))
		{
			Cancel();
		}
		else
		{
			if (!Wave.control.isGameRunning || GetTree().Paused)
			{
				return;
			}
			CommandManager instance = CommandManager.Instance;
			if (instance != null && instance.debug && instance.debugWavePaused)
			{
				return;
			}
			Clock.Advance(delta);
			for (int num = _tickets.Count - 1; num >= 0; num--)
			{
				Ticket ticket = _tickets[num];
				if (!(ticket.Due - Clock.Time > 1E-09))
				{
					_tickets.RemoveAt(num);
					try
					{
						if (Wave.IsPendingSpawnOperationCurrent(ticket.Operation) && !Clock.Frozen && !Wave.control.levelControl.awardCreate)
						{
							SpawnGroup(ticket.Coral);
						}
					}
					catch (Exception value)
					{
						GD.PushError($"[TrioAmbush] Spawn failed: {value}");
					}
					finally
					{
						Wave.CompletePendingSpawnOperation(ticket.Operation);
					}
				}
			}
			_members.RemoveAll((TrioAmbushMember m) => !GodotObject.IsInstanceValid(m));
		}
	}

	private void SpawnGroup(bool coral)
	{
		foreach (Vector2I item in TrioAmbushRules.PickCells(TrioAmbushRules.CandidateCells(TowerDefenseManager.Instance.gridNum, coral, delegate(Vector2I pos)
		{
			TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(pos);
			return GodotObject.IsInstanceValid(mapCell) && (!coral || mapCell.isWater);
		}), coral, (int max) => GD.RandRange(0, max - 1)))
		{
			string text = TrioAmbushRules.PickZombie(GD.RandRange(0, 10999));
			TowerDefenseZombie towerDefenseZombie = TowerDefenseManager.GetPacketConfig(text)?.Plant(item, playAudio: false) as TowerDefenseZombie;
			if (!GodotObject.IsInstanceValid(towerDefenseZombie))
			{
				throw new InvalidOperationException($"Cannot spawn {text} at {item}.");
			}
			int operation = Wave.BeginPendingSpawnOperation();
			TrioAmbushMember trioAmbushMember = TrioAmbushMember.Attach(towerDefenseZombie, coral, (!coral) ? GD.RandRange(3000, 3150) : 0, Wave, operation);
			_members.Add(trioAmbushMember);
			Wave.AddSpawnCharacter(towerDefenseZombie);
			trioAmbushMember.SyncId = TowerDefenseManager.PublishSpawnedCharacter(text, towerDefenseZombie, useCreate: true, 0.0, walkAfterSpawn: false, "", TrioAmbushMember.ExportSpawnState(towerDefenseZombie));
		}
	}

	public void Cancel()
	{
		foreach (Ticket ticket in _tickets)
		{
			Wave?.CompletePendingSpawnOperation(ticket.Operation);
		}
		_tickets.Clear();
		TrioAmbushMember[] array = _members.ToArray();
		foreach (TrioAmbushMember trioAmbushMember in array)
		{
			if (GodotObject.IsInstanceValid(trioAmbushMember))
			{
				trioAmbushMember.CancelEntry();
			}
		}
		_members.Clear();
	}

	public override void _ExitTree()
	{
		if (GodotObject.IsInstanceValid(_bus))
		{
			_bus.OnColdEffectEmit -= OnCold;
		}
		Cancel();
	}
}
