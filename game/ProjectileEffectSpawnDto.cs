public class ProjectileEffectSpawnDto
{
	public string effect_id { get; set; } = "";

	public string variant { get; set; } = "";

	public ulong random_seed { get; set; }

	public double px { get; set; }

	public double py { get; set; }

	public int grid_x { get; set; }

	public int grid_y { get; set; }

	public int camp { get; set; }

	public int collision_flags { get; set; }

	public double height { get; set; }
}
