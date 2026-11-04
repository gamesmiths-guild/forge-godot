// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Networking;
using Gamesmiths.Forge.Godot.Resources;
using Gamesmiths.Forge.Networking;
using Godot;

namespace Gamesmiths.Forge.Godot.Nodes;

/// <summary>
/// What this machine is in a session.
/// </summary>
public enum NetworkMode
{
	/// <summary>
	/// Not in a session: the game runs as a listen server with nobody connected, which is ordinary single player.
	/// </summary>
	Offline = 0,

	/// <summary>
	/// The server, with a player on it.
	/// </summary>
	ListenServer = 1,

	/// <summary>
	/// The server, with nothing to show: no presentation runs.
	/// </summary>
	DedicatedServer = 2,

	/// <summary>
	/// A client of a server elsewhere.
	/// </summary>
	Client = 3,
}

/// <summary>
/// Hosts or joins a networked session over Godot's high-level multiplayer, on ENet, and runs the Forge
/// <see cref="NetworkSession"/> on top of it every physics step: the handshake, the clock, and the players' input up
/// to the server. One per scene that plays online.
/// </summary>
/// <remarks>
/// <para>On a server its physics step runs with the input collectors', before <see cref="ForgeBootstrap"/> begins the
/// entities' ticks, so a command handed to an entity is the one that entity's tick reads. On a client it runs after
/// every tick has begun, a world history's included, and before any entity's, so it sends the input frame its tick
/// reads before simulating the tick. What the server does with a command, which entity it drives, is the game's until milestone 8
/// maps peers to entities: subscribe to <see cref="InputTaken"/>.</para>
/// <para>A client keeps its clock about a round trip ahead of the server's, from the session's clock sync, so its input
/// arrives before the server needs it. In this milestone the clock jumps to the target when it's off by more than
/// <see cref="LeadToleranceTicks"/>; milestone 11 steers it smoothly.</para>
/// <para>The <see cref="LatencyMs"/>, <see cref="JitterMs"/> and <see cref="LossPercent"/> exports condition this
/// machine's outgoing packets, to try the game under a poor link without leaving the desk.</para>
/// </remarks>
[GlobalClass]
public partial class ForgeNetworkSession : Node
{
	/// <summary>
	/// The group every <see cref="ForgeNetworkSession"/> joins, so the overlay can find one.
	/// </summary>
	public const string Group = "forge_network_session";

	private const int DefaultPort = 7777;

	// A server hands commands to entities before their ticks begin, with the input collectors; a client sends after
	// every tick has begun, a world history's at int.MinValue + 2 included.
	private const int ServerPriority = int.MinValue;
	private const int ClientPriority = int.MinValue + 3;

	private GodotTransport? _transport;
	private ConditionedTransport? _conditioned;
	private double _lastStatsAt;
	private long _lastBytesSent;
	private long _lastBytesReceived;

	/// <summary>
	/// Raised on the server when a client joins, after its handshake.
	/// </summary>
	/// <param name="peerId">The client's peer id.</param>
	[Signal]
	public delegate void PeerJoinedEventHandler(long peerId);

	/// <summary>
	/// Raised on the server when a client that had joined leaves.
	/// </summary>
	/// <param name="peerId">The client's peer id.</param>
	[Signal]
	public delegate void PeerLeftEventHandler(long peerId);

	/// <summary>
	/// Raised on a client when the server accepts it.
	/// </summary>
	[Signal]
	public delegate void ConnectedEventHandler();

	/// <summary>
	/// Raised on a client when the server refuses it, with the <see cref="RefusalReason"/>.
	/// </summary>
	/// <param name="reason">Why, as a <see cref="RefusalReason"/>.</param>
	[Signal]
	public delegate void RefusedEventHandler(int reason);

	/// <summary>
	/// Raised on a client when its connection ends, by either side.
	/// </summary>
	[Signal]
	public delegate void DisconnectedEventHandler();

	/// <summary>
	/// Raised on the server every physics step for every joined client, with the command the tick runs for it and how
	/// it arrived. The game feeds the frame to the entity the client controls and raises the command's events on it.
	/// </summary>
	public event Action<NetworkPeer, InputCommand, InputDelivery>? InputTaken;

