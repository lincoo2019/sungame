using System;

public sealed class GuidStateMachineStableIdProvider : IStateMachineStableIdProvider
{
	public string CreateStableId(string scope)
	{
		return (string.IsNullOrWhiteSpace(scope) ? "item" : scope.Trim().ToLowerInvariant()) + "." + Guid.NewGuid().ToString("N");
	}
}
