using Godot;

namespace PVZHE.ModEditor.ResourceEditors.GUI.GameplayLogic;

public sealed record XWGameplayResourceChoice(string Key, string DisplayName, string ResourcePath, Texture2D Thumbnail, XWGameplayResourceKind Kind, bool IsModResource);