	/// <summary>
	/// Gets a value indicating whether the game was started with <c>-- --dedicated</c>, asking for a dedicated server.
	/// The game decides what to do with it, usually <see cref="Host"/> with <c>dedicated</c> set.
	/// </summary>
	public static bool DedicatedRequested =>
		Array.Exists(OS.GetCmdlineUserArgs(), x => string.Equals(x, "--dedicated", StringComparison.Ordinal));

	/// <summary>
	/// Gets or sets the port to host on or join.
	/// </summary>
	[Export]
	public int Port { get; set; } = DefaultPort;

	/// <summary>
	/// Gets or sets how many clients a server accepts.
	/// </summary>
	[Export]
	public int MaxClients { get; set; } = 4;

	/// <summary>
	/// Gets or sets the asset manifest both ends build their registry from. Left empty, the project's
	/// (<see cref="ForgeSettings.NetworkManifestSetting"/>) is used.
	/// </summary>
	[Export]
	public ForgeNetworkManifest? Manifest { get; set; }

	/// <summary>
	/// Gets or sets the entity this machine's player controls, whose input a client sends up each tick.
	/// </summary>
	[Export]
	public ForgeEntity? LocalPlayer { get; set; }

	/// <summary>
	/// Gets or sets how many ticks beyond a round trip a client runs ahead of the server, to absorb jitter.
	/// </summary>
	[Export]
	public int LeadMarginTicks { get; set; } = 2;

	/// <summary>
	/// Gets or sets how far a client's clock may drift from its target before it jumps to it.
	/// </summary>
	[Export]
	public int LeadToleranceTicks { get; set; } = 2;

	/// <summary>
	/// Gets or sets the latency the conditioner adds to this machine's outgoing packets, one way, in milliseconds.
	/// </summary>
	[ExportGroup("Network Conditioner")]
	[Export(PropertyHint.Range, "0,1000,1")]
	public int LatencyMs { get; set; }

	/// <summary>
	/// Gets or sets how much the added latency varies, in milliseconds.
	/// </summary>
	[Export(PropertyHint.Range, "0,500,1")]
	public int JitterMs { get; set; }

	/// <summary>
	/// Gets or sets the share of this machine's outgoing unreliable packets the conditioner loses, in percent.
	/// </summary>
	[Export(PropertyHint.Range, "0,100,0.5")]
	public float LossPercent { get; set; }

	/// <summary>
	/// Gets what this machine is in the session.
	/// </summary>
	public NetworkMode Mode { get; private set; }

	/// <summary>
	/// Gets the Forge session, while hosting or joined.
	/// </summary>
	public NetworkSession? Session { get; private set; }

	/// <summary>
	/// Gets the session clock.
	/// </summary>
	public SimulationClock Clock => ClockOverride ?? ForgeManagers.Instance.Clock;

	/// <summary>
	/// Gets a value indicating whether this machine is the server, listen or dedicated.
	/// </summary>
	public bool IsServer => Mode is NetworkMode.ListenServer or NetworkMode.DedicatedServer;

	/// <summary>
	/// Gets where a client stands with the server; <see cref="SessionState.Ready"/> on a server.
	/// </summary>
	public SessionState State => Session?.State ?? SessionState.Disconnected;

	/// <summary>
	/// Gets a client's round trip to the server, in seconds, or zero.
	/// </summary>
	public double RoundTripTime => Session?.ClockSync.RoundTripTime ?? 0;

	/// <summary>
	/// Gets a client's estimate of the server's tick right now, or the clock's tick on a server.
	/// </summary>
	public double ServerTick => IsServer || Session is null ? Clock.Tick : Session.ClockSync.EstimateServerTick(Now());

	/// <summary>
	/// Gets how many ticks a client's clock runs ahead of its estimate of the server's.
	/// </summary>
	public double TicksAhead => IsServer ? 0 : Clock.Tick - ServerTick;

	/// <summary>
	/// Gets how many of a client's input ticks the server held ahead of the one it last ran, as of the last pong.
	/// </summary>
	public int ServerInputDepth => Session?.ServerInputDepth ?? 0;

	/// <summary>
	/// Gets how many unreliable packets the conditioner has lost on this machine.
	/// </summary>
	public long PacketsLost => _conditioned?.LostPackets ?? 0;

	/// <summary>
	/// Gets the bytes sent per second over the last second, payloads only.
	/// </summary>
	public double BytesSentPerSecond { get; private set; }

