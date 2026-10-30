using Godot;

namespace PVZHE.ModEditor.ScriptEditor;

public abstract partial class XWCodeCompletionProvider : RefCounted
{
	protected readonly CodeEdit CodeEdit;

	protected XWCodeCompletionProvider(CodeEdit codeEdit)
	{
		CodeEdit = codeEdit;
	}

	public virtual void RequestCompletion()
	{
		CodeEdit?.UpdateCodeCompletionOptions(force: true);
	}
}
