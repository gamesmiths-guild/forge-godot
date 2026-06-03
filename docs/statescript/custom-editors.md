# Custom Editors

The Statescript graph editor uses a plugin-like architecture for rendering node property sections and resolver configuration UI. By creating custom editors, you can control exactly how your custom nodes and resolvers appear in the graph editor.

## Custom Node Editors

By default, when a node is displayed in the graph editor, its input properties and output variables are rendered using the standard layout: a resolver dropdown for each input, and a variable dropdown for each output. A **custom node editor** replaces this default layout with a fully custom UI.

### When to Create a Custom Node Editor

- When a node's inputs depend on each other (e.g., the available options for one input change based on another input's selection).
- When you need multi-step workflows (e.g., select a set first, then select a variable from that set).
- When the default layout doesn't communicate the node's intent clearly.

Most custom nodes **don't need** a custom node editor. The default rendering works well for nodes with independent input properties. Only create a custom editor when the default behavior is insufficient.

Port labels are not, by themselves, a reason to create a custom node editor. The default node rendering reads labels from the runtime node's ports, so custom flow, event, and subgraph port names should be defined in the Forge node with `CreatePort<T>(index, "Label")`.

### Creating a Custom Node Editor

**Requirements:**

- Extend `CustomNodeEditor`.
- Add `[Tool]` attribute.
- Override `HandledRuntimeTypeName` to return the fully qualified C# type name of the node this editor handles.
- Override `BuildPropertySections` to construct the custom UI.

**Base class helpers:**

The `CustomNodeEditor` base class provides helper methods that mirror the default behavior, so you can selectively override parts of the rendering while reusing the rest:

| Method | Purpose |
|--------|---------|
| `AddPropertySectionDivider` | Adds a foldable section header (e.g., "Input Properties"). |
| `AddInputPropertyRow` | Renders a standard input property row (resolver dropdown + editor). |
| `AddOutputVariableRow` | Renders a standard output variable row (variable dropdown). |
| `FindBinding` | Finds an existing property binding by direction and index. |
| `EnsureBinding` | Finds or creates a property binding. |
| `RemoveBinding` | Removes a property binding. |
| `ShowResolverEditorUI` | Shows a resolver editor inside a container. |
| `RecordResolverBindingChange` | Records an undo/redo action for a resolver change. |
| `ResetSize` | Requests the graph node to recalculate its size. |
| `RaisePropertyBindingChanged` | Notifies the graph that a binding has changed. |

**Example — Custom Editor with Dependent Inputs:**

```csharp
#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Godot;

namespace MyGame.Editor;

[Tool]
internal sealed partial class MyCustomNodeEditor : CustomNodeEditor
{
    public override string HandledRuntimeTypeName
        => "MyGame.Nodes.MyCustomNode";

    public override void BuildPropertySections(
        StatescriptNodeDiscovery.NodeTypeInfo typeInfo)
    {
        // Add a foldable "Input Properties" section.
        var inputFolded = GetFoldState("_fold_input");
        FoldableContainer inputContainer = AddPropertySectionDivider(
            "Input Properties",
            InputPropertyColor,
            "_fold_input",
            inputFolded);

        // Render the first input with the standard layout.
        if (typeInfo.InputProperties.Count > 0)
        {
            AddInputPropertyRow(typeInfo.InputProperties[0], 0, inputContainer);
        }

        // Render a custom UI for the second input.
        if (typeInfo.InputProperties.Count > 1)
        {
            var customRow = new HBoxContainer();
            inputContainer.AddChild(customRow);

            customRow.AddChild(new Label { Text = "Custom:" });

            var dropdown = new OptionButton
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            };

            // Populate dropdown with custom logic...
            customRow.AddChild(dropdown);
        }

        // Add output variables section with standard layout.
        var outputFolded = GetFoldState("_fold_output");
        FoldableContainer outputContainer = AddPropertySectionDivider(
            "Output Variables",
            OutputVariableColor,
            "_fold_output",
            outputFolded);

        for (var i = 0; i < typeInfo.OutputVariables.Count; i++)
        {
            AddOutputVariableRow(typeInfo.OutputVariables[i], i, outputContainer);
        }
    }
}
#endif
```

