using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

// Disk storage only. No gameplay/scene changes. Slot numbers are 1..3.
public sealed class SaveFileStore
{
    public const int SlotCount = 3;
    private const string Format = "DungeonExpressSave";
    public string DirectoryPath { get; }

    [Serializable]
    private sealed class Envelope
    {
        public string format;
        public int fileVersion;
        public string payload;
        public string checksum;
    }

    public SaveFileStore(string directoryPath = null)
    {
        DirectoryPath = Path.GetFullPath(directoryPath ?? Path.Combine(Application.persistentDataPath, "Saves"));
    }

    public string GetSlotPath(int slot)
    {
        if (slot < 1 || slot > SlotCount) throw new ArgumentOutOfRangeException(nameof(slot));
        return Path.Combine(DirectoryPath, "slot_" + slot + ".json");
    }

    public bool Exists(int slot)
    {
        string path = GetSlotPath(slot);
        return File.Exists(path) || File.Exists(path + ".bak");
    }

    public void Save(int slot, SaveData data, bool overwrite = false)
    {
        string path = GetSlotPath(slot);
        SaveDataValidation.Validate(data);
        if (Exists(slot) && !overwrite) throw new IOException("Slot is occupied; confirm overwrite first.");
        Directory.CreateDirectory(DirectoryPath);
        string payload = JsonUtility.ToJson(data);
        string json = JsonUtility.ToJson(new Envelope
        {
            format = Format, fileVersion = 1, payload = payload, checksum = Hash(payload)
        }, true);
        string temporary = path + ".tmp";
        try
        {
            byte[] bytes = new UTF8Encoding(false).GetBytes(json);
            using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }
            if (File.Exists(path))
            {
                // A corrupt primary must not overwrite an existing healthy backup.
                bool healthy = CanRead(path);
                File.Replace(temporary, path, healthy ? path + ".bak" : null);
            }
            else File.Move(temporary, path);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    public SaveData Load(int slot, out bool usedBackup)
    {
        string path = GetSlotPath(slot);
        usedBackup = false;
        if (!Exists(slot)) throw new FileNotFoundException("Save slot is empty.", path);
        try { return Read(path); }
        catch (NotSupportedException) { throw; } // Never silently downgrade a future-version save.
        catch (Exception primaryError) when (IsReadError(primaryError))
        {
            if (!File.Exists(path + ".bak")) throw;
            SaveData backup = Read(path + ".bak");
            usedBackup = true;
            return backup;
        }
    }

    public void Delete(int slot)
    {
        string path = GetSlotPath(slot);
        if (!Directory.Exists(DirectoryPath)) return;
        // Delete backup first, so a partial deletion cannot resurrect an old save.
        foreach (string suffix in new[] { ".bak", ".tmp", "" })
            File.Delete(path + suffix);
    }

    private static SaveData Read(string path)
    {
        string json = File.ReadAllText(path, Encoding.UTF8);
        Envelope envelope;
        try { envelope = JsonUtility.FromJson<Envelope>(json); }
        catch (ArgumentException e) { throw new InvalidDataException("Invalid save JSON.", e); }
        if (envelope == null || envelope.format != Format || string.IsNullOrEmpty(envelope.payload))
            throw new InvalidDataException("Invalid save header.");
        if (envelope.fileVersion != 1) throw new NotSupportedException("Unsupported save file version.");
        if (!string.Equals(envelope.checksum, Hash(envelope.payload), StringComparison.Ordinal))
            throw new InvalidDataException("Save checksum mismatch.");
        SaveData data;
        try { data = JsonUtility.FromJson<SaveData>(envelope.payload); }
        catch (ArgumentException e) { throw new InvalidDataException("Invalid save payload.", e); }
        SaveDataValidation.Validate(data);
        return data;
    }

    private static bool CanRead(string path)
    {
        try { Read(path); return true; }
        catch (Exception e) when (IsReadError(e) || e is NotSupportedException) { return false; }
    }

    private static bool IsReadError(Exception e) =>
        e is IOException || e is InvalidDataException || e is UnauthorizedAccessException || e is ArgumentException;

    private static string Hash(string text)
    {
        using (var sha = SHA256.Create())
            return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(text)));
    }
}
