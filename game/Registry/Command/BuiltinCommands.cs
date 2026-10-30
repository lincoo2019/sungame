using Godot;
using Godot.Collections;

[GlobalClass]
public partial class BuiltinCommands : RefCounted
{
	public static void Register()
	{
		CommandRegistry.RegisterCommand("help", "显示所有可用指令", "/help [页码]", Callable.From(delegate(int page)
		{
			_CmdHelp(page);
		}), new Array
		{
			new CommandArg("page", 2, _required: false, 1, "页码")
		});
		CommandRegistry.RegisterCommand("clear", "清空控制台输出", "/clear", Callable.From(delegate
		{
			_CmdClear();
		}));
		CommandRegistry.RegisterCommand("echo", "输出文本到控制台", "/echo <文本>", Callable.From(delegate(string text)
		{
			_CmdEcho(text);
		}), new Array
		{
			new CommandArg("text", 4, _required: true, default(Variant), "要输出的文本")
		});
		CommandRegistry.RegisterCommand("history", "显示指令历史记录", "/history", Callable.From(delegate
		{
			_CmdHistory();
		}));
		CommandRegistry.RegisterCommand("quit", "关闭游戏", "/quit", Callable.From(delegate
		{
			_CmdQuit();
		}));
		CommandRegistry.RegisterCommand("fps", "显示当前帧率", "/fps", Callable.From(delegate
		{
			_CmdFps();
		}));
		CommandRegistry.RegisterCommand("timeScale", "设置游戏时间缩放", "/timeScale <倍率>", Callable.From(delegate(double scale)
		{
			_CmdTimeScale(scale);
		}), new Array
		{
			new CommandArg("scale", 3, _required: true, default(Variant), "时间缩放倍率")
		});
	}

	public static void _CmdHelp(int page = 1)
	{
		Array<string> commandNames = CommandRegistry.GetCommandNames();
		int num = 8;
		int num2 = Mathf.Max(1, Mathf.CeilToInt((float)commandNames.Count / (float)num));
		page = Mathf.Clamp(page, 1, num2);
		CommandConsole.Instance.PrintLine($"[color=cyan]═══════ 指令列表 (第{page}页/共{num2}页) ═══════[/color]");
		int num3 = (page - 1) * num;
		int num4 = Mathf.Min(num3 + num, commandNames.Count);
		for (int i = num3; i < num4; i++)
		{
			string text = commandNames[i];
			CommandConfig command = CommandRegistry.GetCommand(text);
			CommandConsole.Instance.PrintLine("[color=green]/" + text + "[/color] - " + command.Description);
			CommandConsole.Instance.PrintLine("[color=gray]  用法: " + command.Usage + "[/color]");
		}
		if (num2 > 1)
		{
			CommandConsole.Instance.PrintInfo($"输入 /help {page + 1} 查看下一页");
		}
	}

	public static void _CmdClear()
	{
		CommandConsole.Instance.ClearLog();
	}

	public static void _CmdEcho(string text)
	{
		CommandConsole.Instance.PrintLine(text);
	}

	public static void _CmdHistory()
	{
		Array<string> history = CommandConsole.Instance.GetHistory();
		if (history.Count == 0)
		{
			CommandConsole.Instance.PrintInfo("暂无指令历史");
			return;
		}
		CommandConsole.Instance.PrintLine("[color=cyan]═══════ 指令历史 ═══════[/color]");
		for (int i = 0; i < history.Count; i++)
		{
			CommandConsole.Instance.PrintLine($"[color=gray]{i + 1}.[/color] {history[i]}");
		}
	}

	public static void _CmdQuit()
	{
		CommandConsole.Instance.PrintWarning("正在退出游戏...");
		((SceneTree)Engine.GetMainLoop()).Quit();
	}

	public static void _CmdFps()
	{
		CommandConsole.Instance.PrintInfo($"当前FPS: {Engine.GetFramesPerSecond()}");
	}

	public static void _CmdTimeScale(double scale)
	{
		Global.TimeScale = scale;
		CommandConsole.Instance.PrintSuccess($"时间缩放已设为 {scale:F1}x");
	}
}