### How Discovery Works

Custom node editors are discovered automatically. The graph editor searches all loaded assemblies for concrete subclasses of `CustomNodeEditor` and matches them to nodes by comparing the node's `RuntimeTypeName` with the editor's `HandledRuntimeTypeName`. No manual registration is needed.

If no custom editor is found for a node type, the default rendering is used.

## Custom Resolver Editors

A **resolver editor** (`NodeEditorProperty`) provides the UI that appears inside a node when the user selects a resolver type from the input property dropdown. Every resolver type needs a corresponding editor.

For the full walkthrough on creating custom resolvers (including the resource and editor), see [Custom Resolvers](custom-resolvers.md).

### Creating a Custom Resolver Editor

**Requirements:**

- Extend `NodeEditorProperty`.
- Add `[Tool]` attribute.
- Override `DisplayName` to set the label shown in the resolver dropdown.
- Override `ResolverTypeId` to match the corresponding `StatescriptResolverResource`'s `ResolverTypeId`.
- Override `IsCompatibleWith` to filter which node input types this resolver supports.
- Override `Setup` to build the editor UI and restore state from an existing binding.
- Override `SaveTo` to write the current configuration to the property binding.

**Lifecycle:**

1. **Discovery**: `StatescriptResolverRegistry` discovers all `NodeEditorProperty` subclasses via reflection.
2. **Dropdown**: When a node input property is rendered, the registry creates temporary instances of each editor, calls `IsCompatibleWith` to filter, and lists the compatible ones by `DisplayName`.
3. **Selection**: When the user selects a resolver type, a new instance is created and `Setup` is called.
4. **Restore**: If a `StatescriptNodeProperty` binding already exists for this input, the existing resolver resource is passed to `Setup` so the editor can restore its state.
5. **Save**: When the user changes a value, the `onChanged` callback fires, and `SaveTo` is called to write the configuration to the property binding.

**Signals and layout:**

- Call `_onChanged?.Invoke()` whenever the user modifies a value. This triggers the graph editor to save the binding.
- Call `RaiseLayoutSizeChanged()` if your UI changes height (e.g., expanding/collapsing a section). This tells the graph node to recalculate its size.
- Override `ClearCallbacks()` if you store additional delegate fields beyond `_onChanged`, to prevent serialization issues during hot-reload.

### ResolverTypeId Matching

The `ResolverTypeId` string is the key that connects the three parts of the resolver system:

| Component | Role |
|-----------|------|
| `StatescriptResolverResource.ResolverTypeId` | Serialized with the graph. Used at build time to construct the core resolver. |
| `NodeEditorProperty.ResolverTypeId` | Used by the editor to match a serialized resource to its UI. |

Both must return the **exact same string**. If they don't match, the editor won't be able to restore the resolver's configuration when reopening the graph.

## Best Practices

1. **Only create custom node editors when needed**: The default rendering handles most cases. Custom editors add maintenance overhead.
2. **Reuse base class helpers**: Use `AddInputPropertyRow` and `AddOutputVariableRow` for inputs that don't need custom rendering, and only build custom UI for the parts that require it.
3. **Always call `_onChanged`**: The graph editor relies on this callback to know when to persist changes. Forgetting it causes lost configuration.
4. **Handle null gracefully**: The `property` parameter in `Setup` may be null for new bindings. Always check before accessing `property.Resolver`.
5. **Use `[Tool]` on all editor classes**: Editor classes run inside the Godot editor process. Without `[Tool]`, your code won't execute.
6. **Wrap in `#if TOOLS`**: Editor code should be wrapped in `#if TOOLS` preprocessor directives to exclude it from game builds.
