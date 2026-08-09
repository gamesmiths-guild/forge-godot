# Gameplay Tags

Tags are hierarchical identifiers used throughout Forge to classify and match entities, effects, abilities, and cues. They use dot notation — `cooldown.skill.dash`, `trait.flammable` — and match by prefix, so `cooldown.skill` matches every skill cooldown beneath it.

This page covers how tags are stored and edited in Godot. For how tags behave at runtime, see the [core Forge documentation](https://github.com/gamesmiths-guild/forge/blob/main/docs/README.md).

## Tag Sources

Tags live in **tag source** resources (`ForgeTagsSource`), and a project can read from as many as it likes. The tags the project knows about are the union of all of them:

- Duplicates are ignored. Two sources may declare the same tag without conflict.
- A tag exists as long as **any** source declares it. To retire one completely, remove it from every source that has it.

That makes sources purely an organizational tool. Split tags by feature — cooldowns in one file, effects in another — or keep a shared set you copy between projects. Nothing at runtime depends on which file a tag came from; the game sees one merged registry.

### Parents come for free, but can also be declared

Declaring `cooldown.skill.dash` also creates `cooldown` and `cooldown.skill` — you never have to declare a parent to use one beneath it.

A parent can still be declared in its own right, and that changes how it behaves:

- **Implied** parents exist only while something beneath them does. Remove the last child and the parent goes with it. In the Tags dock they appear greyed out with their delete button disabled, because there is nothing stored to delete.
- **Declared** parents are ordinary tags. They survive their last child, can be matched and assigned on their own, and are removed like any other tag.

Both are useful. Declare a parent when it is a tag in its own right — the sample source declares `cooldown`, `cooldown.skill`, `cue.vfx`, `event`, `event.damage` and `set_by_caller` for exactly that reason — and leave it implied when it is only a grouping.

### Where sources are configured

The ordered list lives in Project Settings under **Forge → Tags** (`forge/tags/sources`), though the Tags dock manages it for you. Enabling the plugin creates `res://forge_tags.tres` to start with.

Sources are referenced by UID, so moving or renaming one inside the Godot editor keeps the setting valid. Commit them to version control, and keep them **outside `addons/`** — reinstalling or updating Forge replaces that folder wholesale, and your tags must not be inside it.

Order affects display grouping only. The merged tag set is the same whatever the order.

## The Tags Dock

Open the **Tags** dock from the right-hand panel. It has two views.

### By Source

The editable view. Each source is a collapsible section: a header row naming the file, with that source's tags beneath it.

**Adding a tag**

1. Type the key in the **Tag Name** field, using dot notation.
2. Pick the destination source beside it, if the project has more than one.
3. Press Enter or click **Add Tag**.

The **+** buttons are shortcuts into that same field. **+** on a source header points it at that source; **+** on a tag row also prefills the parent key, so adding a child is one click plus the last segment.

**Removing a tag**

Click the **🗑️** on its row. That removes the tag and everything beneath it *from that source only*. If another source also declares it, the tag still exists.

**Per-source controls**, on each header row:

| Button | Action |
| --- | --- |
| ↑ / ↓ | Reorder the source in the list |
| Folder | Reveal the file in the FileSystem dock |
| + | Add a tag to this source |
| 🗑️ | Stop reading from this source. **The file is not deleted.** |

A source whose file is missing shows as `MISSING` in red, keeping its entry so you can fix the path or remove it deliberately.

### Merged

The union of every source, exactly as the running game resolves it, with a **Declared by** column naming the sources behind each tag — useful for spotting a tag that comes from somewhere you did not expect.

This view is read-only. A tag here can come from several sources at once, so removing it would have to ask which one you meant, and adding one would have no obvious destination — questions the By Source view answers by construction.

### Toolbar

- **New Source** — create a tag source and start reading from it.
- **Add Existing** — pick up a tag source that already exists, such as the sample tags.
- **Find Sources** — search the project for tag sources not yet in use and offer to add them.
- **Repair** — check the project for tag references that no source declares any more. See [Repairing Broken References](#repairing-broken-references).
- **Filter tags** — narrow the tree by substring; matches keep their parents visible.
- **Source dropdown** — narrow to a single source (By Source only).

## Editing a Source in the Inspector

Selecting a `ForgeTagsSource` in the FileSystem gives the same tree editor for that one file, rather than a raw list of strings. It works whether or not the source is part of the project — a source can be authored first and attached later, and the inspector offers **Add to project** when it is not yet in the list.

## Assigning Tags

Two resource types carry tags:

- **`ForgeTag`** — a single tag. Shown as a picker with a search box.
- **`ForgeTagContainer`** — a set of tags. Shown as a checklist with a search box.

Both read from the merged registry and update immediately when tags change, so a tag added in the dock is selectable without reopening anything.

## Repairing Broken References

Removing a tag that assets still reference leaves those references dangling. Two places find them and offer to strip them — the **Repair** button in the Tags dock, right where tags get removed, and **Project → Tools → Forge → Repair assets tags**. Both run the same check.

It reports what it found before changing anything, listing each asset and where inside it the reference lives. Repairing rewrites the affected scenes and resources and **cannot be undone**, so commit or back up first.

It looks everywhere a tag key is stored, not just at `ForgeTag` and `ForgeTagContainer`: cue tags on cue handler nodes, and the tag keys Statescript resolvers hold as plain strings — tag resolvers, set-by-caller identifiers and ability cooldowns. A type that gains a tag-valued property has to be added to that list, or a repair will quietly walk past it.

Scenes are read and rewritten as text, never loaded, so scanning cannot run scene scripts or disturb anything. Binary `.scn` scenes are not text and are reported as skipped rather than silently ignored.

## Sample Tags

The `forge_samples` folder declares its own tags in `forge_samples_tags.tres`. Add it as a source — **Find Sources** will offer it — and the sample tags appear under their own header alongside yours. Removing the source later takes them out again in one click, without touching your own tags and without deleting the file.
