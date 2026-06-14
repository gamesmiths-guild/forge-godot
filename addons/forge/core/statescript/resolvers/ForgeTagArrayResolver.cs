// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Godot;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Lazily resolves a set of <see cref="Tag"/> values from their string keys, deferring tag lookup to resolve time so
/// graph building never touches the runtime tags manager before it is ready. Tags that are not registered are skipped
/// with a warning.
/// </summary>
/// <param name="tags">The tag keys to resolve.</param>
internal sealed class ForgeTagArrayResolver(IReadOnlyList<string> tags) : ObjectArrayResolver<Tag>
{
	private readonly IReadOnlyList<string> _tags = tags;

	public override Tag[] ResolveArray(GraphContext graphContext)
	{
		TagsManager tagsManager = ForgeManagers.Instance.TagsManager;
		var resolved = new List<Tag>(_tags.Count);

		for (int i = 0; i < _tags.Count; i++)
		{
			try
			{
				resolved.Add(Tag.RequestTag(tagsManager, _tags[i]));
			}
			catch (TagNotRegisteredException)
			{
				GD.PushWarning($"Tag [{_tags[i]}] is not registered.");
			}
		}

		return [.. resolved];
	}
}
