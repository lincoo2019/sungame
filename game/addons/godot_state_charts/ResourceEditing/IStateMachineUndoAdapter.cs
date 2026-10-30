public interface IStateMachineUndoAdapter
{
	bool CanUndo { get; }

	bool CanRedo { get; }

	void Commit(string label, StateMachineEditCommand command);

	void Undo();

	void Redo();
}
