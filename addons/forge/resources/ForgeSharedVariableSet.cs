// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Statescript;
using Godot;
using Godot.Collections;

namespace Gamesmiths.Forge.Godot.Resources;

/// <summary>
/// Resource containing a collection of shared variable definitions for an entity. Assign this to a
/// <see cref="Nodes.ForgeEntity"/> (or custom <see cref="IForgeEntity"/> implementation) to define which shared
/// variables the entity exposes at runtime.
/// </summary>
[Tool]
[GlobalClass]
[Icon("uid://cu6ncpuumjo20")]
public partial class ForgeSharedVariableSet : Resource
{
	/// <summary>
	/// Gets or sets the shared variable definitions.
	/// </summary>
	[Export]
	public Array<ForgeSharedVariableDefinition> Variables { get; set; } = [];

	/// <summary>
	/// Populates a <see cref="Variables"/> bag with all the definitions in this set, using each variable's name and
	/// initial value.
	/// </summary>
	/// <param name="target">The <see cref="Variables"/> instance to populate.</param>
	public void PopulateVariables(Variables target)
	{
		foreach (ForgeSharedVariableDefinition definition in Variables)
		{
			if (string.IsNullOrEmpty(definition.VariableName))
			{
				continue;
			}

			var key = new StringKey(definition.VariableName);

			if (definition.IsArray)
			{
				var initialValues = new Variant128[definition.InitialArrayValues.Count];
				for (int i = 0; i < definition.InitialArrayValues.Count; i++)
				{
					initialValues[i] = StatescriptVariableTypeConverter.GodotVariantToForge(
						definition.InitialArrayValues[i],
						definition.VariableType);
				}

				target.DefineArrayVariable(key, initialValues);
			}
			else
			{
				Variant128 value = StatescriptVariableTypeConverter.GodotVariantToForge(
					definition.InitialValue,
					definition.VariableType);

				target.DefineVariable(key, value);
			}
		}
	}
}
