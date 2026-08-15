# Repository Guidelines

## Project Vision & Core Principles

**Project:** My Game World  
**Goal:** MMORPG leve baseado em composição procedural determinística.

Todas as decisões técnicas e de conteúdo devem respeitar estes princípios:

1. Assets visuais são finitos e pré-existentes.
2. Entidades são compostas em runtime.
3. Seeds devem produzir resultados determinísticos.
4. O servidor mantém autoridade sobre estado crítico.
5. O cliente é responsável pela representação visual.
6. NPCs possuem cognição parametrizada.
7. Sistemas devem suportar milhares de entidades.
8. IA complexa deve usar LOD cognitivo.
9. LLM não deve controlar o gameplay principal.
10. Sistemas procedurais devem ser versionados.

## Project Structure & Module Organization

This is a Unity 6 (`6000.4.0f1`) project using the Universal Render Pipeline. Keep runtime content under `Assets/`; scenes live in `Assets/Scenes`, render settings in `Assets/Settings`, and future game code should go in `Assets/Game/Scripts`. Place Edit Mode and Play Mode tests in `Assets/Tests/EditMode` and `Assets/Tests/PlayMode`, respectively. Package declarations belong in `Packages/`, Unity configuration in `ProjectSettings/`, and technical or product notes in `docs/`.

Never commit generated directories such as `Library/`, `Temp/`, `Logs/`, `UserSettings/`, or builds. Preserve every Unity `.meta` file when moving or adding assets.

## Build, Test, and Development Commands

- Open the repository through Unity Hub with Unity `6000.4.0f1` for normal development.
- `git lfs install` enables LFS hooks after cloning.
- `git lfs fsck` verifies locally stored LFS objects.
- `git lfs ls-files` lists binary assets tracked by LFS.
- `"<Unity.exe>" -batchmode -projectPath . -runTests -testPlatform EditMode -testResults TestResults.xml -quit` runs Edit Mode tests headlessly. Replace `<Unity.exe>` with the installed editor path; use `PlayMode` for Play Mode tests.

## Coding Style & Naming Conventions

Follow `.editorconfig`: UTF-8, LF endings, four-space indentation, and no trailing whitespace. Use PascalCase for C# types and public members, camelCase for parameters and locals, and `_camelCase` for private serialized fields. Name assets descriptively (`MainMenu.unity`, `PlayerController.cs`). Move and rename assets inside Unity.

## Testing Guidelines

Unity Test Framework `1.6.0` is installed, but there are no authored tests or coverage threshold. Name classes `<Subject>Tests` and methods `Method_State_ExpectedResult`. Prefer Edit Mode for pure logic and Play Mode for scene or integration behavior.

## Commit & Pull Request Guidelines

The existing history uses a short imperative commit subject, such as `Initialize Unity game repository`. Keep commits small and single-purpose. Use branches like `feature/player-movement`, `fix/camera-clipping`, or `chore/update-assets`.

Pull requests must explain purpose, changes, and validation; link related issues and include screenshots or video for visible changes. Confirm CI passes, Unity opens without compilation errors, and new binary assets are tracked by LFS.

## Security & Agent Instructions

Never commit tokens, signing keys, credentials, or `.env` files. Report vulnerabilities privately as described in `SECURITY.md`. Automated agents may edit and inspect local files but must not create branches, commit, push, merge, or change GitHub settings; repository owners perform those operations manually.
