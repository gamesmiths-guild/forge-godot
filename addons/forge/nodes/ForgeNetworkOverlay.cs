// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Networking;
using Gamesmiths.Forge.Tags;
using Godot;

namespace Gamesmiths.Forge.Godot.Nodes;

/// <summary>
/// The F3 overlay: what the session is doing, in a corner. Round trip, server and local tick, how far ahead a client
/// runs, how the server is receiving its input, how often a client's prediction held and how far back its last
/// correction went, loss and bandwidth, and the conditioner's settings. F3 shows and hides it; F4 cycles the
/// conditioner through a perfect, a good, a far and a poor link; F5 prints the tags this machine sent, most sent
/// first.
/// </summary>
/// <remarks>
/// Add it anywhere in a scene that has a <see cref="ForgeNetworkSession"/>. It finds the session on its own when
/// <see cref="Session"/> isn't set.
/// </remarks>
[GlobalClass]
public partial class ForgeNetworkOverlay : CanvasLayer
{
	private static readonly (string Name, NetworkConditions Conditions)[] _presets =
	[
		("perfect", NetworkConditions.Perfect),
		("good", NetworkConditions.Good),
		("far", NetworkConditions.Far),
		("poor", NetworkConditions.Poor),
	];

	private Label _label = null!;
	private int _preset;
	private double _nextRefreshAt;

