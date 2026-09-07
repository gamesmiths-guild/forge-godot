// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Globalization;
using System.Text;
using Gamesmiths.Forge.Godot.Core.Statescript.Physics;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor that authors a collision layer or mask as the bit grid Godot's inspector uses, so a query that
/// searches the third and fourth layers is picked as two squares rather than typed as <c>12</c>.
/// </summary>
/// <remarks>
/// What it contributes to the graph is that bit field as a plain integer constant. The layers are named by the project
/// settings the slot's world owns, and the menu beside the grid lists the named ones so a mask can be read and set
/// without counting squares.
/// </remarks>
[Tool]
internal sealed partial class CollisionMaskResolverEditor : NodeEditorProperty
{
	private Action? _onChanged;
	private CollisionLayerSpace _layerSpace;
	private CollisionLayersGrid? _grid;
	private PopupMenu? _layerMenu;
	private TextureButton? _menuButton;

	/// <inheritdoc/>
	public override string DisplayName => "Collision Mask";

	/// <inheritdoc/>
	public override string ResolverTypeId => "CollisionMask";

	/// <inheritdoc/>
	public override bool IsCompatibleWith(Type expectedType)
	{
		// A layer field resolves to an int, so it fits int inputs and the wildcard slots that accept any authorable
		// value - which is what lets a mask be authored once into a variable and then bound to several queries.
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

		var resource = property?.Resolver as CollisionMaskResolverResource;

		// The slot wins over what was saved: a resolver moved onto a 3D query is picking 3D layers from then on,
		// whatever it was picking before. Only a slot with no world of its own - a graph variable - keeps the space it
		// was authored with.
		_layerSpace = MaskSlot != CollisionLayerSpace.None
			? MaskSlot
			: resource?.LayerSpace ?? CollisionLayerSpace.None;

		SizeFlagsHorizontal = SizeFlags.ExpandFill;

		var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, ClipContents = true };
		AddChild(row);

