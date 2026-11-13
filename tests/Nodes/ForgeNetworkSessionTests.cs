// Copyright © Gamesmiths Guild.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Core.Statescript.Nodes;
using Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Action;
using Gamesmiths.Forge.Godot.Networking;
using Gamesmiths.Forge.Godot.Nodes;
using Gamesmiths.Forge.Godot.Resources;
using Gamesmiths.Forge.Godot.Resources.Abilities;
using Gamesmiths.Forge.Networking;
using Gamesmiths.Forge.Networking.Messages;
using Gamesmiths.Forge.Networking.Replication;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Tags;
using GdUnit4;
using Godot;
using ForgeGraph = Gamesmiths.Forge.Statescript.Graph;
using Node = Godot.Node;

namespace Gamesmiths.Forge.Godot.Tests.Nodes;

/// <summary>
/// A <see cref="ForgeNetworkSession"/> hosting and another joining it over ENet on this machine, in one process through
/// two multiplayer branches: the handshake accepts a matching client and refuses a mismatched one, the client's input
/// reaches the server's playout buffer, networked entities reach the client and follow the server, and leaving is seen
/// on both ends.
/// </summary>
[TestSuite]
public class ForgeNetworkSessionTests
{
	private const string Burning = "res://forge_samples/demos/04_rewind/effects/burning.tres";

	private const string Fallen = "res://forge_samples/demos/03_statescript/effects/fallen.tres";

	private const string OwnerOnlyCooldown =
		"res://forge_samples/demos/05_networked_statescript/abilities/dash_cooldown.tres";

	private const string ServerOnlyGrants =
		"res://forge_samples/demos/05_networked_statescript/abilities/enemy_grants.tres";

	private const string Body = "res://tests/Nodes/networked_body.tscn";

	private const string HideAbility = "res://tests/Nodes/hide_ability.tres";

	// How many ticks the test's client runs ahead of the test's server: enough to cover the bursts of physics steps a
	// process frame can hold, since ENet only moves packets between process frames.
	private const long Lead = 12;

	private static Window Root => ((SceneTree)Engine.GetMainLoop()).Root;

	private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();

	[TestCase]
	[RequireGodotRuntime]
	public async Task A_client_joins_a_host_and_its_input_reaches_the_server()
	{
		using var pair = new Pair(Manifest(Burning), Manifest(Burning));
		var deliveries = new List<(InputDelivery Delivery, ulong Held)>();
		var joined = new List<long>();
		bool connected = false;
		pair.Server.PeerJoined += joined.Add;
		pair.Client.Connected += () => connected = true;
		pair.Server.InputTaken += (_, command, delivery) => deliveries.Add((delivery, command.Frame.Held));

		pair.Server.Host().Should().Be(Error.Ok);
		pair.Client.Join("127.0.0.1").Should().Be(Error.Ok);
		pair.Server.Mode.Should().Be(NetworkMode.ListenServer);
		pair.Client.Mode.Should().Be(NetworkMode.Client);
		pair.Server.ProcessPhysicsPriority.Should().BeLessThan(
			int.MinValue + 1,
			"the server hands commands over before the bootstrap begins the ticks that read them");
		pair.Client.ProcessPhysicsPriority.Should().BeGreaterThan(
			int.MinValue + 2,
			"a client sends the frame its tick read, after every tick, a world history's included, has begun");

		await Pair.RunUntil(() => connected, frames: 600);

		connected.Should().BeTrue("the handshake should complete over the local link");
		joined.Should().ContainSingle();
		pair.Server.Session!.Peers.Should().ContainSingle();
		pair.Client.Session!.SessionSeed.Should().Be(pair.Server.Session.SessionSeed);

		// A second of play: the client sends a held action every tick, and the server takes it tick by tick.
		int before = deliveries.Count;
		await Pair.RunUntil(
			() => deliveries.Count >= before + 60,
			frames: 1200,
			eachFrame: () => pair.Player.Input!.Submit(new InputFrame { Held = 1 }));

		string summary =
			string.Join(" ", deliveries.TakeLast(30).Select(x => $"{x.Delivery}/{x.Held}")) + pair.Diagnostics();
		deliveries.Should().HaveCountGreaterThan(30);
		deliveries.TakeLast(30).Should().OnlyContain(
			x => x.Held == 1,
			"the held action should reach the server; the last 30 were {0}",
			summary);

		// Both ends share one process, so a hitch stalls the client's sends and the server's steps together, and the
		// physics steps that catch up afterwards run before the next frame moves any packet. Most ticks still find
		// their command waiting; on two machines, every one would.
		deliveries.Skip(before).Count(x => x.Delivery == InputDelivery.Received).Should().BeGreaterThanOrEqualTo(
			(deliveries.Count - before) * 8 / 10,
			"most ticks' commands should be there when the server needs them; the last 30 were {0}",
			summary);
		pair.Client.TicksAhead
			.Should().BeInRange(Lead - 3, Lead + 3, "the client sees the server's clock behind its own");
		pair.Client.RoundTripTime
			.Should().BeLessThan(0.1);
		pair.Server.Session.Peers[0].MalformedPackets
			.Should().Be(0);

		bool left = false;
		pair.Server.PeerLeft += _ => left = true;
		pair.Client.Leave();

		await Pair.RunUntil(() => left, frames: 120);

		left.Should().BeTrue();
		pair.Client.Mode.Should().Be(NetworkMode.Offline);
		pair.Client.BytesSentPerSecond.Should().Be(0, "the next session counts its own traffic");
		pair.Server.Session.Peers.Should().BeEmpty();
	}

