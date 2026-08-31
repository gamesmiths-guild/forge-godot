// Copyright © Gamesmiths Guild.

using Godot;
using Node = Godot.Node;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Interop;

/// <summary>
/// Reads and writes the on/off switches a node carries, for the two nodes that set one.
/// </summary>
/// <remarks>
/// Reading matters as much as writing: an override has to put back what it found rather than what it assumes, and only
/// one of these switches has an obvious "off" to fall back to.
/// </remarks>
internal static class NodeEnabledAspects
{
	/// <summary>
	/// Reads a node's current setting for an aspect.
	/// </summary>
	/// <param name="node">The node to read.</param>
	/// <param name="aspect">The switch to read.</param>
	/// <param name="enabled">The current setting, when the node has this switch.</param>
	/// <returns><see langword="true"/> when the node has this switch.</returns>
	public static bool TryRead(Node node, NodeEnabledAspect aspect, out bool enabled)
	{
		switch (aspect)
		{
			case NodeEnabledAspect.Processing:
				enabled = node.IsProcessing();
				return true;

			case NodeEnabledAspect.PhysicsProcessing:
				enabled = node.IsPhysicsProcessing();
				return true;

			case NodeEnabledAspect.Visible:
				enabled = node switch
				{
					Node3D node3D => node3D.Visible,
					CanvasItem canvasItem => canvasItem.Visible,
					_ => false,
				};

				return node is Node3D or CanvasItem;

			case NodeEnabledAspect.Monitoring:
				enabled = node switch
				{
					Area3D area3D => area3D.Monitoring,
					Area2D area2D => area2D.Monitoring,
					_ => false,
				};

				return node is Area3D or Area2D;

			case NodeEnabledAspect.Monitorable:
				enabled = node switch
				{
					Area3D area3D => area3D.Monitorable,
					Area2D area2D => area2D.Monitorable,
					_ => false,
				};

				return node is Area3D or Area2D;

			default:
				enabled = false;
				return false;
		}
	}

	/// <summary>
	/// Writes a node's setting for an aspect.
	/// </summary>
	/// <param name="node">The node to write.</param>
	/// <param name="aspect">The switch to write.</param>
	/// <param name="enabled">The setting to write.</param>
	/// <returns><see langword="true"/> when the node has this switch.</returns>
	public static bool TryWrite(Node node, NodeEnabledAspect aspect, bool enabled)
	{
		switch (aspect)
		{
			case NodeEnabledAspect.Processing:
				node.SetProcess(enabled);
				return true;

			case NodeEnabledAspect.PhysicsProcessing:
				node.SetPhysicsProcess(enabled);
				return true;

			case NodeEnabledAspect.Visible:
				switch (node)
				{
					case Node3D node3D:
						node3D.Visible = enabled;
						return true;

					case CanvasItem canvasItem:
						canvasItem.Visible = enabled;
						return true;

					default:
						return false;
				}

			case NodeEnabledAspect.Monitoring:
				switch (node)
				{
					case Area3D area3D:
						area3D.Monitoring = enabled;
						return true;

					case Area2D area2D:
						area2D.Monitoring = enabled;
						return true;

					default:
						return false;
				}

			case NodeEnabledAspect.Monitorable:
				switch (node)
				{
					case Area3D area3D:
						area3D.Monitorable = enabled;
						return true;

					case Area2D area2D:
						area2D.Monitorable = enabled;
						return true;

					default:
						return false;
				}

			default:
				return false;
		}
	}

	/// <summary>
	/// Describes why a node cannot take an aspect, and what to reach for instead.
	/// </summary>
	/// <remarks>
	/// Monitoring is the one worth spelling out. It looks like it should apply to any body, because a body does have
	/// collision layer and mask bits — but those say which layers it occupies and scans, while monitoring is an area's
	/// switch for whether it tracks what is inside it, and Godot puts it on <c>Area2D</c> and <c>Area3D</c> alone.
	/// </remarks>
	/// <param name="node">The node that could not take the write.</param>
	/// <param name="aspect">The switch that was being set.</param>
	/// <returns>The message, completing "Statescript: {node type} ".</returns>
	public static string DescribeUnsupported(Node node, NodeEnabledAspect aspect)
	{
		string subject = $"cannot set {aspect} on [{node.GetPath()}], a {node.GetType().Name}";

		return aspect switch
		{
			NodeEnabledAspect.Visible => $"{subject}, which is neither a Node3D nor a CanvasItem.",
			NodeEnabledAspect.Monitoring or NodeEnabledAspect.Monitorable =>
				$"{subject}: only an Area2D or Area3D has it. A body's collision layer and mask are a different "
					+ "thing, set with Set Collision Bits 3D or Collision Override 3D.",
			_ => $"{subject}.",
		};
	}
}
