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
| `demos/03_statescript/` | The Real-Time 3D demo's abilities and enemy rebuilt as Statescript graphs, on a level shared with future demos. |
| `shared/` | Attribute sets, shared effect resources, custom executions and calculators. |

Each demo folder has a README of its own that walks through what is in it.

## Adding a demo

The hub is data-driven. Add a `DemoEntry` resource under `hub/entries/` — title, tagline, blurb, highlights, accent colour and the scene to load — then append it to the `Demos` array on the `Hub` node. No code changes.

## A note on `[GlobalClass]`

Sample scripts such as `DashAbilityBehavior` and `ParticlesCueHandler2D` are marked `[GlobalClass]`, so they appear in the editor's node and resource creation dialogs alongside your own types. If that clutter isn't wanted, skip this folder when installing the plugin, or delete it once you're done reading the code.

## Sound credits

The files in `shared/audio/` are based on the [Freesound](https://freesound.org) sounds below. Their authors dedicated them to the public domain under [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/), so they can be reused for any purpose without credit; they are credited here as a courtesy.

| File | Original sound | Author |
| --- | --- | --- |
| `bonfire-loop.ogg` | [Fireplace](https://freesound.org/people/myLoop/sounds/852107/) | myLoop |
| `brazier-toggle.ogg` | [Light Fire Sound.wav](https://freesound.org/people/Wdomino/sounds/507724/) | Wdomino |
| `death.ogg` | [Retro, Underwater Explosion](https://freesound.org/people/LilMati/sounds/459150/) | LilMati |
| `ember-landing.ogg` | [light bulb glass burst drop on floor2.wav](https://freesound.org/people/kyles/sounds/637659/) | kyles |
| `ember-launch.ogg` | [flame burst ignite bbq barbecue.flac](https://freesound.org/people/kyles/sounds/637533/) | kyles |
| `fire-loop.ogg` | [Ambiance_Fire_Bushes_Loop_Stereo.wav](https://freesound.org/people/Nox_Sound/sounds/564621/) | Nox_Sound |
| `fizzle.ogg` | [Click (1).mp3](https://freesound.org/people/7778/sounds/202314/) | 7778 |
| `heal.ogg` | [Heal - Rpg](https://freesound.org/people/colorsCrimsonTears/sounds/562292/) | colorsCrimsonTears |
| `healing-loop.ogg` | [crystal_loop.wav](https://freesound.org/people/markians/sounds/511773/) | markians |
| `hit.ogg` | [Blocking Arm With Hand](https://freesound.org/people/mmasonghi/sounds/321810/) | mmasonghi |
| `quench.ogg` | [04-Vapor.wav](https://freesound.org/people/HidroLion/sounds/491706/) | HidroLion |
| `reflect.ogg` | [Buffer Spell](https://freesound.org/people/deleted_user_3277771/sounds/176741/) | deleted_user_3277771 |
| `shield-hit.ogg` | [Iron Hits.wav](https://freesound.org/people/Mrthenoronha/sounds/371353/) | Mrthenoronha |
| `shield-loop.ogg` | [EnergyShield.mp3](https://freesound.org/people/Beussa/sounds/659967/) | Beussa |
| `shot.ogg` | [ToyGun 007.MP3](https://freesound.org/people/VKProduktion/sounds/215848/) | VKProduktion |
| `swing.ogg` | [Swinging axe.mp3](https://freesound.org/people/ZHR%C3%98/sounds/514162/) | ZHRØ |
| `water-dripping-loop.ogg` | [water dripping.mp3](https://freesound.org/people/sonscharcruterie/sounds/536188/) | sonscharcruterie |
| `woosh.ogg` | [Woosh_2](https://freesound.org/people/gulfstreamav/sounds/842511/) | gulfstreamav |