	[TestCase]
	[RequireGodotRuntime]
	public async Task Raw_transports_carry_unreliable_packets_both_ways_every_frame()
	{
		string suffix = Guid.NewGuid().ToString("N");
		var serverBranch = new Node { Name = $"RawServer {suffix}" };
		var clientBranch = new Node { Name = $"RawClient {suffix}" };
		Root.AddChild(serverBranch);
		Root.AddChild(clientBranch);
		var serverApi = new SceneMultiplayer();
		var clientApi = new SceneMultiplayer();
		Tree.SetMultiplayer(serverApi, serverBranch.GetPath());
		Tree.SetMultiplayer(clientApi, clientBranch.GetPath());
		int port = TestPort();

		GodotTransport.CreateServerPeer(port, 2, out ENetMultiplayerPeer serverPeer).Should().Be(Error.Ok);
		serverApi.MultiplayerPeer = serverPeer;
		GodotTransport.CreateClientPeer("127.0.0.1", port, out ENetMultiplayerPeer clientPeer).Should().Be(Error.Ok);
		clientApi.MultiplayerPeer = clientPeer;

		using var server = new GodotTransport(serverApi, isServer: true);
		using var client = new GodotTransport(clientApi, isServer: false);

		// [channel, reliable]
		int[,] toServer = new int[3, 2];
		int[,] toClient = new int[3, 2];
		int sentFrames = 0;
		PeerId clientId = PeerId.None;
		var unreliableArrivals = new List<int>();

		try
		{
			for (int frame = 0; frame < 240; frame++)
			{
				await Root.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);

				while (server.TryReceive(out TransportEvent received))
				{
					if (received.Kind == TransportEventKind.PeerConnected)
					{
						clientId = received.Peer;
					}
					else if (received.Kind == TransportEventKind.Packet)
					{
						toServer[received.Channel, received.Payload.Span[0]]++;

						if (received.Payload.Span[0] == 0 && received.Channel == 0)
						{
							unreliableArrivals.Add(frame);
						}
					}
				}

				while (client.TryReceive(out TransportEvent received))
				{
					if (received.Kind == TransportEventKind.Packet)
					{
						toClient[received.Channel, received.Payload.Span[0]]++;
					}
				}

				if (!clientId.IsValid || !client.Peers.Contains(PeerId.Server))
				{
					continue;
				}

				sentFrames++;

				for (byte channel = 0; channel < 3; channel++)
				{
					client.Send(PeerId.Server, channel, reliable: true, [1, channel, 2, 3]);
					client.Send(PeerId.Server, channel, reliable: false, [0, channel, 2, 3]);
					server.Send(clientId, channel, reliable: true, [1, channel, 5, 6]);
					server.Send(clientId, channel, reliable: false, [0, channel, 5, 6]);
				}
			}
		}
		finally
		{
			Tree.SetMultiplayer(null, serverBranch.GetPath());
			Tree.SetMultiplayer(null, clientBranch.GetPath());
			serverBranch.Free();
			clientBranch.Free();
		}

		static string Table(int[,] counts)
		{
			return $"ch0 r{counts[0, 1]} u{counts[0, 0]}, ch1 r{counts[1, 1]} u{counts[1, 0]}, ch2 r{counts[2, 1]} " +
				$"u{counts[2, 0]}";
		}

		string buckets = string.Join(
			"/",
			Enumerable.Range(0, 5).Select(bucket => unreliableArrivals.Count(x => x / 50 == bucket)));
		ENetPacketPeer? clientSide = clientPeer.GetPeer(1);
		string throttle = clientSide is null
			? "no peer"
			: $"throttle {clientSide.GetStatistic(ENetPacketPeer.PeerStatistic.PacketThrottle)}, " +
				$"limit {clientSide.GetStatistic(ENetPacketPeer.PeerStatistic.PacketThrottleLimit)}, " +
				$"loss {clientSide.GetStatistic(ENetPacketPeer.PeerStatistic.PacketLoss)}, " +
				$"rtt {clientSide.GetStatistic(ENetPacketPeer.PeerStatistic.RoundTripTime)}";
		string hosts = $"client host sent {clientPeer.Host.PopStatistic(ENetConnection.HostStatistic.SentPackets)} " +
			$"datagrams, received {clientPeer.Host.PopStatistic(ENetConnection.HostStatistic.ReceivedPackets)}; " +
			$"server host sent {serverPeer.Host.PopStatistic(ENetConnection.HostStatistic.SentPackets)}, " +
			$"received {serverPeer.Host.PopStatistic(ENetConnection.HostStatistic.ReceivedPackets)}";
		string counts = $"{hosts}; sent {sentFrames} frames; to server {Table(toServer)}; to client " +
			$"{Table(toClient)}; client sent {client.PacketsSent} with {client.SendErrors} errors, " +
			$"server sent {server.PacketsSent} with {server.SendErrors} errors; " +
			$"unreliable ch0 arrivals per 50 frames {buckets}; client peer {throttle}";
		sentFrames.Should().BeGreaterThan(100, counts);

		for (int channel = 0; channel < 3; channel++)
		{
			for (int reliable = 0; reliable < 2; reliable++)
			{
				toServer[channel, reliable].Should().BeGreaterThanOrEqualTo((sentFrames * 9 / 10) - 1, counts);
				toClient[channel, reliable].Should().BeGreaterThanOrEqualTo((sentFrames * 9 / 10) - 1, counts);
			}
		}

