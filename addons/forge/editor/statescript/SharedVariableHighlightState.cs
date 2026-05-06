// Copyright © Gamesmiths Guild.

#if TOOLS
using System;

namespace Gamesmiths.Forge.Godot.Editor.Statescript;

internal static class SharedVariableHighlightState
{
	private static string? _selectedSetPath;
	private static string? _selectedVariableName;
	private static string? _activeInspectorSetPath;

	public static event Action? Changed;

	public static void SetInspectorContext(string? setPath)
	{
		string normalized = string.IsNullOrWhiteSpace(setPath) ? string.Empty : setPath;
		if (string.Equals(_activeInspectorSetPath, normalized, StringComparison.Ordinal))
		{
			return;
		}

		_activeInspectorSetPath = normalized;
		Changed?.Invoke();
	}

	public static void ClearInspectorContext(string? setPath = null)
	{
		if (setPath is not null
			&& !string.Equals(_activeInspectorSetPath, setPath, StringComparison.Ordinal))
		{
			return;
		}

		if (string.IsNullOrEmpty(_activeInspectorSetPath))
		{
			return;
		}

		_activeInspectorSetPath = string.Empty;
		Changed?.Invoke();
	}

	public static void SetSelection(string? setPath, string? variableName)
	{
		string normalizedSetPath = string.IsNullOrWhiteSpace(setPath) ? string.Empty : setPath;
		string normalizedVariableName = string.IsNullOrWhiteSpace(variableName) ? string.Empty : variableName;

		if (string.IsNullOrEmpty(normalizedSetPath) || string.IsNullOrEmpty(normalizedVariableName))
		{
			normalizedSetPath = string.Empty;
			normalizedVariableName = string.Empty;
		}

		if (string.Equals(_selectedSetPath, normalizedSetPath, StringComparison.Ordinal)
			&& string.Equals(_selectedVariableName, normalizedVariableName, StringComparison.Ordinal))
		{
			return;
		}

		_selectedSetPath = normalizedSetPath;
		_selectedVariableName = normalizedVariableName;
		Changed?.Invoke();
	}

	public static bool TryGetActiveSelection(out string setPath, out string variableName)
	{
		if (string.IsNullOrEmpty(_selectedSetPath)
			|| string.IsNullOrEmpty(_selectedVariableName)
			|| !string.Equals(_activeInspectorSetPath, _selectedSetPath, StringComparison.Ordinal))
		{
			setPath = string.Empty;
			variableName = string.Empty;
			return false;
		}

		setPath = _selectedSetPath;
		variableName = _selectedVariableName;
		return true;
	}

	public static bool HasAnySelection()
	{
		return !string.IsNullOrEmpty(_selectedSetPath) && !string.IsNullOrEmpty(_selectedVariableName);
	}

	public static bool IsSelected(string? setPath, string? variableName)
	{
		return TryGetActiveSelection(out string activeSetPath, out string activeVariableName)
			&& string.Equals(activeSetPath, setPath, StringComparison.Ordinal)
			&& string.Equals(activeVariableName, variableName, StringComparison.Ordinal);
	}
}
#endif
