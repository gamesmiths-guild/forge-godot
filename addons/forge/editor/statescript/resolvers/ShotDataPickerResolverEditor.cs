// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Resources;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor that authors a constant shot resource: a projectile, a blast or a hitscan.
/// </summary>
[Tool]
internal sealed partial class ShotDataPickerResolverEditor : NodeEditorProperty
{
	private Action? _onChanged;
	private Resource? _selected;

	public override string DisplayName => "Constant";

	public override string ResolverTypeId => "ShotData";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(ForgeProjectileData)
			|| expectedType == typeof(ForgeSplashData)
			|| expectedType == typeof(ForgeHitscanData);
	}

	public override void Setup(
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged,
		bool isArray)
	{
		_onChanged = onChanged;
		_selected = (property?.Resolver as ShotDataPickerResolverResource)?.Data;
		SizeFlagsHorizontal = SizeFlags.ExpandFill;

		var picker = new EditorResourcePicker
		{
			BaseType = expectedType.Name,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			EditedResource = _selected,
		};

		picker.ResourceChanged += resource =>
		{
			_selected = resource;
			_onChanged?.Invoke();
		};

		AddChild(picker);
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new ShotDataPickerResolverResource { Data = _selected };
	}

	public override bool TryGetInlineSummary(out string summary)
	{
		if (_selected is null)
		{
			summary = "(None)";
			return true;
		}

		summary = string.IsNullOrEmpty(_selected.ResourcePath)
			? _selected.GetType().Name
			: System.IO.Path.GetFileNameWithoutExtension(_selected.ResourcePath);

		return true;
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_onChanged = null;
	}
}
#endif
