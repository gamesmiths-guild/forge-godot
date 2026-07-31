// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Components;
using Gamesmiths.Forge.Tags;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources;

/// <summary>
/// Selects effects by identity, by the tags they grant, by their source, or by what they modify. Every field is
/// optional and all defined fields are combined with AND; a query with nothing set matches every effect.
/// </summary>
/// <remarks>
/// The runtime <see cref="EffectQuery"/> also supports matching a source entity instance and an arbitrary predicate.
/// Neither can be authored as a resource, so both are code-only.
/// </remarks>
[Tool]
[GlobalClass]
public partial class ForgeEffectQuery : Resource
{
	/// <summary>
	/// Gets or sets the exact effect data the effect must have been built from.
	/// </summary>
	[Export]
	public ForgeEffectData? EffectDefinition { get; set; }

	/// <summary>
	/// Gets or sets a query matched against the effect's own identity tags.
	/// </summary>
	[ExportGroup("Tag Queries")]
	[Export]
	public ForgeQueryExpression? EffectTagQuery { get; set; }

	/// <summary>
	/// Gets or sets a query matched against the tags the effect grants to its target.
	/// </summary>
	[Export]
	public ForgeQueryExpression? GrantedTagQuery { get; set; }

	/// <summary>
	/// Gets or sets a query matched against the effect's own tags together with the tags it grants.
	/// </summary>
	[Export]
	public ForgeQueryExpression? OwningTagQuery { get; set; }

	/// <summary>
	/// Gets or sets the tags the entity that applied the effect must have.
	/// </summary>
	[ExportGroup("Source Requirements")]
	[Export]
	public ForgeTagContainer? SourceRequiredTags { get; set; }

	/// <summary>
	/// Gets or sets the tags the entity that applied the effect must not have.
	/// </summary>
	[Export]
	public ForgeTagContainer? SourceIgnoredTags { get; set; }

	/// <summary>
	/// Gets or sets a query matched against the tags of the entity that applied the effect.
	/// </summary>
	[Export]
	public ForgeQueryExpression? SourceTagQuery { get; set; }

	/// <summary>
	/// Gets or sets an attribute that at least one of the effect's modifiers must target. Leave empty to ignore.
	/// </summary>
	[ExportGroup("Modifiers")]
	[Export]
	public string ModifyingAttribute { get; set; } = string.Empty;

	/// <summary>
	/// Builds the runtime query described by this resource.
	/// </summary>
	/// <returns>The configured <see cref="EffectQuery"/>.</returns>
	public EffectQuery GetEffectQuery()
	{
		return new EffectQuery(
			EffectDefinition?.GetEffectData(),
			BuildTagQuery(EffectTagQuery),
			BuildTagQuery(GrantedTagQuery),
			BuildTagQuery(OwningTagQuery),
			BuildSourceRequirements(),
			BuildModifyingAttribute());
	}

	private static TagQuery? BuildTagQuery(ForgeQueryExpression? queryExpression)
	{
		if (queryExpression is null)
		{
			return null;
		}

		var query = new TagQuery();
		query.Build(queryExpression.GetQueryExpression());
		return query;
	}

	private TagRequirements? BuildSourceRequirements()
	{
		if (SourceRequiredTags is null && SourceIgnoredTags is null && SourceTagQuery is null)
		{
			return null;
		}

		return new TagRequirements(
			SourceRequiredTags?.GetTagContainer(),
			SourceIgnoredTags?.GetTagContainer(),
			BuildTagQuery(SourceTagQuery));
	}

	private StringKey? BuildModifyingAttribute()
	{
		if (string.IsNullOrWhiteSpace(ModifyingAttribute))
		{
			return null;
		}

		return new StringKey(ModifyingAttribute);
	}
}
