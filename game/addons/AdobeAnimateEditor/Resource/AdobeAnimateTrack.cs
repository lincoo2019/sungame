public class AdobeAnimateTrack
{
	public string clip { get; set; } = "";

	public double delay { get; set; }

	public bool loop { get; set; } = true;

	public double blendTime { get; set; } = 0.2;

	public AdobeAnimateTrack()
	{
	}

	public AdobeAnimateTrack(string _clip, double _delay, bool _loop = true, double _blendTime = 0.2)
	{
		clip = _clip;
		delay = _delay;
		loop = _loop;
		blendTime = _blendTime;
	}
}
