using System;
using System.Threading.Tasks;
using Godot;

public partial class ZombieCobCannonExplosionRuntimeTest : Node
{
	private const string ZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/CobCannon/TowerDefenseZombieNormalCobCannon.tscn";

	private const string CannonHeadPath = "SpriteGroup/TransformPoint/ZombieNormalCobCannon/Head";

	private const string CobCannonExplosionParticleName = "CobCannonExplosion";

	private int _checks;

	private int _failures;

	private bool _sawNaturalFireEvent;

	private bool _sawExplosionWrapper;

	private bool _sawExplosionParticles;

	private bool _particlesVisible;

	private bool _particlesEmitting;

	private int _particlesAmount;

	private bool _particlesHaveTexture;

	private bool _particlesHaveProcessMaterial;

	private TowerDefenseEffectParticlesOnce _cobCannonParticlesEffect;

	private string _expectedFireEventName = "fire";

	public override async void _Ready()
	{
		try
		{
			await VerifyRealZombieCannonExplosion();
		}
		catch (Exception value)
		{
			_failures++;
			GD.PushError($"[ZombieCobCannonExplosionRuntimeTest] Unexpected exception: {value}");
		}
		bool flag = _failures == 0;
		GD.Print($"ZOMBIE_COB_CANNON_EXPLOSION_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private async Task VerifyRealZombieCannonExplosion()
	{
		TowerDefenseZombieNormalCobCannon zombie = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/CobCannon/TowerDefenseZombieNormalCobCannon.tscn", null, ResourceLoader.CacheMode.Ignore)?.Instantiate<TowerDefenseZombieNormalCobCannon>(PackedScene.GenEditState.Disabled);
		Check(GodotObject.IsInstanceValid(zombie), "The real zombie cannon scene must instantiate.");
		if (!GodotObject.IsInstanceValid(zombie))
		{
			return;
		}
		zombie.inGame = false;
		zombie.editorPreviewMode = true;
		AddChild(zombie, forceReadableName: false, InternalMode.Disabled);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		for (int i = 0; i < 4; i++)
		{
			if (GodotObject.IsInstanceValid(BulletField.Instance))
			{
				break;
			}
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
		if (!GodotObject.IsInstanceValid(BulletField.Instance))
		{
			TowerDefenseManager.GetCharacterNode().AddChild(new BulletField(), forceReadableName: false, InternalMode.Disabled);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		CannonComponent cannonComponent = zombie.componentManager?.GetRuntime<CannonComponent>("character.cannon");
		Check(cannonComponent != null && !cannonComponent.IsReleased, "The real zombie cannon must expose its CannonComponent runtime.");
		Check(cannonComponent?.projectileData?.projectileName.ToString() == "CobCannonCob", "The real zombie cannon must use CobCannonCob.");
		TowerDefenseProjectileRegistry.Init();
		Check(TowerDefenseProjectileRegistry.GetProjectile("CobCannonCob")?.hitEffect != null, "The projectile registry copy must retain CobCannonCob's hit effect.");
		Check((cannonComponent?.projectileData?.BuildConfig())?.hitEffect != null, "CobCannonCob must retain its configured explosion hit effect.");
		Check(ResourceLoader.Load<TowerDefenseProjectileData>("res://Registry/Projectile/Config/CannonCob/CobCannonCob.tres", null, ResourceLoader.CacheMode.Ignore)?.hitEffect != null, "The canonical CobCannonCob registry resource must load its hit effect.");
		Check(GodotObject.IsInstanceValid(BulletField.Instance), "The production BulletField path must be mounted.");
		AdobeAnimateSpriteBase head = zombie.GetNodeOrNull<AdobeAnimateSpriteBase>("SpriteGroup/TransformPoint/ZombieNormalCobCannon/Head");
		Check(GodotObject.IsInstanceValid(head), "The real nested cannon Head must exist.");
		if (cannonComponent?.projectileData == null || !GodotObject.IsInstanceValid(BulletField.Instance) || !GodotObject.IsInstanceValid(head))
		{
			return;
		}
		Node2D characterNode = TowerDefenseManager.GetCharacterNode();
		Check(GodotObject.IsInstanceValid(characterNode), "The production character/effect node must exist.");
		characterNode.ChildEnteredTree += OnEffectChildEntered;
		head.OnAnimeEvent += OnHeadAnimeEvent;
		try
		{
			cannonComponent.markerVisualTravelDuration = 0.0;
			cannonComponent.targetPos = new Vector2(300f, 200f);
			_expectedFireEventName = cannonComponent.fireEventName;
			head.timeScale = 1.0;
			head.SetAnimation(cannonComponent.fireAnimeClips, loop: false);
			for (int i = 0; i < 180; i++)
			{
				if (ObservedCompleteExplosion())
				{
					break;
				}
				await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
				CaptureCobCannonParticles(_cobCannonParticlesEffect);
			}
		}
		finally
		{
			head.OnAnimeEvent -= OnHeadAnimeEvent;
			characterNode.ChildEnteredTree -= OnEffectChildEntered;
		}
		Check(_sawNaturalFireEvent, "The real nested cannon Head Fire animation must naturally emit its fire timeline event.");
		Check(_sawExplosionWrapper, "The natural Fire animation path must create TowerDefenseProjectileEffectCobCannonExplode on landing.");
		Check(_sawExplosionParticles, "The cob-cannon wrapper must create the concrete CobCannonExplosion particle effect.");
		Check(_particlesVisible, "The concrete CobCannonExplosion particles must be visible.");
		Check(_particlesEmitting, "The concrete CobCannonExplosion particles must be emitting.");
		Check(_particlesAmount == 48, "The concrete CobCannonExplosion particles must keep amount 48.");
		Check(_particlesHaveTexture, "The concrete CobCannonExplosion particles must have a texture.");
		Check(_particlesHaveProcessMaterial, "The concrete CobCannonExplosion particles must have a process material.");
		zombie.QueueFree();
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
	}

	private void OnEffectChildEntered(Node child)
	{
		if (child is TowerDefenseProjectileEffectCobCannonExplode)
		{
			_sawExplosionWrapper = true;
		}
		if (child is TowerDefenseEffectParticlesOnce effect)
		{
			CaptureCobCannonParticles(effect);
		}
	}

	private void OnHeadAnimeEvent(string command, Variant argument)
	{
		if (command == _expectedFireEventName)
		{
			_sawNaturalFireEvent = true;
		}
	}

	private void CaptureCobCannonParticles(TowerDefenseEffectParticlesOnce effect)
	{
		if (GodotObject.IsInstanceValid(effect) && GodotObject.IsInstanceValid(effect.particles) && !(effect.particles.Name != (StringName)"CobCannonExplosion"))
		{
			_cobCannonParticlesEffect = effect;
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
		if (_sawNaturalFireEvent && _sawExplosionWrapper && _sawExplosionParticles && _particlesVisible && _particlesEmitting && _particlesAmount == 48 && _particlesHaveTexture)
		{
			return _particlesHaveProcessMaterial;
		}
		return false;
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[ZombieCobCannonExplosionRuntimeTest] " + message);
		}
	}
}
