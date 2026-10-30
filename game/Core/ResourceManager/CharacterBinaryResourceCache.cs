using Godot;

public static class CharacterBinaryResourceCache
{
	private const string CacheRoot = "user://Csharp/CharacterBinaryCache";

	public static int ClearAllCaches()
	{
		if (DirAccess.DirExistsAbsolute("user://Csharp/CharacterBinaryCache"))
		{
			return RemoveRecursive("user://Csharp/CharacterBinaryCache");
		}
		return 0;
	}

	private static int RemoveRecursive(string path)
	{
		if (!DirAccess.DirExistsAbsolute(path))
		{
			return 0;
		}
		DirAccess dirAccess = DirAccess.Open(path);
		if (dirAccess == null)
		{
			return 0;
		}
		int num = 0;
		string[] files = dirAccess.GetFiles();
		foreach (string file in files)
		{
			if (DirAccess.RemoveAbsolute(path.PathJoin(file)) == Error.Ok)
			{
				num++;
			}
		}
		files = dirAccess.GetDirectories();
		foreach (string file2 in files)
		{
			num += RemoveRecursive(path.PathJoin(file2));
		}
		if (DirAccess.RemoveAbsolute(path) == Error.Ok)
		{
			num++;
		}
		return num;
	}
}
