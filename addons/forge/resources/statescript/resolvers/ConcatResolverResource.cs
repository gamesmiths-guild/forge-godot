// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that concatenates two nested array sources, producing the first source's elements followed by the
/// second's. Both sources must share the same element type.
/// </summary>
[Tool]
[GlobalClass]
public partial class ConcatResolverResource : ArrayTransformResolverResourceBase
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "Concat";

	/// <inheritdoc/>
	public override string PropertyNamePrefix => "concat";

	/// <summary>
	/// Gets or sets the nested resolver providing the trailing elements.
	/// </summary>
	[Export]
	public StatescriptResolverResource? Second { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the second-source section is folded in the editor.
	/// </summary>
	[Export]
	public bool SecondFolded { get; set; } = true;

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
			out IArrayPropertyResolver? firstValueArray,
			out IObjectArrayResolver? firstObjectArray))
		{
			return false;
		}

		if (!ArrayResolverResourceUtilities.TryResolveSource(
			Second,
			ResolverTypeId,
			graph,
			out IArrayPropertyResolver? secondValueArray,
			out IObjectArrayResolver? secondObjectArray))
		{
			valueArrayResolver = firstValueArray;
			objectArrayResolver = firstObjectArray;
			return true;
		}

		if (firstObjectArray is not null && secondObjectArray is not null)
		{
			if (firstObjectArray.ElementType != secondObjectArray.ElementType)
			{
				GD.PushError(
					"Statescript: Concat resolver requires matching element types. Got " +
					$"'{firstObjectArray.ElementType.Name}' and '{secondObjectArray.ElementType.Name}'. Using the " +
					"first source only.");
				objectArrayResolver = firstObjectArray;
				return true;
			}

			objectArrayResolver = ArrayResolverResourceUtilities.CreateObjectArrayOperation(
				typeof(ObjectConcatResolver<>),
				firstObjectArray,
				secondObjectArray);
			return true;
		}

		if (firstValueArray is not null && secondValueArray is not null)
		{
			if (firstValueArray.ElementType != secondValueArray.ElementType)
			{
				GD.PushError(
					"Statescript: Concat resolver requires matching element types. Got " +
					$"'{firstValueArray.ElementType.Name}' and '{secondValueArray.ElementType.Name}'. Using the " +
					"first source only.");
				valueArrayResolver = firstValueArray;
				return true;
			}

			valueArrayResolver = new ConcatResolver(firstValueArray, secondValueArray);
			return true;
		}

		GD.PushError(
			"Statescript: Concat resolver cannot mix value-typed and object-backed sources. Using the first source " +
			"only.");
		valueArrayResolver = firstValueArray;
		objectArrayResolver = firstObjectArray;
		return true;
	}
}
