// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Godot;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that reads the SetByCaller magnitude currently stored on an <see cref="Effect"/> for an identifier
/// tag.
/// </summary>
/// <remarks>
/// The identifier tag is requested lazily on first resolve, so editor-time graph builds never touch the runtime tags
/// manager.
/// </remarks>
[Tool]
[GlobalClass]
public partial class SetByCallerMagnitudeResolverResource : StatescriptResolverResource
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "SetByCallerMagnitude";

	/// <summary>
	/// Gets or sets the SetByCaller identifier tag key.
	/// </summary>
	[Export]
	public string IdentifierTag { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the nested resolver providing the effect to inspect.
	/// </summary>
	[Export]
	public StatescriptResolverResource? Effect { get; set; }

	/// <inheritdoc/>
	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		DefineAndBindInputProperty(graph, runtimeNode, $"__sbcmag_{nodeId}_{index}", index, BuildResolver(graph));
	}

	/// <inheritdoc/>
	public override IPropertyResolver BuildResolver(Graph graph)
	{
		if (string.IsNullOrEmpty(IdentifierTag)
			|| Effect is null
			|| !Effect.TryBuildObjectResolver(graph, out IObjectResolver? effectResolver)
			|| effectResolver is not IObjectResolver<Effect> typedEffectResolver)
		{
			GD.PushError(
				"Statescript: SetByCaller Magnitude resolver requires an identifier tag and an effect source.");
			return new VariantResolver(default, typeof(float));
		}

		string identifierTag = IdentifierTag;

		return new LazyPropertyResolver(
			typeof(float),
			() => new SetByCallerMagnitudeResolver(
				typedEffectResolver,
				Tag.RequestTag(ForgeManagers.Instance.TagsManager, identifierTag)));
	}
}
