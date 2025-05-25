// Copyright © Gamesmiths Guild.

#if TOOLS
using Godot;

namespace Gamesmiths.Forge.Godot.Core;

[Tool]
public partial class ForgeBootstrap : ISerializationListener
{
	public void OnBeforeSerialize()
	{
	}

	public void OnAfterDeserialize()
	{
		ForgeContext.Initialize();
	}
}
#endif
