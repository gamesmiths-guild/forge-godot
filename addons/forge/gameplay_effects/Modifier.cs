// Copyright © 2025 Gamesmiths Guild.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.GameplayEffects.Calculator.Godot;
using Gamesmiths.Forge.GameplayEffects.Magnitudes;
using Gamesmiths.Forge.GameplayEffects.Modifiers;
using Gamesmiths.Forge.GameplayTags;
using Godot;
using Godot.Collections;

using static Gamesmiths.Forge.Godot.Forge;

using ForgeModifier = Gamesmiths.Forge.GameplayEffects.Modifiers.Modifier;
using ForgeScalableFloat = Gamesmiths.Forge.GameplayEffects.Magnitudes.ScalableFloat;
using ScalableFloat = Gamesmiths.Forge.GameplayEffects.Magnitudes.Godot.ScalableFloat;

namespace Gamesmiths.Forge.GameplayEffects.Godot;

[Tool]
[GlobalClass]
public partial class Modifier : Resource
{
	private MagnitudeCalculationType _calculationType;
	private AttributeBasedFloatCalculationType _attributeCalculationType;

	[Export]
	public string Attribute { get; set; }

	[Export]
	public ModifierOperation Operation { get; set; }

	[Export]
	public int Channel { get; set; }

	[ExportGroup("Magnitude")]
	[Export]
	public MagnitudeCalculationType CalculationType
	{
		get => _calculationType;

		set
		{
			_calculationType = value;
			NotifyPropertyListChanged();
		}
	}

	[ExportGroup("Scalable Float")]
	[Export]
	public ScalableFloat ScalableFloat { get; set; }

	[ExportGroup("Attribute Based")]
	[Export]
	public string CapturedAttribute { get; set; }

	[ExportSubgroup("Attribute Based Capture Definition")]
	[Export]
	public AttributeCaptureSource CaptureSource { get; set; }

	[Export]
	public bool SnapshotAttribute { get; set; }

	[ExportSubgroup("Attribute Based Calculation")]
	[Export]
	public AttributeBasedFloatCalculationType AttributeCalculationType
	{
		get => _attributeCalculationType;

		set
		{
			_attributeCalculationType = value;
			NotifyPropertyListChanged();
		}
	}

	[Export]
	public ScalableFloat Coeficient { get; set; } = new(1);

	[Export]
	public ScalableFloat PreMultiplyAdditiveValue { get; set; }

	[Export]
	public ScalableFloat PostMultiplyAdditiveValue { get; set; }

	[Export]
	public int FinalChannel { get; set; }

	[ExportGroup("Custom Calculator Class")]
	[Export]
	public CustomCalculator CustomCalculatorClass { get; set; }

	[Export]
	public ScalableFloat CalculatorCoeficient { get; set; }

	[Export]
	public ScalableFloat CalculatorPreMultiplyAdditiveValue { get; set; }

	[Export]
	public ScalableFloat CalculatorPostMultiplyAdditiveValue { get; set; }

	[ExportGroup("Set by Caller Float")]
	[Export]
	public string CallerTargetTag { get; set; }

