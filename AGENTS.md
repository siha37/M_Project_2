# Repository Guidelines

## Project Structure & Module Organization
- Unity client in `Assets/` (custom gameplay code under `Assets/MyFolder/1. Scripts/…`).
- Packages and settings in `Packages/`, `ProjectSettings/`, `UserSettings/`.
- Build artifacts and caches: `Library/`, `Temp/`, `Logs/` (do not edit; git-ignored).
- Python game server in `Server/` (`main.py`, `handler.py`, `room.py`, `config.json`).

## Build, Test, and Development Commands
- Unity (Editor): open the project folder in Unity Hub; build via File → Build Settings.
- Unity (CLI tests): `"C:\\Program Files\\Unity\\Hub\\Editor\\<version>\\Editor\\Unity.exe" -projectPath . -runTests -testPlatform playmode -logFile Logs\\test_playmode.log`.
- Server (run): `python Server/main.py` (reads `Server/config.json`, writes `Server/server.log`).
- Server (venv, optional): `py -m venv .venv && .venv\\Scripts\\activate && pip install -r requirements.txt`.

## Coding Style & Naming Conventions
- C# (Unity): 4 spaces; Allman braces; PascalCase for types/methods; camelCase for fields; use `[SerializeField] private` fields with public read-only properties. Namespaces mirror folder path (e.g., `MyFolder._1._Scripts…`).
- Assets: avoid renaming top-level folders; keep feature code under `Assets/MyFolder/`.
- Python (Server): PEP 8; 4 spaces; snake_case for files/functions; type hints encouraged.

## Testing Guidelines
- Unity: place EditMode tests in `Assets/Tests/EditMode/` and PlayMode tests in `Assets/Tests/PlayMode/` (NUnit). Name tests `*Tests.cs`. Run via Test Runner or CLI (above).
- Server: add `Server/tests/` with `test_*.py` (pytest). Example: `pytest -q Server/tests`.

## Commit & Pull Request Guidelines
- History shows short, topic-focused messages (often Korean), no strict prefixes. Prefer imperative tense, e.g., `Fix bullet size in playmode`.
- Recommended scope tags when helpful: `[Unity]`, `[Server]`, `[Netcode]`.
- PRs should include: purpose/linked issues, screenshots or GIFs for gameplay/UI, reproduction and test steps, impacted scenes/prefabs/assets, and config changes (`Server/config.json`).

## Security & Configuration Tips
- Do not commit secrets; keep `Server/config.json` generic or document local overrides.
- Respect existing `.gitignore` for `Library/`, `Temp/`, `Logs/`, `obj/`.
- Agents: never modify `Library/`, `Temp/`, or third‑party PackageCache; make focused changes under `Assets/MyFolder/` and `Server/` only.
