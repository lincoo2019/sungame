using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseItemGatlingTX : TowerDefenseItem
{
	private sealed class PendingFireSequence
	{
		public long EventSequence;

		public int NextWave;

		public double DelayRemaining;

		public SceneTreeTimer DelayTimer;

		public int LifecycleVersion;

		public bool Running;
	}

	private const int FireWaveCount = 3;

	private const int ProjectilesPerWave = 5;

	private const double FireWaveDelay = 0.05;

	private FireComponent fireComponent;

	private readonly List<PendingFireSequence> _pendingFireSequences = new List<PendingFireSequence>();

	private long _fireEventSequence;

	private int _fireLifecycleVersion;

	[Export(PropertyHint.None, "")]
	public string projectileName = "Pea";

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint() && !editorPreviewMode)
		{
			fireComponent = componentManager.GetRuntime<FireComponent>("character.fire");
			HitBoxDestroy();
			base.sprite.SetAnimation("Fire", loop: false);
		}
	}

	public override void _ExitTree()
	{
		CancelPendingFireSequences();
		base._ExitTree();
	}

	public override void IdleProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 0.5;
	}

	public override void AnimeEvent(string command, Variant argument)
	{
		base.AnimeEvent(command, argument);
		if (!(command != "fire") && CanContinueFireSequence())
		{
			PendingFireSequence pendingFireSequence = new PendingFireSequence
			{
				EventSequence = ++_fireEventSequence,
				NextWave = 0,
				DelayRemaining = 0.0
			};
			_pendingFireSequences.Add(pendingFireSequence);
			StartPendingFireSequence(pendingFireSequence);
		}
	}

	private void StartPendingFireSequence(PendingFireSequence context)
	{
		if (context != null && !context.Running && context.NextWave < 3)
		{
			context.Running = true;
			context.LifecycleVersion = _fireLifecycleVersion;
			if (context.DelayRemaining > 0.0)
			{
				ResumePendingFireSequenceAfterDelay(context);
			}
			else
			{
				RunPendingFireSequence(context);
			}
		}
	}

	private async void ResumePendingFireSequenceAfterDelay(PendingFireSequence context)
	{
		if (!CanContinueFireSequence(context.LifecycleVersion))
		{
			RemovePendingFireSequence(context);
			return;
		}
		SceneTree tree = GetTree();
		if (!GodotObject.IsInstanceValid(tree))
		{
			RemovePendingFireSequence(context);
			return;
		}
		context.DelayTimer = tree.CreateTimer(context.DelayRemaining);
		await ToSignal(context.DelayTimer, SceneTreeTimer.SignalName.Timeout);
		context.DelayTimer = null;
		context.DelayRemaining = 0.0;
		if (!CanContinueFireSequence(context.LifecycleVersion))
		{
			RemovePendingFireSequence(context);
		}
		else
		{
			RunPendingFireSequence(context);
		}
	}

	private async void RunPendingFireSequence(PendingFireSequence context)
	{
		while (context.NextWave < 3)
		{
			if (!CanContinueFireSequence(context.LifecycleVersion))
			{
				RemovePendingFireSequence(context);
				return;
			}
			int nextWave = context.NextWave;
			EmitFireWave(context.EventSequence, nextWave);
			context.NextWave = nextWave + 1;
			if (context.NextWave >= 3)
			{
				break;
			}
			context.DelayRemaining = 0.05;
			SceneTree tree = GetTree();
			if (!GodotObject.IsInstanceValid(tree))
			{
				RemovePendingFireSequence(context);
				return;
			}
			context.DelayTimer = tree.CreateTimer(context.DelayRemaining);
			await ToSignal(context.DelayTimer, SceneTreeTimer.SignalName.Timeout);
			context.DelayTimer = null;
			context.DelayRemaining = 0.0;
		}
		RemovePendingFireSequence(context);
	}

	private void EmitFireWave(long eventSequence, int waveIndex)
	{
		if (!CanContinueFireSequence())
		{
			return;
		}
		using RandomNumberGenerator randomNumberGenerator = new RandomNumberGenerator
		{
			Seed = NetworkDeterministicSeed.ForCharacterEvent(syncId, eventSequence, 111624470uL)
		};
		int num = waveIndex * 5;
		for (int i = 0; i < num; i++)
		{
			randomNumberGenerator.RandfRange(300f, 800f);
			randomNumberGenerator.RandfRange(-30f, 20f);
			randomNumberGenerator.RandfRange(-30f, 20f);
		}
		float num2 = (instance.hypnoses ? (-1f) : 1f);
		bool flag = Global.IsMultiplayerMode && !MultiPlayerManager.IsHost;
		for (int j = 0; j < 5; j++)
		{
			AudioManager.Instance.AudioPlay("ProjectileThrow");
			Vector2 velocity = new Vector2(randomNumberGenerator.RandfRange(300f, 800f) * num2, randomNumberGenerator.RandfRange(-30f, 20f));
			BulletFieldSpawnOverrides overrides = new BulletFieldSpawnOverrides
			{
				gridYOverride = base.gridPos.Y,
				flipXOverride = (num2 < 0f),
				suppressGameplay = flag
			};
			fireComponent.CreateProjectileByData(0, velocity, fireComponent.fireCheckList[0].projectile.GetProjetile(), (!flag) ? (-1) : 0, base.camp, new Vector2(0f, randomNumberGenerator.RandfRange(-30f, 20f)), overrides);
		}
	}

	private bool CanContinueFireSequence()
	{
		if (inGame && !editorPreviewMode && IsInsideTree() && !isDestroy && GodotObject.IsInstanceValid(instance) && GodotObject.IsInstanceValid(TowerDefenseManager.CurrentControl))
		{
			FireComponent obj = fireComponent;
			if (obj != null && obj.Lifecycle == ComponentRuntimeLifecycle.Active)
			{
				return fireComponent.parent == this;
			}
		}
		return false;
	}

	private bool CanContinueFireSequence(int lifecycleVersion)
	{
		if (lifecycleVersion == _fireLifecycleVersion)
		{
			return CanContinueFireSequence();
		}
		return false;
	}

	private void RemovePendingFireSequence(PendingFireSequence context)
	{
		context.Running = false;
		context.DelayTimer = null;
		context.DelayRemaining = 0.0;
		_pendingFireSequences.Remove(context);
	}

	private double GetRemainingDelay(PendingFireSequence context)
	{
		if (context.DelayTimer != null && GodotObject.IsInstanceValid(context.DelayTimer))
		{
			return Math.Max(0.0, context.DelayTimer.TimeLeft);
		}
		return Math.Max(0.0, context.DelayRemaining);
	}

	private void CancelPendingFireSequences()
	{
		_fireLifecycleVersion++;
		foreach (PendingFireSequence pendingFireSequence in _pendingFireSequences)
		{
			pendingFireSequence.Running = false;
			pendingFireSequence.DelayTimer = null;
		}
		_pendingFireSequences.Clear();
	}

	public override Dictionary ExportVariantSave()
	{
		Dictionary dictionary = base.ExportVariantSave();
		Array<Dictionary> array = new Array<Dictionary>();
		foreach (PendingFireSequence pendingFireSequence in _pendingFireSequences)
		{
			if (pendingFireSequence.NextWave >= 1 && pendingFireSequence.NextWave < 3)
			{
				array.Add(new Dictionary
				{
					["eventSequence"] = pendingFireSequence.EventSequence,
					["nextWave"] = pendingFireSequence.NextWave,
					["delayRemaining"] = GetRemainingDelay(pendingFireSequence)
				});
			}
		}
		dictionary["fireEventSequence"] = _fireEventSequence;
		dictionary["pendingFireSequences"] = array;
		return dictionary;
	}

	public override bool ImportVariantSaveWhenEmpty()
	{
		return true;
	}

	public override void ImportVariantSave(Dictionary data)
	{
		base.ImportVariantSave(data);
		CancelPendingFireSequences();
		_fireEventSequence = Math.Max(0L, data.GetValueOrDefault("fireEventSequence", 0L).AsInt64());
		foreach (Variant item in data.GetValueOrDefault("pendingFireSequences", new Godot.Collections.Array()).AsGodotArray())
		{
			Dictionary dictionary = item.AsGodotDictionary();
			long num = dictionary.GetValueOrDefault("eventSequence", 0L).AsInt64();
			int num2 = dictionary.GetValueOrDefault("nextWave", 0).AsInt32();
			if (num > 0 && num2 >= 1 && num2 < 3)
			{
				_fireEventSequence = Math.Max(_fireEventSequence, num);
				_pendingFireSequences.Add(new PendingFireSequence
				{
					EventSequence = num,
					NextWave = num2,
					DelayRemaining = Math.Max(0.0, dictionary.GetValueOrDefault("delayRemaining", 0.0).AsDouble())
				});
			}
		}
	}

	public override void FinalizeProgressRestore()
	{
		base.FinalizeProgressRestore();
		CallDeferred("StartRestoredPendingFireSequences");
	}

	private void StartRestoredPendingFireSequences()
	{
		PendingFireSequence[] array = _pendingFireSequences.ToArray();
		foreach (PendingFireSequence context in array)
		{
			StartPendingFireSequence(context);
		}
	}

	public override void AnimeCompleted(string clip)
	{
		if (clip == "Fire")
		{
			Destroy();
		}
	}
}
