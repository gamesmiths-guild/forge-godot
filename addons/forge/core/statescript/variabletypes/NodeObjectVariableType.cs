// Copyright © Gamesmiths Guild.

using GodotNode = Godot.Node;

namespace Gamesmiths.Forge.Godot.Core.Statescript.VariableTypes;

/// <summary>
/// Object variable type for references to Godot scene nodes.
/// </summary>
/// <remarks>
/// This is what lets a graph hold on to something it instantiated, or to a node it looked up, and pass it to a later
/// node. The reference is not ownership: a node freed elsewhere leaves the variable pointing at a dead instance, so
/// anything consuming it should gate on the <c>Is Instance Valid</c> resolver rather than a null check.
/// </remarks>
internal sealed class NodeObjectVariableType : StatescriptObjectVariableType<GodotNode>
{
	public override string TypeId => "GodotNode";

	public override string DisplayName => "Node";

	public override string FormatDebugValue(object? value)
	{
		if (value is not GodotNode node)
		{
			return "<null>";
		}

		return global::Godot.GodotObject.IsInstanceValid(node) ? node.GetPath().ToString() : "Node(freed)";
	}
}
