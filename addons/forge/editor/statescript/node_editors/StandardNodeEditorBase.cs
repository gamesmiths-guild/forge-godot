// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Collections.Generic;
using System.Linq;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Gamesmiths.Forge.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

/// <summary>
/// Reusable custom node editor that renders a node's constructor parameters (behavior-selecting enums and flags),
/// standard input-property rows, and output-variable rows with correct object-lane binding.
/// </summary>
/// <remarks>
/// <para>Subclasses declare their handled node type, any constructor parameters, and which output variables are
/// object-backed (so those bind through their object variable type instead of the default value-lane path, which
/// would otherwise break the panel).</para>
/// </remarks>
internal abstract partial class StandardNodeEditorBase : CustomNodeEditor
{
	private const string SettingsFoldKey = "_fold_settings";
	private const string InputFoldKey = "_fold_input";
	private const string OutputFoldKey = "_fold_output";

	private const float SettingsLabelMinWidth = 96.0f;

	/// <summary>
	/// Gets the constructor parameters exposed as editor controls. Empty by default.
	/// </summary>
	protected virtual IReadOnlyList<NodeConfigParam> ConstructorParams => [];

	/// <inheritdoc/>
	public override void BuildPropertySections(StatescriptNodeDiscovery.NodeTypeInfo typeInfo)
	{
		BuildSettingsSection();
		BuildInputSection(typeInfo);
		BuildOutputSection(typeInfo);
	}

	/// <summary>
	/// Gets the object variable type id for an object-backed output variable, or <see langword="null"/> when the
	/// output is value-lane and should use the default rendering.
	/// </summary>
	/// <param name="outputIndex">The output variable index.</param>
	/// <returns>The object type id, or <see langword="null"/> for value-lane outputs.</returns>
	protected virtual string? GetOutputObjectTypeId(int outputIndex)
	{
		return null;
	}

	/// <summary>
	/// Gets the constant value a fresh input slot should be seeded with, or <see langword="null"/> to use the type's
	/// zero value. Override for inputs whose conventional default is not zero (e.g. a level of 1).
	/// </summary>
	/// <param name="inputIndex">The input property index.</param>
	/// <returns>The seed value, or <see langword="null"/> for the default behavior.</returns>
	protected virtual Variant? GetDefaultInputConstant(int inputIndex)
	{
		return null;
	}

	/// <summary>
	/// Gets the resolver a fresh input slot should be seeded with, or <see langword="null"/> to leave it to the
	/// dropdown's own default. Override for inputs whose conventional default is a composed resolver rather than a
	/// constant — an array of the entities a query should pass through, say. The returned resource is bound as-is, so
	/// build a new one per call.
	/// </summary>
	/// <param name="inputIndex">The input property index.</param>
	/// <returns>The resolver to seed, or <see langword="null"/> for the default behavior.</returns>
	protected virtual StatescriptResolverResource? GetDefaultInputResolver(int inputIndex)
	{
		return null;
	}

	/// <summary>
	/// Gets whether the input property at the given index is rendered for the node's current configuration. Returns
	/// <see langword="true"/> by default. Override to hide an input the runtime does not read under the current
	/// settings (for example a value consumed by only one branch of a mode-selecting enum), so an unused row is not
	/// shown. The config parameter that drives the decision must be declared with
	/// <see cref="NodeConfigParam.AffectsLayout"/> set so the panel rebuilds when it changes. A hidden input keeps its
	/// existing binding, so the value returns unchanged when the input becomes relevant again.
	/// </summary>
	/// <param name="inputIndex">The input property index.</param>
	/// <returns><see langword="true"/> to render the input; <see langword="false"/> to hide it.</returns>
	protected virtual bool IsInputVisible(int inputIndex)
	{
		return true;
	}

	/// <summary>
	/// Gets whether the setting with the given key is rendered for the node's current configuration. Returns
	/// <see langword="true"/> by default. Override to hide a setting the runtime does not read under the current
	/// configuration — the path to an existing area on a node currently building its own shape, say — so it cannot be
	/// filled in and silently ignored. The setting that drives the decision must be declared with
	/// <see cref="NodeConfigParam.AffectsLayout"/> set so the panel rebuilds when it changes. A hidden setting keeps
	/// its stored value, so it returns unchanged when it becomes relevant again.
	/// </summary>
	/// <param name="key">The CustomData key, matching the runtime node constructor parameter name.</param>
	/// <returns><see langword="true"/> to render the setting; <see langword="false"/> to hide it.</returns>
	protected virtual bool IsSettingVisible(string key)
	{
		return true;
	}

