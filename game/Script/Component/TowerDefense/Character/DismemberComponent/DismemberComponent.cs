using System.Collections.Generic;
using Godot;
using Godot.Collections;

public sealed class DismemberComponent : CharacterComponentRuntime
{
	private const string DefaultDefinitionPath = "res://Script/Component/TowerDefense/Character/DismemberComponent/DismemberComponentDefinition.tres";

	public static bool dismemberEnabled;

	public Vector2 velocityXRange = new Vector2(-150f, 150f);

	public Vector2 velocityYRange = new Vector2(-450f, -200f);

	public float heightOffset;

	public float fadeDuration = 0.5f;

	public float minimumPartSize;

	public int maxParts;

	public Color partColor = Colors.White;

	public bool includeHiddenLayers;

	public bool hideOriginalSprite = true;

	public bool hideShadow = true;

	public bool useShadowComponentVisibility = true;

	public bool clearSpecialDeathFlagsOnDestroy = true;

	public TowerDefenseCharacter parent;

	public bool _dismembered;

	private static DismemberComponentDefinition _defaultDefinition;

	private bool _eventsConnected;

	private bool _configured;

	private DismemberComponentDefinition Definition => base.ComponentDefinition as DismemberComponentDefinition;

	protected override void OnBound()
	{
		parent = base.Owner;
		ConfigureOnce();
	}

