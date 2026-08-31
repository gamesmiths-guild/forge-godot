// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript;

/// <summary>
/// Writes the bindings a freshly created node's required inputs already appear to have.
/// </summary>
/// <remarks>
/// <para>A required input row shows a resolver from the moment the node is added — a constant zero, the ability's
/// owner, a node path — but nothing had written that to the resource. The row-time fallback that would have cannot
/// run during an add, because adding a node happens inside a replay scope where seeding is suppressed so that an undo
/// clearing a binding is not immediately undone by the rebuild. The result was a node whose input reads
/// <c>&lt;unresolved&gt;</c> at runtime until the value was touched, or until the graph was reopened and the row-time
/// fallback finally ran.</para>
/// <para>Seeding at creation closes that: the defaults are part of what the new node <em>is</em>, so undo and redo of
/// the add carry them like any other authored value, and what the row shows is what the graph runs.</para>
/// </remarks>
internal static class DefaultInputBindings
{
	/// <summary>
	/// Seeds every required input the node does not already have a binding for.
	/// </summary>
	/// <param name="graph">The graph the node belongs to, which resolver editors need to set themselves up.</param>
	/// <param name="nodeResource">The node resource being created.</param>
	/// <param name="typeInfo">The node type's discovered inputs and outputs.</param>
	/// <param name="editor">The node's custom editor, when it has one, so it can opt an input out.</param>
	public static void SeedMissing(
		StatescriptGraph graph,
		StatescriptNode nodeResource,
		StatescriptNodeDiscovery.NodeTypeInfo typeInfo,
		CustomNodeEditor? editor)
	{
		for (int i = 0; i < typeInfo.InputPropertiesInfo.Length; i++)
		{
			StatescriptNodeDiscovery.InputPropertyInfo info = typeInfo.InputPropertiesInfo[i];

			// Optional inputs are never seeded: their fresh state is (None), which is what the runtime documents as
			// their default and what no resolver can reproduce.
			if (info.IsOptional
				|| editor?.SeedsDefaultBinding(i) == false
				|| HasBinding(nodeResource, i)
				|| !TryBuildDefaultResolver(graph, info, out StatescriptResolverResource? resolver))
			{
				continue;
			}

			nodeResource.PropertyBindings.Add(new StatescriptNodeProperty
			{
				Direction = StatescriptPropertyDirection.Input,
				PropertyIndex = i,
				Resolver = resolver,
			});
		}
	}

	private static bool HasBinding(StatescriptNode nodeResource, int index)
	{
		foreach (StatescriptNodeProperty binding in nodeResource.PropertyBindings)
		{
			if (binding.Direction == StatescriptPropertyDirection.Input && binding.PropertyIndex == index)
			{
				return true;
			}
		}

		return false;
	}

	// Built by the same selection the row makes and saved through the same editor, so the two cannot disagree about
	// what a fresh slot holds.
	private static bool TryBuildDefaultResolver(
		StatescriptGraph graph,
		StatescriptNodeDiscovery.InputPropertyInfo info,
		out StatescriptResolverResource? resolver)
	{
		resolver = null;

		List<Func<NodeEditorProperty>> factories =
			StatescriptResolverRegistry.GetCompatibleFactories(info.ExpectedType);

		factories.RemoveAll(StatescriptResolverRegistry.RequiresIterationScope);
		factories.RemoveAll(factory => info.IsArray
			? !StatescriptResolverRegistry.SupportsArrayValues(factory)
			: !StatescriptResolverRegistry.SupportsScalarValues(factory));

		if (factories.Count == 0)
		{
			return false;
		}

		int index = StatescriptResolverRegistry.GetDefaultFactoryIndex(factories, info.ExpectedType, info.IsArray);
		NodeEditorProperty defaultEditor = factories[index]();

		try
		{
			defaultEditor.ConfigureAllowedExpectedTypes(info.ExpectedType);
			defaultEditor.Setup(graph, null, info.ExpectedType, static () => { }, info.IsArray);

			var property = new StatescriptNodeProperty();
			defaultEditor.SaveTo(property);
			resolver = property.Resolver;
		}
		finally
		{
			defaultEditor.ClearCallbacks();

			if (GodotObject.IsInstanceValid(defaultEditor))
			{
				defaultEditor.Free();
			}
		}

		return resolver is not null;
	}
}
#endif
