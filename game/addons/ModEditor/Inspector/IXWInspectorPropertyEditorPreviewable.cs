using Godot;

namespace PVZHE.ModEditor.Inspector;

public interface IXWInspectorPropertyEditorPreviewable
{
	void OnPreviewReady(string path, Texture2D previewTexture, Texture2D previewSmall);
}
