using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Godot;
using Godot.Collections;

namespace PVZHE.ModEditor.PVZIntegration;

public static class PVZApiRegistry
{
	public class MethodInfo
	{
		public string ClassName;

		public string MethodName;

		public string ReturnTypeName;

		public List<ParamInfo> Parameters = new List<ParamInfo>();

		public bool IsStatic;
	}

	public class ParamInfo
	{
		public string Name;

		public string TypeName;

		public int VariantType;

		public string ClassName;
	}

	private static readonly System.Collections.Generic.Dictionary<string, List<MethodInfo>> _classMethods = new System.Collections.Generic.Dictionary<string, List<MethodInfo>>();

	private static bool _initialized = false;

	public static void Initialize()
	{
		if (!_initialized)
		{
			_initialized = true;
			RegisterClass("TowerDefenseManager", typeof(TowerDefenseManager));
			RegisterClass("BattleEventBus", typeof(BattleEventBus));
			RegisterClass("ResourceManager", typeof(ResourceManager));
			RegisterClass("ObjectManager", typeof(ObjectManager));
		}
	}

	private static void RegisterClass(string displayName, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
	{
		List<MethodInfo> list = new List<MethodInfo>();
		BindingFlags bindingAttr = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;
		System.Reflection.MethodInfo[] methods = type.GetMethods(bindingAttr);
		foreach (System.Reflection.MethodInfo methodInfo in methods)
		{
			if (!methodInfo.IsSpecialName && (!methodInfo.Name.StartsWith("_") || !(methodInfo.Name != "_Ready")))
			{
				MethodInfo methodInfo2 = new MethodInfo
				{
					ClassName = displayName,
					MethodName = methodInfo.Name,
					ReturnTypeName = methodInfo.ReturnType.Name,
					IsStatic = methodInfo.IsStatic
				};
				ParameterInfo[] parameters = methodInfo.GetParameters();
				foreach (ParameterInfo parameterInfo in parameters)
				{
					methodInfo2.Parameters.Add(new ParamInfo
					{
						Name = parameterInfo.Name,
						TypeName = parameterInfo.ParameterType.Name,
						VariantType = MapToVariantType(parameterInfo.ParameterType),
						ClassName = ((parameterInfo.ParameterType.IsClass && parameterInfo.ParameterType != typeof(string)) ? parameterInfo.ParameterType.Name : "")
					});
				}
				list.Add(methodInfo2);
			}
		}
		_classMethods[displayName] = list;
		GD.Print($"[PVZApiRegistry] 注册 {displayName}: {list.Count} 个方法");
	}

	private static int MapToVariantType(Type type)
	{
		if (type == typeof(bool))
		{
			return 1;
		}
		if (type == typeof(int) || type == typeof(long))
		{
			return 2;
		}
		if (type == typeof(float) || type == typeof(double))
		{
			return 3;
		}
		if (type == typeof(string))
		{
			return 4;
		}
		if (type == typeof(Vector2))
		{
			return 5;
		}
		if (type == typeof(Vector3))
		{
			return 9;
		}
		if (type == typeof(Color))
		{
			return 20;
		}
		_ = type == typeof(Node);
		return 24;
	}

	public static List<string> GetRegisteredClasses()
	{
		Initialize();
		return new List<string>(_classMethods.Keys);
	}

	public static List<MethodInfo> GetMethods(string className)
	{
		Initialize();
		if (!_classMethods.TryGetValue(className, out var value))
		{
			return new List<MethodInfo>();
		}
		return value;
	}

	public static Dictionary ToMethodData(MethodInfo info)
	{
		Dictionary dictionary = new Dictionary
		{
			{ "base_class_name", info.ClassName },
			{ "method_name", info.MethodName },
			{ "is_static", info.IsStatic },
			{ "return_type", info.ReturnTypeName }
		};
		Godot.Collections.Array array = new Godot.Collections.Array();
		foreach (ParamInfo parameter in info.Parameters)
		{
			array.Add(new Dictionary
			{
				{ "name", parameter.Name },
				{ "type", parameter.VariantType },
				{ "class_name", parameter.ClassName }
			});
		}
		dictionary["args"] = array;
		return dictionary;
	}
}