	/// <summary>
	/// Gets the bytes received per second over the last second, payloads only.
	/// </summary>
	public double BytesReceivedPerSecond { get; private set; }

	// A clock for this node to read instead of the session clock, for tests that run a server and a client in one
	// process: on one clock a client can never run ahead of the server. Set before hosting or joining. A node with one
	// doesn't steer the session clock either, which the same clock also stands in the way of.
	internal SimulationClock? ClockOverride { get; set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="ForgeNetworkSession"/> class.
	/// </summary>
	public ForgeNetworkSession()
	{
		ProcessPhysicsPriority = ClientPriority;
		ProcessMode = ProcessModeEnum.Always;
	}

	/// <summary>
	/// Hosts a session on <see cref="Port"/>.
	/// </summary>
	/// <param name="dedicated">Whether this machine shows nothing, so the presentation passes are skipped.</param>
	/// <returns><see cref="Error.Ok"/>, or why the server couldn't be created.</returns>
	public Error Host(bool dedicated = false)
	{
		Leave();

		Error error = GodotTransport.CreateServerPeer(Port, MaxClients, out ENetMultiplayerPeer peer);

		if (error != Error.Ok)
		{
			GD.PushError($"Forge: {Name} couldn't host on port {Port}: {error}.");
			return error;
		}

		SceneMultiplayer multiplayer = SceneMultiplayerOf(this);
		multiplayer.MultiplayerPeer = peer;
		_transport = new GodotTransport(multiplayer, isServer: true);
		ulong seed = ((ulong)GD.Randi() << 32) | GD.Randi();
		Session = new NetworkSession(
			Conditioned(_transport),
			Clock,
			BuildRegistry(),
			ForgeManagers.Instance.TagsManager,
			ForgeSettings.GetInputActionSet(),
			seed,
			MaxClients);
		Session.PeerJoined += OnPeerJoined;
		Session.PeerLeft += OnPeerLeft;
		ProcessPhysicsPriority = ServerPriority;
		Mode = dedicated ? NetworkMode.DedicatedServer : NetworkMode.ListenServer;
		ForgeManagers.Instance.HasViewer = !dedicated;

		return Error.Ok;
	}

	/// <summary>
	/// Joins a session at an address, on <see cref="Port"/>.
	/// </summary>
	/// <param name="address">The server's address.</param>
	/// <returns><see cref="Error.Ok"/>, or why the client couldn't be created. Whether the connection and the
	/// handshake succeed comes later, through <see cref="Connected"/>, <see cref="Refused"/> or
	/// <see cref="Disconnected"/>.</returns>
	public Error Join(string address)
	{
		Leave();

		Error error = GodotTransport.CreateClientPeer(address, Port, out ENetMultiplayerPeer peer);

		if (error != Error.Ok)
		{
			GD.PushError($"Forge: {Name} couldn't connect to {address}:{Port}: {error}.");
			return error;
		}

		SceneMultiplayer multiplayer = SceneMultiplayerOf(this);
		multiplayer.MultiplayerPeer = peer;
		_transport = new GodotTransport(multiplayer, isServer: false);
		Session = new NetworkSession(
			Conditioned(_transport),
			Clock,
			BuildRegistry(),
			ForgeManagers.Instance.TagsManager,
			ForgeSettings.GetInputActionSet());
		ProcessPhysicsPriority = ClientPriority;
		Mode = NetworkMode.Client;

		return Error.Ok;
	}

	/// <summary>
	/// Leaves the session: a client disconnects, a server disconnects everyone and stops listening. Offline after.
	/// </summary>
	public void Leave()
	{
		if (Session is null)
		{
			return;
		}

		Session.PeerJoined -= OnPeerJoined;
		Session.PeerLeft -= OnPeerLeft;
		Session.Disconnect();
		_transport?.Dispose();

		SceneMultiplayer multiplayer = SceneMultiplayerOf(this);
		multiplayer.MultiplayerPeer?.Close();
		multiplayer.MultiplayerPeer = new OfflineMultiplayerPeer();

		bool wasClient = Mode == NetworkMode.Client;
		Session = null;
		_transport = null;
		_conditioned = null;
		Mode = NetworkMode.Offline;
		ForgeManagers.Instance.HasViewer = true;

		if (wasClient)
		{
			EmitSignal(SignalName.Disconnected);
		}
	}

	/// <inheritdoc/>
	public override void _Ready()
	{
		base._Ready();
		AddToGroup(Group);
	}

	/// <inheritdoc/>
	public override void _ExitTree()
	{
		base._ExitTree();
		Leave();
	}

	/// <inheritdoc/>
	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);

