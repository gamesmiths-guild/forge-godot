# Property Resolvers

Forge for Godot uses the core Forge resolver set and adds Godot-facing resolver resources when visual authoring needs resource-specific behavior.

Use the **core Forge documentation** for runtime resolver behavior and API details. Use the pages in this folder when Godot authoring adds editor, resource, or binding details.

A second group of resolvers — [the engine-facing ones](#godot-engine-resolvers) — has no core counterpart at all: they read the scene tree, the physics world, cameras, input, navigation and the clock. Their pages here are the canonical reference as well as the authoring notes.

## Core Resolver Reference

| Category | Core Docs | Notes |
|----------|-----------|-------|
| Array Operations | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/README.md#array-operations) | LINQ-style access, transform, and reduction resolvers over value and object arrays, plus the element (lambda) resolvers. |
| Boolean Expressions | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/README.md#boolean-expressions) | Logical composition and comparison resolvers. |
| Built-in Resolvers | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/README.md#built-in-resolvers) | Constants, arrays, variables, activation data, attributes, tags, and other general-purpose resolvers. |
| Entity Resolvers | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/README.md#entity-resolvers) | Resolvers that read owner/source/target entities and entity-typed values. |
| Effect Resolvers | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/README.md#effect-resolvers) | Runtime `EffectFromDataResolver`, `EffectArrayFromDataResolver`, and the effect-variable resolvers. |
| Object Utilities | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/README.md#object-utilities) | Validity (non-null) and reference-identity checks over object-backed variables. |
| Math | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/README.md#math) | Numeric, vector, interpolation, and magnitude helpers. |
| Random | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/README.md#random) | Random scalar and spatial resolvers. |
| Spatial Math | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/README.md#spatial-math) | Direction, angle, quaternion, plane, and vector-space helpers. |

## Godot Resolver Resources

These pages cover authoring details that the Godot editor adds on top of the core resolver behavior.

| Resolver | Output Type | Description |
|----------|-------------|-------------|
| [AbilityActivatorResolver](ability-activator-resolver.md) | `AbilityActivator` | Selects an `IAbilityActivationDataProvider` to pass custom typed data into ability activations. |
| [AbilityCooldownResolver](ability-cooldown-resolver.md) | `float` | Reads a cooldown value (remaining/total/fraction dropdown) from an ability, with an optional tag filter and ability source. |
| [AbilityCostResolver](ability-cost-resolver.md) | `int` | Reads the evaluated ability cost for a selected attribute. |
| [AbilityDataResolver](ability-data-resolver.md) | `AbilityData` | Selects a `ForgeAbilityData` resource for the grant/lookup node inputs. |
| [AbilityLevelResolver](ability-level-resolver.md) | `int` | Authors the current ability level as a node input. |
| [AbilityOwnershipResolver](ability-ownership-resolver.md) | `EffectOwnership` | Authors the current ability owner/source pair as a node input. |
| [AbilityStateResolver](ability-state-resolver.md) | `bool` | Reads a state flag (is active/inhibited/valid dropdown) from an ability. |
| [ActiveEffectDataResolver](active-effect-data-resolver.md) | `double`/`int`/`bool` | Reads a selected runtime value (remaining duration, stacks, level, ... dropdown) from an active effect handle. |
| [ActiveEffectEffectResolver](active-effect-effect-resolver.md) | `Effect?` | Reads the live `Effect` behind an active effect handle. |
| [ActiveEffectOwnerResolver](active-effect-source-resolver.md) | `IForgeEntity?` | Reads `Ownership.Owner` — who triggered the action that caused the effect. |
| [ActiveEffectSourceResolver](active-effect-source-resolver.md) | `IForgeEntity?` | Reads `Ownership.Source` — what actually caused the effect (weapon, projectile, trap). |
| [ActiveEffectTagQueryResolver](active-effect-tag-query-resolver.md) | `bool` | Evaluates a tag query against an active effect's own tags, granted tags, or both. |
| [ActiveEffectTargetResolver](active-effect-target-resolver.md) | `IForgeEntity?` | Reads the entity an active effect is applied to. |
| [CanActivateAbilityResolver](can-activate-ability-resolver.md) | `bool` | Checks whether an ability can currently activate. |
| [CueCustomParametersResolver](cue-custom-parameters-resolver.md) | `Dictionary<StringKey, object>` | Selects an `ICueCustomParametersProvider` to author the `CueParameters.CustomParameters` bag for the cue nodes. |
| [CurveSampleResolver](curve-sample-resolver.md) | `float` | Samples a Godot `Curve` resource at a resolved position. |
| [EffectContextDataResolver](effect-context-data-resolver.md) | `EffectApplicationContext` | Selects an `IEffectContextDataProvider` to pass custom context data into effect applications. |
| [EffectQueryMatchResolver](effect-query-match-resolver.md) | `bool` | Matches a full `ForgeEffectQuery` against an active effect handle. |
| [EffectResolver](effect-resolver.md) | `Effect` | Authors a single `Effect` (effect data + level + ownership) for `ApplyEffectNode` and `EffectNode`. |
| [EffectStackDataResolver](effect-stack-data-resolver.md) | `int` | Aggregates stack/instance/level data (dropdown) over active applications of a `ForgeEffectData`. |
| [EnumConstantResolver](../enums.md#authoring-values-the-enum-resolver) | `int` | Authors an integer constant by picking a member of a `ForgeStatescriptEnum` by name. |
| [GetAbilityHandleResolver](get-ability-handle-resolver.md) | `AbilityHandle` | Looks up a granted ability by its `ForgeAbilityData` resource (cross-ability queries). |
| [QueryActiveEffectsResolver](query-active-effects-resolver.md) | `ActiveEffectHandle[]` | Queries active effect handles on an entity, filtered by a `ForgeEffectQuery`. |
| [SetByCallerMagnitudeResolver](set-by-caller-magnitude-resolver.md) | `float` | Reads the SetByCaller magnitude stored on an `Effect` for a selected tag. |
| [EventPayloadOutputResolver](event-payload-resolver.md#listener-side-eventpayloadoutputresolver) | `EventPayloadWriter` | Selects an `IEventPayloadProvider` to write a received payload to graph variables for `EventListenerNode`. |
| [EventPayloadResolver](event-payload-resolver.md#raise-side-eventpayloadresolver) | `EventPayloadRaiser` | Selects an `IEventPayloadProvider` to build and raise a typed event payload for `RaiseEventNode`. |
| [IsValidResolver](is-valid-resolver.md) | `bool` | Authors a validity check over an object-backed variable of any registered type. Rejects null, invalidated handles, and freed Godot objects. |
| [ObjectEqualsResolver](object-equals-resolver.md) | `bool` | Authors a reference-identity check between two object-backed variables of any registered type. |
| [OwnershipResolver](ownership-resolver.md) | `EffectOwnership` | Composes effect ownership from two nested entity resolvers. |
| [TagResolver](tag-resolver.md) | `Tag` | Selects one or more registered tags for any tag input (e.g. the cue nodes). |

### Math and Selection Fills

These resolvers add no Godot-specific authoring beyond the standard nested-operand pickers, so their behavior lives in the core docs. They are available in the editor for any compatible numeric or object input:

- `RemapResolver`, `InverseLerpResolver`, `SmoothStepResolver`, `WrapResolver`, `PingPongResolver`, `DeltaAngleResolver`, `ApproximatelyResolver` — [core Math reference](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/README.md#math). The `Approximately` resource exposes its tolerance as a direct editor field rather than a nested resolver; it defaults to `0.00001` (6-decimal float input precision), and the field itself accepts finer values.
- `ConditionalResolver` / `ConditionalObjectResolver` — ternary select over value and object lanes.
- `IntersectResolver`, `RandomElementResolver`, `ShuffleResolver` (and their `Object...<T>` variants) — [core Array Operations reference](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/README.md#array-operations). The random ones use Godot's `ForgeRandom`.

### Object Variable Types

Object-backed graph values are keyed by an object variable type. In addition to the built-in `Entity`, `Effect`, and `ActiveEffectHandle` types, Forge for Godot registers:

| Type id | Display name | Backing type | Used by |
|---------|--------------|--------------|---------|
| `Tag` | Tag | `Tag` | The tag listener's output. |
| `AbilityHandle` | Ability Handle | `AbilityHandle` | The grant nodes' handle output and the lookup resolvers. |
| `AbilityData` | Ability Data | `AbilityData` | The ability grant inputs. |
| `GodotNode` | Node | `Godot.Node` | Everything in the node lane — spawn outputs, hit nodes, the interop rows. |
| `Scene` | Scene | `Godot.PackedScene` | The scene `Constant` resolver and the instantiating nodes. |
| `Shape3D` | Shape 3D | `Godot.Shape3D` | The 3D [shape resolvers](shapes.md) and every 3D query that takes one. |
| `Shape2D` | Shape 2D | `Godot.Shape2D` | The 2D shape resolvers. Deliberately a separate type — the two physics servers do not mix. |

Because a shape, a scene and a node all travel the object lane, an operand of any of them is a nested picker like any other, and can be seeded with a composed resolver.


## Godot Engine Resolvers

These read the engine rather than the Forge simulation, so nothing about them exists in the core library. They are grouped by the authoring model they share; each page carries a table of the individual resolvers with their outputs and rows.

| Guide | Resolvers | What they answer |
|-------|-----------|------------------|
| [Spatial Getters](spatial-getters.md) | `Entity Position`, `Entity Direction`, `Entity Rotation`, `Entity Scale`, `Entity Velocity`, `Entity Angular Velocity`, `Entity Transform Point`, `Character State`, `Character Motion`, `Can Fit` | What is true of the node an entity lives on. All 2D/3D pairs, all sharing an entity operand and an optional `%marker` path. |
| [Physics Query Resolvers](physics-queries.md) | `Area Overlaps`, `Overlap`, `Entities In Cone`, `Entities At Point`, `Closest Entity`, `Is In Cone`, `Line Of Sight`, `Shapecast` | Who is in there, can I see it, what would this sweep meet. `Entity[]` and `bool` results, so they compose with `Where`, `OrderBy` and `Except`. |
| [Shape Resolvers](shapes.md) | `Sphere`, `Box`, `Capsule`, `Cylinder`, `Cone`, `Circle`, `Rectangle`, `Wedge`, `Constant` | The shape a query sweeps, built from resolvers so every dimension can scale. |
| [Camera and Aim Resolvers](camera-and-aim.md) | `Camera Position`, `Camera Forward 3D`, `Mouse World Position` | Where the graph's own player is looking and pointing. No entity operand — the viewport the owner stands in answers "whose camera". |
| [Input Resolvers](input-resolvers.md) | `Input Action Pressed`, `Input Action Strength`, `Input Axis`, `Input Vector 2` | Button and analog state, for expression gates and condition monitors. |
| [Scene Graph and Interop Resolvers](scene-graph-resolvers.md) | `Constant` (scene and node path), `Node From Entity`, `Entity From Node`, `Entity At Path`, `Node Property`, `Parent Entity`, `Child Entities`, `Nodes In Node Group`, `Entities In Node Group` | Crossing between entities, nodes and scenes, and reading a node's own state. |
| [Navigation Resolvers](navigation-resolvers.md) | `Nav Reachable`, `Nav Path Length`, `Nav Closest Point` | Whether a walk is possible, how long it is, and where the nearest legal ground is. |
| [Engine and Timing Resolvers](engine-resolvers.md) | `Delta Time`, `Engine Time`, `Animation Length` | The running tick's step, monotonic time, and how long a clip is. |

## Authoring Guides

The array-operation resolver family (`Where`, `Order By`, `Take`, `Select`, `Count`, `Any`, `First`, ...) and the element (lambda) resolvers share one editor authoring model rather than per-resolver behavior, so they are documented as a single guide (their per-operation runtime behavior lives in the [core Array Operations reference](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/README.md#array-operations)).

- [Array Operations Authoring](array-operations.md) — nested source, per-element (lambda) operands and element resolvers, iteration scope, and the value/object lanes.

The [Godot engine resolvers](#godot-engine-resolvers) above are grouped the same way and for the same reason.

## Seeding a Nested Operand

A **node input** marked optional renders a `(None)` entry and genuinely stays unbound, which is what lets Raycast read "no mask" as every layer. A nested operand **inside a resolver** has no such state: the picker always selects an editor, and an untouched one is the constant zero.

So any resolver operand whose sensible default is not zero is *seeded* with the resolver that expresses it — `Overlap`'s Position starts on `Entity Position 3D`, its Shape on a Sphere, both ends of `Line Of Sight` on `Entity Position 3D`, `Mouse World Position 3D`'s Max Dist on the same 1000 units the aim payload uses. An untouched row runs what the editor shows.

The same reasoning reaches node inputs whose default is a composed resolver rather than a constant, which is why [Line Of Sight](../nodes/physics-query-nodes.md#line-of-sight)'s Ignore row is required and seeded with an array of the owner and the target rather than being optional.

## Related Docs

- [Variables and Data](../variables.md)
- [Statescript Enums](../enums.md)
- [Custom Resolvers](../custom-resolvers.md)
- [Physics Debug Drawing](../physics-debug-drawing.md)
- [Resolver Template](../templates/resolver-template.md)
