using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Godot;

public static class TransientStaticTextureRelease
{
	private static readonly FieldInfo[] TextureFields = DiscoverTextureFields();

	public static int Release()
	{
		int num = 0;
		FieldInfo[] textureFields = TextureFields;
		foreach (FieldInfo fieldInfo in textureFields)
		{
			try
			{
				if (fieldInfo.GetValue(null) is Texture2D)
				{
					fieldInfo.SetValue(null, null);
					num++;
				}
			}
			catch (Exception)
			{
			}
		}
		return num;
	}

	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Godot preserves registered script types; missing optional types only reduce best-effort cache cleanup.")]
	[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Texture fields are discovered best-effort and every reflective access is guarded.")]
	private static FieldInfo[] DiscoverTextureFields()
	{
		List<FieldInfo> list = new List<FieldInfo>();
		Type[] types;
		try
		{
			types = typeof(TransientStaticTextureRelease).Assembly.GetTypes();
		}
		catch (ReflectionTypeLoadException ex)
		{
			types = ex.Types;
		}
		Type[] array = types;
		foreach (Type type in array)
		{
			if (type == null || IsPersistentAnimationType(type))
			{
				continue;
			}
			FieldInfo[] fields = type.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (FieldInfo fieldInfo in fields)
			{
				if (!fieldInfo.IsInitOnly && !fieldInfo.IsLiteral && typeof(Texture2D).IsAssignableFrom(fieldInfo.FieldType))
				{
					list.Add(fieldInfo);
				}
			}
		}
		return list.ToArray();
	}

	private static bool IsPersistentAnimationType(Type type)
	{
		return (type.FullName ?? type.Name).StartsWith("AdobeAnimate", StringComparison.Ordinal);
	}
}
