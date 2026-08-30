// Copyright © Gamesmiths Guild.

using Godot;
using GodotDictionary = Godot.Collections.Dictionary;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Interop;

/// <summary>
/// Shared checks for the property paths the interop node and resolver author.
/// </summary>
internal static class NodePropertyAccess
{
	/// <summary>
	/// Gets whether a Godot object declares the property a path starts at.
	/// </summary>
	/// <remarks>
	/// Only the first segment is checked, which is where a typo lands; a bad subname past it is Godot's own error to
	/// report. Reading and writing both go through <c>GetIndexed</c> and <c>SetIndexed</c>, which answer an unknown
	/// path with nothing at all rather than with a complaint, so without this a misspelled path is a node that quietly
	/// does nothing.
	/// </remarks>
	/// <param name="target">The object to check.</param>
	/// <param name="propertyPath">The authored property path.</param>
	/// <returns><see langword="true"/> when the object declares the property.</returns>
	public static bool DeclaresProperty(GodotObject target, NodePath propertyPath)
	{
		NodePath asPropertyPath = propertyPath.GetAsPropertyPath();

		if (asPropertyPath.GetSubNameCount() == 0)
		{
			return false;
		}

		string name = asPropertyPath.GetSubName(0);

		foreach (GodotDictionary property in target.GetPropertyList())
		{
			if (property.TryGetValue("name", out Variant declaredName) && declaredName.AsString() == name)
			{
				return true;
			}
		}

		return false;
	}
}
