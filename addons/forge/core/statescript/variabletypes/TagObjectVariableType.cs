// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Godot.Core.Statescript.VariableTypes;

/// <summary>
/// Built-in object variable type for <see cref="Tag"/> values, used by tag-producing outputs such as the tag
/// listener node.
/// </summary>
internal sealed class TagObjectVariableType : StatescriptObjectVariableType<Tag>
{
	public override string TypeId => "Tag";

	public override string DisplayName => "Tag";

	public override string FormatDebugValue(object? value)
	{
		if (value is not Tag tag)
		{
			return "<null>";
		}

		return tag.IsValid ? tag.TagKey.ToString() : "Tag(invalid)";
	}
}
