# Installation

## Requirements

- Godot 4.7 or later, .NET version.
- .NET SDK 8.0 or later.
- **C# only.** Forge does not support GDScript-only projects.

## From the Asset Store

1. Install **Forge Gameplay System** from the Godot Asset Store, via the **Asset Store** tab in the Godot editor.

   When choosing which files to import, keep `Directory.Build.props` selected. MSBuild imports it automatically, and it is what wires Forge into your build without any edit to your `.csproj`. The `forge_samples` folder is optional; see [Sample Project](#sample-project).

2. If your project has no C# solution yet, create one via `Project > Tools > C# > Create C# solution`.

3. Build your project by clicking `Build` in the top-right corner of the editor.

4. Enable **Forge Gameplay System** under `Project > Project Settings > Plugins`.

> **Already have a `Directory.Build.props`?** Don't let Forge's copy replace it. Skip that file when importing, and add this line to your own instead:
> ```xml
> <Import Project="addons/forge/Forge.props" />
> ```

## Manual installation

Download the `.zip` attached to the [latest release](https://github.com/gamesmiths-guild/forge-godot/releases/latest) and extract it into your project root. It contains exactly the same files as the Asset Store package.

Alternatively, copy the `addons/forge` folder and `Directory.Build.props` from this repository into your project. If you'd rather not add `Directory.Build.props`, add this line to your `.csproj` before the closing `</Project>` tag instead:

```xml
<Import Project="addons/forge/Forge.props" />
```

Then follow steps 2–4 above.

## Sample Project

The `forge_samples` folder contains the Quick Start's finished scene plus 2D and 3D demos of attributes, effects, abilities, cues and Statescript in a running game. It is optional — skip it during import if you don't want sample code compiled into your project, along with the two sample attribute sets in `forge_generated/attribute_sets/` (`HeroAttributes` and `MovementAttributes`), which the plugin generated from the samples' definitions.

The samples declare their own gameplay tags in `forge_samples/forge_samples_tags.tres`. To run them, open the **Tags** dock and click **Find Sources**, which will offer to add it. Then open `forge_samples/Main.tscn` and run it to pick a demo from the hub.

## Local Core Development (Optional)

When developing `forge-godot` together with the core `forge` repository, `Forge.props` automatically switches to local project references when both repositories are cloned as sibling folders:

```text
Projects/
├── forge/
└── forge-godot/
```

This allows direct source-level development across both repositories without requiring local NuGet packages.

## Next

Follow the [Quick Start](quick-start.md) to build your first Forge scene.
