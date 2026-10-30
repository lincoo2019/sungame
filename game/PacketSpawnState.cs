using Godot;
using Godot.Collections;

public readonly struct PacketSpawnState(int syncId, string packetName, Vector2 position, double aliveTime, bool isFall, bool useCost, Vector2 velocity, int zIndex, double fallHeight, double height = 1.0, Dictionary runtimeState = null, EconomyAccountId sunAccountId = default(EconomyAccountId), double gravity = 980.0)
{
	public int SyncId { get; } = syncId;

	public string PacketName { get; } = packetName ?? "";

	public Vector2 Position { get; } = position;

	public double AliveTime { get; } = aliveTime;

	public bool IsFall { get; } = isFall;

	public bool UseCost { get; } = useCost;

	public Vector2 Velocity { get; } = velocity;

	public int ZIndex { get; } = zIndex;

	public double FallHeight { get; } = fallHeight;

	public double Height { get; } = height;

	public Dictionary RuntimeState { get; } = runtimeState;

	public EconomyAccountId SunAccountId { get; } = sunAccountId;

	public double Gravity { get; } = gravity;
}
