// Copyright © Gamesmiths Guild.

using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Networking;
using Gamesmiths.Forge.Godot.Resources;
using Gamesmiths.Forge.Networking;
using Gamesmiths.Forge.Networking.Replication;
using Godot;
using NumericsQuaternion = System.Numerics.Quaternion;
using NumericsVector3 = System.Numerics.Vector3;

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
/// <see cref="NetworkSession"/> on top of it every physics step: the handshake, the clock, the players' input up to the
/// server, and the world's state down to every client. One per scene that plays online.
/// </summary>
/// <remarks>
/// <para>On a server its physics step runs with the input collectors', before <see cref="ForgeBootstrap"/> begins the
/// entities' ticks: it sends the state the last step left, then hands each client's command to the entities the client
/// owns, so the command is the one their tick reads. On a client it runs after every tick has begun, a world history's
/// included, and before any entity's, so it sends the input frame its tick reads before simulating the tick.</para>
/// <para>The server replicates every <see cref="ForgeEntity.Networked"/> entity under the same multiplayer branch: the
/// ones in the tree as it starts hosting as placed, which every client's level has too, and the ones that enter later
/// as spawned, from their scene, which the network manifest lists. A client binds its placed ones as it joins, makes
/// the spawned ones under <see cref="SpawnParent"/>, and shows them <see cref="InterpolationDelayMs"/> behind the
/// server: their bodies, and everything else the server sent of them at the same moment, so an animation starts as its
/// body moves.</para>
/// <para>A client keeps its clock about a round trip ahead of the server's, from the session's clock sync, so its input
/// arrives before the server needs it. A <see cref="ClockDilation"/> steers it a tick at a time: a physics step
/// simulates two ticks to catch up or none to wait, and the clock jumps only when it's far off.</para>
/// <para>A client predicts its own player, the networked entity the server spawns for its peer: the entity is made live
/// rather than a mirror, and the session advances it through each tick the clock simulates, ahead of the server, so it
/// answers this machine's input at once. Each state packet puts it right where the server's word for its tick differs,
/// and its movement component eases what a correction moved.</para>
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

	private static readonly List<ForgeNetworkSession> _active = [];

	private readonly Dictionary<ForgeEntity, Node> _spawnedRoots = [];
	private readonly Dictionary<string, EffectContextCodec> _effectContexts = [];
	private readonly ReplicationHost _host;
	private readonly ClockDilation _dilation = new();
	private readonly Dictionary<IForgeEntity, BodyState> _shownBefore = [];
	private GodotTransport? _transport;
	private ConditionedTransport? _conditioned;
	private InterpolationClock? _shown;
	private double _lastStatsAt;
	private long _lastBytesSent;
	private long _lastBytesReceived;
	private bool _exiting;

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
	/// Raised on a client when its connection ends, by either side, unless the session node is leaving the tree. The
	/// placed entities it bound can't simulate again (see <see cref="Join"/>), so a game reloads its level here.
	/// </summary>
	[Signal]
	public delegate void DisconnectedEventHandler();

	/// <summary>
	/// Raised when <see cref="LocalPlayer"/> changes: when the entity this machine's player owns is spawned, on the
	/// server or as a client's mirror, and when it goes.
	/// </summary>
	/// <param name="player">The <see cref="ForgeEntity"/>, or <see langword="null"/>.</param>
	[Signal]
	public delegate void LocalPlayerChangedEventHandler(Node? player);

	/// <summary>
	/// Raised on the server every physics step for every joined client, with the command the tick runs for it and how
	/// it arrived, after the command was handed to the entities the client owns.
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
	/// Gets or sets how many clients a server accepts, or 0 for as many as ENet allows.
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
	/// Gets or sets the entity this machine's player controls, whose input a client sends up each tick. The session
	/// sets it to the networked entity with an input that this machine's peer owns, as it's spawned, and clears it on
	/// leaving.
	/// </summary>
	[Export]
	public ForgeEntity? LocalPlayer { get; set; }

	/// <summary>
	/// Gets or sets the node a client adds the entities the server spawns to. Left empty, the current scene.
	/// </summary>
	[Export]
	public Node? SpawnParent { get; set; }

	/// <summary>
	/// Gets or sets how far from every entity a client owns another entity can be and still reach that client, in
	/// units, or 0 for every entity everywhere. Read as the server starts hosting.
	/// </summary>
	[Export(PropertyHint.Range, "0,1000,0.5,or_greater")]
	public float RelevancyRadius { get; set; }

	/// <summary>
	/// Gets or sets how far behind the newest state it holds a client shows the server's entities, in milliseconds,
	/// so it has the states on either side of the moment it shows. The newest state is itself about half a round trip
	/// behind the server. Everything else the server sent of them waits to be shown at the same moment.
	/// </summary>
	[Export(PropertyHint.Range, "0,500,1")]
	public int InterpolationDelayMs { get; set; } = 100;

	/// <summary>
	/// Gets or sets how many ticks beyond a round trip a client runs ahead of the server, to absorb jitter.
	/// </summary>
	[Export]
	public int LeadMarginTicks { get; set; } = 2;

	/// <summary>
	/// Gets or sets how far a client's clock may drift from its target, in ticks, before it's steered toward it.
	/// </summary>
	[Export]
	public int LeadToleranceTicks { get; set; } = 1;

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
	/// Gets this machine's multiplayer peer id in the session: 1 on the server, 0 offline.
	/// </summary>
	public int LocalPeer => Session?.LocalPeer.Value ?? 0;

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
	public double TicksAhead => IsServer || Session?.ClockSync.HasEstimate != true ? 0 : Clock.Tick - ServerTick;

	/// <summary>
	/// Gets how many of a client's input ticks the server held ahead of the one it last ran, as of the last pong.
	/// </summary>
	public int ServerInputDepth => Session?.ServerInputDepth ?? 0;

	/// <summary>
	/// Gets how many unreliable packets the conditioner has lost on this machine.
	/// </summary>
	public long PacketsLost => _conditioned?.LostPackets ?? 0;

	/// <summary>
	/// Gets how many state packets agreed with what a client predicted of its player.
	/// </summary>
	public long Confirmations => Session?.Mirror?.Confirmations ?? 0;

	/// <summary>
	/// Gets how many times a state packet put a client's player right.
	/// </summary>
	public long Corrections => Session?.Mirror?.Corrections ?? 0;

	/// <summary>
	/// Gets how many ticks the last correction rolled back and ran again.
	/// </summary>
	public int LastCorrectionTicks => Session?.Mirror?.LastCorrectionTicks ?? 0;

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

		// Bodies are placed before anything reads them in the frame, a camera following the player among them.
		ProcessPriority = int.MinValue;
		ProcessMode = ProcessModeEnum.Always;
		_host = new ReplicationHost(this);
	}

	/// <summary>
	/// Finds the session a node plays in: the one hosting or joined in the node's multiplayer branch.
	/// </summary>
	/// <param name="node">A node in the tree.</param>
	/// <returns>The session, or <see langword="null"/> offline.</returns>
	public static ForgeNetworkSession? Of(Node node)
	{
		if (_active.Count == 0 || !node.IsInsideTree())
		{
			return null;
		}

		MultiplayerApi multiplayer = node.Multiplayer;

		foreach (ForgeNetworkSession session in _active)
		{
			if (session.Multiplayer == multiplayer)
			{
				return session;
			}
		}

		return null;
	}

	/// <summary>
	/// Says how the custom data of a type that effects are applied with travels, so a client's copy of the effect holds
	/// it too. Both ends register the same ones, before hosting or joining. A client with other keys is refused, and
	/// only the keys are compared.
	/// </summary>
	/// <typeparam name="TData">The type effects are applied with.</typeparam>
	/// <param name="key">What it's known by on both ends.</param>
	/// <param name="write">Writes the data.</param>
	/// <param name="read">Reads the data, following the same sequence of calls as <paramref name="write"/>.</param>
	public void RegisterEffectContext<TData>(string key, Action<BitWriter, TData> write, Func<BitReader, TData> read)
	{
		if (Session is not null)
		{
			GD.PushWarning(
				$"Forge: {Name} is in a session already, so the effect context '{key}' only counts from the next " +
				"one. Register it before hosting or joining.");
		}

		_effectContexts[key] = EffectContextCodec.For(write, read);
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
		Session.InputTaken += OnInputTaken;
		Session.Replication!.BodySource = CaptureBody;
		Session.Replication.DefaultEffectVisibility = ForgeSettings.DefaultEffectVisibility;
		Session.Replication.EffectNotListed += OnEffectNotListed;
		Session.Replication.AbilityNotListed += OnAbilityNotListed;

		if (RelevancyRadius > 0)
		{
			Session.Replication.Relevancy = new DistanceRelevancy(RelevancyRadius);
		}

		ProcessPhysicsPriority = ServerPriority;
		Mode = dedicated ? NetworkMode.DedicatedServer : NetworkMode.ListenServer;
		ForgeManagers.Instance.HasViewer = !dedicated;
		_active.Add(this);

		List<ForgeEntity> placed = PlacedEntities();

		for (int i = 0; i < placed.Count; i++)
		{
			if (placed[i].IsMirror)
			{
				GD.PushWarning(
					$"Forge: {placed[i].GetPath()} was a client's mirror and can't simulate again, so it won't on " +
					"this server. Reload the level after a client session before hosting.");
			}

			placed[i].NetworkId = new NetworkEntityId((uint)i + 1);
			placed[i].SeedFor(Session!.SessionSeed);
			Register(placed[i], -1);
		}

		return Error.Ok;
	}

	/// <summary>
	/// Joins a session at an address, on <see cref="Port"/>. The level's placed networked entities become mirrors right
	/// away, for good: what they ran is torn down, even if the connection then fails.
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
		Session.Mirror!.Host = _host;
		Session.Mirror.HoldsUntilShown = true;
		Session.Mirror.DefaultEffectVisibility = ForgeSettings.DefaultEffectVisibility;
		Session.Mirror.Spawned += OnMirrorSpawned;
		Session.Mirror.Corrected += OnCorrected;
		_shown = new InterpolationClock(Clock);
		ProcessPhysicsPriority = ClientPriority;
		Mode = NetworkMode.Client;
		_active.Add(this);

		// Bound before the session is ready, so the first state the server sends of them finds them.
		List<ForgeEntity> placed = PlacedEntities();

		for (int i = 0; i < placed.Count; i++)
		{
			ForgeEntity entity = placed[i];
			entity.NetworkId = new NetworkEntityId((uint)i + 1);
			entity.IsMirror = true;
			entity.Replicator = this;
			Session.Mirror.BindPlaced(entity);
			ShowOnFrames(entity);
		}

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
		Session.InputTaken -= OnInputTaken;
		_active.Remove(this);

		// The entities the server spawned go; the placed ones stay as the server last had them.
		if (Session.Mirror is ReplicationClient mirror)
		{
			mirror.Spawned -= OnMirrorSpawned;
			mirror.Corrected -= OnCorrected;
			mirror.Clear();
		}

		Session.Disconnect();
		_transport?.Dispose();

		SceneMultiplayer multiplayer = SceneMultiplayerOf(this);
		multiplayer.MultiplayerPeer?.Close();
		multiplayer.MultiplayerPeer = new OfflineMultiplayerPeer();

		bool wasClient = Mode == NetworkMode.Client;
		Session = null;
		_transport = null;
		_conditioned = null;
		_shown = null;
		_lastStatsAt = 0;
		_lastBytesSent = 0;
		_lastBytesReceived = 0;
		BytesSentPerSecond = 0;
		BytesReceivedPerSecond = 0;
		Mode = NetworkMode.Offline;
		ForgeManagers.Instance.HasViewer = true;

		if (LocalPlayer is not null)
		{
			SetLocalPlayer(null);
		}

		if (wasClient && !_exiting)
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
		_exiting = true;
		Leave();
		_exiting = false;
	}

	/// <inheritdoc/>
	public override void _Process(double delta)
	{
		base._Process(delta);

		if (Mode != NetworkMode.Client || Session?.State != SessionState.Ready)
		{
			return;
		}

		// The newest state a client holds is about half a round trip older than the server's tick now, so the server's
		// entities are shown the delay behind that: their bodies, and with them everything else the server sent.
		ReplicationClient mirror = Session.Mirror!;
		double newest = Session.ClockSync.HasEstimate
			? ServerTick - Clock.TicksIn(Session.ClockSync.RoundTripTime / 2)
			: mirror.LastServerTick;
		_shown!.Advance(delta, newest - Clock.TicksIn(InterpolationDelayMs / 1000.0));

		// The bodies first, so what the shown packets set off, a burst where an enemy fell, happens where its body is
		// shown. One spawned in them is placed as it's bound.
		foreach (IForgeEntity entity in mirror.Entities)
		{
			PlaceShownBody(entity);
		}

		mirror.Show(_shown.Tick);
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

		NoteShownBodies();
		Session.Update(now);
		_shownBefore.Clear();
		UpdateStats(now);

		if (IsServer)
		{
			// The tree is as the last step left it, so this is that tick's state.
			Session.Replication!.Send(Clock.Tick - 1);
			Session.TakeInput(Clock.Tick);
			return;
		}

		if (!ReportClientState(before))
		{
			return;
		}

		PredictStep(now);
	}

	// A networked entity entering the game while hosting gets its id and seed before its setup, so the setup and the
	// graphs it starts roll as on the client that predicts the entity.
	internal void OnEntityEntering(ForgeEntity entity)
	{
		if (!IsServer || entity.Replicator is not null)
		{
			return;
		}

		if (SceneIndexOf(InstanceRoot(entity)) < 0)
		{
			GD.PushWarning(
				$"Forge: {entity.GetPath()} is networked and entered the game while hosting, from a scene the " +
				"network manifest doesn't list, so it isn't replicated. Regenerate the manifest (Project, Tools, " +
				"Forge).");
			return;
		}

		entity.NetworkId = Session!.Replication!.AllocateId();
		SeedSpawn(entity, entity.NetworkId);
	}

	internal void OnEntityReady(ForgeEntity entity)
	{
		if (!IsServer || entity.Replicator is not null || !entity.NetworkId.IsValid)
		{
			return;
		}

		Register(entity, SceneIndexOf(InstanceRoot(entity)));
	}

	internal void OnEntityFreed(ForgeEntity entity)
	{
		Session?.Replication?.Unregister(entity);
		_spawnedRoots.Remove(entity);

		if (LocalPlayer == entity && IsInstanceValid(this))
		{
			SetLocalPlayer(null);
		}
	}

	// Whether a scene is one the session spawns on every machine, which a client leaves to the server.
	internal bool IsNetworkedScene(PackedScene scene)
	{
		return SceneIndexOf(scene.ResourcePath) >= 0;
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

	// The root of the scene instance the entity came from: the first node up from it that a scene file made.
	private static Node? InstanceRoot(ForgeEntity entity)
	{
		Node? node = entity;

		while (node is not null && string.IsNullOrEmpty(node.SceneFilePath))
		{
			node = node.GetParent();
		}

		return node;
	}

	private static void CollectNetworked(Node node, MultiplayerApi multiplayer, List<ForgeEntity> entities)
	{
		if (node is ForgeEntity { Networked: true } entity && entity.IsNodeReady() && entity.Multiplayer == multiplayer)
		{
			entities.Add(entity);
		}

		foreach (Node child in node.GetChildren())
		{
			CollectNetworked(child, multiplayer, entities);
		}
	}

	// A client places its bodies on the frame, so they mustn't also be interpolated against physics ticks.
	private static void ShowOnFrames(IForgeEntity entity)
	{
		if (ForgeEntityBridge.TryGetSpatialNode3D(entity, out Node3D? node3D))
		{
			node3D.PhysicsInterpolationMode = PhysicsInterpolationModeEnum.Off;
		}
		else if (ForgeEntityBridge.TryGetSpatialNode2D(entity, out Node2D? node2D))
		{
			node2D.PhysicsInterpolationMode = PhysicsInterpolationModeEnum.Off;
		}
	}

	// A body moved without a mover is read from the node the entity lives on, so the bodies a graph walks replicate
	// too.
	private static BodyState? CaptureBody(IForgeEntity entity)
	{
		if (entity.Mover is IEntityMover mover)
		{
			return mover.CaptureState();
		}

		if (ForgeEntityBridge.TryGetSpatialNode3D(entity, out Node3D? node3D))
		{
			Quaternion rotation = node3D.GlobalBasis.GetRotationQuaternion();
			Vector3 position = node3D.GlobalPosition;
			var body = node3D as CharacterBody3D;
			Vector3 velocity = body?.Velocity ?? Vector3.Zero;

			return new BodyState(
				new NumericsVector3(position.X, position.Y, position.Z),
				new NumericsQuaternion(rotation.X, rotation.Y, rotation.Z, rotation.W),
				new NumericsVector3(velocity.X, velocity.Y, velocity.Z),
				body?.IsOnFloor() == true);
		}

		if (ForgeEntityBridge.TryGetSpatialNode2D(entity, out Node2D? node2D))
		{
			Vector2 position = node2D.GlobalPosition;
			var body = node2D as CharacterBody2D;
			Vector2 velocity = body?.Velocity ?? Vector2.Zero;

			return new BodyState(
				new NumericsVector3(position.X, position.Y, 0f),
				NumericsQuaternion.CreateFromAxisAngle(NumericsVector3.UnitZ, node2D.GlobalRotation),
				new NumericsVector3(velocity.X, velocity.Y, 0f),
				body?.IsOnFloor() == true);
		}

		return null;
	}

	private static void PlaceBody(IForgeEntity entity, in BodyState state)
	{
		// A body the server's rounding can't tell from where it already stands, such as one the scene placed and
		// nothing moved, stays exactly there: rounded, it would lean or sit a step off from where the server has it.
		bool stands = CaptureBody(entity) is BodyState shown
			&& BodyCodec.Quantize(shown with { Velocity = state.Velocity, Grounded = state.Grounded })
				== BodyCodec.Quantize(state);

		if (ForgeEntityBridge.TryGetSpatialNode3D(entity, out Node3D? node3D))
		{
			if (!stands)
			{
				var rotation = new Quaternion(state.Rotation.X, state.Rotation.Y, state.Rotation.Z, state.Rotation.W);
				node3D.GlobalTransform = new Transform3D(
					new Basis(rotation.Normalized()) * Basis.FromScale(node3D.GlobalBasis.Scale),
					new Vector3(state.Position.X, state.Position.Y, state.Position.Z));
			}

			if (node3D is CharacterBody3D body)
			{
				body.Velocity = new Vector3(state.Velocity.X, state.Velocity.Y, state.Velocity.Z);
			}

			return;
		}

		if (ForgeEntityBridge.TryGetSpatialNode2D(entity, out Node2D? node2D))
		{
			if (!stands)
			{
				node2D.GlobalPosition = new Vector2(state.Position.X, state.Position.Y);
				node2D.GlobalRotation = 2f * MathF.Atan2(state.Rotation.Z, state.Rotation.W);
			}

			if (node2D is CharacterBody2D body)
			{
				body.Velocity = new Vector2(state.Velocity.X, state.Velocity.Y);
			}
		}
	}

	private static void OnEffectNotListed(EffectData data)
	{
		GD.PushWarning(
			$"Forge: the effect '{data.Name}' went active on a networked entity, and the network manifest doesn't " +
			"list it, so no client hears of it. Save it as a resource of its own and regenerate the manifest " +
			"(Project, Tools, Forge), or set its Visibility to Server Only.");
	}

	private static void OnAbilityNotListed(AbilityData data)
	{
		GD.PushWarning(
			$"Forge: the ability '{data.Name}' runs a graph clients show something of on a networked entity, and the " +
			"network manifest doesn't list it, so no client shows it. Save it as a resource of its own and " +
			"regenerate the manifest (Project, Tools, Forge).");
	}

	private NetworkAssetRegistry BuildRegistry()
	{
		ForgeNetworkManifest? manifest = Manifest ?? ForgeNetworkManifest.LoadProjectManifest();

		if (manifest is null)
		{
			GD.PushWarning(
				$"Forge: the project has no network manifest at {ForgeSettings.GetNetworkManifestPath()}, so the " +
				"session knows no assets. Generate one from Project, Tools, Forge.");
		}

		return (manifest ?? new ForgeNetworkManifest()).BuildRegistry(_effectContexts);
	}

	private int SceneIndexOf(Node? root)
	{
		return root is null ? -1 : SceneIndexOf(root.SceneFilePath);
	}

	private int SceneIndexOf(string path)
	{
		if (Session is null || string.IsNullOrEmpty(path))
		{
			return -1;
		}

		long uid = ResourceLoader.GetResourceUid(path);
		return uid == ResourceUid.InvalidId ? -1 : Session.Registry.IndexOf(AssetKind.Scene, ResourceUid.IdToText(uid));
	}

	// Every networked entity in the session's multiplayer branch, in the order of their paths, which is the same on
	// every machine that loaded the same scene: their place in it is their id.
	private List<ForgeEntity> PlacedEntities()
	{
		NodePath rootPath = SceneMultiplayerOf(this).RootPath;
		Node root = rootPath.IsEmpty ? GetTree().Root : GetTree().Root.GetNodeOrNull(rootPath) ?? GetTree().Root;
		var entities = new List<ForgeEntity>();
		CollectNetworked(root, Multiplayer, entities);
		entities.Sort((x, y) => string.CompareOrdinal(root.GetPathTo(x).ToString(), root.GetPathTo(y).ToString()));
		return entities;
	}

	// Seeds an entity about to get ready. Its setup rolls at tick 0, since each machine makes it at a tick of its own.
	private void SeedSpawn(ForgeEntity entity, NetworkEntityId id)
	{
		entity.SpawnSeed = ForgeEntity.NetworkSeed(Session!.SessionSeed, id);
		entity.SpawnTick = 0;
	}

	private void Register(ForgeEntity entity, int sceneIndex)
	{
		entity.Replicator = this;
		Session!.Replication!.Register(entity, sceneIndex, new PeerId(entity.NetworkOwner));

		if (entity.NetworkOwner == LocalPeer && entity.Input is not null && LocalPlayer is null)
		{
			SetLocalPlayer(entity);
		}
	}

	private void SetLocalPlayer(ForgeEntity? player)
	{
		LocalPlayer = player;

		if (!_exiting)
		{
			EmitSignal(SignalName.LocalPlayerChanged, player!);
		}
	}

	private void PlaceShownBody(IForgeEntity entity)
	{
		if (_shown is not null && Session?.Mirror?.BodyOf(entity)?.TrySample(_shown.Tick, out BodyState body) == true)
		{
			PlaceBody(entity, body);
		}
	}

	private void OnMirrorSpawned(IForgeEntity entity)
	{
		if (entity is ForgeEntity { IsPredicted: true } predicted)
		{
			predicted.ApplySetup();
		}

		PlaceShownBody(entity);

		if (entity is ForgeEntity { Input: not null } player && player.NetworkOwner == LocalPeer && LocalPlayer is null)
		{
			SetLocalPlayer(player);
		}
	}

	private void OnInputTaken(NetworkPeer peer, InputCommand command, InputDelivery delivery)
	{
		InputTaken?.Invoke(peer, command, delivery);
	}

	private ConditionedTransport Conditioned(GodotTransport transport)
	{
		_conditioned = new ConditionedTransport(
			transport,
			new NetworkConditions(LatencyMs / 1000.0, JitterMs / 1000.0, LossPercent / 100f),
			new DeterministicRandom(((ulong)GD.Randi() << 32) | GD.Randi()));

		return _conditioned;
	}

	// Whether the client is still in the session and ready after reporting: reporting a refusal or a disconnection
	// leaves the session, and a Connected handler may leave it too.
	private bool ReportClientState(SessionState before)
	{
		SessionState after = Session!.State;

		if (before == after)
		{
			return after == SessionState.Ready;
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

		return Mode == NetworkMode.Client && Session!.State == SessionState.Ready;
	}

	// Simulates the ticks the clock's steering gives this step, predicting this client's own entities through each and
	// sending each tick's input, and moves the clock past them. A node with a clock override doesn't steer.
	private void PredictStep(double now)
	{
		_dilation.MarginTicks = LeadMarginTicks;
		_dilation.ToleranceTicks = LeadToleranceTicks;
		DilatedStep step = ClockOverride is null
			? _dilation.Steer(Clock.Tick, Session!.ClockSync, now, Session.ServerInputDepth)
			: new DilatedStep(Clock.Tick, 1);

		// A clock steered back would send ticks older than the window's newest; those go again from the new tick.
		if (Session!.Input.NewestTick > step.FirstTick)
		{
			Session.Input.Clear();
		}

		for (long tick = step.FirstTick; tick < step.NextTick; tick++)
		{
			Session.Mirror!.Predict(tick, Clock.FixedDeltaTime);

			if (LocalPlayer?.Input is EntityInput input)
			{
				Session.SendInput(new InputCommand(tick, input.Current));
			}
		}

		if (ClockOverride is null && step.NextTick != Clock.Tick + 1)
		{
			ForgeManagers.Instance.SetNextTick(step.NextTick);
		}
	}

	// Where the predicted entities' bodies are before the packets arrive, for a correction to ease from.
	private void NoteShownBodies()
	{
		if (Session?.Mirror is not ReplicationClient mirror)
		{
			return;
		}

		foreach (IForgeEntity entity in mirror.Predicted)
		{
			if (entity.Mover is IEntityMover mover)
			{
				_shownBefore[entity] = mover.CaptureState();
			}
		}
	}

	private void OnCorrected(IForgeEntity entity)
	{
		if (!_shownBefore.TryGetValue(entity, out BodyState shown) || entity.Mover is not IEntityMover mover)
		{
			return;
		}

		NumericsVector3 moved = shown.Position - mover.CaptureState().Position;

		switch (mover)
		{
			case ForgeCharacterMovement3D movement:
				movement.EaseCorrection(new Vector3(moved.X, moved.Y, moved.Z));
				break;

			case ForgeCharacterMovement2D movement:
				movement.EaseCorrection(new Vector2(moved.X, moved.Y));
				break;
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

	// Makes the entities the server spawns on a client, and takes away the ones it despawns.
	private sealed class ReplicationHost(ForgeNetworkSession session) : IReplicationHost
	{
		public IForgeEntity? Spawn(NetworkEntityId id, int sceneIndex, PeerId owner)
		{
			return Make(id, sceneIndex, owner, predicted: false, body: null);
		}

		// This client's own entity is made as the scene makes it, where the server's body was as the server registered
		// it, so its setup, which runs once it's bound as predicted, runs where the server's did.
		public IForgeEntity? SpawnPredicted(NetworkEntityId id, int sceneIndex, PeerId owner, BodyState? body)
		{
			return Make(id, sceneIndex, owner, predicted: true, body);
		}

		public void Despawn(IForgeEntity entity)
		{
			// A mirror can free itself here, as a projectile that hit something does, with the server's despawn to
			// come.
			if (entity is not ForgeEntity node || !IsInstanceValid(node))
			{
				return;
			}

			// A placed entity's own scene instance goes, or the node it lives on when it's part of the level itself:
			// the instance holds this session or another networked entity, or is the current scene.
			if (!session._spawnedRoots.Remove(node, out Node? root))
			{
				root = InstanceRoot(node);

				if (root is null
					|| root == session.GetTree().CurrentScene
					|| root.IsAncestorOf(session)
					|| session.PlacedEntities().Exists(x => x != node && root.IsAncestorOf(x)))
				{
					root = node.GetParent() ?? node;
				}
			}

			if (session.LocalPlayer == node)
			{
				session.SetLocalPlayer(null);
			}

			root.QueueFree();
		}

		// A body a scene's root is, placed in global space under the parent it's about to join.
		private static void PlaceBefore(Node instance, IForgeEntity entity, Node parent, in BodyState state)
		{
			if (ForgeEntityBridge.TryGetSpatialNode3D(entity, out Node3D? node3D) && node3D == instance)
			{
				var rotation = new Quaternion(state.Rotation.X, state.Rotation.Y, state.Rotation.Z, state.Rotation.W);
				var global = new Transform3D(
					new Basis(rotation.Normalized()) * Basis.FromScale(node3D.Basis.Scale),
					new Vector3(state.Position.X, state.Position.Y, state.Position.Z));
				node3D.Transform = parent is Node3D parent3D
					? parent3D.GlobalTransform.AffineInverse() * global
					: global;
			}
			else if (ForgeEntityBridge.TryGetSpatialNode2D(entity, out Node2D? node2D) && node2D == instance)
			{
				var global = new Transform2D(
					2f * MathF.Atan2(state.Rotation.Z, state.Rotation.W),
					new Vector2(state.Position.X, state.Position.Y));
				node2D.Transform = parent is Node2D parent2D
					? parent2D.GlobalTransform.AffineInverse() * global
					: global;
			}
		}

		private ForgeEntity? Make(NetworkEntityId id, int sceneIndex, PeerId owner, bool predicted, BodyState? body)
		{
			if (!session.Session!.Registry.TryGet(AssetKind.Scene, sceneIndex, out PackedScene? scene) || scene is null)
			{
				return null;
			}

			Node instance = scene.Instantiate();

			if (!ForgeEntityBridge.TryGetEntity(instance, out IForgeEntity? found) || found is not ForgeEntity entity)
			{
				GD.PushWarning($"Forge: the networked scene {scene.ResourcePath} has no ForgeEntity at its root.");
				instance.Free();
				return null;
			}

			entity.NetworkId = id;
			entity.NetworkOwner = owner.Value;
			entity.IsMirror = !predicted;
			entity.IsPredicted = predicted;
			entity.Replicator = session;
			session.SeedSpawn(entity, id);
			session._spawnedRoots[entity] = instance;
			Node parent = session.SpawnParent ?? session.GetTree().CurrentScene ?? session.GetTree().Root;

			if (body is BodyState state)
			{
				PlaceBefore(instance, entity, parent, state);
			}

			parent.AddChild(instance, forceReadableName: true);

			if (!predicted)
			{
				ShowOnFrames(entity);
			}

			return entity;
		}
	}
}
