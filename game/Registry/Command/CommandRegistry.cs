using System.Collections.Generic;
using System.Globalization;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class CommandRegistry : RefCounted
{
	public static bool IsInit = false;

	public static System.Collections.Generic.Dictionary<string, CommandConfig> CommandDictionary = new System.Collections.Generic.Dictionary<string, CommandConfig>();

	public static void Init()
	{
		if (!IsInit)
		{
			IsInit = true;
			RegisterInit();
		}
	}

	public static void RegisterInit()
	{
		BuiltinCommands.Register();
		DebugCommands.Register();
	}

	public static void RegisterCommand(string name, string description, string usage, Callable callback, Array argsInfo = null)
	{
		string text = name.ToLower();
		CommandConfig commandConfig = new CommandConfig();
		commandConfig.Name = text;
		commandConfig.Description = description;
		commandConfig.Usage = usage;
		commandConfig.Callback = callback;
		Array<CommandArg> array = new Array<CommandArg>();
		if (argsInfo != null)
		{
			foreach (Variant item in argsInfo)
			{
				array.Add((CommandArg)(GodotObject)item);
			}
		}
		commandConfig.ArgsInfo = array;
		CommandDictionary[text] = commandConfig;
	}

	public static void UnregisterCommand(string name)
	{
		CommandDictionary.Remove(name.ToLower());
	}

	public static bool HasCommand(string name)
	{
		return CommandDictionary.ContainsKey(name.ToLower());
	}

	public static CommandConfig GetCommand(string name)
	{
		return CommandDictionary.GetValueOrDefault(name.ToLower());
	}

	public static System.Collections.Generic.Dictionary<string, CommandConfig> GetAllCommands()
	{
		return CommandDictionary;
	}

	public static Array<string> GetCommandNames()
	{
		Array<string> array = new Array<string>();
		foreach (string key in CommandDictionary.Keys)
		{
			array.Add(key);
		}
		array.Sort();
		return array;
	}

	public static void ExecuteCommand(string input, Callable outputCallback)
	{
		string text = input.StripEdges();
		if (text == "")
		{
			return;
		}
		if (!text.StartsWith("/"))
		{
			outputCallback.Call("[color=red]指令必须以 / 开头[/color]");
			return;
		}
		Array<string> array = _ParseCommand(text.Substr(1, text.Length - 1));
		if (array.Count == 0)
		{
			return;
		}
		string text2 = array[0].ToLower();
		Array<string> array2 = new Array<string>();
		if (array.Count > 1)
		{
			for (int i = 1; i < array.Count; i++)
			{
				array2.Add(array[i]);
			}
		}
		if (!CommandDictionary.ContainsKey(text2))
		{
			outputCallback.Call("[color=red]未知指令: /" + text2 + "  输入 /help 查看所有指令[/color]");
			return;
		}
		CommandConfig commandConfig = CommandDictionary[text2];
		Array array3 = _ConvertArgs(array2, commandConfig.ArgsInfo, outputCallback);
		if (array3 == null)
		{
			outputCallback.Call("[color=red]参数错误！用法: " + commandConfig.Usage + "[/color]");
		}
		else
		{
			commandConfig.Callback.Call(array3);
		}
	}

	public static Array<string> _ParseCommand(string input)
	{
		Array<string> array = new Array<string>();
		string text = "";
		bool flag = false;
		for (int i = 0; i < input.Length; i++)
		{
			string text2 = input.Substr(i, 1);
			if (text2 == "\"")
			{
				flag = !flag;
			}
			else if (text2 == " " && !flag)
			{
				if (text != "")
				{
					array.Add(text);
					text = "";
				}
			}
			else
			{
				text += text2;
			}
		}
		if (text != "")
		{
			array.Add(text);
		}
		return array;
	}

	private static Array _ConvertArgs(Array<string> args, Array<CommandArg> argsInfo, Callable outputCallback)
	{
		Array array = new Array();
		int i = 0;
		foreach (CommandArg item2 in argsInfo)
		{
			if (i >= args.Count)
			{
				if (item2.Required)
				{
					return null;
				}
				array.Add(item2.DefaultValue);
				continue;
			}
			Variant item = _ConvertValue(args[i], item2.Type);
			if (item.VariantType == Variant.Type.Nil)
			{
				outputCallback.Call($"[color=red]参数 '{item2.Name}' 类型错误，期望类型: {((Variant.Type)item2.Type/*cast due to .constrained prefix*/).ToString()}[/color]");
				return null;
			}
			array.Add(item);
			i++;
		}
		for (; i < args.Count; i++)
		{
			array.Add(args[i]);
		}
		return array;
	}

	private static Variant _ConvertValue(string value, int targetType)
	{
		Variant.Type type = (Variant.Type)targetType;
		Variant.Type num = type - 1;
		if ((ulong)num <= 3uL)
		{
			switch ((int)num)
			{
			case 3:
				return value;
			case 1:
			{
				if (int.TryParse(value, out var result2))
				{
					return result2;
				}
				return default(Variant);
			}
			case 2:
			{
				if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
				{
					return (float)result;
				}
				return default(Variant);
			}
			case 0:
				switch (value.ToLower())
				{
				case "1":
				case "yes":
				case "on":
				case "true":
					return true;
				case "0":
				case "off":
				case "no":
				case "false":
					return false;
				default:
					return default(Variant);
				}
			}
		}
		return value;
	}
}
