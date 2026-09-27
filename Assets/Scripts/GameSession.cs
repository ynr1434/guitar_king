public static class GameSession
{
    private static SongDifficulty selectedDifficulty = SongDifficulty.Normal;

    public static string SelectedSongFolder { get; private set; }
    public static SongDifficulty SelectedDifficulty => IsSupportedDifficulty(selectedDifficulty)
        ? selectedDifficulty
        : SongDifficulty.Normal;

    public static void SelectSong(string folderName)
    {
        SelectedSongFolder = folderName;
    }

    public static void SelectSong(string folderName, SongDifficulty difficulty)
    {
        SelectedSongFolder = folderName;
        selectedDifficulty = IsSupportedDifficulty(difficulty) ? difficulty : SongDifficulty.Normal;
    }

    public static void ClearSelection()
    {
        SelectedSongFolder = null;
    }

    private static bool IsSupportedDifficulty(SongDifficulty difficulty)
    {
        return difficulty == SongDifficulty.Normal ||
               difficulty == SongDifficulty.Hard ||
               difficulty == SongDifficulty.Expert;
    }
}
