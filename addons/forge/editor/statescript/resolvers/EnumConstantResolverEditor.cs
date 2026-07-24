// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor that authors an integer constant by picking a member of a <see cref="ForgeStatescriptEnum"/>, so
/// selectors, comparisons and assignments read as <c>Attack</c> rather than <c>2</c>. What it contributes to the graph
/// is the member's ordinal value as a plain integer.
/// </summary>
[Tool]
internal sealed partial class EnumConstantResolverEditor : NodeEditorProperty
{
	private const float LabelWidth = 60.0f;

	private Action? _onChanged;
	private ForgeStatescriptEnum? _enumDefinition;
	private int _value;

	private OptionButton? _memberDropdown;

	/// <inheritdoc/>
	public override string DisplayName => "Enum";

	/// <inheritdoc/>
	public override string ResolverTypeId => "EnumConstant";

	/// <inheritdoc/>
	public override bool IsCompatibleWith(Type expectedType)
	{
		// An enum member resolves to an int, so it fits int inputs and the wildcard slots that accept any authorable
		// value (comparison operands, the Set Variable value, ...).
		return expectedType == typeof(int)
			|| expectedType == typeof(object)
			|| expectedType == typeof(ForgeVariant128);
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

		var existingResource = property?.Resolver as EnumConstantResolverResource;
		_enumDefinition = existingResource?.EnumDefinition;
		_value = existingResource?.Value ?? 0;

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(root);

		OptionButton enumDropdown = new SearchableOptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		StatescriptEnumUtilities.PopulateEnumDropdown(enumDropdown, _enumDefinition);
		enumDropdown.ItemSelected += index => OnEnumSelected(enumDropdown, (int)index);
		root.AddChild(ResolverEditorLayoutUtilities.CreateLabeledRow("Enum:", enumDropdown, LabelWidth));

		_memberDropdown = new SearchableOptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_memberDropdown.ItemSelected += OnMemberSelected;
		root.AddChild(ResolverEditorLayoutUtilities.CreateLabeledRow("Value:", _memberDropdown, LabelWidth));

		PopulateMemberDropdown();
	}

	/// <inheritdoc/>
	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new EnumConstantResolverResource
		{
			EnumDefinition = _enumDefinition,
			Value = _value,
		};
	}

	/// <inheritdoc/>
	public override bool TryGetInlineSummary(out string summary)
	{
		summary = StatescriptEnumUtilities.FormatValue(_enumDefinition, _value);
		return true;
	}

	/// <inheritdoc/>
	public override InlineSummaryBadgeKind GetInlineSummaryBadgeKind()
	{
		return InlineSummaryBadgeKind.Enum;
	}

	/// <inheritdoc/>
	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_onChanged = null;
		_memberDropdown = null;
	}

	private void OnEnumSelected(OptionButton dropdown, int index)
	{
		ForgeStatescriptEnum? enumDefinition = StatescriptEnumUtilities.GetSelectedPath(dropdown, index);

		// Index 0 is (None); any other index that resolves to nothing is an enum that has moved or been deleted, where
		// keeping the current selection beats silently clearing it.
		if (enumDefinition is null && index != 0)
		{
			return;
		}

		_enumDefinition = enumDefinition;

		// A value from the previous enum means nothing in the new one, so the selection restarts from its first member.
		_value = 0;
		PopulateMemberDropdown();
		_onChanged?.Invoke();
	}

	private void OnMemberSelected(long index)
	{
		_value = (int)index;
		_onChanged?.Invoke();
	}

	private void PopulateMemberDropdown()
	{
		if (_memberDropdown is null)
		{
			return;
		}

		_memberDropdown.Clear();

		if (_enumDefinition is null || _enumDefinition.Members.Count == 0)
		{
			_memberDropdown.AddItem(_enumDefinition is null ? "(No enum selected)" : "(Enum has no members)");
			_memberDropdown.Disabled = true;
			return;
		}

		_memberDropdown.Disabled = false;

		for (int i = 0; i < _enumDefinition.Members.Count; i++)
		{
			_memberDropdown.AddItem(StatescriptEnumUtilities.FormatMemberName(_enumDefinition, i));
		}

		// A value left dangling by a member being removed falls back to the first member rather than showing nothing.
		if (_value < 0 || _value >= _enumDefinition.Members.Count)
		{
			_value = 0;
		}

		_memberDropdown.Selected = _value;
	}
}
#endif