	/// <summary>
	/// Gets or sets the session to show. Left empty, the first <see cref="ForgeNetworkSession"/> in the tree is shown.
	/// </summary>
	[Export]
	public ForgeNetworkSession? Session { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the overlay starts shown.
	/// </summary>
	[Export]
	public bool ShownAtStart { get; set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="ForgeNetworkOverlay"/> class.
	/// </summary>
	public ForgeNetworkOverlay()
	{
		Layer = 100;
		ProcessMode = ProcessModeEnum.Always;
	}

	/// <summary>
	/// Lists the tags a manager's machine wrote to packets, most written first, with how many times each was: the
	/// order <see cref="ForgeSettings.CommonlyReplicatedTagsSetting"/> takes them in.
	/// </summary>
	/// <param name="tagsManager">The tags manager.</param>
	/// <returns>The report, a line a tag.</returns>
	public static string TagUsageReport(TagsManager tagsManager)
	{
		StringBuilder text = new StringBuilder("Forge: tags sent, most sent first (")
			.Append(ForgeSettings.CommonlyReplicatedTagsSetting)
			.Append(" takes them in this order):");

		foreach (KeyValuePair<Tag, int> usage in tagsManager.GetNetworkUsage())
		{
			text.Append('\n').Append(CultureInfo.InvariantCulture, $"{usage.Value,8}  {usage.Key}");
		}

		return text.ToString();
	}

	/// <inheritdoc/>
	public override void _Ready()
	{
		base._Ready();

		_label = new Label
		{
			Name = "Readout",
			Position = new Vector2(16, 16),
			Visible = ShownAtStart,
		};
		_label.AddThemeFontSizeOverride("font_size", 13);
		_label.AddThemeColorOverride("font_color", Colors.White);
		_label.AddThemeColorOverride("font_outline_color", Colors.Black);
		_label.AddThemeConstantOverride("outline_size", 3);
		AddChild(_label);
	}

	/// <inheritdoc/>
	public override void _UnhandledKeyInput(InputEvent @event)
	{
		base._UnhandledKeyInput(@event);

		if (@event is not InputEventKey { Pressed: true, Echo: false } key)
		{
			return;
		}

		if (key.Keycode == Key.F3)
		{
			_label.Visible = !_label.Visible;
			GetViewport().SetInputAsHandled();
		}
		else if (key.Keycode == Key.F4 && FindSession() is ForgeNetworkSession session)
		{
			_preset = (_preset + 1) % _presets.Length;
			NetworkConditions conditions = _presets[_preset].Conditions;
			session.LatencyMs = (int)(conditions.LatencySeconds * 1000);
			session.JitterMs = (int)(conditions.JitterSeconds * 1000);
			session.LossPercent = conditions.LossRate * 100;
			_nextRefreshAt = 0;
			GetViewport().SetInputAsHandled();
		}
		else if (key.Keycode == Key.F5)
		{
			GD.Print(TagUsageReport(ForgeManagers.Instance.TagsManager));
			GetViewport().SetInputAsHandled();
		}
	}

	/// <inheritdoc/>
	public override void _Process(double delta)
	{
		base._Process(delta);

		if (!_label.Visible)
		{
			return;
		}

		double now = Time.GetTicksMsec() / 1000.0;

		if (now < _nextRefreshAt)
		{
			return;
		}

		_nextRefreshAt = now + 0.25;
		_label.Text = Describe(FindSession());
	}

	private static string Describe(ForgeNetworkSession? session)
	{
		var text = new StringBuilder();
		SimulationClock clock = ForgeManagers.Instance.Clock;
		text.Append(CultureInfo.InvariantCulture, $"tick {clock.Tick} at {clock.TickRate} Hz\n");

		if (session is null)
		{
			text.Append("no network session in the scene\nF3 hides this");
			return text.ToString();
		}

		text.Append(CultureInfo.InvariantCulture, $"{session.Mode}, {session.State}\n");

		if (session.IsServer && session.Session is NetworkSession server)
		{
			text.Append(CultureInfo.InvariantCulture, $"{server.Peers.Count} client(s), ")
				.Append(CultureInfo.InvariantCulture, $"{server.Replication!.Entities.Count} entities replicated\n");

			foreach (NetworkPeer peer in server.Peers)
			{
				InputPlayoutBuffer input = peer.Input;
				text.Append(CultureInfo.InvariantCulture, $"  {peer.Id}: input depth {input.Depth}, ")
					.Append(CultureInfo.InvariantCulture, $"{input.ReceivedTicks} received, {input.LostTicks} lost, ")
					.Append(CultureInfo.InvariantCulture, $"{input.LateTicks} late, {peer.MalformedPackets} bad\n");
			}
		}
		else if (session.Mode == NetworkMode.Client)
		{
			text.Append(CultureInfo.InvariantCulture, $"rtt {session.RoundTripTime * 1000:0} ms, ")
				.Append(CultureInfo.InvariantCulture, $"server tick {session.ServerTick:0}, ")
				.Append(CultureInfo.InvariantCulture, $"{session.TicksAhead:0.0} ticks ahead, ")
				.Append(CultureInfo.InvariantCulture, $"server holds {session.ServerInputDepth}\n");

			if (session.Session?.Mirror?.InterpolationDelay is { } delay)
			{
				string mode = session.InterpolationMode == InterpolationDelayMode.Adaptive
					? $"adaptive, jitter {delay.Jitter:0.0} ticks, loss {delay.Loss * 100:0.0}%"
					: "fixed";
				text.Append(
						CultureInfo.InvariantCulture,
						$"shown {session.CurrentInterpolationDelayMs:0} ms behind ({mode}), ")
					.Append(CultureInfo.InvariantCulture, $"a send every {delay.SendInterval} ticks\n");
			}

			if (session.Session?.Mirror is { } mirror)
			{
				text.Append(
						CultureInfo.InvariantCulture,
						$"{mirror.Entities.Count - mirror.Predicted.Count} mirrored, ")
					.Append(
						CultureInfo.InvariantCulture,
						$"state of tick {mirror.LastServerTick}, ")
					.Append(
						CultureInfo.InvariantCulture,
						$"{mirror.PacketsLate} late, {mirror.SpawnsRefused} refused\n");

				if (mirror.Predicted.Count > 0)
				{
					text.Append(CultureInfo.InvariantCulture, $"{mirror.Predicted.Count} predicted: ")
						.Append(CultureInfo.InvariantCulture, $"{mirror.Confirmations} confirmed, ")
						.Append(CultureInfo.InvariantCulture, $"{mirror.Corrections} corrected, ")
						.Append(CultureInfo.InvariantCulture, $"last rolled back {mirror.LastCorrectionTicks} ticks\n");
				}
			}
		}

		text.Append(CultureInfo.InvariantCulture, $"{session.BytesSentPerSecond / 1024:0.0} KB/s up, ")
			.Append(CultureInfo.InvariantCulture, $"{session.BytesReceivedPerSecond / 1024:0.0} KB/s down\n")
			.Append(CultureInfo.InvariantCulture, $"conditioner: {session.LatencyMs} ms ± {session.JitterMs} ms, ")
			.Append(CultureInfo.InvariantCulture, $"{session.LossPercent:0.#}% loss, {session.PacketsLost} lost\n")
			.Append("F3 hides this, F4 cycles the conditioner, F5 prints the tags sent");

		return text.ToString();
	}

	private ForgeNetworkSession? FindSession()
	{
		if (Session is not null && IsInstanceValid(Session))
		{
			return Session;
		}

		foreach (Node node in GetTree().GetNodesInGroup(ForgeNetworkSession.Group))
		{
			if (node is ForgeNetworkSession session)
			{
				return session;
			}
		}

		return null;
	}
}
