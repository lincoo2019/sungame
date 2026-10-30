using Godot;

public sealed class EffectCreateComponent : CharacterComponentRuntime
{
	private EffectCreateComponentDefinition Definition => base.ComponentDefinition as EffectCreateComponentDefinition;

	public TowerDefenseEffectParticlesOnce CreateDirt()
	{
		return CreateEffect<TowerDefenseEffectParticlesOnce>(ObjectManagerConfig.OBJECT.PARTICLES_RISE_DIRT, applyLimit: true);
	}

	public TowerDefenseEffectSpriteOnce CreateSplash()
	{
		return CreateEffect<TowerDefenseEffectSpriteOnce>(ObjectManagerConfig.OBJECT.PARTICLES_SPLASH, applyLimit: true);
	}

	public TowerDefenseEffectParticlesOnce CreateIceTrap()
	{
		return CreateEffect<TowerDefenseEffectParticlesOnce>(ObjectManagerConfig.OBJECT.PARTICLES_ICE_TRAP, Definition?.applyLimitToIceTrap ?? false);
	}

	private T CreateEffect<T>(ObjectManagerConfig.OBJECT effectType, bool applyLimit) where T : TowerDefenseEffectBase
	{
		TowerDefenseCharacter owner = base.Owner;
		if (!base.Alive || base.Lifecycle != ComponentRuntimeLifecycle.Active || !GodotObject.IsInstanceValid(owner) || !CanCreateEffect(applyLimit) || !GodotObject.IsInstanceValid(TowerDefenseGroundItemBase.characterNode))
		{
			return null;
		}
		T val = ObjectManager.PoolPop(effectType, TowerDefenseGroundItemBase.characterNode) as T;
		if (!GodotObject.IsInstanceValid(val))
		{
			return null;
		}
		val.gridPos = owner.gridPos;
		val.GlobalPosition = GetEffectPosition(owner);
		return val;
	}

	private bool CanCreateEffect(bool applyLimit)
	{
		int num = Definition?.maxEffectCount ?? 100;
		if (!applyLimit || num < 0)
		{
			return true;
		}
		if (GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
		{
			return TowerDefenseManager.Instance.GetEffectCount() < num;
		}
		return false;
	}

	private static Vector2 GetEffectPosition(TowerDefenseCharacter owner)
	{
		if (GodotObject.IsInstanceValid(owner.shadowSprite))
		{
			ShadowComponent shadowComponent = owner.shadowComponent;
			if (shadowComponent != null && !shadowComponent.IsReleased)
			{
				return new Vector2(owner.GetLogicalGlobalPosition(owner.shadowSprite).X, owner.shadowComponent.GetShadowPosition().Y);
			}
		}
		return owner.GetLogicalGlobalPosition();
	}
}
