// Copyright © Gamesmiths Guild.

#if TOOLS
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

/// <summary>
/// Node editor for the <c>SwitchNode</c>. The case count is a constructor argument, so this editor renders it as a
/// spin box — or, when an enum is bound, follows the enum and names each case port after its member. The trailing
/// Default port, taken whenever the selector falls outside the cases, is not part of the enum.
/// </summary>
[Tool]
internal sealed partial class SwitchNodeEditor : PortCountNodeEditorBase
{
	/// <inheritdoc/>
	public override string HandledRuntimeTypeName => "Gamesmiths.Forge.Statescript.Nodes.Action.SwitchNode";

	/// <inheritdoc/>
	protected override string CountConfigKey => "caseCount";

	/// <inheritdoc/>
	protected override string CountLabel => "Cases";

	/// <inheritdoc/>
	protected override int DefaultCount => 2;

	/// <inheritdoc/>
	protected override int MinCount => 1;

	/// <inheritdoc/>
	protected override int MaxCount => byte.MaxValue;

	/// <inheritdoc/>
	protected override int FirstEnumPortIndex => 0;

	/// <inheritdoc/>
	internal override int RemapOutputPort(
		int runtimePortIndex,
		StatescriptNodeDiscovery.NodeTypeInfo oldTypeInfo,
		StatescriptNodeDiscovery.NodeTypeInfo newTypeInfo)
	{
		int newDefaultPort = newTypeInfo.OutputPortLabels.Length - 1;

		// The Default port is always the last output, so it shifts with every case added or removed. Without this, a
		// wire on Default would silently become a wire on the case that took over its index.
		if (runtimePortIndex == oldTypeInfo.OutputPortLabels.Length - 1)
		{
			return newDefaultPort;
		}

		// A case the new count no longer has is gone, even when its index is still in range — lowering the count to 2
		// leaves index 2 pointing at Default, and a wire from the old Case 2 must not quietly become the fallback.
		return runtimePortIndex < newDefaultPort ? runtimePortIndex : -1;
	}

	/// <inheritdoc/>
	protected override int GetEnumPortCount(StatescriptNodeDiscovery.NodeTypeInfo typeInfo)
	{
		// Every output port is a case except the last one, which is the Default port.
		return typeInfo.OutputPortLabels.Length - 1;
	}
}
#endif
