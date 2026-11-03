// Copyright © Gamesmiths Guild.

using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Networking;
using Godot;

namespace Gamesmiths.Forge.Godot.Networking;

/// <summary>
/// Forge's transport over Godot's <see cref="SceneMultiplayer"/>: raw bytes through <c>SendBytes</c> on ENet
/// channels, with no RPCs and no node paths. What the multiplayer API signals between frames is queued, and the
/// session takes it in its own physics step.
/// </summary>
/// <remarks>
/// <para>Godot polls the multiplayer API once per frame and raises its signals then. This transport only queues what
/// they say, so nothing reaches the simulation from a signal; <see cref="TryReceive"/> hands it over in order when
/// the session asks.</para>
/// <para>A client hears about the server through the connected-to-server and server-disconnected signals and
/// ignores the per-peer ones, so the server appears once. Server relay is off: a client's packets reach the server
/// and nobody else.</para>
/// </remarks>
public sealed class GodotTransport : INetworkTransport, IDisposable
{
	/// <summary>
	/// How many channels a client asks ENet for beyond Godot's system channels: the protocol's three, since a
	/// transfer channel above zero maps past the system ones.
	/// </summary>
	public const int ClientChannels = 3;

	private readonly SceneMultiplayer _multiplayer;
	private readonly Queue<TransportEvent> _inbox = new();
	private readonly HashSet<PeerId> _peers = [];
	private bool _disposed;

	/// <summary>
	/// Initializes a new instance of the <see cref="GodotTransport"/> class over a multiplayer API whose peer has been
	/// created as a server or a client.
	/// </summary>
	/// <param name="multiplayer">The multiplayer API.</param>
	/// <param name="isServer">Whether this end is the server.</param>
	public GodotTransport(SceneMultiplayer multiplayer, bool isServer)
	{
		_multiplayer = multiplayer;
		_multiplayer.ServerRelay = false;
		IsServer = isServer;
		LocalPeer = isServer ? PeerId.Server : new PeerId((int)multiplayer.GetUniqueId());

		if (isServer)
		{
			_multiplayer.PeerConnected += OnPeerConnected;
			_multiplayer.PeerDisconnected += OnPeerDisconnected;
		}
		else
		{
			_multiplayer.ConnectedToServer += OnConnectedToServer;
			_multiplayer.ConnectionFailed += OnConnectionFailed;
			_multiplayer.ServerDisconnected += OnServerDisconnected;
		}

		_multiplayer.PeerPacket += OnPeerPacket;
	}

	/// <summary>
	/// Creates the ENet peer for a server, the way Forge needs it: with no bandwidth limit.
	/// </summary>
	/// <remarks>
	/// Godot 4.7's <c>ENetMultiplayerPeer.create_server</c> passes its arguments to the host in the wrong order when
	/// <c>max_channels</c> is above zero: the channel count lands in the host's incoming bandwidth limit, in bytes
	/// per second, and the client's ENet then throttles nearly every unreliable packet it sends the server. The
	/// server is therefore created with the default channel count, which is plenty, and its bandwidth limits are
	/// set to unlimited explicitly, which also tells every client so.
	/// </remarks>
	/// <param name="port">The port to listen on.</param>
	/// <param name="maxClients">How many clients to accept.</param>
	/// <param name="peer">The peer, when created.</param>
	/// <returns><see cref="Error.Ok"/>, or why the server couldn't be created.</returns>
	public static Error CreateServerPeer(int port, int maxClients, out ENetMultiplayerPeer peer)
	{
		peer = new ENetMultiplayerPeer();
		Error error = peer.CreateServer(port, Math.Max(maxClients, 1));

		if (error == Error.Ok)
		{
			peer.Host.BandwidthLimit(0, 0);
		}

		return error;
	}

	/// <summary>
	/// Creates the ENet peer for a client, with the protocol's channels and no bandwidth limit.
	/// </summary>
	/// <param name="address">The server's address.</param>
	/// <param name="port">The server's port.</param>
	/// <param name="peer">The peer, when created.</param>
	/// <returns><see cref="Error.Ok"/>, or why the client couldn't be created.</returns>
	public static Error CreateClientPeer(string address, int port, out ENetMultiplayerPeer peer)
	{
		peer = new ENetMultiplayerPeer();
		Error error = peer.CreateClient(address, port, ClientChannels);

		if (error == Error.Ok)
		{
			peer.Host.BandwidthLimit(0, 0);
		}

		return error;
	}

	/// <inheritdoc/>
	public PeerId LocalPeer { get; private set; }

	/// <inheritdoc/>
	public bool IsServer { get; }

	/// <inheritdoc/>
	public IReadOnlyCollection<PeerId> Peers => _peers;

	/// <summary>
	/// Gets how many bytes have been sent, payloads only.
	/// </summary>
	public long BytesSent { get; private set; }

	/// <summary>
	/// Gets how many bytes have been received, payloads only.
	/// </summary>
	public long BytesReceived { get; private set; }

	/// <summary>
	/// Gets how many packets have been sent.
	/// </summary>
	public long PacketsSent { get; private set; }

