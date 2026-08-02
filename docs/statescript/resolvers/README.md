# Property Resolvers

Forge for Godot uses the core Forge resolver set and adds Godot-facing resolver resources when visual authoring needs resource-specific behavior.

Use the **core Forge documentation** for runtime resolver behavior and API details. Use the pages in this folder when Godot authoring adds editor, resource, or binding details.

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
| [IsValidResolver](is-valid-resolver.md) | `bool` | Authors a validity (non-null) check over an object-backed variable of any registered type. |
| [ObjectEqualsResolver](object-equals-resolver.md) | `bool` | Authors a reference-identity check between two object-backed variables of any registered type. |
| [OwnershipResolver](ownership-resolver.md) | `EffectOwnership` | Composes effect ownership from two nested entity resolvers. |
| [TagResolver](tag-resolver.md) | `Tag` | Selects one or more registered tags for any tag input (e.g. the cue nodes). |

### Math and Selection Fills

These resolvers add no Godot-specific authoring beyond the standard nested-operand pickers, so their behavior lives in the core docs. They are available in the editor for any compatible numeric or object input:

- `RemapResolver`, `InverseLerpResolver`, `SmoothStepResolver`, `WrapResolver`, `PingPongResolver`, `DeltaAngleResolver`, `ApproximatelyResolver` — [core Math reference](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/README.md#math). The `Approximately` resource exposes its tolerance as a direct editor field rather than a nested resolver; it defaults to `0.00001` (6-decimal float input precision), and the field itself accepts finer values.
- `ConditionalResolver` / `ConditionalObjectResolver` — ternary select over value and object lanes.
- `IntersectResolver`, `RandomElementResolver`, `ShuffleResolver` (and their `Object...<T>` variants) — [core Array Operations reference](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/README.md#array-operations). The random ones use Godot's `ForgeRandom`.

### Object Variable Types

Object-backed graph values are keyed by an object variable type. In addition to the built-in `Entity`, `Effect`, and `ActiveEffectHandle` types, this release registers `Tag`, `AbilityHandle`, and `AbilityData` — used by the tag listener output, the grant nodes' handle output and lookup resolvers, and the ability grant inputs respectively.

## Authoring Guides

The array-operation resolver family (`Where`, `Order By`, `Take`, `Select`, `Count`, `Any`, `First`, ...) and the element (lambda) resolvers share one editor authoring model rather than per-resolver behavior, so they are documented as a single guide (their per-operation runtime behavior lives in the [core Array Operations reference](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/README.md#array-operations)).

- [Array Operations Authoring](array-operations.md) — nested source, per-element (lambda) operands and element resolvers, iteration scope, and the value/object lanes.

## Related Docs

- [Variables and Data](../variables.md)
- [Statescript Enums](../enums.md)
- [Custom Resolvers](../custom-resolvers.md)
- [Resolver Template](../templates/resolver-template.md)
