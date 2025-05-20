// Copyright © Gamesmiths Guild.

#if TOOLS
using Godot;

namespace Gamesmiths.Forge.Godot.Core;

[Tool]
public partial class Forge : ISerializationListener
{
	public void OnBeforeSerialize()
	{
	}

	public void OnAfterDeserialize()
	{
		Initialize();
	}
}
#endif
