// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that removes from a nested array source every element found in a second nested array source.
/// Object-backed elements are matched by reference identity.
/// </summary>
[Tool]
[GlobalClass]
public partial class ExceptResolverResource : ArrayTransformResolverResourceBase
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "Except";

	/// <inheritdoc/>
	public override string PropertyNamePrefix => "except";

	/// <summary>
	/// Gets or sets the nested resolver providing the elements to remove.
	/// </summary>
	[Export]
	public StatescriptResolverResource? Other { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the other-source section is folded in the editor.
	/// </summary>
	[Export]
	public bool OtherFolded { get; set; } = true;

	/// <inheritdoc/>
	public override bool TryBuildArrayResolver(
		Graph graph,
		out IArrayPropertyResolver? valueArrayResolver,
		out IObjectArrayResolver? objectArrayResolver)
	{
		valueArrayResolver = null;
		objectArrayResolver = null;

		if (!TryResolveSource(
			graph,
			out IArrayPropertyResolver? sourceValueArray,
			out IObjectArrayResolver? sourceObjectArray))
		{
			return false;
		}

		if (!ArrayResolverResourceUtilities.TryResolveSource(
			Other,
			ResolverTypeId,
			graph,
			out IArrayPropertyResolver? otherValueArray,
			out IObjectArrayResolver? otherObjectArray))
		{
			valueArrayResolver = sourceValueArray;
			objectArrayResolver = sourceObjectArray;
			return true;
		}

		if (sourceObjectArray is not null && otherObjectArray is not null)
		{
			if (sourceObjectArray.ElementType != otherObjectArray.ElementType)
			{
				GD.PushError(
					"Statescript: Except resolver requires matching element types. Got " +
					$"'{sourceObjectArray.ElementType.Name}' and '{otherObjectArray.ElementType.Name}'. Using the " +
					"source unchanged.");
				objectArrayResolver = sourceObjectArray;
				return true;
			}

			objectArrayResolver = ArrayResolverResourceUtilities.CreateObjectArrayOperation(
				typeof(ObjectExceptResolver<>),
				sourceObjectArray,
				otherObjectArray);
			return true;
		}

		if (sourceValueArray is not null && otherValueArray is not null)
		{
			if (sourceValueArray.ElementType != otherValueArray.ElementType)
			{
				GD.PushError(
					"Statescript: Except resolver requires matching element types. Got " +
					$"'{sourceValueArray.ElementType.Name}' and '{otherValueArray.ElementType.Name}'. Using the " +
					"source unchanged.");
				valueArrayResolver = sourceValueArray;
				return true;
			}

			valueArrayResolver = new ExceptResolver(sourceValueArray, otherValueArray);
			return true;
		}

		GD.PushError(
			"Statescript: Except resolver cannot mix value-typed and object-backed sources. Using the source " +
			"unchanged.");
		valueArrayResolver = sourceValueArray;
		objectArrayResolver = sourceObjectArray;
		return true;
	}
}
