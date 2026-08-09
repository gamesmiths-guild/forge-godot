# Forge for Godot

Forge for Godot is an Unreal GAS-like gameplay framework for the Godot Engine.

It integrates the [Forge Gameplay System](https://github.com/gamesmiths-guild/forge) into Godot, providing a robust, data-driven foundation for gameplay features such as attributes, effects, gameplay tags, abilities, events, cues, and visual ability scripting through Statescript, fully aligned with Godot’s node, resource, and editor workflows.

This plugin enables you to:

- Use **ForgeEntity** nodes or implement `IForgeEntity` to integrate core Forge systems like attributes, effects, abilities, events and tags.
- Define attributes, effects, abilities, cues, and tags directly in the Godot editor.
- Apply and manage gameplay effects with area or raycasting nodes.
- Create hierarchical gameplay tags using the built-in Tags Editor.
- Trigger visual and audio feedback with the Cues system.
- Create player skills, attacks, or behaviors, with support for custom logic, costs, cooldowns, and triggers.
- Build ability behaviors visually with the Statescript graph editor, or implement custom behaviors in C#.

## Features

- **Effects System**: Comprehensive effect application and management, including stacking, periodic, instant, and infinite effects.
- **Attributes System**: Attribute management, supporting sets, modifiers, and configuration.
- **Tags System**: Full hierarchical tag system with Godot editor integration.
- **Abilities System**: Feature-complete ability system, supporting grant/removal, custom behaviors, triggers, cooldowns, and costs.
- **Events System**: Gameplay event bus supporting event-driven logic, subscriptions, and triggers.
- **Cues System**: Visual/audio feedback layer; decouples presentation from game logic.
- **Statescript**: Visual state-based scripting system for implementing ability behaviors with a built-in graph editor.
- **Editor Extensions**: Custom inspector elements, tag editor, and Statescript graph editor with Godot integration.
- **Custom Nodes**: Includes nodes like `ForgeEntity`, `ForgeAttributeSet`, `EffectArea2D`, and more.

## Installation

### Requirements

- Godot 4.7 or later, .NET version.
- .NET SDK 8.0 or later.
- **C# only.** Forge does not support GDScript-only projects.

### Steps

1. Install **Forge Gameplay System** from the Godot Asset Store, via the **Asset Store** tab in the Godot editor.

   When choosing which files to import, keep `Directory.Build.props` selected. MSBuild imports it automatically, and it is what wires Forge into your build without any edit to your `.csproj`. The `forge_samples` folder is optional.

2. If your project has no C# solution yet, create one via `Project > Tools > C# > Create C# solution`.

3. Back in the Godot editor, build your project by clicking `Build` in the top-right corner of the editor.

4. Enable **Forge Gameplay System** in `Project > Project Settings > Plugins`.

> **Already have a `Directory.Build.props`?** Don't let Forge's copy replace it. Skip that file when importing, and add this line to your own instead:
>
> ```xml
> <Import Project="addons/forge/Forge.props" />
> ```

## Getting Started

- See the [Quick Start Guide](https://github.com/gamesmiths-guild/forge-godot/blob/main/docs/quick-start.md) for a basic setup.
- See the [Gameplay Tags guide](https://github.com/gamesmiths-guild/forge-godot/blob/main/docs/gameplay-tags.md) for managing tags and tag sources.
- Install the optional `forge_samples` folder for 2D and 3D scenes demonstrating the system in action. To run them, open the **Tags** dock and click **Find Sources** to pick up `forge_samples/forge_samples_tags.tres`.

## Documentation

Full documentation, examples, and advanced usage are available in the [Forge for Godot GitHub repository](https://github.com/gamesmiths-guild/forge-godot).
For Statescript documentation, see the [Statescript guide](https://github.com/gamesmiths-guild/forge-godot/blob/main/docs/statescript/README.md).
For technical details about core systems, see the [Forge Gameplay System documentation](https://github.com/gamesmiths-guild/forge/blob/main/docs/README.md).

## License

This plugin is licensed under the same terms as the core [Forge Gameplay System](https://github.com/gamesmiths-guild/forge).
