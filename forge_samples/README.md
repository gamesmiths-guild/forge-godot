# Forge Samples

Sample scenes demonstrating Forge for Godot: attributes, effects, gameplay tags, abilities, events, cues, and Statescript, in both 2D and 3D.

This folder is optional. Deleting it has no effect on the plugin.

## Registering the sample tags

The samples rely on gameplay tags such as `cooldown.skill.dash` and `cue.vfx.fire`, declared in `forge_samples_tags.tres`.

Add that file as a tag source, once:

1. Open the **Tags** dock in the Godot editor.
2. Click **Find Sources** and confirm, or use **Add Existing** and select `forge_samples/forge_samples_tags.tres`.

The sample tags then resolve everywhere and appear under their own header in the dock, next to your own. They are never copied into your project's other tag files, and removing the source takes them out again in one click without deleting anything.

Without this step the sample scenes will fail to resolve their tags at runtime.

## Running the samples

Open `forge_samples/Main.tscn` and run it. The hub scene lists every demo and loads the one you pick.

## Layout

| Path | Contents |
| --- | --- |
| `hub/` | The demo hub, plus one `DemoEntry` resource per demo under `entries/`. |
| `demos/01_realtime_3d/` | 3D character, enemies, and the player ability set (dash, projectile, reflect, shield). |
| `demos/02_turnbased_2d/` | 2D character, effect areas, floating text and particle cue handlers. |
| `shared/` | Attribute sets, shared effect resources, custom executions and calculators. |

## Adding a demo

The hub is data-driven. Add a `DemoEntry` resource under `hub/entries/` — title, tagline, blurb, highlights, accent colour and the scene to load — then append it to the `Demos` array on the `Hub` node. No code changes.

## A note on `[GlobalClass]`

Sample scripts such as `DashAbilityBehavior` and `ParticlesCueHandler2D` are marked `[GlobalClass]`, so they appear in the editor's node and resource creation dialogs alongside your own types. If that clutter isn't wanted, skip this folder when installing the plugin, or delete it once you're done reading the code.
