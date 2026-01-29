# Forge for Godot Documentation

This documentation covers the integration of the Forge Gameplay System into the Godot Engine through the Forge for Godot plugin.

## What is Forge?

Forge is an engine-agnostic gameplay framework that provides a robust foundation for building complex and maintainable game systems in C#. It is inspired by Unreal Engine's Gameplay Ability System (GAS), offering a structured approach to key gameplay features such as attributes, effects, abilities, events, tagging, and cues.

For detailed documentation on the core framework concepts, see the [main Forge documentation](https://github.com/gamesmiths-guild/forge/blob/main/docs/README.md).

## Architecture

Forge for Godot connects the Forge system to Godot's node architecture through:

### Core Management

- **ForgeBootstrap**: Autoload singleton initializing and maintaining Forge managers.
- **TagsManager**: Central registry for gameplay tags (hierarchical).
- **CuesManager**: Routing and management for all audio/visual feedback handlers.

### Integration Components

- **Custom Nodes**: Godot nodes that implement or integrate Forge features.
- **Custom Resources**: Godot resources for configuring Forge elements in the Inspector.
- **Editor Extensions**: UI tools to manage tags and Forge data (e.g., the Tags Editor).

### Data Flow

1. **Configuration**: Define attribute sets, effects, tags, abilities, events, and cues via code and resources.
2. **Runtime Initialization**: ForgeBootstrap loads and configures all managers.
3. **Entity Integration**: Use ForgeEntity nodes or implement IForgeEntity on custom nodes.
4. **Gameplay Logic**: Effects and abilities are applied, managed, and resolved system-wide.
5. **Feedback**: Cues trigger visual/audio output based on game events and state changes.

## Core Systems Overview

### Tags System

A hierarchical classification system for entities, effects, and abilities. Tags use dot notation and inheritance for flexible matching (e.g., `"ability.damage.fire"`).

The plugin includes a Forge tab in the Godot editor that allows for easy management of tags.

### Attributes System

Attributes represent numeric values that can be modified by effects:

- Base values with configurable min/max ranges.
- Various modifier operations (additive, multiplicative, etc.) that can be applied to attributes.
- Support for modifiers that derive their values from other attributes.
- Custom attribute sets for grouping related attributes.

### Effects System

Effects are the primary way to modify entity attributes and tags:

- Attribute modifications through various operations and formulas.
- Duration control (instant, timed, infinite).
- Periodic execution for damage-over-time style effects.
- Sophisticated stacking rules for multiple effect instances.
- Custom calculations and execution logic.

### Abilities System

Defines, grants, and activates gameplay powers or actions:

- Supports cooldowns, resource costs, activation requirements, instancing policies, and custom behaviors.
- Can be granted via effects or directly to entities.
- Triggers automatically from events or tags, or manually from code.

### Events System

A flexible event bus for passing tagged event data across entities:

- Used to trigger abilities, propagate gameplay reactions, and decouple systems.
- Subscribe and react to events based on tag and optional payload data.

### Cues System

Cues connect the gameplay simulation layer with the presentation layer, translating gameplay events into visual and audio feedback:

- Triggers audio/visual feedback in response to gameplay events and effect changes.
- Organizes cues and handlers using tags for flexible assignment.
- Passes custom parameters for feedback intensity, color, sound, and more.

## Using Forge in Godot

### Plugin Initialization

When enabled, the plugin:

1. Registers the ForgeBootstrap autoload.
2. Adds the Forge tab to Godot's right dock.
3. Loads and persists tag data.

### Entity Creation

Forge-enabled entities can be created by:

1. Adding a ForgeEntity node as a child, or
2. Implementing IForgeEntity on your custom node class.

### Effect and Ability Application

Apply effects and activate abilities via:

- Direct API calls on the EffectsManager and Abilities manager.
- Dedicated scene nodes (e.g., EffectArea2D) for area/raycast interactions.
- The EffectApplier helper for flexible, reusable logic.

## Next Steps

- [Quick Start Guide](quick-start.md): Get started with Forge for Godot in minutes.
- [Nodes Documentation](nodes.md): Learn about the custom nodes provided by the plugin.
- [Resources Documentation](resources.md): Explore all configurable resource types—effects, abilities, tags, cues, and more.
- [Helper Classes](helper-classes.md): Find utility classes to streamline effect application, manager access, and integration patterns.
- For gameplay programming API and advanced usage, see the [core Forge documentation](https://github.com/gamesmiths-guild/forge/blob/main/docs/README.md).
