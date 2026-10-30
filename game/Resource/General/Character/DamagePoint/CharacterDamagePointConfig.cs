using Godot;

[Tool]
[GlobalClass]
public partial class CharacterDamagePointConfig : Resource
{
	private string _damagePointName = "";

	private string _animeFliterOpen = "";

	private string _animeFliterClose = "";

	[Export(PropertyHint.None, "")]
	public double damagePersontage = 0.5;

	[Export(PropertyHint.None, "")]
	public PackedScene animeEffect;

	[Export(PropertyHint.None, "")]
	public Vector2 animeEffectOffset = Vector2.Zero;

	[Export(PropertyHint.None, "")]
	public bool isDrop = true;

	[Export(PropertyHint.None, "")]
	public StringName replaceMediaName = "";

	[Export(PropertyHint.File, "*.png,*.webp,*.jpg,*.jpeg,*.svg,*.bmp,*.tga")]
	public string replaceMediaTexturePath = "";

	[Export(PropertyHint.None, "")]
	public string damageAudio = "LimbsPop";

	[Export(PropertyHint.None, "")]
	public string damagePointName
	{
		get
		{
			return _damagePointName;
		}
		set
		{
			_damagePointName = value;
			EmitChanged();
		}
	}

	[Export(PropertyHint.MultilineText, "")]
	public string animeFliterOpen
	{
		get
		{
			return _animeFliterOpen;
		}
		set
		{
			_animeFliterOpen = value;
			EmitChanged();
		}
	}

	[Export(PropertyHint.MultilineText, "")]
	public string animeFliterClose
	{
		get
		{
			return _animeFliterClose;
		}
		set
		{
			_animeFliterClose = value;
			EmitChanged();
		}
	}
}
