// Copyright © Gamesmiths Guild.

using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Core;
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
/// the spawned ones under <see cref="SpawnParent"/>, and shows their bodies <see cref="InterpolationDelayMs"/> behind
/// the server.</para>
/// <para>A client keeps its clock about a round trip ahead of the server's, from the session's clock sync, so its input
/// arrives before the server needs it. The clock jumps to the target when it's off by more than
/// <see cref="LeadToleranceTicks"/>.</para>
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
	private readonly ReplicationHost _host;
	private GodotTransport? _transport;
	private ConditionedTransport? _conditioned;
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
	/// behind the server.
	/// </summary>
	[Export(PropertyHint.Range, "0,500,1")]
	public int InterpolationDelayMs { get; set; } = 100;

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
		Session.Mirror.Spawned += OnMirrorSpawned;
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

		// The newest state a client holds is about half a round trip older than the server's tick now, so bodies are
		// shown the delay behind that.
		ReplicationClient mirror = Session.Mirror!;
		double newest = Session.ClockSync.HasEstimate
			? ServerTick - Clock.TicksIn(Session.ClockSync.RoundTripTime / 2)
			: mirror.LastServerTick;
		double shown = newest - Clock.TicksIn(InterpolationDelayMs / 1000.0);

		foreach (IForgeEntity entity in mirror.Entities)
		{
			if (mirror.BodyOf(entity)?.TrySample(shown, out BodyState body) == true)
			{
				PlaceBody(entity, body);
			}
		}
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
			// The tree is as the last step left it, so this is that tick's state.
			Session.Replication!.Send(Clock.Tick - 1);
			Session.TakeInput(Clock.Tick);
			return;
		}

		if (!ReportClientState(before))
		{
			return;
		}

		SteerClock(now);

		// A clock steered back would send ticks older than the window's newest; those go again from the new tick.
		if (Session.Input.NewestTick > Clock.Tick)
		{
			Session.Input.Clear();
		}

		if (LocalPlayer?.Input is EntityInput input)
		{
			Session.SendInput(new InputCommand(Clock.Tick, input.Current));
		}
	}

	internal void OnEntityReady(ForgeEntity entity)
	{
		if (!IsServer || entity.Replicator is not null)
		{
			return;
		}

		int sceneIndex = SceneIndexOf(InstanceRoot(entity));

		if (sceneIndex < 0)
		{
			GD.PushWarning(
				$"Forge: {entity.GetPath()} is networked and entered the game while hosting, from a scene the " +
				"network manifest doesn't list, so it isn't replicated. Regenerate the manifest (Project, Tools, " +
				"Forge).");
			return;
		}

		entity.NetworkId = Session!.Replication!.AllocateId();
		Register(entity, sceneIndex);
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
		if (ForgeEntityBridge.TryGetSpatialNode3D(entity, out Node3D? node3D))
		{
			var rotation = new Quaternion(state.Rotation.X, state.Rotation.Y, state.Rotation.Z, state.Rotation.W);
			node3D.GlobalTransform = new Transform3D(
				new Basis(rotation.Normalized()) * Basis.FromScale(node3D.GlobalBasis.Scale),
				new Vector3(state.Position.X, state.Position.Y, state.Position.Z));

			if (node3D is CharacterBody3D body)
			{
				body.Velocity = new Vector3(state.Velocity.X, state.Velocity.Y, state.Velocity.Z);
			}

			return;
		}

		if (ForgeEntityBridge.TryGetSpatialNode2D(entity, out Node2D? node2D))
		{
			node2D.GlobalPosition = new Vector2(state.Position.X, state.Position.Y);
			node2D.GlobalRotation = 2f * MathF.Atan2(state.Rotation.Z, state.Rotation.W);

			if (node2D is CharacterBody2D body)
			{
				body.Velocity = new Vector2(state.Velocity.X, state.Velocity.Y);
			}
		}
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

	private void OnMirrorSpawned(IForgeEntity entity)
	{
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

	// Makes the entities the server spawns on a client, and takes away the ones it despawns.
	private sealed class ReplicationHost(ForgeNetworkSession session) : IReplicationHost
	{
		public IForgeEntity? Spawn(NetworkEntityId id, int sceneIndex, PeerId owner)
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
			entity.IsMirror = true;
			entity.Replicator = session;
			session._spawnedRoots[entity] = instance;
			Node parent = session.SpawnParent ?? session.GetTree().CurrentScene ?? session.GetTree().Root;
			parent.AddChild(instance, forceReadableName: true);
			ShowOnFrames(entity);
			return entity;
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
	}
}
