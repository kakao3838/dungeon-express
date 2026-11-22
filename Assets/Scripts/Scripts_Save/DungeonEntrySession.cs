using System;

public enum DungeonEntryState { Choosing, SelectingSlot, Saved, Skipped, Cancelled }

// Closing a window is not evidence of a successful save.
public sealed class DungeonEntrySession
{
    public DungeonEntryState State { get; private set; } = DungeonEntryState.Choosing;
    public bool CanEnter => State == DungeonEntryState.Saved || State == DungeonEntryState.Skipped;

    public void ChooseSave()
    {
        Require(DungeonEntryState.Choosing);
        State = DungeonEntryState.SelectingSlot;
    }

    public void SkipSave()
    {
        Require(DungeonEntryState.Choosing);
        State = DungeonEntryState.Skipped;
    }

    // Called only after SaveGameService.Save has returned successfully.
    public void Saved()
    {
        Require(DungeonEntryState.SelectingSlot);
        State = DungeonEntryState.Saved;
    }

    public void Cancel()
    {
        if (State == DungeonEntryState.Choosing || State == DungeonEntryState.SelectingSlot)
            State = DungeonEntryState.Cancelled;
    }

    private void Require(DungeonEntryState expected)
    {
        if (State != expected) throw new InvalidOperationException("Invalid dungeon-entry action: " + State);
    }
}
