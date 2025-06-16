// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Nodes;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Cues;

[Tool]
public partial class CueHandlerInspectorPlugin : EditorInspectorPlugin
{
	public override bool _CanHandle(GodotObject @object)
	{
		// Find out if its an implementation of CueHandler without having to add [Tool] attribute to them.
		if (@object?.GetScript().As<CSharpScript>() is CSharpScript script)
		{
			StringName className = script.GetGlobalName();

			Type baseType = typeof(ForgeCueHandler);
			System.Reflection.Assembly assembly = baseType.Assembly;

			Type? implementationType =
				Array.Find(assembly.GetTypes(), x =>
					x.Name == className &&
					baseType.IsAssignableFrom(x));

			return implementationType is not null;
		}

		return false;
	}

	public override bool _ParseProperty(
		GodotObject @object,
		Variant.Type type,
		string name,
		PropertyHint hintType,
		string hintString,
		PropertyUsageFlags usageFlags,
		bool wide)
	{
		if (name == "CueTag")
		{
			var cueKeyEditorProperty = new CueKeyEditorProperty();
			AddPropertyEditor(name, cueKeyEditorProperty);
			return true;
		}

		return base._ParseProperty(@object, type, name, hintType, hintString, usageFlags, wide);
	}
}
#endif