	public override void _ValidateProperty(Dictionary property)
	{
		if (property["name"].AsStringName() == PropertyName.Attribute ||
			property["name"].AsStringName() == PropertyName.CapturedAttribute)
		{
			property["hint"] = (int)PropertyHint.Enum;
			property["hint_string"] = string.Join(",", GetAttributeOptions());
		}

		if (property["name"].AsStringName() == PropertyName.ScalableFloat
			&& CalculationType != MagnitudeCalculationType.ScalableFloat)
		{
			property["usage"] = (int)PropertyUsageFlags.NoEditor;
		}

		if (CalculationType != MagnitudeCalculationType.AttributeBased
			&& (property["name"].AsStringName() == PropertyName.CapturedAttribute ||
				property["name"].AsStringName() == PropertyName.CaptureSource ||
				property["name"].AsStringName() == PropertyName.SnapshotAttribute ||
				property["name"].AsStringName() == PropertyName.AttributeCalculationType ||
				property["name"].AsStringName() == PropertyName.Coeficient ||
				property["name"].AsStringName() == PropertyName.PreMultiplyAdditiveValue ||
				property["name"].AsStringName() == PropertyName.PostMultiplyAdditiveValue ||
				property["name"].AsStringName() == PropertyName.FinalChannel))
		{
			property["usage"] = (int)PropertyUsageFlags.NoEditor;
		}

		if (property["name"].AsStringName() == PropertyName.FinalChannel &&
			AttributeCalculationType != AttributeBasedFloatCalculationType.AttributeMagnitudeEvaluatedUpToChannel)
		{
			property["usage"] = (int)PropertyUsageFlags.NoEditor;
		}

		if (CalculationType != MagnitudeCalculationType.CustomCalculatorClass
			&& (property["name"].AsStringName() == PropertyName.CustomCalculatorClass ||
				property["name"].AsStringName() == PropertyName.CalculatorCoeficient ||
				property["name"].AsStringName() == PropertyName.CalculatorPreMultiplyAdditiveValue ||
				property["name"].AsStringName() == PropertyName.CalculatorPostMultiplyAdditiveValue))
		{
			property["usage"] = (int)PropertyUsageFlags.NoEditor;
		}

		if (CalculationType != MagnitudeCalculationType.SetByCaller
			&& (property["name"].AsStringName() == PropertyName.CallerTargetTag))
		{
			property["usage"] = (int)PropertyUsageFlags.NoEditor;
		}
	}

	public ForgeModifier GetModifier()
	{
		return new ForgeModifier(
			Attribute,
			Operation,
			new ModifierMagnitude(
				CalculationType,
				GetScalableFloatMagnitude(),
				GetAttributeBasedFloat(),
				GetCustomCalculationBasedFloat(),
				GetSetByCallerFloat()),
			Channel);
	}

	/// <summary>
	/// Uses reflection to gather all classes inheriting from AttributeSet and their fields of type Attribute.
	/// </summary>
	/// <returns>An array with the avaiable attributes.</returns>
	private static string[] GetAttributeOptions()
	{
		var options = new List<string>();

		// Get all types in the current assembly
		System.Type[] allTypes = Assembly.GetExecutingAssembly().GetTypes();

		// Find all types that subclass AttributeSet
		foreach (System.Type attributeSetType in allTypes.Where(x => x.IsSubclassOf(typeof(AttributeSet))))
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

	private ForgeScalableFloat? GetScalableFloatMagnitude()
	{
		if (CalculationType != MagnitudeCalculationType.ScalableFloat)
		{
			return null;
		}

		return ScalableFloat.GetScalableFloat();
	}

	private AttributeBasedFloat? GetAttributeBasedFloat()
	{
		if (CalculationType != MagnitudeCalculationType.AttributeBased)
		{
			return null;
		}

		return new AttributeBasedFloat(
			new AttributeCaptureDefinition(
				CapturedAttribute,
				CaptureSource,
				SnapshotAttribute),
			AttributeCalculationType,
			Coeficient.GetScalableFloat(),
			PreMultiplyAdditiveValue.GetScalableFloat(),
			PostMultiplyAdditiveValue.GetScalableFloat(),
			FinalChannel);
	}

	private CustomCalculationBasedFloat? GetCustomCalculationBasedFloat()
	{
		if (CalculationType != MagnitudeCalculationType.CustomCalculatorClass)
		{
			return null;
		}

		return new CustomCalculationBasedFloat(
				CustomCalculatorClass.GetCustomCalculatorClass(),
				CalculatorCoeficient.GetScalableFloat(),
				CalculatorPreMultiplyAdditiveValue.GetScalableFloat(),
				CalculatorPostMultiplyAdditiveValue.GetScalableFloat(),
				null);
	}

	private SetByCallerFloat? GetSetByCallerFloat()
	{
		if (CalculationType == MagnitudeCalculationType.SetByCaller)
		{
			return null;
		}

		return new SetByCallerFloat(GameplayTag.RequestTag(TagsManager, CallerTargetTag));
	}
}
