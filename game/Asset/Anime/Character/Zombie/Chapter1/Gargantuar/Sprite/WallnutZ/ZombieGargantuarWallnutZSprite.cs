using Godot;

[Tool]
public partial class ZombieGargantuarWallnutZSprite : AdobeAnimateSpriteBase
{
	private AdobeAnimateSpriteBase _wallnutZHead;

	private AdobeAnimateSpriteBase _peashooterZImpHead;

	public AdobeAnimateSpriteBase wallnutZHead => _wallnutZHead;

	public AdobeAnimateSpriteBase peashooterZImpHead => _peashooterZImpHead;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_wallnutZHead = GetNode<AdobeAnimateSpriteBase>("%WallnutZHead");
			_peashooterZImpHead = GetNode<AdobeAnimateSpriteBase>("%PeashooterZImpHead");
		}
	}

	public override void BatchPhysicsUpdate(double delta)
	{
		base.BatchPhysicsUpdate(delta);
		if (GodotObject.IsInstanceValid(_wallnutZHead) && _wallnutZHead.IsRuntimeReadyCached)
		{
			SyncRuntimeChildState(_wallnutZHead);
		}
		if (GodotObject.IsInstanceValid(_peashooterZImpHead) && _peashooterZImpHead.IsRuntimeReadyCached)
		{
			SyncRuntimeChildState(_peashooterZImpHead);
		}
	}

	public override void ResetAnimation()
	{
		base.ResetAnimation();
		if (GodotObject.IsInstanceValid(_wallnutZHead))
		{
			_wallnutZHead.ResetAnimation();
		}
		if (GodotObject.IsInstanceValid(_peashooterZImpHead))
		{
			_peashooterZImpHead.ResetAnimation();
		}
	}
}
