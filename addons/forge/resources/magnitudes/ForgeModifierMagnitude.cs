// Copyright © Gamesmiths Guild.

using System.Diagnostics;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Godot.Resources.Calculators;
using Gamesmiths.Forge.Godot.Resources.Magnitudes;
using Godot;
using Godot.Collections;

namespace Gamesmiths.Forge.Godot.Resources;

[Tool]
[GlobalClass]
[Icon("uid://cesj1p71jsco5")]
public partial class ForgeModifierMagnitude : Resource
{
	private MagnitudeCalculationType _calculationType;
	private AttributeCalculationType _attributeCalculationType;

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
	public ForgeScalableFloat? ScalableFloat { get; set; }

	[ExportGroup("Attribute Based")]
	[Export]
	public string? CapturedAttribute { get; set; }

	[ExportSubgroup("Attribute Based Capture Definition")]
	[Export]
	public AttributeCaptureSource CaptureSource { get; set; }

	[Export]
	public bool SnapshotAttribute { get; set; } = true;

	[ExportSubgroup("Attribute Based Calculation")]
	[Export]
	public AttributeCalculationType AttributeCalculationType
	{
		get => _attributeCalculationType;

		set
		{
			_attributeCalculationType = value;
			NotifyPropertyListChanged();
		}
	}

	[Export]
	public ForgeScalableFloat Coefficient { get; set; } = new(1);

	[Export]
	public ForgeScalableFloat PreMultiplyAdditiveValue { get; set; } = new(0);

	[Export]
	public ForgeScalableFloat PostMultiplyAdditiveValue { get; set; } = new(0);

	[Export]
	public int FinalChannel { get; set; }

	[ExportGroup("Custom Calculator Class")]
	[Export]
	public ForgeCustomCalculator? CustomCalculatorClass { get; set; }

	[Export]
	public ForgeScalableFloat CalculatorCoefficient { get; set; } = new(1);

	[Export]
	public ForgeScalableFloat CalculatorPreMultiplyAdditiveValue { get; set; } = new(0);

	[Export]
	public ForgeScalableFloat CalculatorPostMultiplyAdditiveValue { get; set; } = new(0);

	[ExportGroup("Set by Caller Float")]
	[Export]
	public ForgeTag? CallerTargetTag { get; set; }

#if TOOLS
	public bool IsInstantEffect { get; set; }
#endif

	public ModifierMagnitude GetModifier()
	{
		return new ModifierMagnitude(
				CalculationType,
				GetScalableFloatMagnitude(),
				GetAttributeBasedFloat(),
				GetCustomCalculationBasedFloat(),
				GetSetByCallerFloat());
	}

#if TOOLS
	public override void _ValidateProperty(Dictionary property)
	{
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
				property["name"].AsStringName() == PropertyName.Coefficient ||
				property["name"].AsStringName() == PropertyName.PreMultiplyAdditiveValue ||
				property["name"].AsStringName() == PropertyName.PostMultiplyAdditiveValue ||
				property["name"].AsStringName() == PropertyName.FinalChannel))
		{
			property["usage"] = (int)PropertyUsageFlags.NoEditor;
		}
		else if (property["name"].AsStringName() == PropertyName.SnapshotAttribute && IsInstantEffect)
		{
			property["usage"] = (int)(PropertyUsageFlags.Default | PropertyUsageFlags.ReadOnly);
			SnapshotAttribute = true;
		}

		if (property["name"].AsStringName() == PropertyName.FinalChannel &&
			AttributeCalculationType != AttributeCalculationType.MagnitudeEvaluatedUpToChannel)
		{
			property["usage"] = (int)PropertyUsageFlags.NoEditor;
		}

		if (CalculationType != MagnitudeCalculationType.CustomCalculatorClass
			&& (property["name"].AsStringName() == PropertyName.CustomCalculatorClass ||
				property["name"].AsStringName() == PropertyName.CalculatorCoefficient ||
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
#endif

	private ScalableFloat? GetScalableFloatMagnitude()
	{
		if (CalculationType != MagnitudeCalculationType.ScalableFloat)
		{
			return null;
		}

		Debug.Assert(ScalableFloat is not null, $"{nameof(ScalableFloat)} reference is missing.");

		return ScalableFloat.GetScalableFloat();
	}

	private AttributeBasedFloat? GetAttributeBasedFloat()
	{
		if (CalculationType != MagnitudeCalculationType.AttributeBased)
		{
			return null;
		}

		Debug.Assert(CapturedAttribute is not null, $"{nameof(CapturedAttribute)} reference is missing.");

		return new AttributeBasedFloat(
			new AttributeCaptureDefinition(
				CapturedAttribute,
				CaptureSource,
				SnapshotAttribute),
			AttributeCalculationType,
			Coefficient.GetScalableFloat(),
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

		Debug.Assert(CustomCalculatorClass is not null, $"{nameof(CustomCalculatorClass)} reference is missing.");

		return new CustomCalculationBasedFloat(
				CustomCalculatorClass.GetCustomCalculatorClass(),
				CalculatorCoefficient.GetScalableFloat(),
				CalculatorPreMultiplyAdditiveValue.GetScalableFloat(),
				CalculatorPostMultiplyAdditiveValue.GetScalableFloat(),
				null);
	}

	private SetByCallerFloat? GetSetByCallerFloat()
	{
		if (CalculationType != MagnitudeCalculationType.SetByCaller)
		{
			return null;
		}

		return new SetByCallerFloat(CallerTargetTag!.GetTag());
	}
}
