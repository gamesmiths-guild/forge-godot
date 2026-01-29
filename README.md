# Forge for Godot

[![Godot .NET](https://img.shields.io/badge/Godot-4.6%2B%20.NET-478cbf)](https://godotengine.org/download/)
[![License](https://img.shields.io/github/license/gamesmiths-guild/forge-godot)](LICENSE)

Forge for Godot is an Unreal GAS-like gameplay framework built for the Godot Engine.

It integrates the [Forge Gameplay System](https://github.com/gamesmiths-guild/forge) into Godot, providing a robust, data-driven foundation for implementing gameplay features such as attributes, effects, gameplay tags, abilities, events, and cues, fully aligned with Godot’s node, resource, and editor workflows.

Forge for Godot provides custom nodes, resources, and editor extensions that make building scalable and maintainable gameplay systems straightforward, allowing developers to leverage Forge’s architecture without reimplementing complex gameplay logic from scratch.

**Keywords:** godot plugin, gameplay framework, C#, attributes, gameplay effects, abilities, gameplay tags

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

- **Attributes**: Manages all attribute sets, with lifecycle and modification handled by Forge.
- **Tags**: Handles base and modifier tags with automatic inheritance; supports runtime addition and removal via effects.
- **EffectsManager**: Controls effect application, stacking, periodic execution, expiration, and clean up.
- **Abilities**: Grants, activates, or removes gameplay abilities, including custom ability logic, cooldowns, costs, and triggers.
- **Events**: Entity or global event bus for raising, subscribing, and handling gameplay events.

### Godot-Specific Features

Forge for Godot includes specialized nodes and resources to integrate Forge concepts into Godot's workflow:

- **ForgeAttributeSet**: Configure attribute sets directly in the Godot editor.
- **EffectArea2D/3D**: Apply effects to entities entering/exiting areas.
- **EffectRayCast2D/3D**: Apply effects to entities hit by raycasts.
- **EffectShapeCast2D/3D**: Apply effects to entities hit by shape casts.
- **ForgeCueHandler**: Base class for implementing visual and audio feedback in response to gameplay cues.

## Project Status

⚠️ **Work in Progress**: This plugin is under active development and is not yet recommended for production use.

⚠️ **Godot C# Only**: Currently only works with Godot projects using C#.

### Current Features ✅

- **Attributes System**: Attribute management, supporting sets, modifiers, and configuration.
- **Effects System**: Comprehensive effect application and management, including stacking, periodic, instant, and infinite effects.
- **Tags System**: Full hierarchical tag system with Godot editor integration.
- **Abilities System**: Feature-complete ability system, supporting grant/removal, custom behaviors, triggers, cooldowns, and costs.
- **Events System**: Gameplay event bus supporting event-driven logic, subscriptions, and triggers.
- **Cues System**: Visual/audio feedback layer; decouples presentation from game logic.
- **Editor Extensions**: Custom inspector elements and tag editor with Godot integration.
- **Custom Nodes**: Includes nodes like `ForgeEntity`, `ForgeAttributeSet`, `EffectArea2D`, and more.

### Planned Features 🚧

- **Multiplayer Support**: Network replication for all systems, deterministic/authoritative support.
- **Statescript**: Visual state-based scripting for implementing ability behaviors and custom logic.

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
3. In the Godot editor, build your project by clicking `Build` in the top-right corner of the script editor.
4. Enable **Forge Gameplay System** under `Project > Project Settings > Plugins`.

## Documentation

For comprehensive documentation, explore the [docs](docs) directory:

- [Documentation Overview](docs/README.md)
- [Quick Start Guide](docs/quick-start.md)
- [Forge Nodes](docs/nodes.md)
- [Forge Resources](docs/resources.md)
- [Helper Classes](docs/helper-classes.md)

## Contributing

This project is not currently accepting contributions as it's still in early development. However, if you're interested in contributing or have suggestions, feel free to reach out via GitHub issues or discussions.

## License

Copyright © Gamesmiths Guild. See [LICENSE](LICENSE) for details.
