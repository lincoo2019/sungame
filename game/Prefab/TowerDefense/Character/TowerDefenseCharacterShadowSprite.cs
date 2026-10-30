using Godot;

public sealed partial class TowerDefenseCharacterShadowSprite : Sprite2D
{
	internal readonly struct LocalPoseSnapshot(Transform2D transform, bool topLevel)
	{
		internal readonly Transform2D Transform = transform;

		internal readonly Vector2 Position = transform.Origin;

		internal readonly Vector2 Scale = transform.Scale;

		internal readonly float Rotation = transform.Rotation;

		internal readonly float Skew = transform.Skew;

		internal readonly bool TopLevel = topLevel;
	}

	private LocalPoseSnapshot _cachedLocalPose = new LocalPoseSnapshot(Transform2D.Identity, topLevel: false);

	private ulong _localTransformRevision = 1uL;

	private bool _localPoseReady;

	internal ref readonly LocalPoseSnapshot CachedLocalPose => ref _cachedLocalPose;

	internal Transform2D CachedLocalTransform => _cachedLocalPose.Transform;

	internal ulong LocalTransformRevision => _localTransformRevision;

	internal bool LocalPoseReady => _localPoseReady;

	public override void _EnterTree()
	{
		base._EnterTree();
		CaptureLocalTransform(base.Transform, base.TopLevel);
		AdvanceLocalTransformRevision();
		SetNotifyLocalTransform(enable: true);
	}

	public override void _ExitTree()
	{
		SetNotifyLocalTransform(enable: false);
		base._ExitTree();
	}

	public override void _Notification(int what)
	{
		if ((long)what == 35)
		{
			Transform2D transform = base.Transform;
			bool topLevel = base.TopLevel;
			if (!transform.IsEqualApprox(_cachedLocalPose.Transform) || topLevel != _cachedLocalPose.TopLevel)
			{
				CaptureLocalTransform(transform, topLevel);
				AdvanceLocalTransformRevision();
			}
		}
	}

	private void CaptureLocalTransform(Transform2D localTransform, bool topLevel)
	{
		_cachedLocalPose = new LocalPoseSnapshot(localTransform, topLevel);
		_localPoseReady = true;
	}

	private void AdvanceLocalTransformRevision()
	{
		_localTransformRevision++;
		if (_localTransformRevision == 0L)
		{
			_localTransformRevision = 1uL;
		}
	}
}
