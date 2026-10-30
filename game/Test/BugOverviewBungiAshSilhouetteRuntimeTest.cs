using System;
using System.Threading.Tasks;
using Godot;

public partial class BugOverviewBungiAshSilhouetteRuntimeTest : Node
{
	private const string BungiScenePath = "res://Asset/Anime/Character/Zombie/Chapter5/Bungi/Scene/TowerDefenseZombieBungi.tscn";

	private const string NormalConfigPath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Config/TowerDefenseZombieNormal.tres";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseZombieBungi bungi = null;
		try
		{
			_ = 1;
			try
			{
				PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter5/Bungi/Scene/TowerDefenseZombieBungi.tscn", null, ResourceLoader.CacheMode.Ignore);
				Check(GodotObject.IsInstanceValid(packedScene), "The real Bungi scene must load.");
				TowerDefenseZombieConfig towerDefenseZombieConfig = ResourceLoader.Load<TowerDefenseZombieConfig>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Config/TowerDefenseZombieNormal.tres", null, ResourceLoader.CacheMode.Ignore);
				Check(GodotObject.IsInstanceValid(towerDefenseZombieConfig), "The ordinary-zombie control config must load.");
				Check(GodotObject.IsInstanceValid(towerDefenseZombieConfig?.ashScene), "Ordinary zombies must retain the authored GeneralAsh scene.");
				bungi = packedScene?.Instantiate<TowerDefenseZombieBungi>(PackedScene.GenEditState.Disabled);
				Check(GodotObject.IsInstanceValid(bungi), "The real Bungi zombie must instantiate.");
				if (!GodotObject.IsInstanceValid(bungi))
				{
					goto end_IL_004c;
				}
				bungi.inGame = false;
				bungi.ProcessMode = ProcessModeEnum.Disabled;
				bungi.skipDestroySet = true;
				bungi.skipBungeeTarget = true;
				AddChild(bungi, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(3);
				Check(GodotObject.IsInstanceValid(bungi.sprite), "The real Bungi animation sprite must bind.");
				BugOverviewBungiAshSilhouetteRuntimeTest bugOverviewBungiAshSilhouetteRuntimeTest = this;
				DestroyComponent destroyComponent = bungi.destroyComponent;
				int condition;
				if (destroyComponent != null && !destroyComponent.IsReleased)
				{
					ZombieDeathComponent zombieDeathComponent = bungi.zombieDeathComponent;
					condition = ((zombieDeathComponent != null && !zombieDeathComponent.IsReleased) ? 1 : 0);
				}
				else
				{
					condition = 0;
				}
				bugOverviewBungiAshSilhouetteRuntimeTest.Check((byte)condition != 0, "The real Bungi destroy and death components must be active.");
				Check(bungi.config?.ashScene == null, "Bungi must use its own current silhouette instead of ZombieGeneralAsh.");
				string liveClip = bungi.sprite?.clip ?? string.Empty;
				Check(!string.IsNullOrEmpty(liveClip), "The Bungi fixture must begin on a real authored animation clip.");
				bungi.instance.invincible = false;
				double num = bungi.ExplodeHurt(bungi.instance.hitpoints + 1.0, TowerDefenseEnum.EXPLOSION_DAMAGE_KIND.BOMB, playSplatAudio: false);
				Check(num > 0.0 && bungi.instance.hitpoints <= 0.0, $"A real bomb hit must be lethal; damage={num}, hp={bungi.instance.hitpoints}.");
				Check(bungi.die && bungi.isDestroy, "A lethal bomb hit must enter both death and destroy lifecycles.");
				await WaitFrames(2);
				bool flag = GodotObject.IsInstanceValid(bungi.sprite);
				Check(flag && bungi.sprite.pause, "The shader-based ash fallback must freeze the Bungi silhouette.");
				Check(flag && bungi.sprite.clip == liveClip, "Bungi ash must retain its authored silhouette clip " + liveClip + ".");
				goto end_IL_003a;
				end_IL_004c:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewBungiAshSilhouetteRuntimeTest] Unexpected exception: {value}");
				goto end_IL_003a;
			}
			return;
			end_IL_003a:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(bungi))
			{
				bungi.QueueFree();
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		bool flag2 = _failures == 0 && _checks == 12;
		GD.Print($"BUNGI_ASH_SILHOUETTE_RESULT passed={flag2} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag2) ? 2 : 0);
	}

	private async Task WaitFrames(int count)
	{
		for (int index = 0; index < count; index++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[BugOverviewBungiAshSilhouetteRuntimeTest] " + message);
		}
	}
}
