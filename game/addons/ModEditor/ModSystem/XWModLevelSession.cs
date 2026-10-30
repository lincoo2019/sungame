namespace PVZHE.ModEditor.ModSystem;

public static class XWModLevelSession
{
	public const string BrowserKey = "__ModLevels";

	public static string OwnerModId { get; private set; } = "";

	public static string CatalogKey { get; private set; } = "";

	public static XWModLevelIdentity Current { get; private set; }

	public static bool IsMod => OwnerModId.Length > 0;

	public static void Clear()
	{
		OwnerModId = "";
		CatalogKey = "";
		Current = null;
	}

	public static void SelectCatalog(string owner, string catalog)
	{
		OwnerModId = owner;
		CatalogKey = catalog;
		Current = null;
	}

	public static void Select(XWModLevelIdentity identity)
	{
		Clear();
		if ((object)identity != null && identity.IsMod)
		{
			OwnerModId = identity.OwnerModId;
			CatalogKey = identity.CatalogKey;
			Current = identity;
		}
	}

	public static XWModLevelIdentity ForLevel(string key, string difficulty = null)
	{
		if (IsMod)
		{
			return new XWModLevelIdentity(OwnerModId, CatalogKey, key, difficulty ?? Current?.Difficulty ?? "Normal");
		}
		return null;
	}
}
