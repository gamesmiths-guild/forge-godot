// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Godot;
using GodotCollections = Godot.Collections;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

/// <summary>
/// Base editor for the nodes whose port count is a constructor argument and whose ports are selected by an integer:
/// the Switch node's case ports and the State Machine node's state subgraph ports.
/// </summary>
/// <remarks>
/// <para>The Settings section offers the two ways of deciding how many ports the node has. Leave <b>Enum</b> empty and
/// the count is authored directly as a number. Bind a <see cref="ForgeStatescriptEnum"/> and the node follows it: one
/// port per member, each labeled with the member's name, so the graph reads <c>Attack</c> instead of <c>2</c>.</para>
/// <para>The count is persisted under the runtime constructor parameter name and consumed at graph-build time, so the
/// node the builder creates has exactly the ports drawn here. Changing it goes through the layout-config path, which
/// drops (and restores on undo) the connections attached to ports that no longer exist.</para>
/// </remarks>
internal abstract partial class PortCountNodeEditorBase : CustomNodeEditor
{
	/// <summary>
	/// CustomData key holding the enum the ports follow. Editor-only: it is not a constructor parameter, so the graph
	/// builder ignores it.
	/// </summary>
	protected const string EnumConfigKey = "_port_enum";

	private const string SettingsFoldKey = "_fold_settings";
	private const string InputFoldKey = "_fold_input";
	private const string OutputFoldKey = "_fold_output";

	private const float LabelWidth = 60.0f;

	/// <summary>
	/// Gets the CustomData key holding the port count, matching the runtime node's constructor parameter name.
	/// </summary>
	protected abstract string CountConfigKey { get; }

	/// <summary>
	/// Gets the label for the count control (for example <c>Cases</c> or <c>States</c>).
	/// </summary>
	protected abstract string CountLabel { get; }

	/// <summary>
	/// Gets the count a node uses when nothing is stored yet, matching the runtime constructor default.
	/// </summary>
	protected abstract int DefaultCount { get; }

	/// <summary>
	/// Gets the smallest count the runtime node accepts.
	/// </summary>
	protected abstract int MinCount { get; }

	/// <summary>
	/// Gets the largest count the runtime node accepts.
	/// </summary>
	protected abstract int MaxCount { get; }

	/// <summary>
	/// Gets the runtime index of the first port an enum member names. Ports before it (a State Machine's lifecycle and
	/// event ports) keep their declared labels.
	/// </summary>
	protected abstract int FirstEnumPortIndex { get; }

	/// <summary>
	/// Gets how many of the node's output ports the enum names, so ports the node adds beyond them (a Switch node's
	/// trailing Default port) keep their declared labels.
	/// </summary>
	/// <param name="typeInfo">Discovered metadata about the node type in its current configuration.</param>
	/// <returns>The number of enum-named ports.</returns>
	protected abstract int GetEnumPortCount(StatescriptNodeDiscovery.NodeTypeInfo typeInfo);

	/// <summary>
	/// Gets the input property index of the selector that picks between the enum-driven ports, so a fresh slot can
	/// default to the named-value resolver when an enum is bound.
	/// </summary>
	protected virtual int SelectorInputIndex => 0;

	/// <inheritdoc/>
	public override void BuildPropertySections(StatescriptNodeDiscovery.NodeTypeInfo typeInfo)
	{
		BuildSettingsSection();
		BuildInputSection(typeInfo);
		BuildOutputSection(typeInfo);
	}

	/// <inheritdoc/>
	public override bool SeedsDefaultBinding(int inputIndex)
	{
		// The selector's default follows the enum bound to the node, and a node being created has none yet. Seeding it
		// now would fix it on the plain constant and the named-value resolver could never claim the fresh slot.
		return inputIndex != SelectorInputIndex;
	}

	/// <inheritdoc/>
	internal override string? GetOutputPortLabel(
		int runtimePortIndex,
		StatescriptNodeDiscovery.NodeTypeInfo typeInfo)
	{
		ForgeStatescriptEnum? enumDefinition = ReadEnum();

		if (enumDefinition is null)
		{
			return null;
		}

		int memberValue = runtimePortIndex - FirstEnumPortIndex;

		if (memberValue < 0 || memberValue >= GetEnumPortCount(typeInfo))
		{
			return null;
		}

		string memberName = StatescriptEnumUtilities.GetMemberName(enumDefinition, memberValue);

		return memberName.Length == 0 ? null : memberName;
	}

	/// <summary>
	/// Reads the enum the ports follow, or <see langword="null"/> when the count is authored as a plain number.
	/// </summary>
	/// <returns>The bound enum, or <see langword="null"/>.</returns>
	protected ForgeStatescriptEnum? ReadEnum()
	{
		return NodeResource.CustomData.TryGetValue(EnumConfigKey, out Variant value)
			&& value.VariantType == Variant.Type.Object
				? value.As<ForgeStatescriptEnum>()
				: null;
	}

	/// <summary>
	/// Reads the stored port count, falling back to the runtime constructor default.
	/// </summary>
	/// <returns>The port count.</returns>
	protected int ReadCount()
	{
		return NodeResource.CustomData.TryGetValue(CountConfigKey, out Variant value)
			? Math.Clamp(value.AsInt32(), MinCount, MaxCount)
			: DefaultCount;
	}

