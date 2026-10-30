using Godot;

[GlobalClass]
public partial class AudioStreamPlayerMember : AudioStreamPlayer
{
	[Export(PropertyHint.None, "")]
	public AudioManagerEnum.TYPE type = AudioManagerEnum.TYPE.SFX;

	private double _volumeScale = 1.0;

	[Export(PropertyHint.None, "")]
	public double volumeScale
	{
		get
		{
			return _volumeScale;
		}
		set
		{
			_volumeScale = value;
			VolumRefresh();
		}
	}

	public override void _Ready()
	{
		VolumRefresh();
	}

	public void VolumRefresh()
	{
		base.VolumeDb = (float)Mathf.LinearToDb(volumeScale);
	}
}
