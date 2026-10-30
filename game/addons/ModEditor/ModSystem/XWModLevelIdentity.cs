using Godot;
using Godot.Collections;

namespace PVZHE.ModEditor.ModSystem;

public sealed record XWModLevelIdentity(string OwnerModId, string CatalogKey, string LevelSaveKey, string Difficulty)
{
	public bool IsMod => !string.IsNullOrEmpty(OwnerModId);

	public string Canonical => Json.Stringify(ToDictionary());

	public Dictionary ToDictionary()
	{
		return new Dictionary
		{
			["kind"] = (IsMod ? "Mod" : "Builtin"),
			["owner_mod_id"] = OwnerModId,
			["catalog_key"] = CatalogKey,
			["level_save_key"] = LevelSaveKey,
			["difficulty"] = Difficulty
		};
	}

	public static bool TryParse(Variant value, out XWModLevelIdentity identity)
	{
		identity = null;
		if (value.VariantType != Variant.Type.Dictionary)
		{
			return false;
		}
		Dictionary dictionary = value.AsGodotDictionary();
		if (!Read(dictionary, "kind", out var text))
		{
			return false;
		}
		bool flag = ((text == "Mod" || text == "Builtin") ? true : false);
		Variant value2 = default(Variant);
		string text2 = default(string);
		string text3 = default(string);
		string text4 = default(string);
		bool flag2 = !flag || dictionary.Count != 5 || !dictionary.TryGetValue("owner_mod_id", out value2) || value2.VariantType != Variant.Type.String || !Read(dictionary, "catalog_key", out text2) || !Read(dictionary, "level_save_key", out text3) || !Read(dictionary, "difficulty", out text4);
		if (!flag2)
		{
			bool flag3;
			switch (text4)
			{
			case "Normal":
			case "Difficult":
			case "Ultimate":
				flag3 = true;
				break;
			default:
				flag3 = false;
				break;
			}
			flag2 = !flag3;
		}
		if (flag2)
		{
			return false;
		}
		string text5 = value2.AsString();
		if ((text == "Mod") ? (!Read(dictionary, "owner_mod_id", out var _)) : ((byte)text5.Length != 0))
		{
			return false;
		}
		identity = new XWModLevelIdentity(text5, text2, text3, text4);
		return true;
	}

	private static bool Read(Dictionary data, string key, out string text)
	{
		text = "";
		if (!data.TryGetValue(key, out var value) || value.VariantType != Variant.Type.String)
		{
			return false;
		}
		text = value.AsString();
		if (!string.IsNullOrWhiteSpace(text) && text == text.Trim())
		{
			return text.Length <= 256;
		}
		return false;
	}
}
