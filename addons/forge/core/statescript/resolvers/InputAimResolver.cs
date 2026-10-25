// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using ForgeInputAimResolver = Gamesmiths.Forge.Statescript.Properties.InputAimResolver;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// The core <see cref="ForgeInputAimResolver"/>, which reads the local device through <paramref name="deviceResolver"/>
/// when its input source entity has no input of its own.
/// </summary>
/// <remarks>
/// The aim an entity's input carries was found by whatever collected it, so the device resolver's own settings - a
/// ray's mask, its reach - only apply to device reads.
/// </remarks>
/// <param name="inputSource">Resolves the entity whose input is read.</param>
/// <param name="deviceResolver">Reads the same value off the local device.</param>
/// <param name="value">Which part of the aim to resolve.</param>
internal sealed class InputAimResolver(
	IEntityResolver inputSource,
	IPropertyResolver deviceResolver,
	InputAimValue value)
	: ForgeInputAimResolver(inputSource, value)
{
	private readonly IPropertyResolver _deviceResolver = deviceResolver;

	private bool _reportedMissingAim;

	protected override Variant128 ResolveInput(EntityInput input)
	{
		if (!input.Current.HasAim)
		{
			ReportMissingAimOnce();
		}

		return base.ResolveInput(input);
	}

	protected override Variant128 ResolveWithoutInput(GraphContext graphContext)
	{
		return _deviceResolver.Resolve(graphContext);
	}

	private void ReportMissingAimOnce()
	{
		if (_reportedMissingAim)
		{
			return;
		}

		_reportedMissingAim = true;

		GD.PushWarning(
			$"Statescript: {_deviceResolver.GetType().Name} reads its input source's aim, and that entity's input " +
			"carries none. Give its input collector an aim source. Resolving to a default value.");
	}
}
