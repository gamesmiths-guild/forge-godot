// Copyright © Gamesmiths Guild.

#if TOOLS
namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

/// <summary>
/// Shared editor rules for the interop nodes that pass typed arguments.
/// </summary>
/// <remarks>
/// Arguments are filled in order: the second type is offered only once the first is set, and each argument row appears
/// only once its type is. Without the first rule a call could be assembled with a hole in the middle of its argument
/// list; without the second, a row would be authored and then silently not passed.
/// </remarks>
internal abstract partial class InteropArgumentNodeEditorBase : StandardNodeEditorBase
{
	/// <summary>
	/// The CustomData key of the first argument's type.
	/// </summary>
	protected const string Argument1TypeKey = "arg1Type";

	/// <summary>
	/// The CustomData key of the second argument's type.
	/// </summary>
	protected const string Argument2TypeKey = "arg2Type";

	/// <summary>
	/// The stored name of the entry that says an argument is not passed.
	/// </summary>
	protected const string NoneName = "None";

	/// <summary>
	/// Gets the input property index of the first argument.
	/// </summary>
	protected abstract int Argument1InputIndex { get; }

	/// <inheritdoc/>
	protected override bool IsInputVisible(int inputIndex)
	{
		bool hasFirst = ReadStringConfig(Argument1TypeKey, NoneName) != NoneName;

		if (inputIndex == Argument1InputIndex)
		{
			return hasFirst;
		}

		// The second row needs the first as well, not only its own type. Turning the first argument off leaves the
		// second's stored type behind, and the runtime stops at the first gap - so a row shown on its own type alone
		// would be one the author filled in and nothing passed.
		if (inputIndex == Argument1InputIndex + 1)
		{
			return hasFirst && ReadStringConfig(Argument2TypeKey, NoneName) != NoneName;
		}

		return true;
	}

	/// <inheritdoc/>
	protected override bool IsSettingVisible(string key)
	{
		return key != Argument2TypeKey || ReadStringConfig(Argument1TypeKey, NoneName) != NoneName;
	}
}
#endif
