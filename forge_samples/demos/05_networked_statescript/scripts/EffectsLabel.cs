// Copyright © Gamesmiths Guild.

using System.Globalization;
using System.Text;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Godot.Nodes;
using Godot;

namespace Gamesmiths.Forge.Example;

/// <summary>
/// The effects active on an entity, above it: each one's name, its stacks and what's left of it, read every frame so
/// the time counts down. On a client they're the server's effects, as the entity's mirror holds them. A listen host
/// holds the real ones, so it keeps to what the same player would hear of as a client.
/// </summary>
public partial class EffectsLabel : Label3D
{
	private readonly StringBuilder _text = new();

	private ForgeEntity? _entity;
	private ForgeNetworkSession? _session;

	/// <inheritdoc/>
	public override void _Ready()
	{
		base._Ready();

		_entity = GetNode<ForgeEntity>("%Forge Entity");
		_session = ForgeNetworkSession.Of(this);
	}

	/// <inheritdoc/>
	public override void _Process(double delta)
	{
		base._Process(delta);

		_text.Clear();

		foreach (ActiveEffectHandle effect in _entity!.EffectsManager.GetActiveEffects())
		{
			EffectData data = effect.Effect!.EffectData;

			if (!IsHeard(data.Visibility))
			{
				continue;
			}

			_text.Append(data.Name);

			if (effect.StackCount > 1)
			{
				_text.Append(CultureInfo.InvariantCulture, $" x{effect.StackCount}");
			}

			if (effect.TotalDuration > 0)
			{
				_text.Append(CultureInfo.InvariantCulture, $" {effect.RemainingDuration:0.0}s");
			}

			_text.Append('\n');
		}

		Text = _text.ToString();
	}

	private bool IsHeard(EffectVisibility visibility)
	{
		return visibility switch
		{
			EffectVisibility.ServerOnly => false,
			EffectVisibility.OwnerOnly => _entity!.NetworkOwner == _session?.LocalPeer,
			_ => true,
		};
	}
}
