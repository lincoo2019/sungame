using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class BugOverviewIceCobCannonLaunchAudioRuntimeTest : Node
{
	private const string ScenePath = "res://Asset/Anime/Character/Plant/Gold/IceCobCannon/Scene/TowerDefensePlantIceCobCannon.tscn";

	private int _checks;

	private int _failures;

	private bool _sawFire;

	private bool _sawExplosionWrapper;

	private bool _sawExplosionParticles;

	private bool _particlesVisible;

	private bool _particlesEmitting;

	private int _particlesAmount;

	private bool _particlesHaveTexture;

	private bool _particlesHaveProcessMaterial;

	private TowerDefenseEffectParticlesOnce _iceCobCannonParticlesEffect;

	public override async void _Ready()
	{
		TowerDefensePlantIceCobCannon plant = null;
		ProcessModeEnum previousAudioProcessMode = ProcessModeEnum.Inherit;
		Resource previousCobLaunch = null;
		bool hadCobLaunch = false;
		try
		{
			_ = 4;
			try
			{
				plant = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Plant/Gold/IceCobCannon/Scene/TowerDefensePlantIceCobCannon.tscn", null, ResourceLoader.CacheMode.Ignore)?.Instantiate<TowerDefensePlantIceCobCannon>(PackedScene.GenEditState.Disabled);
				Check(GodotObject.IsInstanceValid(plant), "The real Ice Cob Cannon scene must instantiate.");
				if (!GodotObject.IsInstanceValid(plant))
				{
					goto end_IL_0064;
				}
				plant.editorPreviewMode = true;
				plant.inGame = false;
				AddChild(plant, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(3);
				CannonComponent cannon = plant.componentManager?.GetRuntime<CannonComponent>("character.cannon");
				Check(cannon != null && !cannon.IsReleased, "The real Ice Cob Cannon must expose its CannonComponent runtime.");
				Check(GodotObject.IsInstanceValid(cannon?.sprite), "The CannonComponent must bind the real Ice Cob Cannon animation sprite.");
				Check(cannon?.fireEventName == "fire" && cannon.fireReadyAudioName == "CobLaunch", "Ice Cob Cannon must retain the authored fire-event/audio contract.");
				plant.OnCustomSwitched("Custom0");
				Check(cannon?.projectileData?.skinName == new StringName("IceCream"), "The real Custom0 path must select the IceCream projectile skin.");
				TowerDefenseProjectileRegistry.Init();
				Check(TowerDefenseProjectileRegistry.GetProjectile("IceCobCannonCob")?.hitEffect != null, "The projectile registry copy must retain IceCobCannonCob's hit effect.");
				Check((cannon?.projectileData?.BuildConfig())?.hitEffect != null, "IceCobCannonCob must retain its configured landing effect.");
				BulletField bulletField = BulletField.EnsureMountedOnCharacterNode();
				if (!GodotObject.IsInstanceValid(bulletField))
				{
					TowerDefenseManager.GetCharacterNode().AddChild(new BulletField(), forceReadableName: false, InternalMode.Disabled);
					await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
					bulletField = BulletField.Instance;
				}
				Check(GodotObject.IsInstanceValid(bulletField) && BulletField.Instance == bulletField, "The production BulletField path must be mounted.");
				hadCobLaunch = ResourceManager.Instance?.AUDIOS.TryGetValue("CobLaunch", out previousCobLaunch) ?? false;
				if (!hadCobLaunch && GodotObject.IsInstanceValid(ResourceManager.Instance))
				{
					ResourceManager.Instance.AUDIOS["CobLaunch"] = ResourceLoader.Load<AudioStream>("uid://dg4whq0aisres", null, ResourceLoader.CacheMode.Ignore);
				}
				Check(ResourceManager.Instance?.AUDIOS.ContainsKey("CobLaunch") ?? false, "The production CobLaunch audio resource must be registered.");
				Check(GodotObject.IsInstanceValid(AudioManager.Instance), "The production AudioManager autoload must be available.");
				if (cannon == null || cannon.IsReleased || !GodotObject.IsInstanceValid(cannon.sprite) || !GodotObject.IsInstanceValid(AudioManager.Instance))
				{
					goto end_IL_0064;
				}
				previousAudioProcessMode = AudioManager.Instance.ProcessMode;
				AudioManager.Instance.ProcessMode = ProcessModeEnum.Disabled;
				AudioManager.Instance.audioStack.Clear();
				Node2D characterNode = TowerDefenseManager.GetCharacterNode();
				Check(GodotObject.IsInstanceValid(characterNode), "The production character/effect node must exist.");
				cannon.sprite.OnAnimeEvent += OnAnimeEvent;
				characterNode.ChildEnteredTree += OnEffectChildEntered;
				try
				{
					cannon.markerVisualTravelDuration = 0.0;
					cannon.targetPos = new Vector2(300f, 200f);
					cannon.sprite.timeScale = 4.0;
					cannon.sprite.SetAnimation(cannon.fireAnimeClips, loop: false);
					for (int frame = 0; frame < 180; frame++)
					{
						if (ObservedCompleteExplosion())
						{
							break;
						}
						await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
						CaptureIceCobCannonParticles(_iceCobCannonParticlesEffect);
					}
				}
				finally
				{
					cannon.sprite.OnAnimeEvent -= OnAnimeEvent;
					characterNode.ChildEnteredTree -= OnEffectChildEntered;
				}
				Check(_sawFire, "The real Fire timeline must naturally emit fire.");
				Check(_sawExplosionWrapper, "The natural Fire animation path must create the Ice Cob Cannon landing effect.");
				Check(_sawExplosionParticles, "The landing effect must create the concrete IceCobCannonExplosion particles.");
				Check(_particlesVisible, "The concrete IceCobCannonExplosion particles must be visible.");
				Check(_particlesEmitting, "The concrete IceCobCannonExplosion particles must be emitting.");
				Check(_particlesAmount == 48, "The concrete IceCobCannonExplosion particles must keep amount 48.");
				Check(_particlesHaveTexture, "The concrete IceCobCannonExplosion particles must have a texture.");
				Check(_particlesHaveProcessMaterial, "The concrete IceCobCannonExplosion particles must have a process material.");
				int num = AudioManager.Instance.audioStack.Count((AudioManager.AudioRequest request) => request.Stream == "CobLaunch");
				GD.Print($"ICE_COB_CANNON_AUDIO_DIAGNOSTIC count={num} sameSprite={cannon.sprite == plant.sprite}");
				Check(num == 1, $"The natural fire event must enqueue CobLaunch exactly once; got {num}.");
				await WaitForPhysicsBatchRemainder(1uL);
				AudioManager.Instance._PhysicsProcess(0.0);
				Check(AudioManager.Instance.audioStack.Count((AudioManager.AudioRequest request) => request.Stream == "CobLaunch") == 1, "A non-boundary physics frame must retain the pending SFX batch.");
				await WaitForPhysicsBatchRemainder(0uL);
				AudioManager.Instance._PhysicsProcess(0.0);
				Check(AudioManager.Instance.audioStack.Count == 0, "The fourth physics frame must submit and clear the pending SFX batch.");
				goto end_IL_0045;
				end_IL_0064:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewIceCobCannonLaunchAudioRuntimeTest] Unexpected exception: {value}");
				goto end_IL_0045;
			}
			return;
			end_IL_0045:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(AudioManager.Instance))
			{
				AudioManager.Instance.ProcessMode = previousAudioProcessMode;
			}
			if (GodotObject.IsInstanceValid(ResourceManager.Instance))
			{
				if (hadCobLaunch)
				{
					ResourceManager.Instance.AUDIOS["CobLaunch"] = previousCobLaunch;
				}
				else
				{
					ResourceManager.Instance.AUDIOS.Remove("CobLaunch");
				}
			}
			if (GodotObject.IsInstanceValid(plant))
			{
				plant.QueueFree();
			}
			await WaitFrames(2);
		}
		bool flag = _failures == 0 && _checks == 22;
		GD.Print($"ICE_COB_CANNON_LAUNCH_AUDIO_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private void OnEffectChildEntered(Node child)
	{
		if (child is TowerDefenseProjectileEffectIceCobCannonExplode)
		{
			_sawExplosionWrapper = true;
		}
		if (child is TowerDefenseEffectParticlesOnce effect)
		{
			CaptureIceCobCannonParticles(effect);
		}
	}

	private void OnAnimeEvent(string command, Variant argument)
	{
		if (command == "fire")
		{
			_sawFire = true;
		}
	}

	private void CaptureIceCobCannonParticles(TowerDefenseEffectParticlesOnce effect)
	{
		if (GodotObject.IsInstanceValid(effect) && GodotObject.IsInstanceValid(effect.particles) && !(effect.particles.Name != (StringName)"IceCobCannonExplosion"))
		{
			_iceCobCannonParticlesEffect = effect;
			GPUParticles2DOnece particles = effect.particles;
			_sawExplosionParticles = true;
			_particlesVisible |= particles.Visible;
			_particlesEmitting |= particles.Emitting;
			_particlesAmount = particles.Amount;
			_particlesHaveTexture |= particles.Texture != null;
			_particlesHaveProcessMaterial |= particles.ProcessMaterial != null;
		}
	}

	private bool ObservedCompleteExplosion()
	{
		if (_sawFire && _sawExplosionWrapper && _sawExplosionParticles && _particlesVisible && _particlesEmitting && _particlesAmount == 48 && _particlesHaveTexture)
		{
			return _particlesHaveProcessMaterial;
		}
		return false;
	}

	private async Task WaitFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private async Task WaitForPhysicsBatchRemainder(ulong remainder)
	{
		while (Engine.GetPhysicsFrames() % 4 != remainder)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[BugOverviewIceCobCannonLaunchAudioRuntimeTest] " + message);
		}
	}
}
