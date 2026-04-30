# Chat Review Export Contents

Generate with:

```powershell
Tools/GenerateChatReviewExport.ps1
```

Output:
- `ProjectSnapshot/CHAT_REVIEW_EXPORT.zip`

Included:
- `Assets/Game/**/*.cs`
- `Assets/Game/**/*.asmdef`
- `Assets/Game/**/*.asmref`
- small text gameplay files under `Assets/Game` such as `.asset`, `.prefab`, `.json`, `.txt`, `.md`, `.uxml`, `.uss`, and `.inputactions`
- `ProjectSnapshot/*.md`
- `Packages/manifest.json`
- `Packages/packages-lock.json`
- `ProjectSettings/ProjectVersion.txt`
- `ProjectSettings/TagManager.asset`
- `ProjectSettings/InputManager.asset`

Excluded:
- `Library`, `Logs`, `Temp`, `Obj`, `UserSettings`, `.git`, and build output folders
- generated zip exports and temporary export folders
- imported models, images, audio, video, binaries, and heavy third-party payloads
- large text assets over the script threshold, currently 512 KB by default

Notes:
- The export is allowlisted on purpose so Unity cache folders and heavyweight content cannot sneak into the review package.
- The zip itself is ignored by git and should be regenerated locally when needed.
