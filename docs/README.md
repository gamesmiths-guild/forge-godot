# Forge for Godot Documentation

This documentation covers the integration of the Forge Gameplay System into the Godot Engine through the Forge for Godot plugin.

## What is Forge?

Forge is an engine-agnostic gameplay framework designed to provide a robust foundation for building game systems in C#. It draws inspiration from Unreal Engine's Gameplay Ability System (GAS) and provides a structured approach to implementing common gameplay features.

For detailed documentation on the core framework concepts, please refer to the [main Forge documentation](https://github.com/gamesmiths-guild/forge/blob/main/docs/README.md).

## Architecture

Forge for Godot bridges the Forge system with Godot's node-based architecture through several key components:

### Core Management

- **ForgeBootstrap**: An autoload singleton that initializes and maintains the core Forge managers.
- **TagsManager**: Central registry for hierarchical gameplay tags.
- **CuesManager**: Central registry for audio/visual feedback handlers.

### Integration Components

- **Custom Nodes**: Godot nodes that implement or interface with Forge systems.
- **Custom Resources**: Godot resources for configuring Forge components through the inspector.
- **Editor Extensions**: UI tools to help manage Forge data (e.g., the Tags Editor).

### Data Flow

1. **Configuration**: Define attribute sets, effects, tags, and cues through C# code and resources.
2. **Runtime Initialization**: ForgeBootstrap loads and configures Forge systems.
3. **Entity Integration**: ForgeEntity nodes (or custom nodes implementing IForgeEntity) connect to the system.
4. **Gameplay Logic**: Effects are applied and processed through the system.
5. **Feedback**: Cues trigger visual and audio feedback based on gameplay events.

## Core Systems Overview

### Tags System

Tags provide a hierarchical classification system for entities and effects. They use a dot notation format (e.g., "ability.damage.fire") and support inheritance.

The plugin includes a Tags Editor tab in the Godot editor that allows for easy management of tags.

### Attributes System

Attributes represent numeric values that can be modified by effects. Key features include:

- Base values with configurable min/max ranges.
- Various modifier operations (additive, multiplicative, etc.) that can be applied to attributes.
- Support for modifiers that derive their values from other attributes.
- Custom attribute sets for grouping related attributes.

### Effects System

Effects are the primary way to modify entity attributes and trigger gameplay events:

- Attribute modifications through various operations and formulas.
- Duration control (instant, timed, infinite).
- Periodic execution for damage-over-time style effects.
- Sophisticated stacking rules for multiple effect instances.
- Custom calculations and execution logic.

### Cues System

Cues connect the gameplay simulation layer with the presentation layer, translating gameplay events into visual and audio feedback:

- Automatic triggering when effects are applied/executed/removed.
- Custom parameters for controlling feedback intensity.
- Registration through tags for flexible assignment.

## Using Forge in Godot

### Plugin Initialization

When enabled, the plugin:

1. Registers the ForgeBootstrap autoload.
2. Adds the Tags Editor tab to the right dock.
3. Loads saved tag data.

### Entity Creation

Two approaches to creating Forge-enabled entities:

1. Add a ForgeEntity node as a child of your game object.
2. Implement IForgeEntity directly on your custom node class.

### Effect Application

Multiple ways to apply effects:

1. Direct API calls on the EffectsManager.
2. Through specialized nodes like EffectArea2D.
3. Through the EffectApplier helper class.

## Next Steps

- [Quick Start Guide](quick-start.md): Get up and running with Forge in minutes.
- [Nodes Documentation](nodes.md): Learn about the custom nodes provided by the plugin.
