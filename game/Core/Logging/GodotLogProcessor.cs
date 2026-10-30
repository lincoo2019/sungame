using System;
using System.Threading.Tasks;
using Godot;
using Microsoft.Extensions.Logging;
using ZLogger;

internal sealed class GodotLogProcessor : IAsyncLogProcessor, IAsyncDisposable
{
	public ValueTask DisposeAsync()
	{
		return default(ValueTask);
	}

	public void Post(IZLoggerEntry log)
	{
		string text = log.ToString();
		switch (log.LogInfo.LogLevel)
		{
		case LogLevel.Trace:
		case LogLevel.Debug:
			GD.Print(text);
			break;
		case LogLevel.Information:
			GD.Print(text);
			break;
		case LogLevel.Warning:
			GD.PushWarning(text);
			break;
		case LogLevel.Error:
		case LogLevel.Critical:
			GD.PrintErr(text);
			GD.PushError(text);
			break;
		default:
			GD.Print(text);
			break;
		}
		log.Return();
	}
}