	/// <summary>
	/// Gets how many packets have been received.
	/// </summary>
	public long PacketsReceived { get; private set; }

	/// <summary>
	/// Gets how many sends the multiplayer API refused.
	/// </summary>
	public long SendErrors { get; private set; }

	/// <inheritdoc/>
	public void Send(PeerId peer, byte channel, bool reliable, ReadOnlySpan<byte> payload)
	{
		if (_disposed || !_peers.Contains(peer))
		{
			return;
		}

		// Godot's packet signal doesn't say which channel a packet came on, so the channel rides in the first byte.
		byte[] framed = new byte[payload.Length + 1];
		framed[0] = channel;
		payload.CopyTo(framed.AsSpan(1));

		// Unreliable sends go sequenced (Godot's "unreliable ordered"): a packet older than one already delivered on
		// the channel is dropped rather than delivered late, which suits input windows and state that supersede
		// themselves. ENet's unsequenced mode, Godot's plain "unreliable", stalled after a hundred packets from a
		// client in testing.
		Error error = _multiplayer.SendBytes(
			framed,
			peer.Value,
			reliable ? MultiplayerPeer.TransferModeEnum.Reliable : MultiplayerPeer.TransferModeEnum.UnreliableOrdered,
			channel);

		if (error == Error.Ok)
		{
			BytesSent += framed.Length;
			PacketsSent++;
		}
		else
		{
			SendErrors++;
		}
	}

	/// <inheritdoc/>
	public bool TryReceive(out TransportEvent transportEvent)
	{
		return _inbox.TryDequeue(out transportEvent);
	}

	/// <inheritdoc/>
	public void Disconnect(PeerId peer)
	{
		if (_disposed || !_peers.Contains(peer))
		{
			return;
		}

		if (IsServer)
		{
			_multiplayer.MultiplayerPeer?.DisconnectPeer(peer.Value);
			return;
		}

		// A client leaving closes its only connection. Godot doesn't signal a close the client asked for, so the
		// disconnection is queued here.
		_multiplayer.MultiplayerPeer?.Close();
		OnServerDisconnected();
	}

	/// <summary>
	/// Stops listening to the multiplayer API. Call it when the session ends.
	/// </summary>
	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;

		if (IsServer)
		{
			_multiplayer.PeerConnected -= OnPeerConnected;
			_multiplayer.PeerDisconnected -= OnPeerDisconnected;
		}
		else
		{
			_multiplayer.ConnectedToServer -= OnConnectedToServer;
			_multiplayer.ConnectionFailed -= OnConnectionFailed;
			_multiplayer.ServerDisconnected -= OnServerDisconnected;
		}

		_multiplayer.PeerPacket -= OnPeerPacket;
	}

	private void OnPeerConnected(long id)
	{
		var peer = new PeerId((int)id);

		if (_peers.Add(peer))
		{
			KeepUnreliablePackets((int)id);
			_inbox.Enqueue(TransportEvent.Connected(peer));
		}
	}

	private void OnPeerDisconnected(long id)
	{
		var peer = new PeerId((int)id);

		if (_peers.Remove(peer))
		{
			_inbox.Enqueue(TransportEvent.Disconnected(peer));
		}
	}

	private void OnConnectedToServer()
	{
		// The unique id is assigned by the time the connection is up.
		LocalPeer = new PeerId((int)_multiplayer.GetUniqueId());

		if (_peers.Add(PeerId.Server))
		{
			KeepUnreliablePackets(PeerId.Server.Value);
			_inbox.Enqueue(TransportEvent.Connected(PeerId.Server));
		}
	}

	// ENet drops unreliable packets on its own when a peer's round trip fluctuates, down to all of them. Forge sends
	// its input and state unreliably by design and copes with loss itself, so the throttle is pinned fully open: the
	// probability of sending is raised to the scale at once and never lowered.
	private void KeepUnreliablePackets(int peerId)
	{
		if (_multiplayer.MultiplayerPeer is ENetMultiplayerPeer enet)
		{
			// ENet's default measuring interval, 5 seconds; Godot doesn't name it.
			enet.GetPeer(peerId)?.ThrottleConfigure(5000, (int)ENetPacketPeer.PacketThrottleScale, 0);
		}
	}

	private void OnConnectionFailed()
	{
		_inbox.Enqueue(TransportEvent.Disconnected(PeerId.Server));
	}

	private void OnServerDisconnected()
	{
		if (_peers.Remove(PeerId.Server))
		{
			_inbox.Enqueue(TransportEvent.Disconnected(PeerId.Server));
		}
	}

	private void OnPeerPacket(long id, byte[] packet)
	{
		var peer = new PeerId((int)id);

		if (!_peers.Contains(peer))
		{
			return;
		}

		// The channel is the first byte, put there by Send.
		BytesReceived += packet.Length;
		PacketsReceived++;
		_inbox.Enqueue(TransportEvent.PacketFrom(peer, packet.Length > 0 ? packet[0] : (byte)0, packet.AsMemory(1)));
	}
}
