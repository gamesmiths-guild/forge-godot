// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Nodes;
using Gamesmiths.Forge.Statescript;
using NumericsVector2 = System.Numerics.Vector2;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes;

// Reading and writing shared by the shot nodes.
internal static class ShotNodeIO
{
	// The entity a shot node fires from: the ability's owner, which has to be a Forge entity in the scene.
	public static ForgeEntity? Instigator(GraphContext graphContext)
	{
		return graphContext.TryGetActivationContext(out AbilityBehaviorContext? context)
			? context.Owner as ForgeEntity
			: null;
	}

	public static void Write(GraphContext graphContext, OutputVariable output, NumericsVector3 value)
	{
		if (Variables(graphContext, output) is Variables variables)
		{
			variables.SetVar(output.BoundName, value);
		}
	}

	public static void Write(GraphContext graphContext, OutputVariable output, NumericsVector2 value)
	{
		if (Variables(graphContext, output) is Variables variables)
		{
			variables.SetVar(output.BoundName, value);
		}
	}

	public static void Write(GraphContext graphContext, OutputVariable output, int value)
	{
		if (Variables(graphContext, output) is Variables variables)
		{
			variables.SetVar(output.BoundName, value);
		}
	}

	// The variables an output is bound to, when it's bound to one that's there: a graph can outlive the variable a
	// shared binding names.
	private static Variables? Variables(GraphContext graphContext, OutputVariable output)
	{
		if (output.BoundName == StringKey.Empty)
		{
			return null;
		}

		Variables? variables = output.Scope == VariableScope.Shared
			? graphContext.SharedVariables
			: graphContext.GraphVariables;

		return variables?.TryGetVariant(output.BoundName, out _) == true ? variables : null;
	}
}
