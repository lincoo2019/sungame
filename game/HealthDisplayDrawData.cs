using Godot;

internal readonly struct HealthDisplayDrawData(Vector2 center, string shieldText, string helmetText, string bodyText, Color shieldColor, Color helmetColor, Color bodyColor)
{
	public readonly Vector2 Center = center;

	public readonly string ShieldText = shieldText;

	public readonly string HelmetText = helmetText;

	public readonly string BodyText = bodyText;

	public readonly Color ShieldColor = shieldColor;

	public readonly Color HelmetColor = helmetColor;

	public readonly Color BodyColor = bodyColor;
}
