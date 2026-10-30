using Godot;

namespace PVZHE.ModEditor.ResourceEditors.GUI.GameplayLogic;

public interface IXWGameplayLogicPresenter
{
	bool CanPresent(Resource resource);

	void Mount(XWGameplayLogicPresentationContext context);

	void Refresh();

	void Unmount();
}
