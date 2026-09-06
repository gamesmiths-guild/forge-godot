// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Resolves how long the tick currently running has taken, in seconds.
/// </summary>
/// <remarks>
/// <para>The step a graph needs to do its own integration: a charge that fills at a rate, a drain spent per tick, a
/// speed multiplied back into a distance. A node's own update is handed this as an argument, so anything written in
/// C# already has it; a graph expressing the same arithmetic through resolvers had no way to reach it.</para>
/// <para>It answers for whichever rail is running rather than making the author say which. The two rails have
/// different steps - the fixed one is the constant the project configured, the frame one is however long the last
/// frame took - and a resolver cannot see which of them is walking it, so it asks the engine. Read from a node on the
/// fixed rail this is the physics step; read from one on the frame rail it is the frame.</para>
/// <para>Zero before there is a tree to ask, which is the same answer the engine gives a node asking outside one.
/// </para>
/// </remarks>
internal sealed class DeltaTimeResolver : IPropertyResolver
{
	public Type ValueType => typeof(double);

	public Variant128 Resolve(GraphContext graphContext)
	{
		if (Engine.GetMainLoop() is not SceneTree tree)
		{
			return new Variant128(0.0);
		}

		return new Variant128(Engine.IsInPhysicsFrame()
			? tree.Root.GetPhysicsProcessDeltaTime()
			: tree.Root.GetProcessDeltaTime());
	}
}
