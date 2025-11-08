# Forge for Godot

A Godot plugin for integrating the [Forge Gameplay System](https://github.com/gamesmiths-guild/forge) into Godot Engine.

Forge for Godot bridges the powerful, data-driven systems of the Forge system with Godot Engine's node-based architecture. This plugin provides custom nodes, resources, and editor extensions that make implementing robust gameplay systems in Godot straightforward and maintainable.

The plugin allows Godot developers to leverage Forge's comprehensive gameplay system with its attribute management, effects system, and tagging capabilities, without having to reimplement these systems from scratch for each project.

**Keywords:** godot plugin, gameplay system, C# game development, attribute system, status effects, tag system, gameplay integration

## Quick Start

New to Forge for Godot? Check out the [Quick Start Guide](docs/quick-start.md) to integrate Forge into your Godot project in minutes.

If you'd like to see sample scenes demonstrating the system in action, you can clone the repository directly and explore the examples included in the `examples` folder.

## Architecture Overview

Forge for Godot extends the core Forge system architecture with Godot-specific implementations:

### Core Integration

- **ForgeBootstrap**: Autoload singleton that initializes the core system managers.
- **Tags Editor**: Built-in editor tool for managing hierarchical gameplay tags.
- **Custom Resources**: Godot resources for configuring Forge components through the Inspector.
- **Custom Nodes**: Node classes that encapsulate Forge concepts for easy integration and visual composition in the scene tree.

### Entity Integration

Game objects use Forge in one of two ways:

- **ForgeEntity Node**: Add this node as a child to provide Forge capabilities to any Godot node.
- **IForgeEntity Implementation**: Implement the interface directly on your custom node classes.

Both approaches provide:

- `Attributes` - Manages all attributes and attribute sets.
- `Tags` - Handles base and modifier tags with automatic inheritance.
- `EffectsManager` - Controls effect application, stacking, and lifecycle.

### Godot-Specific Features

Forge for Godot includes specialized nodes that integrate Forge concepts with Godot's workflow:

- **ForgeAttributeSet**: Configure attribute sets directly in the Godot editor.
- **EffectArea2D/3D**: Apply effects to entities entering/exiting areas.
- **EffectRayCast2D/3D**: Apply effects to entities hit by raycasts.
- **EffectShapeCast2D/3D**: Apply effects to entities hit by shape casts.
- **ForgeCueHandler**: Base class for implementing visual and audio feedback.

## Project Status

⚠️ **Work in Progress** - This plugin is currently under active development and not ready for production use.

⚠️ **Godot C# Only** - Currently only works with Godot projects using C#.

### Current Features ✅

- **Tags System**: Complete hierarchical tag system with editor integration.
- **Attributes System**: Full attribute management with editor configuration.
- **Effects System**: Comprehensive effect application with specialized nodes.
- **Cues System**: Visual feedback system for effect application/removal.
- **Editor Extensions**: Custom inspector elements and tag editor.

### Planned Features 🚧

- **Abilities System**: Complete ability system similar to GAS abilities.
- **Multiplayer Support**: Network replication for all systems.
- **Events System**: Gameplay event handling and propagation.

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

## Documentation

For comprehensive documentation, explore the [docs](docs) directory:

- [Documentation Overview](docs/README.md)
- [Quick Start Guide](docs/quick-start.md)
- [Custom Nodes](docs/nodes.md)

## Contributing

This project is not currently accepting contributions as it's still in early development. However, if you're interested in contributing or have suggestions, feel free to reach out via GitHub issues or discussions.

## License

Copyright © Gamesmiths Guild. See [LICENSE](LICENSE) for details.