	/// <summary>
	/// Reads a string-valued config entry (a constructor parameter persisted in the node's <c>CustomData</c>),
	/// returning <paramref name="defaultValue"/> when it is unset. Use from an <see cref="IsInputVisible"/> override to
	/// read the enum member name that drives an input's visibility.
	/// </summary>
	/// <param name="key">The CustomData key, matching the runtime node constructor parameter name.</param>
	/// <param name="defaultValue">The value returned when the entry is unset.</param>
	protected string ReadStringConfig(string key, string defaultValue)
	{
		return NodeResource.CustomData.TryGetValue(key, out Variant value) ? value.AsString() : defaultValue;
	}

	/// <summary>
	/// Reads a boolean-valued config entry (a constructor parameter persisted in the node's <c>CustomData</c>),
	/// returning <paramref name="defaultValue"/> when it is unset. Use from an <see cref="IsInputVisible"/> override to
	/// read the flag that drives an input's visibility.
	/// </summary>
	/// <param name="key">The CustomData key, matching the runtime node constructor parameter name.</param>
	/// <param name="defaultValue">The value returned when the entry is unset.</param>
	protected bool ReadBoolConfig(string key, bool defaultValue)
	{
		return NodeResource.CustomData.TryGetValue(key, out Variant value) ? value.AsBool() : defaultValue;
	}

	private static Label BuildSettingsLabel(NodeConfigParam parameter)
	{
		return new Label
		{
			Text = $"{parameter.Label}:",
			CustomMinimumSize = new Vector2(SettingsLabelMinWidth, 0),
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Center,
		};
	}