	protected override void OnActivated()
	{
		ConnectParentEvents();
	}

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		DisconnectParentEvents();
		parent = null;
	}

	protected override void OnReleased()
	{
		DisconnectParentEvents();
		parent = null;
	}

	protected override void OnAliveChanged(bool alive)
	{
		if (alive && base.Lifecycle == ComponentRuntimeLifecycle.Active)
		{
			ConnectParentEvents();
		}
		else
		{
			DisconnectParentEvents();
		}
	}

	private void ConfigureOnce()
	{
		if (!_configured)
		{
			DismemberComponentDefinition definition = Definition;
			velocityXRange = definition?.velocityXRange ?? new Vector2(-150f, 150f);
			velocityYRange = definition?.velocityYRange ?? new Vector2(-450f, -200f);
			heightOffset = definition?.heightOffset ?? 0f;
			fadeDuration = definition?.fadeDuration ?? 0.5f;
			minimumPartSize = definition?.minimumPartSize ?? 0f;
			maxParts = definition?.maxParts ?? 0;
			partColor = definition?.partColor ?? Colors.White;
			includeHiddenLayers = definition?.includeHiddenLayers ?? false;
			hideOriginalSprite = definition?.hideOriginalSprite ?? true;
			hideShadow = definition?.hideShadow ?? true;
			useShadowComponentVisibility = definition?.useShadowComponentVisibility ?? true;
			clearSpecialDeathFlagsOnDestroy = definition?.clearSpecialDeathFlagsOnDestroy ?? true;
			_configured = true;
		}
	}

	private void ConnectParentEvents()
	{
		if (!_eventsConnected && base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && GodotObject.IsInstanceValid(parent))
		{
			parent.OnCharacterNearDie += OnCharacterNearDie;
			parent.OnDestroy += OnCharacterDestroy;
			_eventsConnected = true;
		}
	}

	private void DisconnectParentEvents()
	{
		if (_eventsConnected && GodotObject.IsInstanceValid(parent))
		{
			parent.OnCharacterNearDie -= OnCharacterNearDie;
			parent.OnDestroy -= OnCharacterDestroy;
		}
		_eventsConnected = false;
	}

	public void OnCharacterNearDie(TowerDefenseCharacter character)
	{
		if (dismemberEnabled && base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && !_dismembered && character == parent)
		{
			_dismembered = TryDismember(destroyAfterFade: true);
		}
	}

	public void OnCharacterDestroy(TowerDefenseCharacter character)
	{
		if (dismemberEnabled && base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && !_dismembered && character == parent)
		{
			if (clearSpecialDeathFlagsOnDestroy && GodotObject.IsInstanceValid(parent))
			{
				parent.isExplode = false;
				parent.isSmash = false;
			}
			_dismembered = TryDismember(destroyAfterFade: false);
		}
	}

	public void Dismember()
	{
		if (!_dismembered)
		{
			_dismembered = TryDismember(destroyAfterFade: true);
		}
	}

	private bool TryDismember(bool destroyAfterFade)
	{
		if (!TryGetParent(out var character) || !(character is TowerDefenseZombie) || character.mowerDeathVisualOwned || !GodotObject.IsInstanceValid(character.sprite))
		{
			return false;
		}
		AdobeAnimateSprite sprite = character.sprite;
		AdobeAnimateData flashAnimeData = sprite.flashAnimeData;
		if (!GodotObject.IsInstanceValid(flashAnimeData))
		{
			return false;
		}
		AdobeAnimateRuntimeDefinition orBuild = AdobeAnimateDefinitionCache.GetOrBuild(flashAnimeData);
		int frameIndex = sprite.frameIndex;
		if (orBuild == null || frameIndex < 0 || frameIndex >= orBuild.Frames.Length)
		{
			return false;
		}
		DamagePartBatcher orCreate = DamagePartBatcher.GetOrCreate();
		if (!GodotObject.IsInstanceValid(orCreate))
		{
			return false;
		}
		if (SpawnVisibleParts(character, sprite, flashAnimeData, frameIndex, orCreate) == 0)
		{
			return false;
		}
		ApplyDismemberVisual(character, sprite);
		if (destroyAfterFade)
		{
			ScheduleDestroy(character, sprite);
		}
		return true;
	}

	private int SpawnVisibleParts(TowerDefenseCharacter character, AdobeAnimateSprite animeSprite, AdobeAnimateData animeData, int frameIndex, DamagePartBatcher damagePartBatcher)
	{
		int num = 0;
		Dictionary layerDictionary = animeData.layerDictionary;
		Array<Texture2D> mediaReplace = animeSprite.CreateActiveMediaReplaceSnapshotForRender();
		Array<bool> layerVisibleForInternalRead = animeSprite.GetLayerVisibleForInternalRead();
		Array<string> mediaReplaceAtlasPaths = animeSprite.CreateActiveMediaReplaceAtlasPathSnapshotForRender();
		Transform2D logicalGlobalTransform = character.GetLogicalGlobalTransform(animeSprite);
		foreach (Variant key in layerDictionary.Keys)
		{
			if (maxParts > 0 && num >= maxParts)
			{
				break;
			}
			int num2 = layerDictionary[key].AsInt32();
			if (num2 < 0 || num2 >= layerVisibleForInternalRead.Count || (!includeHiddenLayers && !layerVisibleForInternalRead[num2]) || !animeSprite.TryGetInterpolatedLayerPoseForRender(num2, out var mediaId, out var transform) || mediaId == 65535)
			{
				continue;
			}
			Vector2 mediaSizeForRender = animeSprite.GetMediaSizeForRender(mediaId);
			float num3 = Mathf.Max(0f, minimumPartSize);
			if (!(mediaSizeForRender.X <= num3) && !(mediaSizeForRender.Y <= num3))
			{
				Vector2 origin = logicalGlobalTransform * (transform * (mediaSizeForRender / 2f) + animeSprite.offset);
				Vector2 velocity = new Vector2((float)GD.RandRange(velocityXRange.X, velocityXRange.Y), (float)GD.RandRange(velocityYRange.X, velocityYRange.Y));
				float num4 = (GodotObject.IsInstanceValid(character.shadowSprite) ? character.shadowSprite.Position.Y : 0f);
				double height = character.GetGroundHeight(origin.Y) - character.groundHeight + (double)num4 + (double)heightOffset;
				Vector2 vector = (GodotObject.IsInstanceValid(character.transformPoint) ? character.transformPoint.Scale : Vector2.One);
				Transform2D rootWorldTransform = new Transform2D(0f, character.Scale * vector * animeSprite.Scale, 0f, origin);
				Transform2D mediaTransform = transform.Translated(-transform.Origin).TranslatedLocal(-mediaSizeForRender / 2f);
				if (damagePartBatcher.TrySpawnAdobeMedia(animeData, mediaId, mediaTransform, partColor, mediaReplace, mediaReplaceAtlasPaths, rootWorldTransform, height, velocity, character.gridPos))
				{
					num++;
				}
			}
		}
		return num;
	}

	private void ApplyDismemberVisual(TowerDefenseCharacter character, AdobeAnimateSprite animeSprite)
	{
		if (hideOriginalSprite)
		{
			animeSprite.Visible = false;
		}
		animeSprite.pause = true;
		if (!hideShadow)
		{
			return;
		}
		if (useShadowComponentVisibility)
		{
			ShadowComponent shadowComponent = character.shadowComponent;
			if (shadowComponent != null && !shadowComponent.IsReleased)
			{
				character.shadowComponent.SetShadowVisible(visible: false);
				return;
			}
		}
		if (GodotObject.IsInstanceValid(character.shadowSprite))
		{
			character.shadowSprite.Visible = false;
		}
	}

	private void ScheduleDestroy(TowerDefenseCharacter character, AdobeAnimateSprite animeSprite)
	{
		float num = Mathf.Max(0f, fadeDuration);
		if (num <= 0f)
		{
			character.Destroy();
			return;
		}
		Tween tween = character.CreateTween();
		tween.SetParallel();
		tween.TweenProperty(character, "modulate:a", 0f, num);
		tween.TweenProperty(animeSprite, "meshColor:a", 0f, num);
		tween.Chain().TweenCallback(Callable.From(delegate
		{
			character.Destroy();
		}));
	}

	private bool TryGetParent(out TowerDefenseCharacter character)
	{
		character = parent;
		if (base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && GodotObject.IsInstanceValid(character))
		{
			return GodotObject.IsInstanceValid(character.instance);
		}
		return false;
	}

	private static DismemberComponentDefinition GetDefaultDefinition()
	{
		if (!GodotObject.IsInstanceValid(_defaultDefinition))
		{
			_defaultDefinition = ResourceLoader.Load<DismemberComponentDefinition>("res://Script/Component/TowerDefense/Character/DismemberComponent/DismemberComponentDefinition.tres", null, ResourceLoader.CacheMode.Reuse);
		}
		return _defaultDefinition;
	}

	public static void InjectToCharacter(TowerDefenseCharacter character)
	{
		if (!GodotObject.IsInstanceValid(character) || !(character is TowerDefenseZombie) || !GodotObject.IsInstanceValid(character.componentManager))
		{
			return;
		}
		ComponentManager componentManager = character.componentManager;
		DismemberComponent runtime = componentManager.GetRuntime<DismemberComponent>();
		if (runtime != null && !runtime.IsReleased)
		{
			character.dismemberComponent = runtime;
			return;
		}
		DismemberComponentDefinition defaultDefinition = GetDefaultDefinition();
		if (GodotObject.IsInstanceValid(defaultDefinition) && componentManager.AddRuntimeComponent(defaultDefinition) is DismemberComponent { IsReleased: false } dismemberComponent)
		{
			character.dismemberComponent = dismemberComponent;
		}
	}

	public static void RemoveFromCharacter(TowerDefenseCharacter character)
	{
		if (GodotObject.IsInstanceValid(character) && GodotObject.IsInstanceValid(character.componentManager))
		{
			ComponentManager componentManager = character.componentManager;
			DismemberComponent runtime = componentManager.GetRuntime<DismemberComponent>();
			if (runtime != null && !runtime.IsReleased)
			{
				componentManager.RemoveRuntimeComponent(runtime);
			}
			if (character.dismemberComponent == runtime)
			{
				character.dismemberComponent = null;
			}
		}
	}

	public static void InjectAll()
	{
		if (!GodotObject.IsInstanceValid(TowerDefenseManager.Instance?.characterRegistry))
		{
			return;
		}
		List<TowerDefenseCharacter> cleanCharactersList = TowerDefenseManager.Instance.characterRegistry.GetCleanCharactersList();
		for (int i = 0; i < cleanCharactersList.Count; i++)
		{
			if (cleanCharactersList[i] is TowerDefenseZombie character)
			{
				InjectToCharacter(character);
			}
		}
	}

	public static void RemoveAll()
	{
		if (!GodotObject.IsInstanceValid(TowerDefenseManager.Instance?.characterRegistry))
		{
			return;
		}
		List<TowerDefenseCharacter> cleanCharactersList = TowerDefenseManager.Instance.characterRegistry.GetCleanCharactersList();
		for (int i = 0; i < cleanCharactersList.Count; i++)
		{
			if (cleanCharactersList[i] is TowerDefenseZombie character)
			{
				RemoveFromCharacter(character);
			}
		}
	}
}
