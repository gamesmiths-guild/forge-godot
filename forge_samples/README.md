# Forge Samples

Sample scenes demonstrating Forge for Godot: attributes, effects, gameplay tags, abilities, events, cues, and Statescript, in both 2D and 3D.

This folder is optional. Deleting it has no effect on the plugin.

## Registering the sample tags

The samples rely on gameplay tags such as `cooldown.skill.dash` and `cue.vfx.fire`, declared in `forge_samples_tags.tres`. 

Attach that file as a read-only tag source, once:

1. Open the **Tags** dock in the Godot editor.
2. Click the sources button next to `Add Tag`.
3. Choose **Add Source...** and select `forge_samples/forge_samples_tags.tres`.

The sample tags then resolve everywhere and show up in the tag tree, dimmed to mark them read-only. They are never copied into your project's own tag file, and detaching the source removes them again in one click.

Without this step the sample scenes will fail to resolve their tags at runtime.

## Running the samples

Open `forge_samples/Main.tscn` and run it. The hub scene links to the 2D and 3D sample levels.

## Layout

| Path | Contents |
| --- | --- |
| `2d/` | 2D character, effect areas, floating text and particle cue handlers. |
| `3d/` | 3D character, enemies, and the player ability set (dash, projectile, reflect, shield). |
| `common/` | Attribute sets, shared effect resources, custom executions and calculators. |

## A note on `[GlobalClass]`

Sample scripts such as `DashAbilityBehavior` and `ParticlesCueHandler2D` are marked `[GlobalClass]`, so they appear in the editor's node and resource creation dialogs alongside your own types. If that clutter isn't wanted, skip this folder when installing the plugin, or delete it once you're done reading the code.
