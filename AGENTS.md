# Repository Guidance

## Project
- This is a Unity 6 project using editor version `6000.1.14f1` and C# 9 (`netstandard2.1`). Keep changes compatible with that toolchain.
- Runtime scripts are under `Assets/Scripts/`. Use the existing MonoBehaviour and Unity lifecycle patterns in nearby scripts.
- Packages include URP, the Input System, and ML-Agents. Preserve the behavior and configuration of existing assets and packages; do not migrate systems or change package versions unless requested.
- There are currently no assembly definition files. Keep script dependencies compatible with the default `Assembly-CSharp` assembly.

## Editing
- Treat Unity scene, prefab, and asset serialization as authored project data. Make focused edits and preserve existing references and `.meta` files.
- Do not edit generated or transient content such as `Assembly-CSharp.csproj`, `Library/`, `Temp/`, `obj/`, or build outputs. Unity regenerates these files and directories.
- Follow naming, formatting, and input/physics conventions of the specific neighboring scripts being changed; avoid unrelated refactors.

## Validation
- Prefer validation in the Unity Editor, including the Console and Test Runner when relevant. The Unity Test Framework package is installed.
- A generated IDE project build can help catch C# errors when the local Unity Editor references are available, but it does not validate scenes, prefabs, or runtime behavior.
