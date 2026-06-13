// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Godot.Core.Statescript.VariableTypes;

/// <summary>
/// Built-in object variable type for <see cref="IForgeEntity"/> references.
/// </summary>
internal sealed class EntityObjectVariableType : StatescriptObjectVariableType<IForgeEntity>
{
	public override string TypeId => "Entity";

	public override string DisplayName => "Entity";

	public override string FormatDebugValue(object? value)
	{
		if (value is null)
		{
			return "<null>";
		}

		if (value is global::Godot.Node node)
		{
			return node.GetPath().ToString();
		}

		return value.GetType().FullName ?? value.GetType().Name;
	}
}
