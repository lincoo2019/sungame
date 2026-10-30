using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class CharacterCustomConfig : Resource
{
	[Export(PropertyHint.None, "")]
	public string openKey = "";

	[Export(PropertyHint.None, "")]
	public string customName = "";

	[Export(PropertyHint.Enum, "White,Gold")]
	public string type = "White";

	[Export(PropertyHint.None, "")]
	public string customHandbookName = "";

	[Export(PropertyHint.None, "")]
	public string customHandbookAccess = "";

	[Export(PropertyHint.None, "")]
	public string customHandbookStory = "";

	private string _animeFliterOpen = "";

	private string _animeFliterClose = "";

	[Export(PropertyHint.None, "")]
	public string damagePointChangeMediaName = "";

	[Export(PropertyHint.File, "*.png,*.webp,*.jpg,*.jpeg,*.svg,*.bmp,*.tga")]
	public Array<string> damagePointChangeMediaTexturePaths = new Array<string>();

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
