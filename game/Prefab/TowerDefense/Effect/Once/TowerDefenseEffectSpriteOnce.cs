using System;
using Godot;

[GlobalClass]
public partial class TowerDefenseEffectSpriteOnce : TowerDefenseEffectBase
{
	private static PackedScene _towerDefenseEffectSpriteOnce;

	private static readonly StringName EffectGroupName = "Effect";

	[Export(PropertyHint.None, "")]
	public ObjectManagerConfig.OBJECT objectId;

	private AdobeAnimateSprite _sprite;

	[Export(PropertyHint.None, "")]
	public string[] clipsList;

	private bool _useGpuBatch = true;

	public int currentIndex;

	private AdobeAnimateSprite _animeCompletedSprite;

	private bool _gpuBatchActive;

	private TowerDefenseEffectSpriteOnceBatcher _gpuBatcher;

	private int _gpuSuppressionToken;

	private AdobeAnimateSprite _gpuSuppressedSprite;

	private int _gpuBatchVisualRevision;

	private int _gpuBatchEligibilityRevision;

	private string _preparedRequestedClip;

	private static PackedScene TOWER_DEFENSE_EFFECT_SPRITE_ONCE => _towerDefenseEffectSpriteOnce ?? (_towerDefenseEffectSpriteOnce = GD.Load<PackedScene>("uid://dwvgduivkprow"));

