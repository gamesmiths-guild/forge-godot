// Copyright © Gamesmiths Guild.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Networking;
using Gamesmiths.Forge.Godot.Nodes;
using Gamesmiths.Forge.Godot.Resources;
using Gamesmiths.Forge.Networking;
using GdUnit4;
using Godot;

namespace Gamesmiths.Forge.Godot.Tests.Nodes;

/// <summary>
/// A <see cref="ForgeNetworkSession"/> hosting and another joining it over ENet on this machine, in one process
/// through two multiplayer branches: the handshake accepts a matching client and refuses a mismatched one, the
/// client's input reaches the server's playout buffer, and leaving is seen on both ends.
/// </summary>
[TestSuite]
public class ForgeNetworkSessionTests
{
	private const string Burning = "res://forge_samples/demos/04_rewind/effects/burning.tres";

	// How many ticks the test's client runs ahead of the test's server: enough to cover the bursts of physics steps
	// a process frame can hold, since ENet only moves packets between process frames.
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
		pair.Server.PeerJoined += id => joined.Add(id);
		pair.Client.Connected += () => connected = true;
		pair.Server.InputTaken += (_, command, delivery) => deliveries.Add((delivery, command.Frame.Held));

		pair.Server.Host().Should().Be(Error.Ok);
		pair.Client.Join("127.0.0.1").Should().Be(Error.Ok);
		pair.Server.Mode.Should().Be(NetworkMode.ListenServer);
		pair.Client.Mode.Should().Be(NetworkMode.Client);

		await pair.RunUntil(() => connected, frames: 600);

		connected.Should().BeTrue("the handshake should complete over the local link");
		joined.Should().ContainSingle();
		pair.Server.Session!.Peers.Should().ContainSingle();
		pair.Client.Session!.SessionSeed.Should().Be(pair.Server.Session.SessionSeed);

		// A second of play: the client sends a held action every tick, and the server takes it tick by tick.
		int before = deliveries.Count;
		await pair.RunUntil(
			() => deliveries.Count >= before + 60,
			frames: 1200,
			eachFrame: () => pair.Player.Input!.Submit(new InputFrame { Held = 1 }));

		string summary = string.Join(" ", deliveries.TakeLast(30).Select(x => $"{x.Delivery}/{x.Held}")) + pair.Diagnostics();
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
		pair.Client.TicksAhead.Should().BeInRange(Lead - 3, Lead + 3, "the client sees the server's clock behind its own");
		pair.Client.RoundTripTime.Should().BeLessThan(0.1);
		pair.Server.Session.Peers[0].MalformedPackets.Should().Be(0);

		bool left = false;
		pair.Server.PeerLeft += _ => left = true;
		pair.Client.Leave();

		await pair.RunUntil(() => left, frames: 120);

		left.Should().BeTrue();
		pair.Client.Mode.Should().Be(NetworkMode.Offline);
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
		int port = 30000 + (int)(GD.Randi() % 20000);

		GodotTransport.CreateServerPeer(port, 2, out ENetMultiplayerPeer serverPeer).Should().Be(Error.Ok);
		serverApi.MultiplayerPeer = serverPeer;
		GodotTransport.CreateClientPeer("127.0.0.1", port, out ENetMultiplayerPeer clientPeer).Should().Be(Error.Ok);
		clientApi.MultiplayerPeer = clientPeer;

		using var server = new GodotTransport(serverApi, isServer: true);
		using var client = new GodotTransport(clientApi, isServer: false);
		// [channel, reliable]
		var toServer = new int[3, 2];
		var toClient = new int[3, 2];
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
			return $"ch0 r{counts[0, 1]} u{counts[0, 0]}, ch1 r{counts[1, 1]} u{counts[1, 0]}, ch2 r{counts[2, 1]} u{counts[2, 0]}";
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
		string counts = $"{hosts}; sent {sentFrames} frames; to server {Table(toServer)}; to client {Table(toClient)}; " +
			$"client sent {client.PacketsSent} with {client.SendErrors} errors, " +
			$"server sent {server.PacketsSent} with {server.SendErrors} errors; " +
			$"unreliable ch0 arrivals per 50 frames {buckets}; client peer {throttle}";
		sentFrames.Should().BeGreaterThan(100, counts);

