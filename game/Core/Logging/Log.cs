using System;
using Microsoft.Extensions.Logging;
using ZLogger;
using ZLogger.Formatters;

public static class Log
{
	private static ILoggerFactory _factory;

	private static ILogger _rootLogger;

	private static bool _initialized;

	public static void Init(bool debugMode)
	{
		if (_initialized)
		{
			return;
		}
		LogLevel minLevel = (debugMode ? LogLevel.Debug : LogLevel.Information);
		_factory = LoggerFactory.Create(delegate(ILoggingBuilder builder)
		{
			builder.SetMinimumLevel(minLevel);
			builder.AddZLoggerLogProcessor(delegate(ZLoggerOptions options)
			{
				options.UsePlainTextFormatter(delegate(PlainTextZLoggerFormatter formatter)
				{
					MessageTemplateHandler format = new MessageTemplateHandler(5, 2);
					format.AppendLiteral("[");
					format.AppendFormatted(0);
					format.AppendLiteral("][");
					format.AppendFormatted(1);
					format.AppendLiteral("] ");
					formatter.SetPrefixFormatter(format, delegate(MessageTemplate template, LogInfo info)
					{
						template.Format(GetLevelTag(info.LogLevel), info.Category.Name ?? "");
					});
				});
				return new GodotLogProcessor();
			});
		});
		_rootLogger = _factory.CreateLogger("Game");
		_initialized = true;
	}

	public static ILogger CreateLogger<T>()
	{
		return CreateLogger(typeof(T).Name);
	}

	public static ILogger CreateLogger(string category)
	{
		EnsureInitialized();
		return _factory.CreateLogger(category);
	}

	public static void Debug(string message)
	{
		ILogger rootLogger = _rootLogger;
		bool enabled;
		ZLoggerDebugInterpolatedStringHandler message2 = new ZLoggerDebugInterpolatedStringHandler(0, 1, rootLogger, out enabled);
		if (enabled)
		{
			message2.AppendFormatted(message, 0, null, "message");
		}
		rootLogger.ZLogDebug(ref message2);
	}

	public static void Info(string message)
	{
		ILogger rootLogger = _rootLogger;
		bool enabled;
		ZLoggerInformationInterpolatedStringHandler message2 = new ZLoggerInformationInterpolatedStringHandler(0, 1, rootLogger, out enabled);
		if (enabled)
		{
			message2.AppendFormatted(message, 0, null, "message");
		}
		rootLogger.ZLogInformation(ref message2);
	}

	public static void Warning(string message)
	{
		ILogger rootLogger = _rootLogger;
		bool enabled;
		ZLoggerWarningInterpolatedStringHandler message2 = new ZLoggerWarningInterpolatedStringHandler(0, 1, rootLogger, out enabled);
		if (enabled)
		{
			message2.AppendFormatted(message, 0, null, "message");
		}
		rootLogger.ZLogWarning(ref message2);
	}

	public static void Error(string message)
	{
		ILogger rootLogger = _rootLogger;
		bool enabled;
		ZLoggerErrorInterpolatedStringHandler message2 = new ZLoggerErrorInterpolatedStringHandler(0, 1, rootLogger, out enabled);
		if (enabled)
		{
			message2.AppendFormatted(message, 0, null, "message");
		}
		rootLogger.ZLogError(ref message2);
	}

	public static void Error(Exception ex, string message = null)
	{
		ZLoggerErrorInterpolatedStringHandler message2;
		bool enabled;
		if (message != null)
		{
			ILogger rootLogger = _rootLogger;
			ILogger logger = rootLogger;
			message2 = new ZLoggerErrorInterpolatedStringHandler(2, 2, rootLogger, out enabled);
			if (enabled)
			{
				message2.AppendFormatted(message, 0, null, "message");
				message2.AppendLiteral(": ");
				message2.AppendFormatted(ex, 0, null, "ex");
			}
			logger.ZLogError(ref message2);
		}
		else
		{
			ILogger rootLogger = _rootLogger;
			ILogger logger2 = rootLogger;
			message2 = new ZLoggerErrorInterpolatedStringHandler(0, 1, rootLogger, out enabled);
			if (enabled)
			{
				message2.AppendFormatted(ex, 0, null, "ex");
			}
			logger2.ZLogError(ref message2);
		}
	}

	public static void Shutdown()
	{
		_factory?.Dispose();
		_factory = null;
		_rootLogger = null;
		_initialized = false;
	}

	private static void EnsureInitialized()
	{
		if (!_initialized)
		{
			Init(debugMode: false);
		}
	}

	private static string GetLevelTag(LogLevel level)
	{
		return level switch
		{
			LogLevel.Trace => "TRACE", 
			LogLevel.Debug => "DEBUG", 
			LogLevel.Information => "INFO", 
			LogLevel.Warning => "WARN", 
			LogLevel.Error => "ERROR", 
			LogLevel.Critical => "FATAL", 
			_ => "NONE", 
		};
	}
}
