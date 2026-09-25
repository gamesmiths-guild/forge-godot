// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Linq;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript;

/// <summary>
/// Entry points used by the editor test suite to drive the variables panel the way its controls do.
/// </summary>
/// <remarks>
/// A partial of the shipping panel, kept under <c>tests/</c> so it compiles only for this repository's own Debug
/// builds and never reaches an installed copy of the plugin.
/// </remarks>
internal sealed partial class StatescriptVariablePanel
{
	/// <summary>
	/// Changes a variable's initial value through the recording path its value editor uses.
	/// </summary>
	/// <param name="variableName">The variable to change.</param>
	/// <param name="value">The new initial value.</param>
	internal void TestOnlySetVariableValue(string variableName, Variant value)
	{
		StatescriptGraphVariable? variable = _graph?.Variables.FirstOrDefault(x => x.VariableName == variableName);
		if (variable is not null)
		{
			SetVariableValue(variable, value);
		}
	}
}
#endif