	private static Label BuildWarningLabel(string text)
	{
		var label = new Label
		{
			Text = text,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};

		label.AddThemeColorOverride("font_color", EditorInterface.Singleton.GetEditorTheme()
			.GetColor("warning_color", "Editor"));

		return label;
	}

	private void BuildSettingsSection()
	{
		ForgeStatescriptEnum? enumDefinition = ReadEnum();
		int count = SyncCountToEnum(enumDefinition);

		FoldableContainer container = AddPropertySectionDivider(
			"Settings",
			InputPropertyColor,
			SettingsFoldKey,
			GetFoldState(SettingsFoldKey));

		var root = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		container.AddChild(root);

		OptionButton enumDropdown = new SearchableOptionButton
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			TooltipText = "Optional. When set, the node has one port per enum member, named after it. "
				+ $"Leave on (None) to author the {CountLabel.ToLowerInvariant()} count directly.",
		};

		StatescriptEnumUtilities.PopulateEnumDropdown(enumDropdown, enumDefinition);
		enumDropdown.ItemSelected += index => OnEnumSelected(enumDropdown, (int)index);
		root.AddChild(ResolverEditorLayoutUtilities.CreateLabeledRow("Enum:", enumDropdown, LabelWidth));

		var countSpinBox = new SpinBox
		{
			MinValue = MinCount,
			MaxValue = MaxCount,
			Step = 1,
			Value = count,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,

			// The count follows the enum's member count while one is bound, so editing it directly would only be undone
			// on the next rebuild.
			Editable = enumDefinition is null,
		};

		countSpinBox.ValueChanged += OnCountChanged;
		root.AddChild(ResolverEditorLayoutUtilities.CreateLabeledRow($"{CountLabel}:", countSpinBox, LabelWidth));

		// The runtime node requires at least one port, so an empty enum cannot be followed literally.
		if (enumDefinition?.Members.Count == 0)
		{
			string displayName = enumDefinition.GetDisplayName();
			string subject = displayName.Length == 0 ? "The selected enum" : $"'{displayName}'";

			root.AddChild(BuildWarningLabel(
				$"{subject} has no members yet, so the node shows a single unnamed port. "
				+ "Add member names to the enum resource."));
		}
	}

	/// <summary>
	/// Keeps the stored count in step with the bound enum, so a member added to (or removed from) the enum asset is
	/// reflected the next time the node is drawn.
	/// </summary>
	/// <param name="enumDefinition">The bound enum, if any.</param>
	/// <returns>The count the node should show.</returns>
	private int SyncCountToEnum(ForgeStatescriptEnum? enumDefinition)
	{
		int count = ReadCount();

		if (enumDefinition is null)
		{
			return count;
		}

		int enumCount = Math.Clamp(enumDefinition.Members.Count, MinCount, MaxCount);

		if (enumCount != count)
		{
			SetNodeLayoutConfig(
				new GodotCollections.Dictionary { { CountConfigKey, enumCount } },
				$"Sync {CountLabel} To Enum");
		}

		return enumCount;
	}

	private void OnEnumSelected(OptionButton dropdown, int index)
	{
		ForgeStatescriptEnum? enumDefinition = StatescriptEnumUtilities.GetSelectedEnum(dropdown, index);

		// Index 0 is (None) and means unbind; any other index that resolves to nothing is an enum that has moved or
		// been deleted, where leaving the node as it is beats silently unbinding it.
		if (enumDefinition is null && index != 0)
		{
			return;
		}

		var customData = new GodotCollections.Dictionary
		{
			{ EnumConfigKey, enumDefinition is null ? default : Variant.From(enumDefinition) },
		};

		// Binding an enum takes the count over immediately; clearing it keeps the count the node already had so no
		// ports (and no connections) are lost by unbinding alone.
		if (enumDefinition is not null)
		{
			customData[CountConfigKey] = Math.Clamp(enumDefinition.Members.Count, MinCount, MaxCount);
		}

		SetNodeLayoutConfig(customData, "Change Node Enum");
	}

	private void OnCountChanged(double value)
	{
		SetNodeLayoutConfig(
			new GodotCollections.Dictionary
			{
				{ CountConfigKey, Math.Clamp((int)value, MinCount, MaxCount) },
			},
			$"Change {CountLabel} Count");
	}

	private void BuildInputSection(StatescriptNodeDiscovery.NodeTypeInfo typeInfo)
	{
		if (typeInfo.InputPropertiesInfo.Length == 0)
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

		bool hasEnum = ReadEnum() is not null;

		for (int i = 0; i < typeInfo.InputPropertiesInfo.Length; i++)
		{
			// With an enum bound, a fresh selector slot starts on the named-value resolver, which is the whole point of
			// binding one. Any other resolver (a variable, an expression) stays available in the dropdown.
			AddInputPropertyRow(
				typeInfo.InputPropertiesInfo[i],
				i,
				root,
				preferredDefaultResolverTypeId: hasEnum && i == SelectorInputIndex ? "EnumConstant" : null);
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
			StatescriptNodeDiscovery.OutputVariableInfo info = typeInfo.OutputVariablesInfo[i];
			AddScalarOutputVariableRow(root, info.Label, i, info.ValueType);
		}
	}
}
#endif
