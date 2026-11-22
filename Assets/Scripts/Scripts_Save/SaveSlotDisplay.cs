using System;
using System.Globalization;

public static class SaveSlotDisplay
{
    public static string SavedAt(SaveData data)
    {
        DateTimeOffset timestamp;
        if (data == null || !DateTimeOffset.TryParse(data.savedAtUtc,
            CultureInfo.InvariantCulture, DateTimeStyles.None, out timestamp))
            return "-";
        return timestamp.ToLocalTime().ToString("yyyy.MM.dd HH:mm", CultureInfo.InvariantCulture);
    }

    public static string PlayTime(SaveData data)
    {
        if (data == null || float.IsNaN(data.playTimeSeconds) ||
            float.IsInfinity(data.playTimeSeconds) || data.playTimeSeconds < 0f)
            return "--:--:--";
        double seconds = Math.Floor(data.playTimeSeconds);
        return string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}:{2:00}",
            Math.Floor(seconds / 3600), Math.Floor(seconds / 60) % 60, seconds % 60);
    }

    public static string Chapter(SaveData data)
    {
        if (data == null) return "-";
        return data.chapter > 0 ? "Chapter " + data.chapter : "Prologue";
    }
}
