# Forge for Godot

Forge for Godot is an Unreal GAS-like gameplay framework for the Godot Engine.

It integrates the [Forge Gameplay System](https://github.com/gamesmiths-guild/forge) into Godot, providing a robust, data-driven foundation for gameplay features such as attributes, effects, gameplay tags, abilities, events, and cues, fully aligned with Godot’s node, resource, and editor workflows.

This plugin enables you to:

- Use **ForgeEntity** nodes or implement `IForgeEntity` to integrate core Forge systems like attributes, effects, abilities, events and tags.
- Define attributes, effects, abilities, cues, and tags directly in the Godot editor.
- Apply and manage gameplay effects with area or raycasting nodes.
- Create hierarchical gameplay tags using the built-in Tags Editor.
- Trigger visual and audio feedback with the Cues system.
- Create player skills, attacks, or behaviors, with support for custom logic, costs, cooldowns, and triggers.

## Features

- **Effects System**: Comprehensive effect application and management, including stacking, periodic, instant, and infinite effects.
- **Attributes System**: Attribute management, supporting sets, modifiers, and configuration.
- **Tags System**: Full hierarchical tag system with Godot editor integration.
- **Abilities System**: Feature-complete ability system, supporting grant/removal, custom behaviors, triggers, cooldowns, and costs.
- **Events System**: Gameplay event bus supporting event-driven logic, subscriptions, and triggers.
- **Cues System**: Visual/audio feedback layer; decouples presentation from game logic.
- **Editor Extensions**: Custom inspector elements and tag editor with Godot integration.
- **Custom Nodes**: Includes nodes like `ForgeEntity`, `ForgeAttributeSet`, `EffectArea2D`, and more.

## Installation

### Requirements

- Godot 4.6 or later with .NET support.
- .NET SDK 8.0 or later.

### Steps

1. [Install the plugin](https://docs.godotengine.org/en/stable/tutorials/plugins/editor/installing_plugins.html) by copying over the `addons` folder.
2. Add the following line in your `.csproj` file (before the closing `</Project>` tag). The `.csproj` file can be created through Godot by navigating to `Project > Tools > C# > Create C# solution`:
   ```xml
   <Import Project="addons/forge/Forge.props" />
   ```
3. Back in the Godot editor, build your project by clicking `Build` in the top-right corner of the script editor.
4. Enable **Forge Gameplay System** in `Project > Project Settings > Plugins`.

## Getting Started

- See the [Quick Start Guide](https://github.com/gamesmiths-guild/forge-godot/blob/main/docs/quick-start.md) for a basic setup.
- Explore [sample scenes](https://github.com/gamesmiths-guild/forge-godot/tree/main/examples) by cloning the full repo.

## Documentation

Full documentation, examples, and advanced usage are available in the [Forge for Godot GitHub repository](https://github.com/gamesmiths-guild/forge-godot).
For technical details about core systems, see the [Forge Gameplay System documentation](https://github.com/gamesmiths-guild/forge/blob/main/docs/README.md).

## License

This plugin is licensed under the same terms as the core [Forge Gameplay System](https://github.com/gamesmiths-guild/forge).
