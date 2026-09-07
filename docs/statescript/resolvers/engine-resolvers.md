# Engine and Timing Resolvers

> **Namespace:** `Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers`
>
> **Runtime:** `Gamesmiths.Forge.Godot.Core.Statescript.Resolvers`

These resolvers are **Godot-only** — there is no core Forge counterpart, so this page is their reference rather than a Godot-authoring supplement to one.

They read the engine itself rather than the scene: the running tick, the clock, and how long an animation is.

| Resolver | Output | Rows |
|---|---|---|
| **Delta Time** | `double` | — |
| **Engine Time** | `double` | — |
| **Animation Length** | `float` | Of (entity); **Player** (path); **Animation** (name) |

## Delta Time

The step of the tick currently running. It matters because a graph has [two rails](../README.md#in-godot) with different deltas.

**It has no Frame-or-Fixed switch, deliberately.** The author already knows which rail their node runs on, so a switch would only ever be set to agree with it — and set wrong it reports a plausible number from the other clock, which is the worst kind of bug to look at. `Engine.IsInPhysicsFrame()` answers it instead, so one resolver reads correctly from either rail.

This is the one place a resolver *can* tell which rail is walking it. `GraphContext.UpdateStamp` cannot, because it counts passes over both and has no engine call to fall back on.

## Engine Time

Monotonic engine time, for timestamping. Two stamps always subtract to a real interval.

## Animation Length

Feeds timer durations so a windup stays in sync with the art.

**It reads the clip, not the playback**, which is what lets it be answered at activation before anything is playing — the whole point, since a timer fed from here cannot drift from the animation it is pacing. The **Player** path follows the same rule as the [presentation nodes](../nodes/presentation-nodes.md#the-player-path): empty means the entity's first animation player child.

It does not divide by playback speed, because a clip does not know what speed it will be played at. A graph that also drives the speed divides itself.

## Related Docs

- [Resolvers Reference](README.md)
- [Presentation Nodes](../nodes/presentation-nodes.md)
- [Statescript](../README.md#in-godot) — the frame and fixed rails
