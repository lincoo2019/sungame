using System;
using System.Threading.Tasks;
using Godot;

public partial class BugOverviewPlantExplosionAshRuntimeTest : Node
{
	private const string PlantScenePath = "res://Asset/Anime/Character/Plant/Chapter0/PeaShooterSingle/Scene/TowerDefensePlantPeaShooterSingle.tscn";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefensePlant nonLethalPlant = null;
		TowerDefensePlant lethalPlant = null;
		try
		{
			_ = 4;
			try
			{
				PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Plant/Chapter0/PeaShooterSingle/Scene/TowerDefensePlantPeaShooterSingle.tscn", null, ResourceLoader.CacheMode.Ignore);
				Check(GodotObject.IsInstanceValid(packedScene), "The real single Peashooter scene must load.");
				nonLethalPlant = packedScene?.Instantiate<TowerDefensePlant>(PackedScene.GenEditState.Disabled);
				lethalPlant = packedScene?.Instantiate<TowerDefensePlant>(PackedScene.GenEditState.Disabled);
				Check(GodotObject.IsInstanceValid(nonLethalPlant) && GodotObject.IsInstanceValid(lethalPlant), "The control and lethal fixtures must instantiate real plants.");
				if (!GodotObject.IsInstanceValid(nonLethalPlant) || !GodotObject.IsInstanceValid(lethalPlant))
				{
					return;
				}
				PreparePlant(nonLethalPlant, "NonLethalPeashooter");
				PreparePlant(lethalPlant, "LethalPeashooter");
				AddChild(nonLethalPlant, forceReadableName: false, InternalMode.Disabled);
				AddChild(lethalPlant, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(3);
				Check(nonLethalPlant.config?.name == "PlantPeaShooterSingle" && lethalPlant.config?.name == "PlantPeaShooterSingle" && nonLethalPlant.config.ashScene == null && lethalPlant.config.ashScene == null, "The regression must use ordinary plants without a special ash scene.");
				BugOverviewPlantExplosionAshRuntimeTest bugOverviewPlantExplosionAshRuntimeTest = this;
				DestroyComponent destroyComponent = nonLethalPlant.destroyComponent;
				int condition;
				if (destroyComponent != null && !destroyComponent.IsReleased)
				{
					destroyComponent = lethalPlant.destroyComponent;
					if (destroyComponent != null && !destroyComponent.IsReleased)
					{
						ShaderEffectComponent shaderEffectComponent = nonLethalPlant.shaderEffectComponent;
						if (shaderEffectComponent != null && !shaderEffectComponent.IsReleased)
						{
							shaderEffectComponent = lethalPlant.shaderEffectComponent;
							condition = ((shaderEffectComponent != null && !shaderEffectComponent.IsReleased) ? 1 : 0);
							goto IL_0267;
						}
					}
				}
				condition = 0;
				goto IL_0267;
				IL_0267:
				bugOverviewPlantExplosionAshRuntimeTest.Check((byte)condition != 0, "Both real plants must bind the production destroy and shader runtimes.");
				int ashFlag = ShaderEffectComponent.ShaderEffectFlags["ash"];
				Check((nonLethalPlant.shaderEffectComponent.GetEffectFlags() & ashFlag) == 0 && (lethalPlant.shaderEffectComponent.GetEffectFlags() & ashFlag) == 0 && !nonLethalPlant.sprite.pause && !lethalPlant.sprite.pause, "Both fixtures must begin alive without an ash visual.");
				nonLethalPlant.instance.invincible = false;
				double hitpoints = nonLethalPlant.instance.hitpoints;
				double num = nonLethalPlant.ExplodeHurt(1.0, TowerDefenseEnum.EXPLOSION_DAMAGE_KIND.BOMB, playSplatAudio: false);
				Check(num >= 0.0 && Math.Abs(nonLethalPlant.instance.hitpoints - (hitpoints - 1.0)) < 0.001 && nonLethalPlant.instance.hitpoints > 0.0, $"The control explosion must be non-lethal; damage={num}, hp={nonLethalPlant.instance.hitpoints}.");
				await WaitFrames(2);
				Check(!nonLethalPlant.isDestroy && !nonLethalPlant.die && (nonLethalPlant.shaderEffectComponent.GetEffectFlags() & ashFlag) == 0 && !nonLethalPlant.sprite.pause, "A non-lethal explosion must not create a false ash corpse.");
				string liveClip = lethalPlant.sprite?.clip ?? string.Empty;
				Check(!string.IsNullOrEmpty(liveClip), "The lethal fixture must begin on a real authored animation clip.");
				lethalPlant.instance.invincible = false;
				double num2 = lethalPlant.ExplodeHurt(lethalPlant.instance.hitpoints + 1.0, TowerDefenseEnum.EXPLOSION_DAMAGE_KIND.BOMB, playSplatAudio: false);
				Check(num2 > 0.0 && lethalPlant.instance.hitpoints <= 0.0, $"The real bomb hit must be lethal; damage={num2}, hp={lethalPlant.instance.hitpoints}.");
				Check(lethalPlant.die && lethalPlant.isDestroy && !lethalPlant.isExplode, "Lethal explosion damage must enter destruction while restoring the transient damage flag.");
				await WaitFrames(2);
				bool flag = GodotObject.IsInstanceValid(lethalPlant) && !lethalPlant.IsQueuedForDeletion();
				Check(flag, "The exploded plant must remain visible during the authored ash delay.");
				Check(flag && (lethalPlant.shaderEffectComponent.GetEffectFlags() & ashFlag) != 0, "The exploded ordinary plant must enable the production ash render flag.");
				Check(flag && lethalPlant.sprite.pause && lethalPlant.sprite.clip == liveClip, "The ash corpse must freeze the real plant silhouette on " + liveClip + ".");
				Check(flag && GodotObject.IsInstanceValid(lethalPlant.shadowSprite) && lethalPlant.shadowSprite.Visible, "Ash death must remain distinct from smash death and keep the plant shadow visible.");
				await WaitSeconds(1.05);
				await WaitFrames(3);
				Check(!GodotObject.IsInstanceValid(lethalPlant) || lethalPlant.IsQueuedForDeletion(), "The ash corpse must be released after the configured one-second delay.");
				goto end_IL_0036;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[PlantExplosionAsh] Unexpected exception: {value}");
				goto end_IL_0036;
			}
			end_IL_0036:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(nonLethalPlant))
			{
				nonLethalPlant.QueueFree();
			}
			if (GodotObject.IsInstanceValid(lethalPlant))
			{
				lethalPlant.QueueFree();
			}
			await WaitFrames(3);
		}
		bool flag2 = _failures == 0 && _checks == 15;
		GD.Print($"PLANT_EXPLOSION_ASH_RESULT passed={flag2} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag2) ? 2 : 0);
	}

	private static void PreparePlant(TowerDefensePlant plant, string name)
	{
		plant.Name = name;
		plant.inGame = false;
		plant.ProcessMode = ProcessModeEnum.Disabled;
	}

	private async Task WaitFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private async Task WaitSeconds(double seconds)
	{
		await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[PlantExplosionAsh] " + message);
		}
	}
}
