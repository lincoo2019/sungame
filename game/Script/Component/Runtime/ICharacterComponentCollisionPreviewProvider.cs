public interface ICharacterComponentCollisionPreviewProvider
{
	int CollisionPreviewShapeCount { get; }

	int CollisionPreviewRayCount { get; }

	AabbShape2DResource GetCollisionPreviewShape(int index);

	AabbRay2DResource GetCollisionPreviewRay(int index);
}
