using Godot;
using Godot.Collections;

public static class DictionaryExtensions
{
	public static Variant GetValueOrDefault(this Dictionary dictionary, Variant key, Variant defaultValue = default(Variant))
	{
		if (dictionary.ContainsKey(key))
		{
			return dictionary[key];
		}
		return defaultValue;
	}
}
