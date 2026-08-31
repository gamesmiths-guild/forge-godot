// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Core.Statescript.Interop;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using Node = Godot.Node;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Reads a property off a resolved scene node, shared by the four shapes the Node Property resolver comes in.
/// </summary>
/// <remarks>
/// The value lane and the object lane, scalar and array, differ only in how the Godot variant is converted afterwards,
/// so the node resolution, the path lookup and the one-shot reporting live here once rather than four times.
/// </remarks>
/// <param name="nodeResolver">Resolves the node to read from.</param>
/// <param name="propertyPath">The property to read, as a path from that node.</param>
// The parsed path is built once and lives as long as the graph does. A property resolver has no teardown to dispose it
// from, and rebuilding one per read would allocate a native path every tick, which is what this exists to avoid.
#pragma warning disable CA1001 // Types that own disposable fields should be disposable
internal sealed class NodePropertyReader(IObjectResolver<Node> nodeResolver, string propertyPath)
#pragma warning restore CA1001 // Types that own disposable fields should be disposable
{
	private readonly IObjectResolver<Node> _nodeResolver = nodeResolver;
	private readonly string _propertyPathText = propertyPath ?? string.Empty;
	private readonly NodePath _propertyPath = new(propertyPath ?? string.Empty);

	private bool _reported;

	/// <summary>
	/// Reads the property.
	/// </summary>
	/// <param name="graphContext">The graph execution context.</param>
	/// <param name="value">The raw Godot value, when one was read.</param>
	/// <returns><see langword="true"/> when the property was read.</returns>
	public bool TryRead(GraphContext graphContext, out Variant value)
	{
		value = default;

		if (_propertyPathText.Length == 0)
		{
			ReportOnce("has no property path, so there is nothing for it to read.");
			return false;
		}

		Node? node = _nodeResolver.Resolve(graphContext);

		if (node is null || !GodotObject.IsInstanceValid(node))
		{
			ReportOnce($"resolved no live node to read [{_propertyPathText}] from.");
			return false;
		}

		value = node.GetIndexed(_propertyPath);

		if (value.VariantType != Variant.Type.Nil)
		{
			return true;
		}

		// Nothing came back, which is either an object-typed property that is genuinely unset or a path the node does
		// not declare at all. Only the second is a mistake, and telling them apart costs a scan of the property list -
		// so it runs here, on the answer that is already unusual, rather than on every read.
		if (!NodePropertyAccess.DeclaresProperty(node, _propertyPath))
		{
			ReportOnce($"found no property [{_propertyPathText}] on [{node.GetPath()}].");
			return false;
		}

		return true;
	}

	// Resolvers run every tick, so a warning left unsuppressed would repeat every frame.
	private void ReportOnce(string message)
	{
		if (_reported)
		{
			return;
		}

		_reported = true;

		GD.PushWarning($"Statescript: NodeProperty {message} Resolving to a default value.");
	}
}
