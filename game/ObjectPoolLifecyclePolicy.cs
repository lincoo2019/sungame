using Godot;

internal static class ObjectPoolLifecyclePolicy
{
	public static bool HasBuiltInCSharpScript(Node node)
	{
		if (!GodotObject.IsInstanceValid(node))
		{
			return false;
		}
		Variant script = node.GetScript();
		if (script.VariantType != Variant.Type.Nil)
		{
			return script.AsGodotObject() is CSharpScript;
		}
		return false;
	}
}
