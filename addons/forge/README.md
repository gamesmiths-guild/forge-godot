# Forge for Godot

Forge for Godot is a plugin that integrates the [Forge Gameplay System](https://github.com/gamesmiths-guild/forge) into the Godot Engine. It bridges the powerful, data-driven systems of Forge with Godot's node-based architecture, providing tools to streamline game development.

This plugin enables you to:

- Use **ForgeEntity** nodes or implement `IForgeEntity` to integrate core Forge systems like attributes, effects, and tags.
- Configure attributes and effects directly in the Godot editor using custom resources and inspector properties.
- Apply and manage gameplay effects with area or raycasting nodes.
- Create hierarchical gameplay tags using the built-in Tags Editor.
- Trigger visual and audio feedback with the Cues system.

## Features

- **Attributes System**: Manage gameplay attributes with configurable ranges, modifiers, and calculations.
- **Effects System**: Apply instant, periodic, or infinite effects with stacking and custom rules.
- **Tags System**: Define hierarchical tags for classification and targeting.
- **Cues System**: Translate gameplay events into visual and audio feedback.
- **Custom Nodes**: Includes nodes like `ForgeEntity`, `ForgeAttributeSet`, `EffectArea2D`, and more.

## Installation

### Requirements

- Godot 4.4 or later with .NET support.
- .NET SDK 8.0 or later.

### Steps

1. [Install the plugin](https://docs.godotengine.org/en/stable/tutorials/plugins/editor/installing_plugins.html) by copying over the `addons` folder.
2. Add the following line in your `.csproj` file (before the closing `</Project>` tag). The `.csproj` file can be created through Godot by navigating to `Project > Tools > C# > Create C# solution`:
   ```xml
   <Import Project="addons/forge/Forge.props" />
   ```
3. Back in the Godot editor, build your project by clicking `Build` in the top-right corner of the script editor.
4. Enable **Forge Gameplay System** in `Project > Project Settings > Plugins`.

## Quick Start

Refer to the [Quick Start Guide](https://github.com/gamesmiths-guild/forge-godot/blob/main/docs/quick-start.md) to set up your project and start using Forge in minutes.

If you'd like to see sample scenes demonstrating the system in action, you can clone the repository directly and explore the examples included in the `examples` folder.

## Documentation

For detailed documentation, examples, and advanced usage, visit the [Forge for Godot repository](https://github.com/gamesmiths-guild/forge-godot).

## License

This plugin is licensed under the terms of the [Forge Gameplay System](https://github.com/gamesmiths-guild/forge) license.
