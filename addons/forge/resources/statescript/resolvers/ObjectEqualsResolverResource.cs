// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that checks whether two nested object-backed resolvers produce the same instance (reference
/// identity). Works with any registered object variable type: entities, effects, active effect handles, and
/// game-registered types.
/// </summary>
[Tool]
[GlobalClass]
public partial class ObjectEqualsResolverResource : StatescriptResolverResource
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "ObjectEquals";

	/// <summary>
	/// Gets or sets the registered object variable type id the operands are authored against. This is editor metadata
	/// used to restore the type dropdown; the runtime comparison works on whatever the operands produce.
	/// </summary>
	[Export]
	public string ObjectTypeId { get; set; } = "Entity";

	/// <summary>
	/// Gets or sets the nested resolver providing the left operand of the comparison.
	/// </summary>
	[Export]
	public StatescriptResolverResource? Left { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the left operand section is folded in the editor.
	/// </summary>
	[Export]
	public bool LeftFolded { get; set; } = true;

	/// <summary>
	/// Gets or sets the nested resolver providing the right operand of the comparison.
	/// </summary>
	[Export]
	public StatescriptResolverResource? Right { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the right operand section is folded in the editor.
	/// </summary>
	[Export]
	public bool RightFolded { get; set; } = true;

	/// <inheritdoc/>
	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		var propertyName = new StringKey($"__objectequals_{nodeId}_{index}");
		graph.VariableDefinitions.DefineProperty(propertyName, BuildResolver(graph));
		runtimeNode.BindInput(index, propertyName);
	}

	/// <inheritdoc/>
	public override IPropertyResolver BuildResolver(Graph graph)
	{
		if (!TryBuildOperand(graph, Left, "left", out IObjectResolver? leftResolver)
			|| !TryBuildOperand(graph, Right, "right", out IObjectResolver? rightResolver))
		{
			return new VariantResolver(new Variant128(false), typeof(bool));
		}

		return new ObjectEqualsResolver(leftResolver!, rightResolver!);
	}

	private static bool TryBuildOperand(
		Graph graph,
		StatescriptResolverResource? operand,
		string operandName,
		out IObjectResolver? objectResolver)
	{
		objectResolver = null;

		if (operand is null)
		{
			GD.PushError($"Statescript: Object Equals resolver is missing its {operandName} operand; resolving to " +
				"false.");
			return false;
		}

		if (!operand.TryBuildObjectResolver(graph, out objectResolver))
		{
			GD.PushError(
				$"Statescript: Object Equals resolver {operandName} operand '{operand.ResolverTypeId}' does not " +
				"produce an object-backed value; resolving to false.");
			return false;
		}

		return true;
	}
}
