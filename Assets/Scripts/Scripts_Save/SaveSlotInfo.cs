using System;

public enum SaveSlotState { Empty, Ready, Backup, Unreadable }

public sealed class SaveSlotInfo
{
    public int Slot { get; }
    public SaveSlotState State { get; }
    public SaveData Data { get; }
    public bool CanLoad => State == SaveSlotState.Ready || State == SaveSlotState.Backup;
    public bool Occupied => State != SaveSlotState.Empty;

    private SaveSlotInfo(int slot, SaveSlotState state, SaveData data = null)
    {
        Slot = slot;
        State = state;
        Data = data;
    }

    public static SaveSlotInfo Read(SaveFileStore files, int slot)
    {
        files.GetSlotPath(slot); // Invalid slot numbers are programming errors.
        try
        {
            if (!files.Exists(slot)) return new SaveSlotInfo(slot, SaveSlotState.Empty);
            bool backup;
            SaveData data = files.Load(slot, out backup);
            return new SaveSlotInfo(slot, backup ? SaveSlotState.Backup : SaveSlotState.Ready, data);
        }
        catch (Exception e) when (e is System.IO.IOException || e is System.IO.InvalidDataException ||
            e is UnauthorizedAccessException || e is NotSupportedException || e is ArgumentException)
        {
            return new SaveSlotInfo(slot, SaveSlotState.Unreadable);
        }
    }
}