		_grid = new CollisionLayersGrid
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			Value = unchecked((uint)(resource?.Value ?? 0)),
			Expanded = resource?.Expanded ?? false,
		};

		_grid.FlagChanged += _ => NotifyChanged();
		_grid.ExpandedChanged += OnExpandedChanged;
		row.AddChild(_grid);

		_menuButton = new TextureButton
		{
			StretchMode = TextureButton.StretchModeEnum.KeepCentered,
			ToggleMode = true,
			TooltipText = "Pick layers by name.",
		};

		_menuButton.Pressed += OnMenuButtonPressed;
		row.AddChild(_menuButton);

		_layerMenu = new PopupMenu { HideOnCheckableItemSelection = false };
		_layerMenu.IdPressed += OnLayerMenuIdPressed;
		_layerMenu.PopupHide += OnLayerMenuHidden;
		AddChild(_layerMenu);

		RefreshLayerNames();
	}

	/// <inheritdoc/>
	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new CollisionMaskResolverResource
		{
			Value = unchecked((int)ReadValue()),
			LayerSpace = _layerSpace,
			Expanded = _grid is not null && IsInstanceValid(_grid) && _grid.Expanded,
		};
	}

	/// <inheritdoc/>
	public override bool TryGetInlineSummary(out string summary)
	{
		uint value = ReadValue();

		if (value == 0)
		{
			summary = "None";
			return true;
		}

		var builder = new StringBuilder();

		for (int i = 0; i < CollisionLayersGrid.LayerCount; i++)
		{
			if ((value & (1u << i)) == 0)
			{
				continue;
			}

			if (builder.Length > 0)
			{
				builder.Append(", ");
			}

			string name = _grid is not null && IsInstanceValid(_grid) ? _grid.GetLayerName(i) : string.Empty;
			builder.Append(name.Length > 0 ? name : (i + 1).ToString(CultureInfo.InvariantCulture));
		}

		summary = builder.ToString();
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
		_grid?.ClearCallbacks();
		_grid = null;
		_layerMenu = null;
		_menuButton = null;
	}

	/// <inheritdoc/>
	public override void _Notification(int what)
	{
		if (what == NotificationThemeChanged && _menuButton is not null && IsInstanceValid(_menuButton))
		{
			Texture2D icon = GetThemeIcon("GuiTabMenuHl", "EditorIcons");
			_menuButton.TextureNormal = icon;
			_menuButton.TexturePressed = icon;
		}
	}

	private static string GetLayerSettingBasename(CollisionLayerSpace layerSpace)
	{
		return layerSpace == CollisionLayerSpace.Physics3D ? "layer_names/3d_physics" : "layer_names/2d_physics";
	}

	private uint ReadValue()
	{
		return _grid is not null && IsInstanceValid(_grid) ? _grid.Value : 0;
	}

	private void RefreshLayerNames()
	{
		if (_grid is null || !IsInstanceValid(_grid))
		{
			return;
		}

		string[] names = new string[CollisionLayersGrid.LayerCount];
		string[] tooltips = new string[CollisionLayersGrid.LayerCount];

		// A slot with no world of its own has no name set to read, so the squares stay numbered - which is what the
		// engine shows for an unnamed layer anyway.
		string basename = _layerSpace == CollisionLayerSpace.None
			? string.Empty
			: GetLayerSettingBasename(_layerSpace);

		for (int i = 0; i < CollisionLayersGrid.LayerCount; i++)
		{
			string setting = $"{basename}/layer_{i + 1}";

			names[i] = basename.Length > 0 && ProjectSettings.HasSetting(setting)
				? ProjectSettings.GetSetting(setting).AsString()
				: string.Empty;

			string displayName = names[i].Length > 0
				? names[i]
				: $"Layer {(i + 1).ToString(CultureInfo.InvariantCulture)}";

			tooltips[i] = $"{displayName}\nBit {i.ToString(CultureInfo.InvariantCulture)}, value " +
				(1u << i).ToString(CultureInfo.InvariantCulture);
		}

		_grid.SetLayerNames(names, tooltips);
	}

	private void OnMenuButtonPressed()
	{
		if (_grid is null || !IsInstanceValid(_grid) || _layerMenu is null || !IsInstanceValid(_layerMenu)
			|| _menuButton is null || !IsInstanceValid(_menuButton))
		{
			return;
		}

		// Read afresh, so a layer named in Project Settings while this graph was open shows up on the next open.
		RefreshLayerNames();

		_layerMenu.Clear();

		for (int i = 0; i < CollisionLayersGrid.LayerCount; i++)
		{
			string name = _grid.GetLayerName(i);

			if (name.Length == 0)
			{
				continue;
			}

			_layerMenu.AddCheckItem(name, i);
			_layerMenu.SetItemChecked(_layerMenu.GetItemIndex(i), (_grid.Value & (1u << i)) != 0);
		}

		if (_layerMenu.ItemCount == 0)
		{
			_layerMenu.AddItem("No Named Layers");
			_layerMenu.SetItemDisabled(0, true);
		}

		Vector2 buttonPosition = _menuButton.GetScreenPosition();
		_layerMenu.ResetSize();
		_layerMenu.Position = (Vector2I)(buttonPosition - new Vector2(_layerMenu.GetContentsMinimumSize().X, 0));
		_layerMenu.Popup();
	}

	private void OnLayerMenuIdPressed(long id)
	{
		if (_grid is null || !IsInstanceValid(_grid) || _layerMenu is null || !IsInstanceValid(_layerMenu))
		{
			return;
		}

		_grid.Value ^= 1u << (int)id;
		_grid.QueueRedraw();
		_layerMenu.SetItemChecked(_layerMenu.GetItemIndex((int)id), (_grid.Value & (1u << (int)id)) != 0);
		NotifyChanged();
	}

	private void OnLayerMenuHidden()
	{
		if (_menuButton is not null && IsInstanceValid(_menuButton))
		{
			_menuButton.ButtonPressed = false;
		}
	}

	private void OnExpandedChanged()
	{
		// Whether the blocks that did not fit are unfolded is view state, so it is written back without an undo step.
		SaveViewState(_onChanged);
		RaiseLayoutSizeChanged();
	}

	private void NotifyChanged()
	{
		_onChanged?.Invoke();
	}
}
#endif