		for (int channel = 0; channel < 3; channel++)
		{
			for (int reliable = 0; reliable < 2; reliable++)
			{
				toServer[channel, reliable].Should().BeGreaterThanOrEqualTo(sentFrames * 9 / 10 - 1, counts);
				toClient[channel, reliable].Should().BeGreaterThanOrEqualTo(sentFrames * 9 / 10 - 1, counts);
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

		await pair.RunUntil(() => refusal is not null, frames: 600);

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
		registry.Get<Effects.EffectData>(AssetKind.Effect, 0).Name.Should().NotBeEmpty();
		registry.Hash.Should().Be(Manifest(Burning).BuildRegistry().Hash, "the same manifest hashes the same");
		registry.Hash.Should().NotBe(new ForgeNetworkManifest().BuildRegistry().Hash);
	}

	private static string Uid(string path)
	{
		return ResourceUid.IdToText(ResourceLoader.GetResourceUid(path));
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
		private readonly Node _serverBranch;
		private readonly Node _clientBranch;

		public Pair(ForgeNetworkManifest serverManifest, ForgeNetworkManifest clientManifest)
		{
			int port = 30000 + (int)(GD.Randi() % 20000);
			string suffix = Guid.NewGuid().ToString("N");

			_serverBranch = new Node { Name = $"Server {suffix}" };
			_clientBranch = new Node { Name = $"Client {suffix}" };
			Root.AddChild(_serverBranch);
			Root.AddChild(_clientBranch);
			Tree.SetMultiplayer(new SceneMultiplayer(), _serverBranch.GetPath());
			Tree.SetMultiplayer(new SceneMultiplayer(), _clientBranch.GetPath());

			Player = new ForgeEntity();
			_clientBranch.AddChild(Player);
			Player.Input = new EntityInput(ForgeSettings.GetInputActionSet());

			// Both ends share the process's session clock, on which a client can't run ahead, so the server keeps a
			// clock of its own that the test holds a few ticks behind: what a real client's lead looks like from here.
			ServerClock = new SimulationClock(ForgeManagers.Instance.Clock.TickRate, ForgeManagers.Instance.Clock.Tick - Lead);
			Server = new ForgeNetworkSession { Port = port, Manifest = serverManifest, MaxClients = 2, ClockOverride = ServerClock };
			// The client reads the session clock itself but doesn't steer it: steering would chase the gap between
			// physics time and wall time that a test runner's bursts open up, and move the shared clock under both.
			Client = new ForgeNetworkSession
			{
				Port = port,
				Manifest = clientManifest,
				LocalPlayer = Player,
				ClockOverride = ForgeManagers.Instance.Clock,
			};
			_serverBranch.AddChild(Server);
			_clientBranch.AddChild(Client);
			Tree.PhysicsFrame += OnPhysicsFrame;
		}

		public ForgeNetworkSession Server { get; }

		public ForgeNetworkSession Client { get; }

		public ForgeEntity Player { get; }

		public SimulationClock ServerClock { get; }

		public string Diagnostics()
		{
			NetworkPeer? peer = Server.Session?.Peers.FirstOrDefault();
			InputPlayoutBuffer? input = peer?.Input;

			return $"\n[clock {ForgeManagers.Instance.Clock.Tick}, server clock {ServerClock.Tick}, client {Client.State} " +
				$"window newest {Client.Session?.Input.NewestTick} acked {Client.Session?.AckedInputTick} " +
				$"ahead {Client.TicksAhead:0.0} rtt {Client.RoundTripTime * 1000:0} ms; " +
				$"server newest {input?.NewestTick} last taken {input?.LastTakenTick} depth {input?.Depth} " +
				$"received {input?.ReceivedTicks} lost {input?.LostTicks} late {input?.LateTicks} " +
				$"malformed {peer?.MalformedPackets} session malformed {Server.Session?.MalformedPackets} " +
				$"pings {peer?.Pings} heard {peer?.LastHeardAt:0.00}]";
		}

		// Lets the game run until the condition holds or the frames run out.
		public async Task RunUntil(Func<bool> condition, int frames, Action? eachFrame = null)
		{
			for (int frame = 0; frame < frames && !condition(); frame++)
			{
				eachFrame?.Invoke();
				await Root.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);
			}
		}

		// At the start of every physics step, before any node runs it, the server's clock is put the lead behind the
		// session clock's tick, the one the step is about to simulate.
		private void OnPhysicsFrame()
		{
			ServerClock.Set(ForgeManagers.Instance.Clock.Tick - Lead);
		}

		public void Dispose()
		{
			Tree.PhysicsFrame -= OnPhysicsFrame;
			Client.Leave();
			Server.Leave();
			Tree.SetMultiplayer(null, _serverBranch.GetPath());
			Tree.SetMultiplayer(null, _clientBranch.GetPath());
			_serverBranch.Free();
			_clientBranch.Free();
		}
	}
}