	private void BuildSettingsSection()
	{
		List<NodeConfigParam> parameters = [.. ConstructorParams.Where(x => IsSettingVisible(x.Key))];

		if (parameters.Count == 0)
		{
			return;
		}

		FoldableContainer container = AddPropertySectionDivider(
			"Settings",
			InputPropertyColor,
			SettingsFoldKey,
			GetFoldState(SettingsFoldKey));

		var root = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		container.AddChild(root);

		// Bool-only sections stay a plain checkbox stack. When any dropdown is present the rows go into a two-column
		// grid so the label column is sized to the widest label by the layout engine itself (using each label's real
		// minimum size) and every dropdown starts at the same x — no fragile font measurement that can mismatch the
		// node's actual render font and let a long label shove its dropdown right.
		if (!parameters.Exists(x => x.EnumNames is { Length: > 0 } || x.IsText))
		{
			foreach (NodeConfigParam parameter in parameters)
			{
				root.AddChild(BuildBoolRow(parameter));
			}

			return;
		}

		var grid = new GridContainer
		{
			Columns = 2,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		grid.AddThemeConstantOverride("h_separation", 5);
		root.AddChild(grid);

		foreach (NodeConfigParam parameter in parameters)
		{
			AddParamRow(grid, parameter);
		}
	}

	// Text is written straight into CustomData on every keystroke so a save or a run mid-edit can never drop what was
	// typed - the field still owning focus is exactly when that happens. Those syncs record nothing, and the undo step
	// is recorded once when the edit finishes, against the value the field held before it started. Without restoring
	// that original first, the recording call would see no change from the keystroke syncs and register nothing at all.
	private void BindTextRow(LineEdit lineEdit, NodeConfigParam parameter)
	{
		string originalValue = ReadStringConfig(parameter.Key, parameter.DefaultName ?? string.Empty);

		lineEdit.FocusEntered += () =>
			originalValue = ReadStringConfig(parameter.Key, parameter.DefaultName ?? string.Empty);
		lineEdit.TextChanged += text => SyncNodeConfig(parameter.Key, text);

		void CommitEdit()
		{
			string finalValue = lineEdit.Text;

			if (finalValue == originalValue)
			{
				return;
			}

			SyncNodeConfig(parameter.Key, originalValue);
			SetNodeConfig(parameter.Key, finalValue, $"Change {parameter.Label}", parameter.AffectsLayout);
			originalValue = finalValue;
		}

		lineEdit.TextSubmitted += _ => CommitEdit();
		lineEdit.FocusExited += CommitEdit;
	}

	private void AddParamRow(GridContainer grid, NodeConfigParam parameter)
	{
		if (parameter.IsText)
		{
			grid.AddChild(BuildSettingsLabel(parameter));

			var lineEdit = new LineEdit
			{
				Text = ReadStringConfig(parameter.Key, parameter.DefaultName ?? string.Empty),
				PlaceholderText = parameter.Placeholder,
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			};

			BindTextRow(lineEdit, parameter);

			grid.AddChild(lineEdit);
			return;
		}

		if (parameter.EnumNames is { Length: > 0 } enumNames)
		{
			grid.AddChild(BuildSettingsLabel(parameter));

			string currentName = ReadStringConfig(parameter.Key, parameter.DefaultName ?? enumNames[0]);
			int currentIndex = System.Array.IndexOf(enumNames, currentName);

			var dropdown = new OptionButton { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
			for (int i = 0; i < enumNames.Length; i++)
			{
				dropdown.AddItem(enumNames[i]);
			}

			dropdown.Selected = currentIndex >= 0 ? currentIndex : 0;

			// Enum members are stored by name so flags enums parse correctly at graph-build time.
			dropdown.ItemSelected += index =>
				SetNodeConfig(parameter.Key, enumNames[(int)index], $"Change {parameter.Label}", parameter.AffectsLayout);

			grid.AddChild(dropdown);
			return;
		}

		// Empty label cell keeps a mixed section's checkbox aligned under the dropdown column.
		grid.AddChild(new Control());
		grid.AddChild(BuildBoolRow(parameter));
	}

	private CheckBox BuildBoolRow(NodeConfigParam parameter)
	{
		var checkBox = new CheckBox
		{
			Text = parameter.Label,
			ButtonPressed = ReadBoolConfig(parameter.Key, parameter.DefaultBool),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		checkBox.Toggled += pressed =>
			SetNodeConfig(parameter.Key, pressed, $"Change {parameter.Label}", parameter.AffectsLayout);
		return checkBox;
	}

	private void BuildInputSection(StatescriptNodeDiscovery.NodeTypeInfo typeInfo)
	{
		if (typeInfo.InputPropertiesInfo.Length == 0)
		{
			return;
		}

		// Resolve visibility up front so a section whose inputs are all hidden by the current configuration renders no
		// header either. The row index passed to AddInputPropertyRow stays the true runtime input index so bindings
		// still map correctly across the skipped rows.
		var visibleIndices = new List<int>();
		for (int i = 0; i < typeInfo.InputPropertiesInfo.Length; i++)
		{
			if (IsInputVisible(i))
			{
				visibleIndices.Add(i);
			}
		}

		if (visibleIndices.Count == 0)
		{
			return;
		}

		FoldableContainer container = AddPropertySectionDivider(
			"Input Properties",
			InputPropertyColor,
			InputFoldKey,
			GetFoldState(InputFoldKey));

		var root = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		container.AddChild(root);

		foreach (int i in visibleIndices)
		{
			AddInputPropertyRow(
				typeInfo.InputPropertiesInfo[i],
				i,
				root,
				defaultConstantValue: GetDefaultInputConstant(i),
				defaultResolver: GetDefaultInputResolver(i));
		}
	}

	private void BuildOutputSection(StatescriptNodeDiscovery.NodeTypeInfo typeInfo)
	{
		if (typeInfo.OutputVariablesInfo.Length == 0)
		{
			return;
		}

		FoldableContainer container = AddPropertySectionDivider(
			"Output Variables",
			OutputVariableColor,
			OutputFoldKey,
			GetFoldState(OutputFoldKey));

		var root = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		container.AddChild(root);

		for (int i = 0; i < typeInfo.OutputVariablesInfo.Length; i++)
		{
			string? objectTypeId = GetOutputObjectTypeId(i);

			if (objectTypeId is null)
			{
				StatescriptNodeDiscovery.OutputVariableInfo info = typeInfo.OutputVariablesInfo[i];
				AddScalarOutputVariableRow(root, info.Label, i, info.ValueType);
				continue;
			}

			AddObjectOutputRow(root, typeInfo.OutputVariablesInfo[i].Label, i, objectTypeId);
		}
	}

	private void AddObjectOutputRow(VBoxContainer container, string label, int index, string objectTypeId)
	{
		var candidates = new List<string>();

		foreach (StatescriptGraphVariable variable in Graph.Variables)
		{
			if (variable.ObjectTypeId == objectTypeId
				&& !variable.IsArray
				&& !string.IsNullOrEmpty(variable.VariableName))
			{
				candidates.Add(variable.VariableName);
			}
		}

		StatescriptNodeProperty? binding = FindBinding(StatescriptPropertyDirection.Output, index);
		string? current = binding?.Resolver is VariableResolverResource resolver ? resolver.VariableName : null;

		if (!string.IsNullOrEmpty(current) && !candidates.Contains(current))
		{
			current = null;
			RemoveBinding(StatescriptPropertyDirection.Output, index);
		}

		AddOutputVariableBadgeRow(
			container,
			label,
			$"_fold_output_obj_{index}",
			candidates,
			current,
			variableName => OnObjectOutputSelected(variableName, index, objectTypeId));
	}

	private void OnObjectOutputSelected(string? variableName, int index, string objectTypeId)
	{
		VariableResolverResource? newResolver = string.IsNullOrEmpty(variableName)
			? null
			: new VariableResolverResource
			{
				VariableName = variableName,
				Scope = VariableScope.Graph,
				ObjectTypeId = objectTypeId,
				IsArray = false,
			};

		ApplyOutputVariableBinding(index, newResolver, "Change Output Variable");
	}
}
#endif
