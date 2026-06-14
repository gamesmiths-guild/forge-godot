// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Tags;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Gamesmiths.Forge.Tags;
using Godot;

using GodotStringArray = Godot.Collections.Array<string>;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor for a tag input. Embeds the shared <see cref="TagContainerSelectionControl"/> tag picker so the
/// designer selects one or more tags; the selection is stored on a <see cref="TagResolverResource"/>.
/// </summary>
[Tool]
internal sealed partial class TagResolverEditor : NodeEditorProperty
{
	private GodotStringArray _selectedTags = [];
	private Action? _onChanged;
	private TagContainerSelectionControl? _selectionControl;

	/// <inheritdoc/>
	public override string DisplayName => "Tag";

	/// <inheritdoc/>
	public override string ResolverTypeId => "Tag";

	/// <inheritdoc/>
	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(Tag);
	}

	/// <inheritdoc/>
	public override void Setup(
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged,
		bool isArray)
	{
		_onChanged = onChanged;
		SizeFlagsHorizontal = SizeFlags.ExpandFill;

		_selectedTags = [];
		if (property?.Resolver is TagResolverResource resource)
		{
			_selectedTags.AddRange(resource.Tags);
		}

		_selectionControl = new TagContainerSelectionControl { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(_selectionControl);
		_selectionControl.SetValue(_selectedTags);
		_selectionControl.ValueChanged += OnSelectionChanged;
	}

	/// <inheritdoc/>
	public override void SaveTo(StatescriptNodeProperty property)
	{
		var tags = new GodotStringArray();
		tags.AddRange(_selectedTags);
		property.Resolver = new TagResolverResource { Tags = tags };
	}

	/// <inheritdoc/>
	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_onChanged = null;
		_selectionControl = null;
	}

	private void OnSelectionChanged(GodotStringArray tags)
	{
		_selectedTags = tags;
		_onChanged?.Invoke();
	}
}
#endif
