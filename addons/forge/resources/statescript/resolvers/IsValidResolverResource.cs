// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that checks whether a nested object-backed resolver produces a valid (non-null) value. Works
/// with any registered object variable type: entities, effects, active effect handles, and game-registered types.
/// </summary>
[Tool]
[GlobalClass]
public partial class IsValidResolverResource : StatescriptResolverResource
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "IsValid";

	/// <summary>
	/// Gets or sets the registered object variable type id the source is authored against. This is editor metadata
	/// used to restore the type dropdown; the runtime check works on whatever the source produces.
	/// </summary>
	[Export]
	public string ObjectTypeId { get; set; } = "Entity";

	/// <summary>
	/// Gets or sets the nested resolver whose result is checked.
	/// </summary>
	[Export]
	public StatescriptResolverResource? Source { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the source section is folded in the editor.
	/// </summary>
	[Export]
	public bool SourceFolded { get; set; } = true;

	/// <inheritdoc/>
	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		var propertyName = new StringKey($"__isvalid_{nodeId}_{index}");
		graph.VariableDefinitions.DefineProperty(propertyName, BuildResolver(graph));
		runtimeNode.BindInput(index, propertyName);
	}

	/// <inheritdoc/>
	public override IPropertyResolver BuildResolver(Graph graph)
	{
		if (Source is null)
		{
			GD.PushError("Statescript: Is Valid resolver is missing its source; resolving to false.");
			return new VariantResolver(new Variant128(false), typeof(bool));
		}

		if (!Source.TryBuildObjectResolver(graph, out IObjectResolver? sourceResolver))
		{
			GD.PushError(
				$"Statescript: Is Valid resolver source '{Source.ResolverTypeId}' does not produce an object-backed " +
				"value; resolving to false.");
			return new VariantResolver(new Variant128(false), typeof(bool));
		}

		return new IsValidResolver(sourceResolver!);
	}
}
