using System;
using Godot;

public sealed class StateMachineAuthorTypeDescriptor
{
	public string TypeId { get; }

	public string DisplayName { get; }

	public string Description { get; }

	public string IconPath { get; }

	public bool IsState { get; }

	internal Func<Resource> Factory { get; }

	internal StateMachineAuthorTypeDescriptor(string typeId, string displayName, string description, string iconPath, bool isState, Func<Resource> factory)
	{
		TypeId = typeId ?? string.Empty;
		DisplayName = displayName ?? string.Empty;
		Description = description ?? string.Empty;
		IconPath = iconPath ?? string.Empty;
		IsState = isState;
		Factory = factory;
	}
}
