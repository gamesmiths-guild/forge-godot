// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Nodes;
using Gamesmiths.Forge.Networking;
using Gamesmiths.Forge.Networking.Messages;
using Godot;
using GodotArray = Godot.Collections.Array<Godot.Node>;

namespace Gamesmiths.Forge.Example;

/// <summary>
/// Sample 05's game: a menu to host, join or run a dedicated server, a player for every peer on the server, enemies the
/// server keeps coming, and the camera on this machine's player.
/// </summary>
/// <remarks>
/// <para>Everything that changes the world happens on the server: it spawns the players and the enemies as networked
/// scenes, and the session replicates them. A client only picks what to join and follows its own player with the
/// camera.</para>
/// <para>Started with <c>-- --host</c>, <c>-- --dedicated</c> or <c>-- --join</c> (or <c>--join=address</c>), it skips
/// the menu, which suits running several instances from the editor, each with its own arguments.</para>
/// </remarks>
public partial class NetworkedLevel : Node
{
	private readonly Dictionary<long, Node3D> _players = [];
	private readonly RandomNumberGenerator _random = new();
	private double _nextEnemyAt;

	/// <summary>
	/// Gets or sets the session.
	/// </summary>
	[Export]
	public required ForgeNetworkSession Session { get; set; }

	/// <summary>
	/// Gets or sets the scene a player spawns from.
	/// </summary>
	[Export]
	public required PackedScene PlayerScene { get; set; }

	/// <summary>
	/// Gets or sets the scene an enemy spawns from.
	/// </summary>
	[Export]
	public required PackedScene EnemyScene { get; set; }

	/// <summary>
	/// Gets or sets the node players and enemies are added to, on the server and on every client.
	/// </summary>
	[Export]
	public required Node3D Spawned { get; set; }

	/// <summary>
	/// Gets or sets the camera rig that follows this machine's player.
	/// </summary>
	[Export]
	public required FollowCamera3D Camera { get; set; }

	/// <summary>
	/// Gets or sets the menu shown until this machine is in a session.
	/// </summary>
	[Export]
	public required Control Menu { get; set; }

	/// <summary>
	/// Gets or sets the field the server's address is typed in.
	/// </summary>
	[Export]
	public required LineEdit Address { get; set; }

	/// <summary>
	/// Gets or sets the label that says how joining went.
	/// </summary>
	[Export]
	public required Label Status { get; set; }

	/// <summary>
	/// Gets or sets how many enemies the server keeps in the level.
	/// </summary>
	[Export]
	public int MaxEnemies { get; set; } = 4;

	/// <summary>
	/// Gets or sets how long the server waits between two enemies, in seconds.
	/// </summary>
	[Export]
	public double EnemyInterval { get; set; } = 3;

	/// <inheritdoc/>
	public override void _Ready()
	{
		base._Ready();

		Session.PeerJoined += OnPeerJoined;
		Session.PeerLeft += OnPeerLeft;
		Session.Connected += OnConnected;
		Session.Refused += OnRefused;
		Session.Disconnected += OnDisconnected;
		Session.LocalPlayerChanged += OnLocalPlayerChanged;
		Session.SpawnParent = Spawned;

		if (ForgeNetworkSession.DedicatedRequested)
		{
			CallDeferred(MethodName.HostDedicated);
			return;
		}

		foreach (string argument in OS.GetCmdlineUserArgs())
		{
			if (argument == "--host")
			{
				CallDeferred(MethodName.Host);
			}
			else if (argument.StartsWith("--join", System.StringComparison.Ordinal))
			{
				int equals = argument.IndexOf('=');
				Address.Text = equals < 0 ? Address.Text : argument[(equals + 1)..];
				CallDeferred(MethodName.Join);
			}
		}
	}

	/// <inheritdoc/>
	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);

		if (!Session.IsServer)
		{
			return;
		}

		_nextEnemyAt -= delta;

		if (_nextEnemyAt > 0 || GetTree().GetNodesInGroup("enemies").Count >= MaxEnemies)
		{
			return;
		}

		_nextEnemyAt = EnemyInterval;
		GodotArray markers = GetTree().GetNodesInGroup("enemy_spawns");

		if (markers.Count == 0)
		{
			return;
		}

		var marker = (Node3D)markers[_random.RandiRange(0, markers.Count - 1)];
		Node3D enemy = EnemyScene.Instantiate<Node3D>();
		enemy.Position = marker.GlobalPosition;
		Spawned.AddChild(enemy, forceReadableName: true);
	}

	/// <summary>
	/// Hosts a session with a player on this machine. Connected to the menu's Host button.
	/// </summary>
	public void Host()
	{
		if (Session.Host() != Error.Ok)
		{
			Status.Text = $"Couldn't host on port {Session.Port}.";
			return;
		}

		Menu.Visible = false;
		SpawnPlayer(PeerId.Server.Value);
	}

	/// <summary>
	/// Hosts a session with nobody playing on this machine. Connected to the menu's Dedicated button, and run on its own
	/// when the game is started with <c>-- --dedicated</c>.
	/// </summary>
	public void HostDedicated()
	{
		if (Session.Host(dedicated: true) != Error.Ok)
		{
			Status.Text = $"Couldn't host on port {Session.Port}.";
			return;
		}

		Menu.Visible = false;
	}

	/// <summary>
	/// Joins the session at the address typed in. Connected to the menu's Join button.
	/// </summary>
	public void Join()
	{
		string address = Address.Text.Trim();

		if (Session.Join(address.Length > 0 ? address : "127.0.0.1") != Error.Ok)
		{
			Status.Text = "Couldn't start joining.";
			return;
		}

		Status.Text = "Joining...";
	}

	private void SpawnPlayer(int peer)
	{
		Node3D player = PlayerScene.Instantiate<Node3D>();
		player.GetNode<ForgeEntity>("Forge Entity").NetworkOwner = peer;
		player.Name = $"Player {peer}";
		player.Position = new Vector3(((_players.Count % 4) - 1.5f) * 2, 0, 7);
		_players[peer] = player;
		Spawned.AddChild(player);
	}

	private void OnPeerJoined(long peerId)
	{
		SpawnPlayer((int)peerId);
	}

	private void OnPeerLeft(long peerId)
	{
		if (_players.Remove(peerId, out Node3D? player))
		{
			player.QueueFree();
		}
	}

	private void OnConnected()
	{
		Menu.Visible = false;
	}

	private void OnRefused(int reason)
	{
		Status.Text = $"The server refused to let this game in: {(RefusalReason)reason}.";
	}

	private void OnDisconnected()
	{
		Menu.Visible = true;

		if (Status.Text == "Joining...")
		{
			Status.Text = "Couldn't reach the server.";
		}
	}

	private void OnLocalPlayerChanged(Node? player)
	{
		Camera.Target = player?.GetParent<Node3D>();
	}
}
