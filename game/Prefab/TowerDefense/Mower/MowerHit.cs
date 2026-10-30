using Godot;

[GlobalClass]
public partial class MowerHit : TowerDefenseGroundItemBase
{
	private AdobeAnimateSlot _slot;

	private AdobeAnimateSprite _mowerHitSprite;

	public bool _isReady;

	public TowerDefenseCharacter character;

	public override void _Ready()
	{
		_slot = GetNodeOrNull<AdobeAnimateSlot>("%AdobeAnimateSlot");
		_mowerHitSprite = GetNodeOrNull<AdobeAnimateSprite>("MowerHitSprite");
		if (GodotObject.IsInstanceValid(_mowerHitSprite))
		{
			_mowerHitSprite.OnAnimeCompleted += AnimeComplete;
			_mowerHitSprite.frameIndex = 0;
		}
	}

	public bool Init(TowerDefenseCharacter target)
	{
		if (!GodotObject.IsInstanceValid(target) || !GodotObject.IsInstanceValid(_slot) || !GodotObject.IsInstanceValid(_mowerHitSprite))
		{
			return false;
		}
		character = target;
		character.mowerDeathVisualOwned = true;
		Vector2 globalScale = character.GlobalScale;
		character.Reparent(_slot);
		character.SetLogicalGlobalPosition(_slot.GlobalPosition);
		character.GlobalScale = globalScale;
		character.Rotation = 0f;
		if (GodotObject.IsInstanceValid(character.shadowSprite))
		{
			character.shadowSprite.Visible = false;
		}
		TowerDefenseGroundItemBase towerDefenseGroundItemBase = character;
		if (towerDefenseGroundItemBase != null)
		{
			base.itemLayer = towerDefenseGroundItemBase.itemLayer;
		}
		_mowerHitSprite.SetAnimation("animation", loop: false);
		_mowerHitSprite.pause = false;
		_isReady = true;
		return true;
	}

	public void AnimeComplete(string clip)
	{
		if (_isReady)
		{
			QueueFree();
		}
	}
}
