// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Abilities;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that binds a node property to a field declared by an <see cref="IActivationDataProvider"/>.
/// </summary>
/// <remarks>
/// At build time this resource constructs Forge's core <see cref="AbilityActivationDataResolver"/> so the selected
/// member is read directly from the typed activation-data payload.
/// </remarks>
[Tool]
[GlobalClass]
public partial class AbilityActivationDataResolverResource : StatescriptResolverResource
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "AbilityActivationData";

	/// <summary>
	/// Gets or sets the class name of the <see cref="IActivationDataProvider"/> implementation that declares the field.
	/// </summary>
	[Export]
	public string ProviderClassName { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the name of the activation data field to bind to.
	/// </summary>
	[Export]
	public string FieldName { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the expected type of the activation data field.
	/// </summary>
	[Export]
	public StatescriptVariableType FieldType { get; set; } = StatescriptVariableType.Int;

	/// <inheritdoc/>
	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		if (string.IsNullOrEmpty(ProviderClassName))
		{
			GD.PushError(
				$"Statescript: Ability Activation Data resolver on node '{nodeId}' (input {index}) " +
				"has no provider selected. Select a provider and field in the graph editor.");
			return;
		}

		if (string.IsNullOrEmpty(FieldName))
		{
			GD.PushError(
				$"Statescript: Ability Activation Data resolver on node '{nodeId}' (input {index}) " +
				$"has provider '{ProviderClassName}' but no field selected. " +
				"Select a field in the graph editor.");
			return;
		}

		DefineAndBindInputProperty(
			graph,
			runtimeNode,
			$"__activation_{nodeId}_{index}",
			index,
			BuildAbilityActivationDataResolver(nodeId, index));
	}

	/// <inheritdoc/>
	public override IPropertyResolver BuildResolver(Graph graph)
	{
		if (string.IsNullOrEmpty(ProviderClassName) || string.IsNullOrEmpty(FieldName))
		{
			GD.PushError(
				"Statescript: Ability Activation Data resolver has incomplete configuration " +
				$"(provider: '{ProviderClassName}', field: '{FieldName}'). " +
				"The resolver will return a default value.");
			return new VariantResolver(default, typeof(int));
		}

		return BuildAbilityActivationDataResolver();
	}

	private IPropertyResolver BuildAbilityActivationDataResolver(string? nodeId = null, byte? index = null)
	{
		IActivationDataProvider? provider = StatescriptAbilityBehavior.InstantiateProvider(ProviderClassName);
		if (provider is null)
		{
			string location = nodeId is not null && index is not null
				? $" on node '{nodeId}' (input {index})"
				: string.Empty;

			GD.PushError(
				$"Statescript: Could not instantiate activation data provider '{ProviderClassName}'{location}. " +
				"The resolver will return a default value.");
			return new VariantResolver(default, typeof(int));
		}

		// Godot math-typed fields (e.g. Godot.Vector3) are not Variant128-constructable, so convert them in the Godot
		// layer before falling back to the core resolver for natively supported types.
		if (GodotActivationDataResolver.TryCreate(
			provider.ActivationDataType,
			FieldName,
			out IPropertyResolver resolver))
		{
			return resolver;
		}

		return new AbilityActivationDataResolver(provider.ActivationDataType, FieldName);
	}
}
