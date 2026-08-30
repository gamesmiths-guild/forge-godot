// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript;

/// <summary>
/// Signal handler helpers used by <see cref="StatescriptEditorControls"/> to keep C# delegates off Godot signals.
/// Each handler is a <see cref="Node"/> so it can be parented to its owning control and freed automatically.
/// </summary>
/// <remarks>
/// <para><b>These are connected by method name, never with <c>+=</c>.</b> A C# event connection is a
/// <c>Delegate::Invoke</c> callable holding a GC handle, and an assembly reload has to take every one of them down and
/// put it back. Both ends here are script instances in the same reloading assembly, and the handler - a child of the
/// control that emits to it - goes first: Godot core drops connections targeting a freed object, so the emitter's own
/// cleanup then disconnects something that is already gone.</para>
/// <para>A <see cref="Callable"/> over a Godot object and a method name carries no GC handle, so there is nothing to
/// serialize, disconnect or restore and the reload passes over it. The handlers stay Godot objects because that form
/// requires one, and their parameterless constructors stay because Godot instantiates script classes through them.
/// </para>
/// </remarks>
internal static partial class StatescriptEditorControls
{
	/// <summary>
	/// Handles <see cref="BaseButton.Toggled"/> for boolean editors, forwarding to an <see cref="Action{T}"/>.
	/// </summary>
	[Tool]
	internal sealed partial class BoolSignalHandler : Node
	{
		public Action<bool>? OnChanged { get; set; }

		public void HandleToggled(bool pressed)
		{
			OnChanged?.Invoke(pressed);
		}
	}

	/// <summary>
	/// Handles <see cref="OptionButton.ItemSelected"/> for the variable type dropdown.
	/// </summary>
	[Tool]
	internal sealed partial class VariableTypeDropdownHandler : Node
	{
		private readonly OptionButton _dropdown;

		public Action<VariableTypeSelection>? OnChanged { get; set; }

		public VariableTypeDropdownHandler()
		{
			_dropdown = null!;
		}

		public VariableTypeDropdownHandler(OptionButton dropdown)
		{
			_dropdown = dropdown;
		}

		public void HandleItemSelected(long index)
		{
			OnChanged?.Invoke(GetVariableTypeSelection(_dropdown, (int)index));
		}
	}

	/// <summary>
	/// Handles <see cref="OptionButton.ItemSelected"/> for the value shape dropdown, which also refreshes the tooltip
	/// describing the shape it now shows.
	/// </summary>
	[Tool]
	internal sealed partial class ValueShapeDropdownHandler : Node
	{
		private readonly OptionButton _dropdown;

		public Action<bool>? OnChanged { get; set; }

		public ValueShapeDropdownHandler()
		{
			_dropdown = null!;
		}

		public ValueShapeDropdownHandler(OptionButton dropdown)
		{
			_dropdown = dropdown;
		}

		public void HandleItemSelected(long index)
		{
			_dropdown.Text = string.Empty;
			UpdateValueShapeDropdownTooltip(_dropdown);
			OnChanged?.Invoke(_dropdown.GetItemId((int)index) == 1);
		}
	}

	/// <summary>
	/// Handles <see cref="EditorSpinSlider"/> signals (<c>ValueChanged</c>, <c>Grabbed</c>, <c>Ungrabbed</c>,
	/// <c>FocusExited</c>) for numeric editors with drag-commit semantics.
	/// </summary>
	[Tool]
	internal sealed partial class NumericSpinHandler : Node
	{
		private readonly EditorSpinSlider _spin;
		private bool _isDragging;

		public Action<double>? OnChanged { get; set; }

		public NumericSpinHandler()
		{
			_spin = null!;
		}

		public NumericSpinHandler(EditorSpinSlider spin)
		{
			_spin = spin;
		}

		public void HandleValueChanged(double value)
		{
			if (!_isDragging)
			{
				OnChanged?.Invoke(value);
			}
		}

		public void HandleGrabbed()
		{
			_isDragging = true;
		}

		public void HandleUngrabbed()
		{
			_isDragging = false;
			OnChanged?.Invoke(_spin.Value);
		}

		public void HandleFocusExited()
		{
			_isDragging = false;
		}
	}

	/// <summary>
	/// Holds the shared state (values array, drag flag, callback) for a multi-component vector editor.
	/// </summary>
	[Tool]
	internal sealed partial class VectorComponentHandler : Node
	{
		private readonly double[] _values;

		public Action<double[]>? OnChanged { get; set; }

		public bool IsDragging { get; set; }

		public VectorComponentHandler()
		{
			_values = [];
		}

		public VectorComponentHandler(double[] values)
		{
			_values = values;
		}

		public void SetValue(int index, double value)
		{
			_values[index] = value;
		}

		public void RaiseChanged()
		{
			OnChanged?.Invoke(_values);
		}
	}

	/// <summary>
	/// Handles <see cref="EditorSpinSlider"/> signals for a single component of a vector editor.
	/// Forwards to the shared <see cref="VectorComponentHandler"/>.
	/// </summary>
	[Tool]
	internal sealed partial class VectorSpinHandler : Node
	{
		private readonly VectorComponentHandler _parent;
		private readonly int _componentIndex;

		public VectorSpinHandler()
		{
			_parent = null!;
		}

		public VectorSpinHandler(VectorComponentHandler parent, int componentIndex)
		{
			_parent = parent;
			_componentIndex = componentIndex;
		}

		public void HandleValueChanged(double value)
		{
			_parent.SetValue(_componentIndex, value);

			if (!_parent.IsDragging)
			{
				_parent.RaiseChanged();
			}
		}

		public void HandleGrabbed()
		{
			_parent.IsDragging = true;
		}

		public void HandleUngrabbed()
		{
			_parent.IsDragging = false;
			_parent.RaiseChanged();
		}

		public void HandleFocusExited()
		{
			_parent.IsDragging = false;
		}
	}
}
#endif
