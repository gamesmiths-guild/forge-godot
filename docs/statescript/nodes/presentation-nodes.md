# Presentation Nodes

> **Namespace:** `Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Action` and `.State`
>
> **Add Node group:** Action → **Presentation**, State → **Presentation**
>
> **Network kind:** Presentation

These nodes are **Godot-only** — there is no core Forge counterpart, so this page is their reference rather than a Godot-authoring supplement to one.

They drive an animation player or an audio player that is already in the entity's scene. All four are **unpaired**: an `AnimationPlayer` is the same node whether the game is 2D or 3D, so they resolve from the nearest spatial ancestor of *either* kind.

Each comes in a fire-and-forget Action and a State that plays for as long as what it plays lasts.

## Shown at the end of the frame, timed in fixed steps

All four are [presentation nodes](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/state/README.md#simulation-and-presentation): the editor marks them with a **Presentation** badge.

- **The state nodes start and stop playback in the presentation pass**, which the Forge bootstrap runs at the end of each frame, once every node has processed. They play what was resolved when they activated: Speed, Blend, Volume Db and Pitch are read then, not in the pass. A node that activates and ends within one frame plays nothing, and one that keeps running is started once.
- **Their length is counted in fixed steps from activation**, not read from the player. The node finishes on the same physics step on every machine, whatever the engine is doing with playback — a graph waiting on `OnFinished` never depends on whether a frame was dropped.
- **The one-shot actions play when they execute.**

## The player path

The **Player** setting is a path to the player, from the node the entity lives on. **An empty path means the entity's first matching child**, one level deep, first match wins — which is not what an empty path means anywhere else in the layer, and deliberately so: the [spatial getters](../resolvers/spatial-getters.md) read an empty path as the entity's own node because that node *is* a transform, and it is never an `AnimationPlayer`. Requiring a path here would make it mandatory in the overwhelmingly common case of a scene with exactly one player. Name the path when the player is nested inside an imported model scene.

## Animation

| Node | Arch | Settings | Ports |
|---|---|---|---|
| `PlayAnimationOneShotNode` | Action | Player; Animation | — |
| `PlayAnimationNode` | State | Player; Animation; **Stop On Deactivate** (default on); **Restart On Retrigger** (default off) | 4 `OnFinished` |

| Index | Label | Type | Notes |
|---|---|---|---|
| 0 | Entity | `IForgeEntity` | Optional. Unbound means the ability's owner. |
| 1 | Speed | `double` | Optional. |
| 2 | Blend | `double` | Optional. Cross-fade seconds. |

`PlayAnimationNode` deactivates and emits `OnFinished` once the clip's length has run: the clip's length divided by its speed (the Speed input times the player's own Speed Scale), counted in fixed steps from activation. Both are read when the node activates, so a game must not drive the player's Speed Scale from a presentation node such as Node Property Override: what was presented would change how long the node lasts. **A looping clip, or a speed of zero, never finishes on its own**, which is the channelling shape: the node runs until something aborts it. Another animation taking over the player does not end the node; its length does.

The animation starts in the presentation pass, from its first frame: if the same animation is already playing, the node stops it first, because the length it counts starts at the beginning. An activation presented part way through, as a [rewind](../../nodes.md#forgeworldhistory) brings one back, starts at the pose its elapsed fixed steps give it instead.

**After a rollback** that keeps the activation running at another point of it, as scrubbing a timeline does, the animation seeks to the pose the restored step gives it: exactly while the game is paused, and only when it's off by more than a few steps while the game runs, so a replay that comes out the same never makes it hitch. Scrubbing while paused plays it again if it had played out or another animation had taken over the player; a running game leaves what took the player over in place.

**Stop On Deactivate** stops the player, in the next presentation pass, if the node's animation is still the one playing when the node ends early, which covers an abort, a subgraph ending and the graph stopping — what an interrupted cast needs. A natural finish cannot trigger it, because by then the animation is over.

**Restart On Retrigger** plays the animation again from its start when a message reaches the node mid-animation, with Speed and Blend read again and the length counted from the restart; off, the message is ignored. Godot's `Play` leaves an animation that is already playing where it is, so the restart stops it first — which also means a restart snaps to the first frame rather than blending into it.

Melee swing timing, cast bars, windups.

## Audio

| Node | Arch | Settings | Ports |
|---|---|---|---|
| `PlayAudioOneShotNode` | Action | Player | — |
| `PlayAudioNode` | State | Player; **Stop On Deactivate** (default on); **Restart On Retrigger** (default off) | 4 `OnFinished` |

| Index | Label | Type | Notes |
|---|---|---|---|
| 0 | Entity | `IForgeEntity` | Optional. |
| 1 | Volume Db | `double` | Optional. |
| 2 | Pitch | `double` | Optional. |

Audio has the same shape: the length is the stream's length divided by the pitch (the Pitch input, or when it is unbound the Pitch Scale the player had before any node overrode it, so a length never depends on what an earlier node played), counted in fixed steps. **A looping stream, or one whose length Godot cannot tell, such as a generator, never finishes on its own.** Channel hums, beam loops.

**Restart On Retrigger** plays the sound again from the beginning, with Volume Db and Pitch read again. With **Stop On Deactivate** on, the old sound is stopped first, so a player that allows polyphony has it replaced rather than a second voice layered over it.

A sound presented part way through its activation starts where its elapsed fixed steps put it. A looping stream plays through once and then repeats its loop, from its loop offset or, for a WAV, between its loop bounds in its loop mode, which is where such a sound is started. **While the game is paused, sounds stay quiet**: a sound started then waits for the game to go on, as the sounds that were playing when it paused do, and one a [rewind](../../nodes.md#forgeworldhistory) moves to another point waits where it is, then plays on from the right place once the game goes on. Godot would play a moment of a sound moved while paused, so the move waits rather than crackling as the timeline is scrubbed. While the game runs, a sound is moved only when it's off by more than a tenth of a second.

The three Godot audio players derive from their dimension's spatial node and share no base, so an `AudioPlayers` switch written once resolves "the entity's player" for both these nodes and the [`AudioCueHandler`](../../nodes.md#audiocuehandler).

## Zero is a value, so unbound cannot be zero

Speed, Blend, Volume Db and Pitch are all optional inputs, and all four need a distinction the physics rows do not: **zero is meaningful for every one of them**, so "unbound" cannot be read off the zero a missing binding resolves to. They resolve as nullable, and a null leaves whatever the player itself was authored with — which is what makes an unbound Volume Db mean "the mix the sound designer set" rather than "0 dB".

Blend is an input rather than a setting for the same reason a poll interval is: there is no numeric setting control, and a blend that varies with an ability level is then just a resolver.

## A node that cannot play does not stall the ability

If the player, the stream or the animation is missing, both state nodes finish on their next fixed step rather than holding themselves open, and warn. Their whole job is to be waited on, so a graph whose next step hangs off `OnFinished` would never continue and the ability would stay active until something aborted it — which for an ability with no timer is never. Nothing having started is the same answer as having stopped; the warning is what reports the misconfiguration.

## Two recipes that replace nodes

- **Animation-keyed hit windows.** Use an `AnimationPlayer` method track that raises a Forge event, and pick it up with core's `EventListenerNode`. Nothing in this family needs to know about keyframes.
- **Bone-attached cast points.** Add a `BoneAttachment3D` child and point a [spatial getter's](../resolvers/spatial-getters.md) **Node** path at it.

Clip length is readable before anything plays, through the [`Animation Length`](../resolvers/engine-resolvers.md#animation-length) resolver, so a timer feeding a windup cannot drift from the art it is pacing.

## Related Docs

- [Nodes Reference](README.md)
- [Engine Resolvers](../resolvers/engine-resolvers.md) — `Animation Length`, `Engine Time`, `Delta Time`
- [Forge Nodes](../../nodes.md#cue-handler-library) — the cue handler library, for presentation driven by effects rather than by a graph
