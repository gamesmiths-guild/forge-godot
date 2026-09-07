# Presentation Nodes

> **Namespace:** `Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Action` and `.State`
>
> **Add Node group:** Action → **Presentation**, State → **Presentation**

These nodes are **Godot-only** — there is no core Forge counterpart, so this page is their reference rather than a Godot-authoring supplement to one.

They drive an animation player or an audio player that is already in the entity's scene. All four are **unpaired**: an `AnimationPlayer` is the same node whether the game is 2D or 3D, so they resolve from the nearest spatial ancestor of *either* kind.

Each comes in a fire-and-forget Action and a State that owns the playback for its own lifetime.

## The player path

The **Player** setting is a path to the player, from the node the entity lives on. **An empty path means the entity's first matching child**, one level deep, first match wins — which is not what an empty path means anywhere else in the layer, and deliberately so: the [spatial getters](../resolvers/spatial-getters.md) read an empty path as the entity's own node because that node *is* a transform, and it is never an `AnimationPlayer`. Requiring a path here would make it mandatory in the overwhelmingly common case of a scene with exactly one player. Name the path when the player is nested inside an imported model scene.

## Animation

| Node | Arch | Settings | Ports |
|---|---|---|---|
| `PlayAnimationOneShotNode` | Action | Player; Animation | — |
| `PlayAnimationNode` | State | Player; Animation; **Stop On Deactivate** (default on) | 4 `OnFinished` |

| Index | Label | Type | Notes |
|---|---|---|---|
| 0 | Entity | `IForgeEntity` | Optional. Unbound means the ability's owner. |
| 1 | Speed | `double` | Optional. |
| 2 | Blend | `double` | Optional. Cross-fade seconds. |

`PlayAnimationNode` deactivates and emits `OnFinished` when the animation stops being the one playing. **One comparison answers three questions**: Godot reports `CurrentAnimation` only while the player is playing, so `CurrentAnimation != animation` covers the animation ending, something stopping the player, and another animation taking over — three endings a graph has no reason to tell apart, since in all three the node has stopped being the thing driving the character.

**Stop On Deactivate** stops the player if the node is still driving it when it deactivates, which covers an abort, a subgraph ending and the graph stopping — what an interrupted cast needs. A natural finish cannot trigger it, because by then the animation is over.

Melee swing timing, cast bars, windups.

## Audio

| Node | Arch | Settings | Ports |
|---|---|---|---|
| `PlayAudioOneShotNode` | Action | Player | — |
| `PlayAudioNode` | State | Player; **Stop On Deactivate** (default on) | 4 `OnFinished` |

| Index | Label | Type | Notes |
|---|---|---|---|
| 0 | Entity | `IForgeEntity` | Optional. |
| 1 | Volume Db | `double` | Optional. |
| 2 | Pitch | `double` | Optional. |

Audio has the same shape one step simpler: `IsPlaying` going false is the end. Channel hums, beam loops.

The three Godot audio players derive from their dimension's spatial node and share no base, so an `AudioPlayers` switch written once resolves "the entity's player" for both these nodes and the [`AudioCueHandler`](../../nodes.md#audiocuehandler).

## Zero is a value, so unbound cannot be zero

Speed, Blend, Volume Db and Pitch are all optional inputs, and all four need a distinction the physics rows do not: **zero is meaningful for every one of them**, so "unbound" cannot be read off the zero a missing binding resolves to. They resolve as nullable, and a null leaves whatever the player itself was authored with — which is what makes an unbound Volume Db mean "the mix the sound designer set" rather than "0 dB".

Blend is an input rather than a setting for the same reason a poll interval is: there is no numeric setting control, and a blend that varies with an ability level is then just a resolver.

## A node that cannot play does not stall the ability

If the player or the animation is missing, both state nodes warn and finish on their first update rather than holding themselves open. Their whole job is to be waited on, so a graph whose next step hangs off `OnFinished` would never continue and the ability would stay active until something aborted it — which for an ability with no timer is never. Nothing having started is the same answer as having stopped; the warning is what reports the misconfiguration.

## Two recipes that replace nodes

- **Animation-keyed hit windows.** Use an `AnimationPlayer` method track that raises a Forge event, and pick it up with core's `EventListenerNode`. Nothing in this family needs to know about keyframes.
- **Bone-attached cast points.** Add a `BoneAttachment3D` child and point a [spatial getter's](../resolvers/spatial-getters.md) **Node** path at it.

Clip length is readable before anything plays, through the [`Animation Length`](../resolvers/engine-resolvers.md#animation-length) resolver, so a timer feeding a windup cannot drift from the art it is pacing.

## Related Docs

- [Nodes Reference](README.md)
- [Engine Resolvers](../resolvers/engine-resolvers.md) — `Animation Length`, `Engine Time`, `Delta Time`
- [Forge Nodes](../../nodes.md#cue-handler-library) — the cue handler library, for presentation driven by effects rather than by a graph
