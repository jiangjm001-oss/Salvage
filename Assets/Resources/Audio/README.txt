Audio Resources Folder Structure
=================================

This folder contains audio files that can be loaded at runtime by the AudioManager.

Folder Structure:
- SFX/     : Sound effects (e.g., item_pickup.wav, zoom_in.wav, trigger.wav)
- Music/   : Background music tracks

Supported Audio Formats:
- .wav (recommended for SFX)
- .mp3 (recommended for Music)
- .ogg

How to Use:
1. Place your audio files in the appropriate subfolder
2. Remove the file extension when referencing in code
3. Use the path relative to Resources folder

Examples:
- File: Resources/Audio/SFX/item_pickup.wav
  Code: AudioManager.Instance.PlaySFX("Audio/SFX/item_pickup");

- File: Resources/Audio/Music/background.mp3
  Code: AudioManager.Instance.PlayMusic(...);

Common SFX Files Needed:
- item_pickup.wav  : Played when picking up items
- zoom_in.wav      : Played when zooming into objects
- trigger.wav      : Played when triggering events
