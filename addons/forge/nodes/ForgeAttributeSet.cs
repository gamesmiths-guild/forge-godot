// Copyright © Gamesmiths Guild.

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Gamesmiths.Forge.Attributes;
using Gamesmiths.Forge.Godot.Editor.Attributes;
using Godot;
using Godot.Collections;

namespace Gamesmiths.Forge.Godot.Nodes;

[Tool]
[GlobalClass]
[Icon("uid://dnqaqpc02lx3p")]
public partial class ForgeAttributeSet : Node
{
	[Export]
	public string AttributeSetClass { get; set; } = string.Empty;

	[Export]
	public Dictionary<string, AttributeValues>? InitialAttributeValues { get; set; }

	public override void _Ready()
	{
		base._Ready();

		InitialAttributeValues ??= [];
	}

	public AttributeSet? GetAttributeSet()
	{
		if (string.IsNullOrEmpty(AttributeSetClass))
		{
			return null;
		}

		Debug.Assert(
			InitialAttributeValues is not null,
			$"{nameof(InitialAttributeValues)} should have been initialized on _Ready.");

		Type? type = AppDomain.CurrentDomain.GetAssemblies()
			.SelectMany(x => x.GetTypes())
			.FirstOrDefault(x => x.Name == AttributeSetClass);

		if (type is null || !typeof(AttributeSet).IsAssignableFrom(type))
		{
			return null;
		}

		var instance = (AttributeSet?)Activator.CreateInstance(type);
		if (instance is null)
		{
			return null;
		}

		foreach (PropertyInfo prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
		{
			if (prop.PropertyType == typeof(EntityAttribute))
			{
				var name = prop.Name;
				if (InitialAttributeValues.TryGetValue(name, out AttributeValues? value))
				{
					SetAttributeValue("SetAttributeBaseValue", instance, prop, value.Default);
					SetAttributeValue("SetAttributeMinValue", instance, prop, value.Min);
					SetAttributeValue("SetAttributeMaxValue", instance, prop, value.Max);
				}
			}
		}

		return instance;
	}

	private static void SetAttributeValue(string methodName, AttributeSet instance, PropertyInfo prop, int value)
	{
#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
		// Do not attempt this in production environments without adult supervision.
		MethodInfo? method = typeof(AttributeSet).GetMethod(
			methodName,
			BindingFlags.Static | BindingFlags.NonPublic);
#pragma warning restore S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields

		method?.Invoke(null, [(EntityAttribute?)prop.GetValue(instance), value]);
	}
}
