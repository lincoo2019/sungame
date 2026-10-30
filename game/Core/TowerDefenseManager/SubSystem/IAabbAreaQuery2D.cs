using Godot;

public interface IAabbAreaQuery2D
{
	bool Enabled { get; }

	uint CollisionLayer { get; }

	uint CollisionMask { get; }

	bool Monitoring { get; }

	Node RegistryOwner { get; }

	bool TryGetWorldRect(out Rect2 rect);
}
