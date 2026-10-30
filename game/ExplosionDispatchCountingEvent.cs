using System.Collections.Generic;
using Godot;

public partial class ExplosionDispatchCountingEvent : TowerDefenseCharacterEventBase
{
	public static readonly Dictionary<ulong, int> HitsByPhysicsFrame = new Dictionary<ulong, int>();

	public static ExplosionDispatchCountingEvent ExpectedInstance;

	public static int TotalHits;

	public static bool PreservedReference = true;

	public static void Reset(ExplosionDispatchCountingEvent expected)
	{
		HitsByPhysicsFrame.Clear();
		ExpectedInstance = expected;
		TotalHits = 0;
		PreservedReference = true;
	}

	public override void Execute(Vector2 pos, TowerDefenseCharacter target)
	{
		ulong physicsFrames = Engine.GetPhysicsFrames();
		HitsByPhysicsFrame.TryGetValue(physicsFrames, out var value);
		HitsByPhysicsFrame[physicsFrames] = value + 1;
		TotalHits++;
		PreservedReference &= this == ExpectedInstance;
	}
}
