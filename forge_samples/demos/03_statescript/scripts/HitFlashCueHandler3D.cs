// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using System.Linq;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Nodes;
using Godot;

namespace Gamesmiths.Forge.Example;

/// <summary>
/// Cue handler that flashes every mesh of the target with one material for a moment, which is how a hit reads on a
/// character made of primitives with no animation to flinch with.
/// </summary>
/// <remarks>
/// The flash is written as a material override and the previous override put back afterwards, so the meshes' own
/// materials are never touched. A hit landing mid-flash restarts the timer without capturing the flash as the
/// material to put back.
/// </remarks>
[GlobalClass]
public partial class HitFlashCueHandler3D : ForgeCueHandler
{
	// Shared by every handler, because two of them can flash one mesh at once - a burning projectile lands its hit and
	// the fire's first tick in the same instant - and each would otherwise record the other's flash as the material to
	// put back. The first flash to reach a mesh records it, the last one to end restores it.
	private static readonly Dictionary<MeshInstance3D, Hold> _holds = [];

	private readonly Dictionary<Node3D, Flash> _flashes = [];

	[Export]
	public Material? FlashMaterial { get; set; }

	[Export]
	public float Duration { get; set; } = 0.08f;

	public override void _ExitTree()
	{
		foreach (Node3D node in _flashes.Keys.ToArray())
		{
			Restore(node);
		}

		base._ExitTree();
	}

	public override void _CueOnExecute(IForgeEntity forgeEntity, CueParameters? parameters)
	{
		if (FlashMaterial is null || !ForgeEntityBridge.TryGetSpatialNode3D(forgeEntity, out Node3D? node))
		{
			return;
		}

		if (_flashes.TryGetValue(node, out Flash? flash))
		{
			flash.Timer.Kill();
		}
		else
		{
			flash = new Flash();
			CollectMeshes(node, flash.Meshes);
			_flashes[node] = flash;

			foreach (MeshInstance3D mesh in flash.Meshes)
			{
				if (_holds.TryGetValue(mesh, out Hold? hold))
				{
					hold.Holders++;
				}
				else
				{
					_holds[mesh] = new Hold(mesh.MaterialOverride);
				}
			}
		}

		foreach (MeshInstance3D mesh in flash.Meshes)
		{
			mesh.MaterialOverride = FlashMaterial;
		}

		flash.Timer = CreateTween();
		flash.Timer.TweenCallback(Callable.From(() => Restore(node))).SetDelay(Duration);
	}

	private static void CollectMeshes(Node node, List<MeshInstance3D> meshes)
	{
		foreach (Node child in node.GetChildren())
		{
			if (child is MeshInstance3D mesh)
			{
				meshes.Add(mesh);
			}

			CollectMeshes(child, meshes);
		}
	}

	private void Restore(Node3D node)
	{
		if (!_flashes.Remove(node, out Flash? flash))
		{
			return;
		}

		if (flash.Timer.IsValid())
		{
			flash.Timer.Kill();
		}

		foreach (MeshInstance3D mesh in flash.Meshes)
		{
			if (!_holds.TryGetValue(mesh, out Hold? hold) || --hold.Holders > 0)
			{
				continue;
			}

			_holds.Remove(mesh);

			if (IsInstanceValid(mesh))
			{
				mesh.MaterialOverride = hold.Original;
			}
		}
	}

	private sealed class Hold(Material? original)
	{
		public Material? Original { get; } = original;

		public int Holders { get; set; } = 1;
	}

	private sealed class Flash
	{
		public List<MeshInstance3D> Meshes { get; } = [];

		public Tween Timer { get; set; } = null!;
	}
}
