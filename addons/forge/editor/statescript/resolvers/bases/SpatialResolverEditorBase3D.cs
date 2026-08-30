// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;

/// <summary>
/// Base editor for every resolver that reads something off the 3D node an entity lives on.
/// </summary>
/// <remarks>
/// Renders the two rows they all share - the entity operand and the optional descendant path - and leaves the
/// resolver's own settings to <see cref="BuildSettingsRows"/>.
/// </remarks>
internal abstract partial class SpatialResolverEditorBase3D : EntityScopedResolverEditorBase
{
	private const float LabelWidth = 74.0f;

	private LineEdit? _nodePathField;

	/// <summary>
	/// Gets the type this resolver produces, used for input compatibility.
	/// </summary>
	protected abstract Type ValueClrType { get; }

	/// <summary>
	/// Creates the resource for this resolver with its own settings already applied. The base fills in the shared
	/// entity and path operands.
	/// </summary>
	/// <returns>The resource to serialize.</returns>
	protected abstract SpatialResolverResourceBase3D BuildResource();

	/// <summary>
	/// Gets the graph being edited, for subclasses that host nested resolver pickers.
	/// </summary>
	protected StatescriptGraph? Graph { get; private set; }

	/// <inheritdoc/>
	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == ValueClrType || expectedType == typeof(Forge.Statescript.Variant128);
	}

	/// <inheritdoc/>
	public override void Setup(
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged,
		bool isArray)
	{
		var existingResource = property?.Resolver as SpatialResolverResourceBase3D;

		Graph = graph;
		InitializeEntityScope(graph, onChanged, existingResource?.EntityResolver);

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(root);

		root.AddChild(CreateEntitySelectorRow());

		BuildSettingsRows(root, existingResource);

		_nodePathField = new LineEdit
		{
			PlaceholderText = "%CastPoint",
			Text = existingResource?.NodePath ?? string.Empty,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			TooltipText = "Optional. A child node to read instead of the entity's own, such as a %Muzzle marker.",
		};
		_nodePathField.TextChanged += _ => NotifyChanged();
		root.AddChild(ResolverEditorLayoutUtilities.CreateLabeledRow("Node:", _nodePathField, LabelWidth));
	}

	/// <inheritdoc/>
	public override void SaveTo(StatescriptNodeProperty property)
	{
		SpatialResolverResourceBase3D resource = BuildResource();
		resource.EntityResolver = BuildEntityResolverResource();
		resource.NodePath = _nodePathField is not null && IsInstanceValid(_nodePathField)
			? _nodePathField.Text
			: string.Empty;

		property.Resolver = resource;
	}

	/// <inheritdoc/>
	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_nodePathField = null;
	}

	/// <summary>
	/// Adds this resolver's own setting rows. The default adds none.
	/// </summary>
	/// <param name="root">The container to add rows to.</param>
	/// <param name="existingResource">The resource being edited, when one exists.</param>
	protected virtual void BuildSettingsRows(VBoxContainer root, SpatialResolverResourceBase3D? existingResource)
	{
	}

	/// <summary>
	/// Builds a labeled dropdown row for an enum setting.
	/// </summary>
	/// <param name="root">The container to add the row to.</param>
	/// <param name="label">The row label.</param>
	/// <param name="itemNames">The entries, in enum order.</param>
	/// <param name="selectedIndex">The entry to start on.</param>
	/// <returns>The dropdown, so the caller can read its selection when saving.</returns>
	protected OptionButton BuildEnumRow(VBoxContainer root, string label, string[] itemNames, int selectedIndex)
	{
		var dropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };

		foreach (string itemName in itemNames)
		{
			dropdown.AddItem(itemName);
		}

		dropdown.Selected = Math.Clamp(selectedIndex, 0, itemNames.Length - 1);
		dropdown.ItemSelected += _ => NotifyChanged();
		root.AddChild(ResolverEditorLayoutUtilities.CreateLabeledRow(label, dropdown, LabelWidth));
		return dropdown;
	}
}
#endif
