using Godot;

public partial class CactusQXVolleyCountRuntimeTest : Node
{
	private int _checks;

	private int _failures;

	public override void _Ready()
	{
		FireComponentCheckConfig fireComponentCheckConfig = new FireComponentCheckConfig();
		FireComponentCheckConfig fireComponentCheckConfig2 = new FireComponentCheckConfig
		{
			useParentCollision = false,
			collisionFlags = 2
		};
		Check(fireComponentCheckConfig.useParentCollision && fireComponentCheckConfig.GetCollisionFlags() == -1, "The ground check must expose the inherited -1 collision sentinel.");
		Check(!fireComponentCheckConfig2.useParentCollision && fireComponentCheckConfig2.GetCollisionFlags() == 2, "The air check must expose the explicit off-ground collision flag.");
		Check(TowerDefensePlantCactusQX.ResolveVolleyBulletCount(fireComponentCheckConfig, 1) == 1, "CactusQX ground fire must resolve to exactly one projectile.");
		Check(TowerDefensePlantCactusQX.ResolveVolleyBulletCount(fireComponentCheckConfig2, 1) == 2, "CactusQX air fire must resolve to exactly two projectiles.");
		bool flag = _failures == 0 && _checks == 4;
		GD.Print($"CACTUS_QX_VOLLEY_COUNT_RUNTIME_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[CactusQXVolleyCountRuntimeTest] " + message);
		}
	}
}