	[Export(PropertyHint.None, "")]
	public AdobeAnimateSprite sprite
	{
		get
		{
			return _sprite;
		}
		set
		{
			if (_sprite != value)
			{
				_sprite = value;
				MarkGpuBatchEligibilityChanged();
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public bool useGpuBatch
	{
		get
		{
			return _useGpuBatch;
		}
		set
		{
			if (_useGpuBatch != value)
			{
				_useGpuBatch = value;
				MarkGpuBatchEligibilityChanged();
			}
		}
	}

	public new Color Modulate
	{
		get
		{
			return base.Modulate;
		}
		set
		{
			if (!(base.Modulate == value))
			{
				base.Modulate = value;
				_gpuBatchVisualRevision++;
				MarkGpuBatchEligibilityChanged();
			}
		}
	}

	public new Color SelfModulate
	{
		get
		{
			return base.SelfModulate;
		}
		set
		{
			if (!(base.SelfModulate == value))
			{
				base.SelfModulate = value;
				_gpuBatchVisualRevision++;
				MarkGpuBatchEligibilityChanged();
			}
		}
	}

	public new int ZIndex
	{
		get
		{
			return base.ZIndex;
		}
		set
		{
			if (base.ZIndex != value)
			{
				base.ZIndex = value;
				if (IsValid(sprite))
				{
					sprite.InvalidateEffectOnceBatchRenderState();
				}
			}
		}
	}

	public new bool ZAsRelative
	{
		get
		{
			return base.ZAsRelative;
		}
		set
		{
			if (base.ZAsRelative != value)
			{
				base.ZAsRelative = value;
				if (IsValid(sprite))
				{
					sprite.InvalidateEffectOnceBatchRenderState();
				}
			}
		}
	}

	internal event Action EffectOnceBatchEligibilityChanged;

	private void MarkGpuBatchEligibilityChanged()
	{
		_gpuBatchEligibilityRevision++;
		this.EffectOnceBatchEligibilityChanged?.Invoke();
	}

	internal int GetGpuBatchVisualRevision()
	{
		return _gpuBatchVisualRevision;
	}

	internal int GetGpuBatchEligibilityRevision()
	{
		return _gpuBatchEligibilityRevision;
	}

	public static TowerDefenseEffectSpriteOnce Create()
	{
		return TOWER_DEFENSE_EFFECT_SPRITE_ONCE.Instantiate<TowerDefenseEffectSpriteOnce>(PackedScene.GenEditState.Disabled);
	}

	public void Refresh()
	{
		AddToGroup(EffectGroupName);
		currentIndex = 0;
		StopGpuBatch();
		ConnectSpriteSignals();
		ConfigureSpriteForRuntimeManager();
		PlayCurrentClip(0.0);
	}

	public void Recycle()
	{
		RemoveFromGroup(EffectGroupName);
		StopGpuBatch();
		if (IsValid(sprite))
		{
			sprite.pause = true;
			sprite.Visible = false;
			sprite.RefreshProcessScheduling();
		}
	}

	public override void _Ready()
	{
		base._Ready();
		ConnectSpriteSignals();
		ConfigureSpriteForRuntimeManager();
		if (!TryActivatePreparedGpuBatchForCurrentClip())
		{
			PlayCurrentClip(0.0);
		}
	}

	public void InitScene(AdobeAnimateSprite _sprite, string _clip = "")
	{
		StopGpuBatch();
		if (sprite != _sprite)
		{
			DisconnectSpriteSignals();
			sprite = _sprite;
		}
		if (_clip != "")
		{
			currentIndex = 0;
			clipsList = _clip.Split('|', StringSplitOptions.RemoveEmptyEntries);
		}
		EnsureSpriteChild();
		ConnectSpriteSignals();
		ConfigureSpriteForRuntimeManager();
		PlayCurrentClip(0.0);
	}

	public void Init(PackedScene spriteScene, string _clip = "")
	{
		if (spriteScene != null)
		{
			AdobeAnimateSprite adobeAnimateSprite = spriteScene.Instantiate<AdobeAnimateSprite>(PackedScene.GenEditState.Disabled);
			InitScene(adobeAnimateSprite, _clip);
		}
	}

	public void AnimeCompleted(string clip)
	{
		if (_gpuBatchActive)
		{
			StopGpuBatch();
		}
		CompleteClip();
	}

	internal void AbortGpuBatchAndResumeCpu(string clip, float elapsedFrame)
	{
		AdobeAnimateSprite gpuSuppressedSprite = _gpuSuppressedSprite;
		AdobeAnimateSprite adobeAnimateSprite = sprite;
		int gpuSuppressionToken = _gpuSuppressionToken;
		_gpuBatchActive = false;
		_gpuBatcher = null;
		_gpuSuppressionToken = 0;
		_gpuSuppressedSprite = null;
		if (IsValid(gpuSuppressedSprite))
		{
			gpuSuppressedSprite.EndEffectOnceGpuBatchSuppression(gpuSuppressionToken);
		}
		if (!IsValid(adobeAnimateSprite))
		{
			return;
		}
		if (adobeAnimateSprite == gpuSuppressedSprite)
		{
			adobeAnimateSprite.forceLocalRender = false;
			adobeAnimateSprite.RefreshProcessScheduling();
			return;
		}
		if (IsValid(gpuSuppressedSprite))
		{
			gpuSuppressedSprite.Visible = false;
			gpuSuppressedSprite.pause = true;
		}
		EnsureSpriteChild();
		ConnectSpriteSignals();
		ConfigureSpriteForRuntimeManager();
		if (!string.IsNullOrEmpty(clip))
		{
			adobeAnimateSprite.SetAnimation(clip, loop: false);
			adobeAnimateSprite.TryImportEffectOnceGpuPlaybackPositionWithoutEvents(adobeAnimateSprite.clip, elapsedFrame);
		}
	}

	public override void _ExitTree()
	{
		StopGpuBatch();
		DisconnectSpriteSignals();
	}

	private void CompleteClip()
	{
		if (clipsList != null && clipsList.Length != 0 && currentIndex < clipsList.Length - 1)
		{
			currentIndex++;
			PlayCurrentClip(0.1);
		}
		else if (objectId == ObjectManagerConfig.OBJECT.NOONE)
		{
			QueueFree();
		}
		else
		{
			ObjectManager.PoolPush(objectId, this);
		}
	}

	private static bool IsValid(GodotObject obj)
	{
		if (obj != null)
		{
			return GodotObject.IsInstanceValid(obj);
		}
		return false;
	}

	private void ConnectSpriteSignals()
	{
		if (_animeCompletedSprite != sprite || !IsValid(_animeCompletedSprite))
		{
			DisconnectSpriteSignals();
			if (IsValid(sprite))
			{
				sprite.OnAnimeCompleted += AnimeCompleted;
				_animeCompletedSprite = sprite;
			}
		}
	}

	private void DisconnectSpriteSignals()
	{
		AdobeAnimateSprite animeCompletedSprite = _animeCompletedSprite;
		_animeCompletedSprite = null;
		if (IsValid(animeCompletedSprite))
		{
			animeCompletedSprite.OnAnimeCompleted -= AnimeCompleted;
		}
	}

	private void EnsureSpriteChild()
	{
		if (!IsValid(sprite))
		{
			return;
		}
		Node parent = sprite.GetParent();
		if (parent != this)
		{
			if (IsValid(parent))
			{
				sprite.Reparent(this, keepGlobalTransform: false);
			}
			else
			{
				AddChild(sprite, forceReadableName: false, InternalMode.Disabled);
			}
		}
	}

	private void ConfigureSpriteForRuntimeManager()
	{
		if (IsValid(sprite))
		{
			sprite.forceLocalRender = false;
			sprite.pause = false;
			sprite.Visible = true;
			sprite.RefreshProcessScheduling();
		}
	}

	private void PlayCurrentClip(double blendTime)
	{
		if (!IsValid(sprite) || clipsList == null || clipsList.Length == 0)
		{
			return;
		}
		if (currentIndex < 0 || currentIndex >= clipsList.Length)
		{
			currentIndex = 0;
		}
		string text = clipsList[currentIndex];
		if (!string.IsNullOrEmpty(text))
		{
			StopGpuBatch();
			ConfigureSpriteForRuntimeManager();
			sprite.SetAnimation(text, loop: false, blendTime);
			_preparedRequestedClip = text;
			if (clipsList.Length <= 1 && !(blendTime > 0.0) && !sprite.blend)
			{
				TryActivateGpuBatch(sprite.clip);
			}
		}
	}

	private bool TryActivatePreparedGpuBatchForCurrentClip()
	{
		if (!IsValid(sprite) || clipsList == null || clipsList.Length == 0)
		{
			return false;
		}
		if (currentIndex < 0 || currentIndex >= clipsList.Length)
		{
			currentIndex = 0;
		}
		string b = clipsList[currentIndex];
		if (clipsList.Length > 1 || !string.Equals(_preparedRequestedClip, b, StringComparison.Ordinal) || !sprite.IsEffectOnceGpuBatchPlaybackPrepared(sprite.clip))
		{
			return false;
		}
		return TryActivateGpuBatch(sprite.clip);
	}

	private bool TryActivateGpuBatch(string clip)
	{
		if (!useGpuBatch || Engine.IsEditorHint() || !IsInsideTree() || !IsValid(sprite) || string.IsNullOrEmpty(clip))
		{
			return false;
		}
		TowerDefenseEffectSpriteOnceBatcher orCreate = TowerDefenseEffectSpriteOnceBatcher.GetOrCreate();
		if (!GodotObject.IsInstanceValid(orCreate))
		{
			return false;
		}
		int num = sprite.BeginEffectOnceGpuBatchSuppression();
		if (!orCreate.RegisterOrRestart(this, sprite, clip))
		{
			sprite.EndEffectOnceGpuBatchSuppression(num);
			return false;
		}
		_gpuBatcher = orCreate;
		_gpuBatchActive = true;
		_gpuSuppressedSprite = sprite;
		_gpuSuppressionToken = num;
		return true;
	}

	private void StopGpuBatch()
	{
		AdobeAnimateSprite gpuSuppressedSprite = _gpuSuppressedSprite;
		int gpuSuppressionToken = _gpuSuppressionToken;
		if (_gpuBatchActive)
		{
			if (GodotObject.IsInstanceValid(_gpuBatcher))
			{
				_gpuBatcher.Unregister(this);
			}
			else
			{
				TowerDefenseEffectSpriteOnceBatcher.UnregisterIfExists(this);
			}
		}
		_gpuBatcher = null;
		_gpuBatchActive = false;
		_gpuSuppressionToken = 0;
		_gpuSuppressedSprite = null;
		if (IsValid(gpuSuppressedSprite))
		{
			gpuSuppressedSprite.EndEffectOnceGpuBatchSuppression(gpuSuppressionToken);
		}
	}
}
