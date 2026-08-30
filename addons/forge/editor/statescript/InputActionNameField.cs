// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using Godot;
using GodotDictionary = Godot.Collections.Dictionary;

namespace Gamesmiths.Forge.Godot.Editor.Statescript;

/// <summary>
/// A text field for an input action name with a dropdown offering the project's own actions.
/// </summary>
/// <remarks>
/// <para>Action names are free text because the action map belongs to the game and is reached through an engine-generic
/// mechanism, exactly as animation names are. Typing one from memory is still how most of them get misspelled, so the
/// project's actions are offered beside the field - a picker for the ones that exist, free text for the ones the
/// project does not define yet.</para>
/// <para>The field stays editable rather than becoming a plain dropdown because the project's action list is a
/// <em>subset</em> of what the runtime accepts: Godot's <c>ui_*</c> presets are valid and deliberately not listed, and
/// a game may register actions from code that no project setting knows about. A dropdown would also have to decide what
/// to show once an action is renamed away under a graph that still names it, and both answers are wrong - the node
/// settings row would display the first action while the node runs the stored one, and a resolver editor would rewrite
/// the stored name on the next save. A name that is kept and flagged beats a name silently replaced.</para>
/// <para>The same control serves node settings and resolver editors, so an action is authored the same way wherever it
/// appears. It wraps a field the caller built and bound, rather than owning one, because the two callers persist a
/// change differently: a node setting records an undo step against its <c>CustomData</c>, while a resolver writes
/// through its editor's save callback.</para>
/// </remarks>
[Tool]
internal sealed partial class InputActionNameField : HBoxContainer
{
	/// <summary>
	/// The project setting every input action is stored under.
	/// </summary>
	private const string ActionSettingPrefix = "input/";

	/// <summary>
	/// The prefix Godot's own preset actions carry. They are left out of the dropdown: every project has all hundred
	/// of them, none is what a gameplay graph is watching, and listing them buries the ones that are. They are still
	/// accepted when typed, which is one of the reasons the field stays free text.
	/// </summary>
	private const string PresetActionPrefix = "ui_";

	private const float DropdownWidth = 24.0f;

	/// <summary>
	/// Names the project defines, cached so validating a keystroke does not walk every project setting.
	/// </summary>
	/// <remarks>
	/// Refreshed when the row is built and whenever the dropdown opens, which are the moments an author can have
	/// changed the action map since it was last read.
	/// </remarks>
	private readonly HashSet<string> _knownActions = new(StringComparer.Ordinal);

	private LineEdit? _field;
	private PopupMenu? _popup;
	private Action? _onPicked;
	private string _baseTooltip = string.Empty;

	/// <summary>
	/// Builds the row around an already-configured text field.
	/// </summary>
	/// <param name="field">The field holding the action name. Reparented into this control.</param>
	/// <param name="onPicked">Called after the dropdown writes a name into the field. Setting the text in code raises
	/// no change signal of its own, so this is what tells the caller to persist it.</param>
	public void Initialize(LineEdit field, Action onPicked)
	{
		_field = field;
		_onPicked = onPicked;
		_baseTooltip = field.TooltipText;

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		AddThemeConstantOverride("separation", 2);
		AddChild(field);

		field.TextChanged += _ => UpdateValidationCue();

		var dropdown = new MenuButton
		{
			Text = "▾",
			CustomMinimumSize = new Vector2(DropdownWidth, 0),
			TooltipText = "Pick one of the project's input actions.",
		};

		_popup = dropdown.GetPopup();
		dropdown.AboutToPopup += PopulateActions;
		_popup.IndexPressed += OnActionPicked;

		AddChild(dropdown);

		RefreshKnownActions();
		UpdateValidationCue();
	}

	/// <inheritdoc cref="NodeEditorProperty.ClearCallbacks"/>
	public void ClearCallbacks()
	{
		_field = null;
		_popup = null;
		_onPicked = null;
	}

	private static IEnumerable<string> EnumerateActionNames()
	{
		foreach (GodotDictionary property in ProjectSettings.Singleton.GetPropertyList())
		{
			string settingName = property["name"].AsString();

			if (!settingName.StartsWith(ActionSettingPrefix, StringComparison.Ordinal))
			{
				continue;
			}

			string actionName = settingName[ActionSettingPrefix.Length..];

			// A feature override is stored as a second setting under the same action - "input/skill_1.macos" - and is
			// not a name anything can be watched by. The action's own setting is always there beside it.
			if (!actionName.Contains('.'))
			{
				yield return actionName;
			}
		}
	}

	// Read on every popup rather than once, so an action added to the project while the graph is open is offered
	// without reopening it.
	private void PopulateActions()
	{
		if (_popup is null || !IsInstanceValid(_popup))
		{
			return;
		}

		_popup.Clear();
		RefreshKnownActions();

		foreach (string actionName in EnumerateActionNames())
		{
			if (!actionName.StartsWith(PresetActionPrefix, StringComparison.Ordinal))
			{
				_popup.AddItem(actionName);
			}
		}

		if (_popup.ItemCount == 0)
		{
			_popup.AddItem("(no actions in the project's Input Map)");
			_popup.SetItemDisabled(0, true);
		}

		// Applied here rather than at construction because the popup has to be in the tree to know which screen to cap
		// its height against, and a project with a lot of actions is exactly the case that needs both.
		SearchablePopup.Configure(_popup, GetWindow());

		// Opening the dropdown is also when a name typed before its action existed stops being unknown.
		UpdateValidationCue();
	}

	private void RefreshKnownActions()
	{
		_knownActions.Clear();

		foreach (string actionName in EnumerateActionNames())
		{
			_knownActions.Add(actionName);
		}
	}

	// Marks a name the project does not define without touching it. The value is still what runs, because it may
	// legitimately be a preset or an action the game registers from code - this says what is known, not what is wrong.
	private void UpdateValidationCue()
	{
		if (_field is null || !IsInstanceValid(_field))
		{
			return;
		}

		string actionName = _field.Text;

		// An empty field is a row nobody has filled in yet rather than a mistake, and its placeholder already says so.
		if (actionName.Length == 0 || _knownActions.Contains(actionName))
		{
			_field.RemoveThemeColorOverride("font_color");
			_field.TooltipText = _baseTooltip;
			return;
		}

		Theme editorTheme = EditorInterface.Singleton.GetEditorTheme();

		if (editorTheme.HasColor("warning_color", "Editor"))
		{
			_field.AddThemeColorOverride("font_color", editorTheme.GetColor("warning_color", "Editor"));
		}

		string warning = $"\"{actionName}\" is not an action this project defines.\n\n" +
			"It still works if the game registers it at runtime, or if it is one of Godot's built-in ui_ actions;\n" +
			"otherwise nothing is watched and the graph warns once when it runs.";

		_field.TooltipText = _baseTooltip.Length > 0 ? $"{_baseTooltip}\n\n{warning}" : warning;
	}

	private void OnActionPicked(long index)
	{
		if (_field is null || !IsInstanceValid(_field) || _popup is null || !IsInstanceValid(_popup))
		{
			return;
		}

		_field.Text = _popup.GetItemText((int)index);
		UpdateValidationCue();
		_onPicked?.Invoke();
	}
}
#endif
