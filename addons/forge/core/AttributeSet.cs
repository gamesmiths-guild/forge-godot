// Copyright © 2025 Gamesmiths Guild.

using System;
using System.Linq;
using System.Reflection;
using Godot;
using Godot.Collections;

using ForgeAttributeSet = Gamesmiths.Forge.Core.AttributeSet;

namespace Gamesmiths.Forge.Core.Godot;

[Tool]
public partial class AttributeSet : Node
{
	[Export]
	public string AttributeSetClass { get; set; }

	[Export]
	public Dictionary<string, AttributeValues> InitialAttributeValues { get; set; } = [];

	public ForgeAttributeSet GetAttributeSet()
	{
		if (string.IsNullOrEmpty(AttributeSetClass))
		{
			return null;
		}

		Type type = AppDomain.CurrentDomain.GetAssemblies()
			.SelectMany(x => x.GetTypes())
			.FirstOrDefault(x => x.Name == AttributeSetClass);

		if (type is null || !typeof(ForgeAttributeSet).IsAssignableFrom(type))
		{
			return null;
		}

		var instance = (ForgeAttributeSet)Activator.CreateInstance(type);
		if (instance is null)
		{
			return null;
		}

		foreach (PropertyInfo prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
		{
			if (prop.PropertyType == typeof(Attribute) && prop.CanWrite)
			{
				var name = prop.Name;
				if (InitialAttributeValues.TryGetValue(name, out AttributeValues value))
				{
#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
					// Do not attempt this in production environments without adult supervision.
					MethodInfo method = type.GetMethod(
						"InitializeAttribute",
						BindingFlags.Instance | BindingFlags.NonPublic);
#pragma warning restore S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields

					if (method is not null)
					{
						var attribute = (Attribute)method.Invoke(instance, [name, value.Current, value.Min, value.Max]);
						prop.SetValue(instance, attribute);
					}
				}
			}
		}

		return instance;
	}
}
