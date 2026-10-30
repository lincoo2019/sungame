using Godot;

internal readonly struct TowerDefenseDeathRecord(TowerDefensePacketConfig packet, Vector2 position, Vector2I gridPosition, TowerDefenseEnum.CHARACTER_CAMP camp, double scale, double hitpointScale, bool invisible, bool canAngelRevive)
{
	public TowerDefensePacketConfig Packet { get; } = packet;

	public Vector2 Position { get; } = position;

	public Vector2I GridPosition { get; } = gridPosition;

	public TowerDefenseEnum.CHARACTER_CAMP Camp { get; } = camp;

	public double Scale { get; } = scale;

	public double HitpointScale { get; } = hitpointScale;

	public bool Invisible { get; } = invisible;

	public bool CanAngelRevive { get; } = canAngelRevive;
}
