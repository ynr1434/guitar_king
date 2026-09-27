# Guitar King

Private Unity rhythm-game prototype. First friends build: **0.1.0**.

## Play

- Hold top-row **1 / 2 / 3 / 4 / 5** for the colored frets.
- Press **Space** to strum. Chords require all their frets.
- Keep holding sustain notes until their tails end.
- **Esc** pauses the song. **HOW TO PLAY** in the pause menu opens the Russian guide.
- Select a song in SET LIST. Normal difficulty is currently available.

## Open the project

Use **Unity 6000.3.24f1**, then open `Assets/Scenes/SongSelect.unity`.
Assets, Packages and ProjectSettings belong in source control; Library and Builds do not.

## Build

Install macOS Build Support and Windows Build Support (Mono) for this editor.
Use **Guitar King → Build → macOS for friends / Windows for friends**.

- macOS: `Builds/macOS/Guitar King.app`, universal Intel + Apple Silicon.
- Windows: `Builds/Windows/Guitar King.exe`, x86-64. Share the entire Windows folder.

Both builds start in SongSelect and include RhythmGame.
Song packages live in `Assets/StreamingAssets/Songs/<folder>/` and contain metadata, charts, audio and optional background video.

This repository contains prototype media supplied for testing. No license to redistribute third-party songs or videos is granted by this repository.