		if (Session is null)
		{
			return;
		}

		double now = Now();
		SessionState before = Session.State;

		if (_conditioned is not null)
		{
			_conditioned.Conditions = new NetworkConditions(LatencyMs / 1000.0, JitterMs / 1000.0, LossPercent / 100f);
			_conditioned.Update(now);
		}

		Session.Update(now);
		UpdateStats(now);

		if (IsServer)
		{
			TakeInput();
			return;
		}

		ReportClientState(before);

		if (Session.State != SessionState.Ready)
		{
			return;
		}

		SteerClock(now);

		if (LocalPlayer?.Input is EntityInput input)
		{
			Session.SendInput(new InputCommand(Clock.Tick, input.Current));
		}
	}

	private static SceneMultiplayer SceneMultiplayerOf(Node node)
	{
		return node.Multiplayer as SceneMultiplayer
			?? throw new InvalidOperationException("Forge networking needs Godot's SceneMultiplayer.");
	}

	private static double Now()
	{
		return Time.GetTicksUsec() / 1_000_000.0;
	}

	private NetworkAssetRegistry BuildRegistry()
	{
		ForgeNetworkManifest? manifest = Manifest ?? ForgeNetworkManifest.LoadProjectManifest();

		if (manifest is not null)
		{
			return manifest.BuildRegistry();
		}

		GD.PushWarning(
			$"Forge: the project has no network manifest at {ForgeSettings.GetNetworkManifestPath()}, so the session " +
			"knows no assets. Generate one from Project, Tools, Forge.");

		var registry = new NetworkAssetRegistry();
		registry.Seal();
		return registry;
	}

	private ConditionedTransport Conditioned(GodotTransport transport)
	{
		_conditioned = new ConditionedTransport(
			transport,
			new NetworkConditions(LatencyMs / 1000.0, JitterMs / 1000.0, LossPercent / 100f),
			new DeterministicRandom(((ulong)GD.Randi() << 32) | GD.Randi()));

		return _conditioned;
	}

	private void TakeInput()
	{
		foreach (NetworkPeer peer in Session!.Peers)
		{
			InputDelivery delivery = peer.Input.Take(Clock.Tick, out InputCommand command);
			InputTaken?.Invoke(peer, command, delivery);
		}
	}

	private void ReportClientState(SessionState before)
	{
		SessionState after = Session!.State;

		if (before == after)
		{
			return;
		}

		switch (after)
		{
			case SessionState.Ready:
				EmitSignal(SignalName.Connected);
				break;
			case SessionState.Refused:
				EmitSignal(SignalName.Refused, (int)(Session.Refusal ?? 0));
				Leave();
				break;
			case SessionState.Disconnected:
				Leave();
				break;
		}
	}

	private void SteerClock(double now)
	{
		if (!Session!.ClockSync.HasEstimate || ClockOverride is not null)
		{
			return;
		}

		long target = Session.ClockSync.TargetClientTick(now, LeadMarginTicks);

		if (Math.Abs(target - Clock.Tick) > LeadToleranceTicks)
		{
			ForgeManagers.Instance.SetNextTick(target);
		}
	}

	private void UpdateStats(double now)
	{
		if (_transport is null || now - _lastStatsAt < 1)
		{
			return;
		}

		double seconds = Math.Max(now - _lastStatsAt, 1e-3);
		BytesSentPerSecond = (_transport.BytesSent - _lastBytesSent) / seconds;
		BytesReceivedPerSecond = (_transport.BytesReceived - _lastBytesReceived) / seconds;
		_lastBytesSent = _transport.BytesSent;
		_lastBytesReceived = _transport.BytesReceived;
		_lastStatsAt = now;
	}

	private void OnPeerJoined(NetworkPeer peer)
	{
		EmitSignal(SignalName.PeerJoined, (long)peer.Id.Value);
	}

	private void OnPeerLeft(NetworkPeer peer)
	{
		EmitSignal(SignalName.PeerLeft, (long)peer.Id.Value);
	}
}
