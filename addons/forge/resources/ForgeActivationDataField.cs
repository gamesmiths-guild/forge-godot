// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources.Statescript;

namespace Gamesmiths.Forge.Godot.Resources;

/// <summary>
/// Describes a single field exposed by an <see cref="IActivationDataProvider"/>. Each field defines a name and type
/// that graph nodes can bind to via the Activation Data resolver.
/// </summary>
/// <param name="FieldName">The name of this data field. This name is used as the graph variable name at runtime.
/// </param>
/// <param name="FieldType">The type of this data field.</param>
public readonly record struct ForgeActivationDataField(string FieldName, StatescriptVariableType FieldType);