		serverPeer.Close();
		clientPeer.Close();
	}

	[TestCase]
	[RequireGodotRuntime]
	public async Task A_client_with_other_assets_is_refused()
	{
		using var pair = new Pair(Manifest(Burning), new ForgeNetworkManifest());
		int? refusal = null;
		pair.Client.Refused += reason => refusal = reason;

		pair.Server.Host().Should().Be(Error.Ok);
		pair.Client.Join("127.0.0.1").Should().Be(Error.Ok);

		await Pair.RunUntil(() => refusal is not null, frames: 600);

		refusal.Should().Be((int)RefusalReason.AssetRegistry);
		pair.Client.Mode.Should().Be(NetworkMode.Offline, "a refused client leaves");
		pair.Server.Session!.Peers.Should().BeEmpty();
	}

	[TestCase]
	[RequireGodotRuntime]
	public void A_dedicated_host_presents_nothing_until_it_leaves()
	{
		using var pair = new Pair(Manifest(Burning), Manifest(Burning));

		pair.Server.Host(dedicated: true).Should().Be(Error.Ok);

		pair.Server.Mode.Should().Be(NetworkMode.DedicatedServer);
		ForgeManagers.Instance.HasViewer.Should().BeFalse();

		pair.Server.Leave();

		pair.Server.Mode.Should().Be(NetworkMode.Offline);
		ForgeManagers.Instance.HasViewer.Should().BeTrue();
	}

	[TestCase]
	[RequireGodotRuntime]
	public void A_manifest_builds_a_registry_the_handshake_can_compare()
	{
		NetworkAssetRegistry registry = Manifest(Burning).BuildRegistry();
		string uid = Uid(Burning);

		registry.IsSealed.Should().BeTrue();
		registry.Count(AssetKind.Effect).Should().Be(1);
		registry.IndexOf(AssetKind.Effect, uid).Should().Be(0);
		registry.Get<EffectData>(AssetKind.Effect, 0).Name.Should().NotBeEmpty();
		registry.Hash.Should().Be(Manifest(Burning).BuildRegistry().Hash, "the same manifest hashes the same");
		registry.Hash.Should().NotBe(new ForgeNetworkManifest().BuildRegistry().Hash);
	}

	[TestCase]
	[RequireGodotRuntime]
	public async Task A_networked_scene_the_server_spawns_for_a_client_is_predicted_there_and_walks_by_its_input()
	{
		using var pair = new Pair(SceneManifest(Body), SceneManifest(Body), withPlayer: false);
		await pair.Connect();

		CharacterBody3D body = ResourceLoader.Load<PackedScene>(Body).Instantiate<CharacterBody3D>();
		ForgeEntity entity = body.GetNode<ForgeEntity>("Forge Entity");
		entity.NetworkOwner = pair.Client.LocalPeer;
		DeterministicRandom? readied = null;

		void OnAdded(Node node)
		{
			if (node is ForgeEntity { IsPredicted: true } added)
			{
				added.Ready += () =>
				{
					readied = added.Random;
					added.AddChild(new ForgeEffect
					{
						EffectData = new ForgeEffectData { DurationType = DurationType.Infinite },
					});
				};
			}
		}

		Tree.NodeAdded += OnAdded;

		try
		{
			pair.ServerBranch.AddChild(body);

			entity.NetworkId.IsValid.Should().BeTrue("the server registers a networked entity as it gets ready");
			pair.Server.Session!.Replication!.Find(entity)!.Owner.Value.Should().Be(pair.Client.LocalPeer);

			await Pair.RunUntil(() => pair.Client.LocalPlayer is not null, frames: 300);
		}
		finally
		{
			Tree.NodeAdded -= OnAdded;
		}

		ForgeEntity predicted = pair.Client.LocalPlayer!;
		var predictedBody = (CharacterBody3D)predicted.GetParent();
		predicted.Should().NotBeSameAs(entity);
		predicted.IsMirror.Should().BeFalse();
		predicted.IsPredicted.Should().BeTrue("a client predicts the entity it owns");
		predicted.NetworkId.Should().Be(entity.NetworkId);
		predicted.Random.Seed.Should().Be(entity.Random.Seed, "both roll from the session's seed and the id");
		predicted.Random.Should().BeSameAs(
			readied,
			"it's seeded before its setup, so a graph the setup starts rolls with the entity's random");
		predicted.EffectsManager.GetActiveEffects().Should().ContainSingle(
			"its setup runs once it's bound as predicted, after it got ready");
		predictedBody.GetParent().Should().BeSameAs(pair.ClientBranch, "a client spawns under its spawn parent");
		pair.Client.Session!.Mirror!.Predicted.Should().Equal(predicted);
		pair.Server.LocalPlayer.Should().BeNull("the server's own peer owns nothing here");

		Vector3 start = body.GlobalPosition;
		Input.ActionPress("move_right");

		try
		{
			await Pair.RunUntil(() => body.GlobalPosition.X > start.X + 0.5f, frames: 600);
		}
		finally
		{
			Input.ActionRelease("move_right");
		}

		body.GlobalPosition.X.Should().BeGreaterThan(start.X + 0.5f, "the client's input walks the server's copy");
		predictedBody.GlobalPosition.X.Should().BeGreaterThanOrEqualTo(
			body.GlobalPosition.X - 0.01f,
			"the client walks its own copy ahead of the server");
		pair.Client.Confirmations.Should().BeGreaterThan(
			pair.Client.Corrections,
			"what the client predicted is mostly what the server ran");

		body.QueueFree();
		await Pair.RunUntil(() => pair.Client.LocalPlayer is null, frames: 300);

		pair.Client.LocalPlayer.Should().BeNull("freed on the server, it goes from the client");
		pair.Client.Session!.Mirror!.Entities.Should().BeEmpty();
		GodotObject.IsInstanceValid(predictedBody).Should().BeFalse();
	}

	[TestCase]
	[RequireGodotRuntime]
	public async Task A_spawned_mirror_is_where_its_body_is_shown_before_what_its_spawn_sets_off_runs()
	{
		using var pair = new Pair(SceneManifest(Body), SceneManifest(Body), withPlayer: false);
		await pair.Connect();

		CharacterBody3D body = ResourceLoader.Load<PackedScene>(Body).Instantiate<CharacterBody3D>();
		body.Position = new Vector3(5, 0, 0);
		Vector3? boundAt = null;
		pair.Client.Session!.Mirror!.Spawned += x => boundAt ??= ((ForgeEntity)x).GetParent<Node3D>().GlobalPosition;
		pair.ServerBranch.AddChild(body);

		await Pair.RunUntil(() => boundAt is not null, frames: 300);

		boundAt.Should().NotBeNull();
		boundAt!.Value.X
			.Should().BeApproximately(5, 0.01f, "it's placed as it's bound, not left where its scene has it");
	}

	[TestCase]
	[RequireGodotRuntime]
	public async Task A_networked_entity_placed_on_both_ends_is_bound_by_its_place_and_follows_the_server()
	{
		using var pair = new Pair(SceneManifest(Body), SceneManifest(Body), withPlayer: false);
		CharacterBody3D serverBody = ResourceLoader.Load<PackedScene>(Body).Instantiate<CharacterBody3D>();
		CharacterBody3D clientBody = ResourceLoader.Load<PackedScene>(Body).Instantiate<CharacterBody3D>();
		pair.ServerBranch.AddChild(serverBody);
		pair.ClientBranch.AddChild(clientBody);
		pair.ClientBranch.AddChild(new ForgeWorldHistory());
		ForgeEntity entity = serverBody.GetNode<ForgeEntity>("Forge Entity");
		ForgeEntity mirror = clientBody.GetNode<ForgeEntity>("Forge Entity");
		await Pair.RunUntil(() => mirror.WorldHistory is not null, frames: 10);
		mirror.WorldHistory.Should().NotBeNull("the client's level history took it in before the session joined");
		await pair.Connect();

		mirror.IsMirror.Should().BeTrue("the client bound it as it joined");
		mirror.WorldHistory.Should().BeNull("a mirror leaves the world history it was in, which would advance it");
		clientBody.PhysicsInterpolationMode.Should().Be(Node.PhysicsInterpolationModeEnum.Off);
		mirror.NetworkId.Should().Be(entity.NetworkId).And.Be(new NetworkEntityId(1));
		pair.Server.Session!.Replication!.Find(entity)!.IsPlaced.Should().BeTrue();

		var target = new Vector3(3, 0, -2);
		serverBody.GlobalPosition = target;
		await Pair.RunUntil(() => clientBody.GlobalPosition.DistanceTo(target) < 0.01f, frames: 300);

		clientBody.GlobalPosition.DistanceTo(target).Should().BeLessThan(0.01f);
		mirror.Input!.Current.Held.Should().Be(0, "the world owns it, so no machine reads its devices");
	}

	[TestCase]
	[RequireGodotRuntime]
	public async Task A_placed_entity_nothing_moved_stays_exactly_where_its_scene_has_it()
	{
		using var pair = new Pair(SceneManifest(Body), SceneManifest(Body), withPlayer: false);
		var placed = new Transform3D(
			new Basis(new Vector3(1, 1, 0).Normalized(), 0.3f),
			new Vector3(4.0722361f, 0.0004f, -3.1852062f));
		CharacterBody3D serverBody = ResourceLoader.Load<PackedScene>(Body).Instantiate<CharacterBody3D>();
		CharacterBody3D clientBody = ResourceLoader.Load<PackedScene>(Body).Instantiate<CharacterBody3D>();
		serverBody.Transform = placed;
		clientBody.Transform = placed;
		pair.ServerBranch.AddChild(serverBody);
		pair.ClientBranch.AddChild(clientBody);
		await pair.Connect();
		await Pair.RunUntil(() => false, frames: 30);

		clientBody.GlobalTransform.Should().Be(placed, "the server's rounding can't tell it from where it stands");
	}

	[TestCase]
	[RequireGodotRuntime]
	public async Task A_networked_scene_is_the_server_s_to_spawn_and_belongs_to_whoever_spawned_it()
	{
		using var pair = new Pair(SceneManifest(Body), SceneManifest(Body), withPlayer: false);
		await pair.Connect();
		PackedScene scene = ResourceLoader.Load<PackedScene>(Body);

		SceneInstantiationUtilities.Instantiate3D(
			new GraphContext(),
			0,
			scene,
			InstantiateParentMode.Node,
			pair.ClientBranch,
			null,
			null,
			null,
			passOwnership: false).Should().BeNull("a client gets networked scenes from the server");

		CharacterBody3D spawner = scene.Instantiate<CharacterBody3D>();
		spawner.GetNode<ForgeEntity>("Forge Entity").NetworkOwner = pair.Client.LocalPeer;
		pair.ServerBranch.AddChild(spawner);

		Node? spawned = SceneInstantiationUtilities.Instantiate3D(
			new GraphContext(),
			0,
			scene,
			InstantiateParentMode.Node,
			pair.ServerBranch,
			spawner.GetNode<ForgeEntity>("Forge Entity"),
			null,
			null,
			passOwnership: false);

		spawned.Should().NotBeNull();
		spawned!.GetNode<ForgeEntity>("Forge Entity").NetworkOwner.Should().Be(pair.Client.LocalPeer);
	}

	[TestCase]
	[RequireGodotRuntime]
	public void A_Server_Only_ability_resource_makes_ability_data_its_owner_doesn_t_predict()
	{
		var ability = new ForgeAbilityData { Name = "Smite", ServerOnly = true };

		ability.GetAbilityData().ServerOnly.Should().BeTrue();
		new ForgeAbilityData { Name = "Dash" }.GetAbilityData().ServerOnly.Should().BeFalse();
	}

	[TestCase]
	[RequireGodotRuntime]
	public void A_session_leaves_entities_of_another_multiplayer_branch_alone()
	{
		using var pair = new Pair(SceneManifest(Body), SceneManifest(Body), withPlayer: false);
		CharacterBody3D body = ResourceLoader.Load<PackedScene>(Body).Instantiate<CharacterBody3D>();
		pair.ServerBranch.AddChild(body);
		var session = new ForgeNetworkSession
		{
			Port = TestPort(),
			Manifest = SceneManifest(Body),
		};
		Root.AddChild(session);

		session.Host().Should().Be(Error.Ok);

		body.GetNode<ForgeEntity>("Forge Entity").NetworkId.IsValid
			.Should().BeFalse("a session on the tree's multiplayer sees the whole tree, and this is another's");
		session.Leave();
		session.Free();
	}

	[TestCase]
	[RequireGodotRuntime]
	public async Task Leaving_clears_the_local_player_and_only_a_session_in_the_tree_reports_it()
	{
		using var pair = new Pair(Manifest(Burning), Manifest(Burning));
		await pair.Connect();
		int disconnected = 0;
		pair.Client.Disconnected += () => disconnected++;

		pair.ClientBranch.RemoveChild(pair.Client);
		pair.ClientBranch.AddChild(pair.Client);
		disconnected.Should().Be(0, "a level on its way out has nothing to do about it");
		pair.Client.Mode.Should().Be(NetworkMode.Offline);

		pair.Client.Join("127.0.0.1").Should().Be(Error.Ok);
		pair.Client.Leave();

		disconnected.Should().Be(1);
		pair.Client.LocalPlayer.Should().BeNull();
	}

	[TestCase]
	[RequireGodotRuntime]
	public async Task An_entity_that_stops_being_this_machine_s_lets_go_of_its_input()
	{
		using var pair = new Pair(SceneManifest(Body), SceneManifest(Body), withPlayer: false);
		CharacterBody3D body = ResourceLoader.Load<PackedScene>(Body).Instantiate<CharacterBody3D>();
		pair.ServerBranch.AddChild(body);
		Input.ActionPress("move_right");

		try
		{
			await Pair.RunUntil(() => body.GlobalPosition.X > 0.5f, frames: 300);
			body.GlobalPosition.X.Should().BeGreaterThan(0.5f, "offline, this machine reads its devices");

			pair.Server.Host().Should().Be(Error.Ok);
			await Pair.RunUntil(() => false, frames: 60);
			float stopped = body.GlobalPosition.X;
			await Pair.RunUntil(() => false, frames: 30);

			body.GlobalPosition.X.Should().BeApproximately(stopped, 0.01f, "the world owns it now, and nobody's input");
		}
		finally
		{
			Input.ActionRelease("move_right");
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public async Task A_mirror_that_freed_itself_is_nothing_to_despawn()
	{
		using var pair = new Pair(SceneManifest(Body), SceneManifest(Body), withPlayer: false);
		await pair.Connect();
		CharacterBody3D body = ResourceLoader.Load<PackedScene>(Body).Instantiate<CharacterBody3D>();
		pair.ServerBranch.AddChild(body);
		await Pair.RunUntil(() => pair.Client.Session!.Mirror!.Entities.Count > 0, frames: 300);

		((ForgeEntity)pair.Client.Session!.Mirror!.Entities[0]).GetParent().Free();

		pair.Client.Invoking(x => x.Leave()).Should().NotThrow();
	}

	[TestCase]
	[RequireGodotRuntime]
	public async Task Despawning_part_of_a_level_leaves_the_level()
	{
		using var pair = new Pair(SceneManifest(Body), SceneManifest(Body), withPlayer: false);
		(Node3D serverGone, Node3D _) = PlaceTwoInALevel(pair.ServerBranch);
		(Node3D clientGone, Node3D clientKept) = PlaceTwoInALevel(pair.ClientBranch);
		await pair.Connect();

		serverGone.QueueFree();
		await Pair.RunUntil(() => !GodotObject.IsInstanceValid(clientGone), frames: 300);

		GodotObject.IsInstanceValid(clientGone).Should().BeFalse("the server despawned it");
		GodotObject.IsInstanceValid(clientKept).Should().BeTrue("the level it was part of stays");
		clientKept.GetParent().IsInsideTree().Should().BeTrue();
	}

	[TestCase]
	[RequireGodotRuntime]
	public async Task A_mirror_holds_the_effects_of_the_server_s_entity_and_runs_none_of_them()
	{
		using var pair = new Pair(EffectsManifest(), EffectsManifest(), withPlayer: false);
		await pair.Connect();
		pair.Server.Session!.Replication!.DefaultEffectVisibility.Should().Be(EffectVisibility.Public);

		CharacterBody3D body = ResourceLoader.Load<PackedScene>(Body).Instantiate<CharacterBody3D>();
		ForgeEntity entity = body.GetNode<ForgeEntity>("Forge Entity");
		entity.BaseTags = new ForgeTagContainer { ContainerTags = ["trait.flammable"] };
		pair.ServerBranch.AddChild(body);
		ActiveEffectHandle handle = Apply(entity, Burning)!;

		await Pair.RunUntil(() => Mirrored(pair, entity)?.EffectsManager.GetActiveEffects().Any() == true, frames: 300);

		ForgeEntity mirror = Mirrored(pair, entity)!;
		ActiveEffectHandle mirrored = mirror.EffectsManager.GetActiveEffects().Should().ContainSingle().Subject;
		mirrored.Effect!.EffectData.Should().Be(ResourceLoader.Load<ForgeEffectData>(Burning).GetEffectData());
		mirrored.Effect.Ownership.Source.Should().BeSameAs(mirror, "the server's entity gave it to itself");
		mirrored.TotalDuration.Should().Be(handle.TotalDuration);
		mirrored.RemainingDuration.Should().BeInRange(0.1, handle.TotalDuration);

		double before = mirrored.RemainingDuration;
		await Pair.RunUntil(() => mirrored.RemainingDuration < before - 0.2, frames: 300);

		mirrored.RemainingDuration.Should().BeLessThan(before - 0.2, "what's left follows the server");
		mirrored.ComponentInstances.Should().BeEmpty();

		entity.EffectsManager.RemoveEffect(handle);
		await Pair.RunUntil(() => !mirrored.IsValid, frames: 300);

		mirrored.IsValid.Should().BeFalse("it ends when the server says");
		mirror.EffectsManager.GetActiveEffects().Should().BeEmpty();
	}

	[TestCase]
	[RequireGodotRuntime]
	public async Task An_effect_s_visibility_decides_which_mirrors_hold_it()
	{
		using var pair = new Pair(EffectsManifest(), EffectsManifest(), withPlayer: false);
		await pair.Connect();
		CharacterBody3D mineBody = ResourceLoader.Load<PackedScene>(Body).Instantiate<CharacterBody3D>();
		CharacterBody3D theirsBody = ResourceLoader.Load<PackedScene>(Body).Instantiate<CharacterBody3D>();
		ForgeEntity mine = mineBody.GetNode<ForgeEntity>("Forge Entity");
		ForgeEntity theirs = theirsBody.GetNode<ForgeEntity>("Forge Entity");
		mine.NetworkOwner = pair.Client.LocalPeer;
		pair.ServerBranch.AddChild(mineBody);
		pair.ServerBranch.AddChild(theirsBody);

		Apply(mine, OwnerOnlyCooldown);
		Apply(theirs, OwnerOnlyCooldown);
		Apply(mine, ServerOnlyGrants);
		Apply(theirs, ServerOnlyGrants);

		// A public effect travels in the same entry as anything that leaked, so once it's here, a leak would be too.
		Apply(mine, Fallen);
		Apply(theirs, Fallen);

		await Pair.RunUntil(
			() => Mirrored(pair, mine)?.EffectsManager.GetActiveEffects().Any() == true
				&& Mirrored(pair, theirs)?.EffectsManager.GetActiveEffects().Any() == true,
			frames: 300);

		EffectNames(Mirrored(pair, mine)!).Should().Equal("Dash Cooldown", "Fallen");
		EffectNames(Mirrored(pair, theirs)!).Should().Equal(["Fallen"], "the cooldown is another's and owner-only");
	}

	[TestCase]
	[RequireGodotRuntime]
	public async Task Custom_data_reaches_a_mirror_by_the_codec_both_ends_registered()
	{
		using var pair = new Pair(EffectsManifest(), EffectsManifest(), withPlayer: false);
		pair.Server.RegisterEffectContext("count", WriteCount, ReadCount);
		pair.Client.RegisterEffectContext("count", WriteCount, ReadCount);
		await pair.Connect();

		CharacterBody3D body = ResourceLoader.Load<PackedScene>(Body).Instantiate<CharacterBody3D>();
		ForgeEntity entity = body.GetNode<ForgeEntity>("Forge Entity");
		pair.ServerBranch.AddChild(body);
		EffectData data = ResourceLoader.Load<ForgeEffectData>(Fallen).GetEffectData();
		entity.EffectsManager.ApplyEffect(new Effect(data, new EffectOwnership(entity, entity)), 7);

		await Pair.RunUntil(() => Mirrored(pair, entity)?.EffectsManager.GetActiveEffects().Any() == true, frames: 300);

		ActiveEffectHandle mirrored =
			Mirrored(pair, entity)!.EffectsManager.GetActiveEffects().Should().ContainSingle().Subject;
		mirrored.TryGetContextData(out int count).Should().BeTrue();
		count.Should().Be(7);

		static void WriteCount(BitWriter writer, int value)
		{
			writer.WriteVarInt(value);
		}

		static int ReadCount(BitReader reader)
		{
			return (int)reader.ReadVarInt();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public async Task A_client_with_other_effect_codecs_is_refused()
	{
		using var pair = new Pair(EffectsManifest(), EffectsManifest());
		pair.Server.RegisterEffectContext("count", static (writer, value) => writer.WriteVarInt(value), Read);
		int? refusal = null;
		pair.Client.Refused += reason => refusal = reason;

		pair.Server.Host().Should().Be(Error.Ok);
		pair.Client.Join("127.0.0.1").Should().Be(Error.Ok);

		await Pair.RunUntil(() => refusal is not null, frames: 600);

		refusal.Should().Be((int)RefusalReason.AssetRegistry);

		static int Read(BitReader reader)
		{
			return (int)reader.ReadVarInt();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void An_effect_s_visibility_comes_from_its_resource()
	{
		new ForgeEffectData { DurationType = DurationType.Infinite, Visibility = EffectVisibility.ServerOnly }
			.GetEffectData().Visibility.Should().Be(EffectVisibility.ServerOnly);
		new ForgeEffectData { DurationType = DurationType.Infinite }
			.GetEffectData().Visibility.Should().Be(EffectVisibility.Default);
		ForgeSettings.DefaultEffectVisibility.Should().Be(EffectVisibility.Public);
	}

	[TestCase]
	[RequireGodotRuntime]
	public async Task A_mirror_shows_what_the_server_s_graph_shows_until_the_server_s_run_ends()
	{
		ForgeNetworkManifest manifest = Manifest();
		manifest.Abilities.Add(Uid(HideAbility));
		manifest.Scenes.Add(Uid(Body));
		using var pair = new Pair(manifest, manifest, withPlayer: false);
		await pair.Connect();

		CharacterBody3D body = ResourceLoader.Load<PackedScene>(Body).Instantiate<CharacterBody3D>();
		ForgeEntity entity = body.GetNode<ForgeEntity>("Forge Entity");
		pair.ServerBranch.AddChild(body);
		AbilityHandle hide = entity.Abilities.GrantAbilityPermanently(
			ResourceLoader.Load<ForgeAbilityData>(HideAbility).GetAbilityData(),
			1,
			LevelComparison.Higher,
			null);
		hide.TryActivate(out _).Should().BeTrue();

		await Pair.RunUntil(() => Mirrored(pair, entity)?.GetParent<Node3D>().Visible == false, frames: 300);

		Node3D mirror = Mirrored(pair, entity)!.GetParent<Node3D>();
		mirror.Visible.Should().BeFalse("the mirror's copy of the graph holds the override the server's holds");

		hide.Cancel();
		await Pair.RunUntil(() => mirror.Visible, frames: 300);

		mirror.Visible.Should().BeTrue("the override ends when the server's run does");
	}

	[TestCase]
	[RequireGodotRuntime]
	public async Task A_networked_scene_the_client_s_player_spawns_shows_at_once_and_the_server_s_spawn_binds_it()
	{
		using var pair = new Pair(SceneManifest(Body), SceneManifest(Body), withPlayer: false);
		await pair.Connect();
		PackedScene scene = ResourceLoader.Load<PackedScene>(Body);
		(_, ForgeEntity player) = await JoinShooter(pair, scene);
		ReplicationClient mirror = pair.Client.Session!.Mirror!;

		await Press("skill_1", () => mirror.Predicted.Count > 1);

		var shot = (ForgeEntity)mirror.Predicted[^1];
		shot.Should().NotBeSameAs(player);
		shot.IsPredicted.Should().BeTrue();
		shot.NetworkId.IsValid.Should().BeFalse("the client made it ahead of the server");

		await Pair.RunUntil(() => shot.NetworkId.IsValid, frames: 300);

		shot.NetworkId.IsValid.Should().BeTrue("the server's spawn bound it");
		mirror.Find(shot.NetworkId).Should().BeSameAs(shot);
		pair.Server.Session!.Replication!.Find(shot.NetworkId)!.SpawnKey.Should().NotBeNull();
		Bodies(player).Should().ContainSingle("the client made the shot once, and the server's spawn made no other");
	}

	[TestCase]
	[RequireGodotRuntime]
	public async Task A_predicted_spawn_freed_outright_leaves_the_client_s_timeline()
	{
		using var pair = new Pair(SceneManifest(Body), SceneManifest(Body), withPlayer: false);
		await pair.Connect();
		(_, ForgeEntity player) = await JoinShooter(pair, ResourceLoader.Load<PackedScene>(Body));
		ReplicationClient mirror = pair.Client.Session!.Mirror!;

		await Press("skill_1", () => mirror.Predicted.Count > 1);
		var shot = (ForgeEntity)mirror.Predicted[^1];
		shot.GetParent().QueueFree();

		await Pair.RunUntil(() => !mirror.Predicted.Contains(shot), frames: 30);

		mirror.Predicted
			.Should().NotContain(shot, "something freed it outright, so the timeline no longer advances it");
		mirror.Predicted
			.Should().Contain(player);

		await Pair.RunUntil(() => mirror.Predicted.Count > 1 && mirror.Predicted[^1].NetworkId.IsValid, frames: 300);

		mirror.Predicted.Should().HaveCount(2, "the server's spawn found nothing to bind, and made its own");
	}

	[TestCase]
	[RequireGodotRuntime]
	public async Task A_correction_holds_again_what_only_this_machine_shows_that_the_tick_it_runs_again_made()
	{
		ForgeNetworkManifest manifest = EffectsManifest();
		using var pair = new Pair(manifest, manifest, withPlayer: false);
		await pair.Connect();
		(ForgeEntity served, ForgeEntity player) = await JoinShooter(pair, Cosmetic());
		long beforePress = pair.Client.Clock.Tick;

		Apply(served, Fallen);
		await Press("skill_1", () => Bodies(player).Count > 0);
		Node3D shown = Bodies(player).Single();
		long? replayedFrom = null;
		pair.Client.Session!.Mirror!.Replayed += x => replayedFrom = Math.Min(replayedFrom ?? x, x);

		await Pair.RunUntil(() => replayedFrom < beforePress, frames: 300);

		replayedFrom
			.Should().BeLessThan(beforePress, "what landed before the press puts the player right from before it");
		Bodies(player)
			.Should().Equal([shown], "running the tick it was made in found it again");
		GodotObject.IsInstanceValid(shown)
			.Should().BeTrue();
		shown.IsQueuedForDeletion()
			.Should().BeFalse();
	}

	[TestCase]
	[RequireGodotRuntime]
	public async Task A_correction_frees_what_only_this_machine_shows_that_no_tick_it_runs_again_makes()
	{
		ForgeNetworkManifest manifest = EffectsManifest();
		using var pair = new Pair(manifest, manifest, withPlayer: false);
		await pair.Connect();
		(ForgeEntity served, ForgeEntity player) = await JoinShooter(pair, Cosmetic(), blockedBy: "state.fallen");
		long corrections = pair.Client.Corrections;

		Apply(served, Fallen);
		await Press("skill_1", () => Bodies(player).Count > 0);
		Node3D shown = Bodies(player).Single();

		await Pair.RunUntil(
			() => !GodotObject.IsInstanceValid(shown) || shown.IsQueuedForDeletion(),
			frames: 300);

		pair.Client.Corrections.Should().BeGreaterThan(corrections);
		(!GodotObject.IsInstanceValid(shown) || shown.IsQueuedForDeletion())
			.Should().BeTrue("the press was blocked once the server's word reached the tick before it");
	}

	[TestCase]
	[RequireGodotRuntime]
	public async Task A_client_keeps_a_clock_of_its_own_and_the_clock_every_entity_runs_on_counts_its_steps()
	{
		using var pair = new Pair(SceneManifest(Body), SceneManifest(Body));
		pair.Client.ClockOverride = null;
		await pair.Connect();

		pair.Client.Clock.Should().NotBeSameAs(ForgeManagers.Instance.Clock);
		int steps = 0;

		void Count()
		{
			steps++;
		}

		long start = ForgeManagers.Instance.Clock.Tick;
		Tree.PhysicsFrame += Count;

		try
		{
			await Pair.RunUntil(() => false, frames: 120);
		}
		finally
		{
			Tree.PhysicsFrame -= Count;
		}

		ForgeManagers.Instance.Clock.Tick.Should().Be(start + steps, "steering the client's clock leaves it alone");
	}

	// The client's player, spawned by the server for its peer, both copies granted an ability that instantiates the
	// scene under the player's body as skill_1 is pressed.
	private static async Task<(ForgeEntity Served, ForgeEntity Predicted)> JoinShooter(
		Pair pair,
		PackedScene shot,
		string? blockedBy = null)
	{
		CharacterBody3D body = ResourceLoader.Load<PackedScene>(Body).Instantiate<CharacterBody3D>();
		ForgeEntity served = body.GetNode<ForgeEntity>("Forge Entity");
		served.NetworkOwner = pair.Client.LocalPeer;

		void OnAdded(Node node)
		{
			if (node is ForgeEntity { IsPredicted: true } added)
			{
				added.Ready += () => Grant(added, shot, blockedBy);
			}
		}

		Tree.NodeAdded += OnAdded;

		try
		{
			pair.ServerBranch.AddChild(body);
			Grant(served, shot, blockedBy);
			await Pair.RunUntil(() => pair.Client.LocalPlayer is not null, frames: 300);
		}
		finally
		{
			Tree.NodeAdded -= OnAdded;
		}

		pair.Client.LocalPlayer.Should().NotBeNull();
		await Pair.RunUntil(() => false, frames: 30);
		return (served, pair.Client.LocalPlayer!);
	}

	private static void Grant(ForgeEntity entity, PackedScene shot, string? blockedBy)
	{
		var graph = new ForgeGraph();
		graph.VariableDefinitions.DefineObjectVariable("shot", shot);
		var instantiate = new InstantiateScene3DNode(InstantiateParentMode.Entity, passOwnership: false);
		instantiate.BindInput(InstantiateSceneNodeBase.SceneInput, "shot");
		graph.AddNode(instantiate);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			instantiate.InputPorts[ActionNode.InputPort]));

		TagsManager tags = ForgeManagers.Instance.TagsManager;
		var shoot = new AbilityData(
			"Shoot",
			activationBlockedTags: blockedBy is null ? null : new TagContainer(tags, [Tag.RequestTag(tags, blockedBy)]),
			behaviorFactory: () => new GraphAbilityBehavior(graph),
			inputBinding: AbilityInputBinding.ForAction("skill_1"));
		entity.Abilities.GrantAbilityPermanently(shoot, 1, LevelComparison.None, entity);
	}

	// A scene only this machine shows: a bare node, which nothing networks.
	private static PackedScene Cosmetic()
	{
		var template = new Node3D { Name = "Shown" };
		var scene = new PackedScene();
		scene.Pack(template);
		template.Free();
		return scene;
	}

	// The instances the player's ability made under its body.
	private static List<Node3D> Bodies(ForgeEntity player)
	{
		return [.. player.GetParent().GetChildren().OfType<Node3D>().Where(x => x is not CollisionShape3D)];
	}

	// Presses the action as a key would, through an input event the collector latches the press from.
	private static async Task Press(string action, Func<bool> until)
	{
		Input.ParseInputEvent(new InputEventAction { Action = action, Pressed = true });

		try
		{
			await Pair.RunUntil(until, frames: 120);
		}
		finally
		{
			Input.ParseInputEvent(new InputEventAction { Action = action, Pressed = false });
		}
	}

	private static ActiveEffectHandle? Apply(ForgeEntity entity, string effectPath)
	{
		EffectData data = ResourceLoader.Load<ForgeEffectData>(effectPath).GetEffectData();
		return entity.EffectsManager.ApplyEffect(new Effect(data, new EffectOwnership(entity, entity)));
	}

	private static ForgeEntity? Mirrored(Pair pair, ForgeEntity entity)
	{
		return pair.Client.Session?.Mirror?.Find(entity.NetworkId) as ForgeEntity;
	}

	private static string[] EffectNames(ForgeEntity entity)
	{
		return [.. entity.EffectsManager.GetActiveEffects().Select(x => x.Effect!.EffectData.Name)];
	}

	private static ForgeNetworkManifest EffectsManifest()
	{
		ForgeNetworkManifest manifest = Manifest(Burning, OwnerOnlyCooldown, ServerOnlyGrants, Fallen);
		manifest.Scenes.Add(Uid(Body));
		return manifest;
	}

	// Two networked entities written into one scene instance, as a level holds them.
	private static (Node3D A, Node3D B) PlaceTwoInALevel(Node branch)
	{
		var level = new Node3D { SceneFilePath = "res://tests/level_for_the_test.tscn" };
		var a = new Node3D { Name = "A" };
		var b = new Node3D { Name = "B" };
		a.AddChild(new ForgeEntity { Networked = true });
		b.AddChild(new ForgeEntity { Networked = true });
		level.AddChild(a);
		level.AddChild(b);
		branch.AddChild(level);
		return (a, b);
	}

	// Below the ranges the system sets aside for itself, where a port may not bind: from 32768 on Linux, and from 49152
	// on Windows, which excludes whole blocks of it.
	private static int TestPort()
	{
		return 20000 + (int)(GD.Randi() % 12000);
	}

	private static string Uid(string path)
	{
		return ResourceUid.IdToText(ResourceLoader.GetResourceUid(path));
	}

	private static ForgeNetworkManifest SceneManifest(string scenePath)
	{
		var manifest = new ForgeNetworkManifest();
		manifest.Scenes.Add(Uid(scenePath));
		return manifest;
	}

	private static ForgeNetworkManifest Manifest(params string[] effectPaths)
	{
		var manifest = new ForgeNetworkManifest();

		foreach (string path in effectPaths)
		{
			manifest.Effects.Add(Uid(path));
		}

		return manifest;
	}

	// A server and a client in one tree, each on its own multiplayer branch, on a port of their own.
	private sealed class Pair : IDisposable
	{
		public ForgeNetworkSession Server { get; }

		public ForgeNetworkSession Client { get; }

		public ForgeEntity Player { get; }

		public SimulationClock ClientClock { get; }

		public Node ServerBranch { get; }

		public Node ClientBranch { get; }

		public Pair(ForgeNetworkManifest serverManifest, ForgeNetworkManifest clientManifest, bool withPlayer = true)
		{
			int port = TestPort();
			string suffix = Guid.NewGuid().ToString("N");

			ServerBranch = new Node { Name = $"Server {suffix}" };
			ClientBranch = new Node { Name = $"Client {suffix}" };
			Root.AddChild(ServerBranch);
			Root.AddChild(ClientBranch);
			Tree.SetMultiplayer(new SceneMultiplayer(), ServerBranch.GetPath());
			Tree.SetMultiplayer(new SceneMultiplayer(), ClientBranch.GetPath());

			Player = new ForgeEntity();
			ClientBranch.AddChild(Player);
			Player.Input = new EntityInput(ForgeSettings.GetInputActionSet());

			// Both ends share the process's session clock, on which a client can't run ahead. The server runs on it, as
			// every entity's tick begins at it, and the client keeps a clock of its own that the test holds a few ticks
			// ahead: what a real client's lead looks like from here. A client with a clock of its own doesn't steer:
			// steering would chase the gap between physics time and wall time that a test runner's bursts open up.
			ClientClock = new SimulationClock(
				ForgeManagers.Instance.Clock.TickRate,
				ForgeManagers.Instance.Clock.Tick + Lead);

			Server = new ForgeNetworkSession
			{
				Port = port,
				Manifest = serverManifest,
				MaxClients = 2,
			};

			Client = new ForgeNetworkSession
			{
				Port = port,
				Manifest = clientManifest,
				LocalPlayer = withPlayer ? Player : null,
				SpawnParent = ClientBranch,
				ClockOverride = ClientClock,
			};
			ServerBranch.AddChild(Server);
			ClientBranch.AddChild(Client);
			Tree.PhysicsFrame += OnPhysicsFrame;
		}

		// Lets the game run until the condition holds or the frames run out.
		public static async Task RunUntil(Func<bool> condition, int frames, Action? eachFrame = null)
		{
			for (int frame = 0; frame < frames && !condition(); frame++)
			{
				eachFrame?.Invoke();
				await Root.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);
			}
		}

		// Hosts, joins and waits for the handshake.
		public async Task Connect()
		{
			bool connected = false;
			Client.Connected += () => connected = true;
			Server.Host().Should().Be(Error.Ok);
			Client.Join("127.0.0.1").Should().Be(Error.Ok);

			await RunUntil(() => connected, frames: 600);

			connected.Should().BeTrue("the handshake should complete over the local link");
		}

		public string Diagnostics()
		{
			NetworkPeer? peer = Server.Session?.Peers.Count > 0
				? Server.Session.Peers[0]
				: null;

			InputPlayoutBuffer? input = peer?.Input;

			return $"\n[clock {ForgeManagers.Instance.Clock.Tick}, client clock {ClientClock.Tick}, client " +
				$"{Client.State} window newest {Client.Session?.Input.NewestTick} acked " +
				$"{Client.Session?.AckedInputTick} ahead {Client.TicksAhead:0.0} rtt {Client.RoundTripTime * 1000:0} " +
				$"ms; server newest {input?.NewestTick} last taken {input?.LastTakenTick} depth {input?.Depth} " +
				$"received {input?.ReceivedTicks} lost {input?.LostTicks} late {input?.LateTicks} " +
				$"malformed {peer?.MalformedPackets} session malformed {Server.Session?.MalformedPackets} " +
				$"pings {peer?.Pings} heard {peer?.LastHeardAt:0.00}]";
		}

		public void Dispose()
		{
			Tree.PhysicsFrame -= OnPhysicsFrame;
			Client.Leave();
			Server.Leave();
			Tree.SetMultiplayer(null, ServerBranch.GetPath());
			Tree.SetMultiplayer(null, ClientBranch.GetPath());
			ServerBranch.Free();
			ClientBranch.Free();
		}

		// At the start of every physics step, before any node runs it, the client's clock is put the lead ahead of the
		// session clock's tick, the one the step is about to simulate.
		private void OnPhysicsFrame()
		{
			ClientClock.Set(ForgeManagers.Instance.Clock.Tick + Lead);
		}
	}
}
