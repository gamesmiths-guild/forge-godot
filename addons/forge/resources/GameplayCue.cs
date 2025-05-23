// Copyright © Gamesmiths Guild.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.GameplayCues;
using Godot;
using Godot.Collections;

using static Gamesmiths.Forge.Godot.Core.Forge;

using Attribute = Gamesmiths.Forge.Core.Attribute;

namespace Gamesmiths.Forge.Godot.Resources;

[Tool]
[GlobalClass]
[Icon("uid://din7fexs0x53h")]
public partial class GameplayCue : Resource
{
	private CueMagnitudeType _magnitudeType;

	[Export]
	public string? CueKey { get; set; }

	[Export]
	public int MinValue { get; set; }

	[Export]
	public int MaxValue { get; set; }

	[Export]
	public CueMagnitudeType MagnitudeType
	{
		get => _magnitudeType;

		set
		{
			_magnitudeType = value;
			NotifyPropertyListChanged();
		}
	}

	[Export]
	public string? MagnitudeAttribute { get; set; }

	public GameplayCueData GetGameplayCueData()
	{
		Debug.Assert(!string.IsNullOrEmpty(CueKey), $"{nameof(CueKey)} should have been defined.");

		return new GameplayCueData(
			CueKey,
			MinValue,
			MaxValue,
			MagnitudeType,
			string.IsNullOrEmpty(MagnitudeAttribute) ? null : MagnitudeAttribute);
	}

	public override void _ValidateProperty(Dictionary property)
	{
		if (property["name"].AsStringName() == PropertyName.MagnitudeAttribute)
		{
			property["hint"] = (int)PropertyHint.Enum;
			property["hint_string"] = string.Join(",", GetAttributeOptions());

			if (MagnitudeType == CueMagnitudeType.EffectLevel || MagnitudeType == CueMagnitudeType.StackCount)
			{
				property["usage"] = (int)(PropertyUsageFlags.Default | PropertyUsageFlags.ReadOnly);
				MagnitudeAttribute = null;
			}
		}

		if (property["name"].AsStringName() == PropertyName.CueKey)
		{
			property["hint"] = (int)PropertyHint.Enum;
			property["hint_string"] = string.Join(",", GetCueOptions());
		}
	}

	/// <summary>
	/// Uses reflection to gather all classes inheriting from AttributeSet and their fields of type Attribute.
	/// </summary>
	/// <returns>An array with the avaiable attributes.</returns>
	private static string[] GetAttributeOptions()
	{
		var options = new List<string>();

		// Get all types in the current assembly
		Type[] allTypes = Assembly.GetExecutingAssembly().GetTypes();

		// Find all types that subclass AttributeSet
		foreach (Type attributeSetType in allTypes.Where(x => x.IsSubclassOf(typeof(AttributeSet))))
		{
			// Get public instance properties of type Attribute
			IEnumerable<PropertyInfo> attributeProperties =
				attributeSetType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
					.Where(x => x.PropertyType == typeof(Attribute));

			foreach (PropertyInfo field in attributeProperties)
			{
				// Build the dropdown option string in the format ClassName.FieldName
				var option = $"{attributeSetType.Name}.{field.Name}";
				options.Add(option);
			}
		}

		return [.. options];
	}

	private static string[] GetCueOptions()
	{
		if (RegisteredCues is null)
		{
			return [];
		}

		return [.. RegisteredCues];
	}
}
