// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using System.Linq;
using Gamesmiths.Forge.Godot.Editor.Attributes;
using Gamesmiths.Forge.Godot.Nodes;
using Gamesmiths.Forge.Networking;
using Gamesmiths.Forge.Networking.Messages;
using Godot;

namespace Gamesmiths.Forge.Example;

/// <summary>
/// The arena's game: a menu to host, join or run a dedicated server, a numbered player for every peer on the server,
/// and the mouse captured for first-person play.
/// </summary>
/// <remarks>
/// <para>Everything that changes the world happens on the server: it spawns the players as networked scenes, and the
/// session replicates them. A client only picks what to join.</para>
/// <para>Started with <c>-- --host</c>, <c>-- --dedicated</c> or <c>-- --join</c> (or <c>--join=address</c>), it skips
/// the menu, which suits running several instances from the editor, each with its own arguments.</para>
/// </remarks>
public partial class ArenaGame : Node
{
	/// <summary>
	/// The group of every player's body, on every machine.
	/// </summary>
	public const string PlayersGroup = "arena_players";

	/// <summary>
	/// The group of the spawn points.
	/// </summary>
	public const string SpawnsGroup = "arena_spawns";

	private const int MaxPlayers = 4;

	private const string NumberMeta = "arena_number";

	private static string? _lastAddress;
	private static string _lastStatus = string.Empty;
	private static bool _argumentsRead;

	private readonly Dictionary<long, Node3D> _players = [];

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
	/// Gets or sets the node players are added to, on the server and on every client.
	/// </summary>
	[Export]
	public required Node3D Spawned { get; set; }

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

	/// <inheritdoc/>
	public override void _Ready()
	{
		base._Ready();

		Session.PeerJoined += OnPeerJoined;
		Session.PeerLeft += OnPeerLeft;
		Session.Connected += OnConnected;
		Session.Refused += OnRefused;
		Session.Disconnected += OnDisconnected;
		Session.SpawnParent = Spawned;
		Address.Text = _lastAddress ?? Address.Text;
		Status.Text = _lastStatus;
		_lastStatus = string.Empty;

		if (_argumentsRead)
		{
			return;
		}

		_argumentsRead = true;

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
	public override void _ExitTree()
	{
		base._ExitTree();
		Input.MouseMode = Input.MouseModeEnum.Visible;
	}

	/// <inheritdoc/>
	public override void _UnhandledInput(InputEvent @event)
	{
		base._UnhandledInput(@event);

		if (Menu.Visible)
		{
			return;
		}

		if (@event.IsActionPressed("ui_cancel"))
		{
			Input.MouseMode = Input.MouseModeEnum.Visible;
		}
		else if (@event is InputEventMouseButton { Pressed: true } && Input.MouseMode != Input.MouseModeEnum.Captured)
		{
			Input.MouseMode = Input.MouseModeEnum.Captured;
			GetViewport().SetInputAsHandled();
		}
	}

	/// <summary>
	/// Hosts a session with a player on this machine. Connected to the menu's Host button.
	/// </summary>
	public void Host()
	{
		// This machine's player is one of the four.
		Session.MaxClients = MaxPlayers - 1;

		if (Session.Host() != Error.Ok)
		{
			Status.Text = $"Couldn't host on port {Session.Port}.";
			return;
		}

		Play();
		SpawnPlayer(PeerId.Server.Value);
	}

	/// <summary>
	/// Hosts a session with nobody playing on this machine. Connected to the menu's Dedicated button, and run on its
	/// own when the game is started with <c>-- --dedicated</c>.
	/// </summary>
	public void HostDedicated()
	{
		Session.MaxClients = MaxPlayers;

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

	private static int NumberOf(Node3D player)
	{
		return player.GetMeta(NumberMeta).AsInt32();
	}

	private void Play()
	{
		Menu.Visible = false;

		if (DisplayServer.GetName() != "headless")
		{
			Input.MouseMode = Input.MouseModeEnum.Captured;
		}
	}

	private void SpawnPlayer(int peer)
	{
		int number = Enumerable.Range(1, MaxPlayers).First(x => _players.Values.All(p => NumberOf(p) != x));
		Node3D player = PlayerScene.Instantiate<Node3D>();
		player.GetNode<ForgeEntity>("Forge Entity").NetworkOwner = peer;
		player.GetNode<ForgeAttributeSet>("Forge Entity/Arena Attributes").InitialAttributeValues!["Number"] =
			new AttributeValues(number, 0, 99);
		player.Name = $"Player {number}";
		player.SetMeta(NumberMeta, number);

		// The spawn point furthest from everyone already in, so a peer doesn't spawn inside someone.
		player.Position = GetTree().GetNodesInGroup(SpawnsGroup)
			.OfType<Node3D>()
			.Select(x => x.GlobalPosition)
			.MaxBy(x => _players.Values.Select(p => p.Position.DistanceTo(x)).DefaultIfEmpty(float.MaxValue).Min());
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
		Play();
		Status.Text = string.Empty;
	}

	private void OnRefused(int reason)
	{
		Status.Text = $"The server refused to let this game in: {(RefusalReason)reason}.";
	}

	// Joining made mirrors of the level's dummies for good, so the menu comes back on a fresh level.
	private void OnDisconnected()
	{
		_lastAddress = Address.Text;
		_lastStatus = Status.Text switch
		{
			"Joining..." => "Couldn't reach the server.",
			"" => "The connection to the server ended.",
			_ => Status.Text,
		};

		CallDeferred(MethodName.ReloadLevel);
	}

	private void ReloadLevel()
	{
		if (GetParent() is Main main)
		{
			main.ChangeScene(SceneFilePath);
		}
		else
		{
			GetTree().ReloadCurrentScene();
		}
	}
}
