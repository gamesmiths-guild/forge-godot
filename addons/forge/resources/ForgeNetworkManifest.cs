// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Resources.Abilities;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Networking;
using Godot;
using Godot.Collections;

namespace Gamesmiths.Forge.Godot.Resources;

/// <summary>
/// The assets a networked session can refer to, as resource UIDs: every effect, ability, Statescript graph and
/// networked scene in the project. The editor plugin generates it (Project, Tools, Forge, Regenerate network
/// manifest, and on saving one of those resources), and a session loads it to build its asset registry, so it works
/// in an export, where the project's files can't be scanned.
/// </summary>
/// <remarks>
/// A UID identifies a resource whatever its name or folder, so renaming or moving a file changes nothing on the
/// wire. Both ends of a session must have the same manifest: the registry's hash is compared in the handshake, and a
/// client whose differs is refused.
/// </remarks>
[Tool]
[GlobalClass]
public partial class ForgeNetworkManifest : Resource
{
	/// <summary>
	/// Gets or sets the UIDs of the project's effects (<see cref="ForgeEffectData"/>).
	/// </summary>
	[Export]
	public Array<string> Effects { get; set; } = [];

	/// <summary>
	/// Gets or sets the UIDs of the project's abilities (<see cref="ForgeAbilityData"/>).
	/// </summary>
	[Export]
	public Array<string> Abilities { get; set; } = [];

	/// <summary>
	/// Gets or sets the UIDs of the project's Statescript graphs (<see cref="StatescriptGraph"/>).
	/// </summary>
	[Export]
	public Array<string> Graphs { get; set; } = [];

	/// <summary>
	/// Gets or sets the UIDs of the scenes networked entities spawn from. Empty until milestone 8 marks scenes as
	/// networked.
	/// </summary>
	[Export]
	public Array<string> Scenes { get; set; } = [];

	/// <summary>
	/// Loads the project's manifest from <see cref="ForgeSettings.NetworkManifestSetting"/>.
	/// </summary>
	/// <returns>The manifest, or <see langword="null"/> when the project has none yet.</returns>
	public static ForgeNetworkManifest? LoadProjectManifest()
	{
		string path = ForgeSettings.GetNetworkManifestPath();

		return ResourceLoader.Exists(path) ? ResourceLoader.Load<ForgeNetworkManifest>(path) : null;
	}

	/// <summary>
	/// Builds and seals the asset registry the manifest describes, loading each resource. A UID that no longer
	/// resolves is reported and left out, which changes the registry's hash, so a stale manifest shows up as a
	/// refused handshake rather than a silent disagreement.
	/// </summary>
	/// <returns>The sealed registry.</returns>
	public NetworkAssetRegistry BuildRegistry()
	{
		var registry = new NetworkAssetRegistry();

		foreach (string uid in Effects)
		{
			if (Load<ForgeEffectData>(uid, "effect") is ForgeEffectData effect)
			{
				registry.Register(AssetKind.Effect, uid, effect.GetEffectData());
			}
		}

		foreach (string uid in Abilities)
		{
			if (Load<ForgeAbilityData>(uid, "ability") is ForgeAbilityData ability)
			{
				registry.Register(AssetKind.Ability, uid, ability.GetAbilityData());
			}
		}

		foreach (string uid in Graphs)
		{
			if (Load<StatescriptGraph>(uid, "graph") is StatescriptGraph graph)
			{
				registry.Register(AssetKind.Graph, uid, graph);
			}
		}

		foreach (string uid in Scenes)
		{
			if (Load<PackedScene>(uid, "scene") is PackedScene scene)
			{
				registry.Register(AssetKind.Scene, uid, scene);
			}
		}

		registry.Seal();
		return registry;
	}

	private static T? Load<T>(string uid, string what)
		where T : Resource
	{
		if (ResourceLoader.Exists(uid) && ResourceLoader.Load(uid) is T resource)
		{
			return resource;
		}

		GD.PushWarning(
			$"Forge: the network manifest lists the {what} {uid}, which doesn't load. Regenerate the manifest " +
			"(Project, Tools, Forge) so every machine builds the same registry.");
		return null;
	}
}
