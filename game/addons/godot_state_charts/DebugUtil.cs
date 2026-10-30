using Godot;

public static class DebugUtil
{
	public static string PathOf(Node node)
	{
		if (node == null)
		{
			return "";
		}
		if (!node.IsInsideTree())
		{
			return string.Concat(node.Name, " (not in tree)");
		}
		return node.GetPath().ToString();
	}
}
