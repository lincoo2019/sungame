using Godot;

[GlobalClass]
[Tool]
public partial class PhonkComponentDefinition : CharacterComponentDefinition
{
	[ExportGroup("Spring", "")]
	[Export(PropertyHint.Range, "0,5000,1")]
	public float stiffness { get; set; } = 800f;

	[Export(PropertyHint.Range, "0,100,0.1")]
	public float damping { get; set; } = 3.5f;

	[Export(PropertyHint.Range, "0,5000,1")]
	public float rotStiffness { get; set; } = 1400f;

	[Export(PropertyHint.Range, "0,100,0.1")]
	public float rotDamping { get; set; } = 5f;

	[Export(PropertyHint.Range, "0,2,0.01")]
	public float maxScaleDeform { get; set; } = 0.6f;

	[Export(PropertyHint.Range, "0,2,0.01")]
	public float maxRotDeform { get; set; } = 0.5f;

	[ExportGroup("Impulses", "")]
	[Export(PropertyHint.None, "")]
	public float hurtIntensity { get; set; } = 1.2f;

	[Export(PropertyHint.None, "")]
	public float dieIntensity { get; set; } = 1.8f;

	[Export(PropertyHint.None, "")]
	public float attackIntensity { get; set; } = 0.6f;

	[Export(PropertyHint.None, "")]
	public float produceIntensity { get; set; } = 0.5f;

	[Export(PropertyHint.None, "")]
	public float plantIntensity { get; set; } = 0.8f;

	[Export(PropertyHint.Range, "0,5,0.01")]
	public float hurtCooldown { get; set; } = 0.5f;

	[ExportGroup("Simulation", "")]
	[Export(PropertyHint.Range, "0.001,0.1,0.001")]
	public float maxSimulationStep { get; set; } = 1f / 60f;

	[Export(PropertyHint.Range, "1,16,1")]
	public int maxSimulationSubsteps { get; set; } = 4;

	[Export(PropertyHint.Range, "0,100,0.1")]
	public float transformResponse { get; set; } = 25f;

	[Export(PropertyHint.None, "")]
	public float scaleSleepThreshold { get; set; } = 0.001f;

	[Export(PropertyHint.None, "")]
	public float velocitySleepThreshold { get; set; } = 0.01f;

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new PhonkComponent();
	}
}
