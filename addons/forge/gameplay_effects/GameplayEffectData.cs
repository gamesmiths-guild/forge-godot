// Copyright © 2025 Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.GameplayCues;
using Gamesmiths.Forge.GameplayCues.Godot;
using Gamesmiths.Forge.GameplayEffects.Calculator.Godot;
using Gamesmiths.Forge.GameplayEffects.Components;
using Gamesmiths.Forge.GameplayEffects.Components.Godot;
using Gamesmiths.Forge.GameplayEffects.Duration;
using Gamesmiths.Forge.GameplayEffects.Periodic;
using Gamesmiths.Forge.GameplayEffects.Stacking;
using Godot;
using Godot.Collections;
using ForgeExecution = Gamesmiths.Forge.GameplayEffects.Calculator.Execution;
using ForgeGameplayEffectData = Gamesmiths.Forge.GameplayEffects.GameplayEffectData;
using ForgeModifier = Gamesmiths.Forge.GameplayEffects.Modifiers.Modifier;
using ForgeScalableFloat = Gamesmiths.Forge.GameplayEffects.Magnitudes.ScalableFloat;
using ScalableFloat = Gamesmiths.Forge.GameplayEffects.Magnitudes.Godot.ScalableFloat;
using ScalableInt = Gamesmiths.Forge.GameplayEffects.Magnitudes.Godot.ScalableInt;

namespace Gamesmiths.Forge.GameplayEffects.Godot;

[Tool]
public partial class GameplayEffectData : Resource
{
	private ForgeGameplayEffectData? _data;

	private DurationType _durationType;
	private bool _hasPeriodicApplication;
	private bool _canStack;

	private StackPolicy _sourcePolicy;
	private StackLevelPolicy _levelPolicy;
	private StackOwnerOverridePolicy _instigatorOverridePolicy;
	private LevelComparison _levelOverridePolicy;

	[Export]
	public string Name { get; set; }

	[Export]
	public bool SnapshotLevel { get; set; }

	[Export]
	public bool RequireModifierSuccessToTriggerCue { get; set; }

	[Export]
	public bool SuppressStackingCues { get; set; }

	[ExportGroup("Modifier Data")]

	[Export(PropertyHint.ResourceType, "Modifier")]
	public Array<Modifier> Modifiers { get; set; } = [];

	[ExportGroup("Components")]

	[Export(PropertyHint.ResourceType, "EffectComponent")]
	public Array<EffectComponent> Components { get; set; } = [];

	[ExportGroup("Executions")]

	[Export(PropertyHint.ResourceType, "Execution")]
	public Array<Execution> Executions { get; set; } = [];

	[ExportGroup("Duration Data")]
	[Export]
	public DurationType DurationType
	{
		get => _durationType;

		set
		{
			_durationType = value;

			if (value == DurationType.Instant)
			{
				_hasPeriodicApplication = false;
				_canStack = false;
			}

			NotifyPropertyListChanged();
		}
	}

	[Export]
	public ScalableFloat Duration { get; set; }

	[ExportGroup("Periodic Data")]

	[Export]
	public bool HasPeriodicApplication
	{
		get => _hasPeriodicApplication;

		set
		{
			_hasPeriodicApplication = value;
			NotifyPropertyListChanged();
		}
	}

	[Export]
	public ScalableFloat Period { get; set; }

	[Export]
	public bool ExecuteOnApplication { get; set; }

	[ExportGroup("Stacking Data")]
	[Export]
	public bool CanStack
	{
		get => _canStack;

		set
		{
			_canStack = value;
			NotifyPropertyListChanged();
		}
	}

	[Export]
	public ScalableInt StackLimit { get; set; } = new ScalableInt(1);

	[Export]
	public ScalableInt InitialStack { get; set; } = new ScalableInt(1);

	[Export]
	public StackPolicy SourcePolicy
	{
		get => _sourcePolicy;

		set
		{
			_sourcePolicy = value;
			NotifyPropertyListChanged();
		}
	}

	[Export]
	[ExportSubgroup("Aggregate by Target", "Instigator")]
	public StackOwnerDenialPolicy InstigatorDenialPolicy { get; set; }

	[Export]
	public StackOwnerOverridePolicy InstigatorOverridePolicy
	{
		get => _instigatorOverridePolicy;

		set
		{
			_instigatorOverridePolicy = value;
			NotifyPropertyListChanged();
		}
	}

	[Export]
	public StackOwnerOverrideStackCountPolicy InstigatorOverrideStackCountPolicy { get; set; }

	[Export]
	public StackLevelPolicy LevelPolicy
	{
		get => _levelPolicy;

		set
		{
			_levelPolicy = value;
			NotifyPropertyListChanged();
		}
	}

	[Export]
	[ExportSubgroup("Aggregate Levels", "Level")]
	public LevelComparison LevelDenialPolicy { get; set; }

	[Export]
	public LevelComparison LevelOverridePolicy
	{
		get => _levelOverridePolicy;

		set
		{
			_levelOverridePolicy = value;
			NotifyPropertyListChanged();
		}
	}

	[Export]
	public StackLevelOverrideStackCountPolicy LevelOverrideStackCountPolicy { get; set; }

	[Export]
	public StackMagnitudePolicy MagnitudePolicy { get; set; }

	[Export]
	public StackOverflowPolicy OverflowPolicy { get; set; }

	[Export]
	public StackExpirationPolicy ExpirationPolicy { get; set; }

	[Export]
	[ExportSubgroup("Has Duration")]
	public StackApplicationRefreshPolicy ApplicationRefreshPolicy { get; set; }

	[Export]
	[ExportSubgroup("Periodic Effects")]
	public StackApplicationResetPeriodPolicy ApplicationResetPeriodPolicy { get; set; }

	[Export]
	public bool ExecuteOnSuccessfulApplication { get; set; }

	[ExportGroup("Gameplay Cues")]
	[Export(PropertyHint.ResourceType, "GameplayCue")]
	public Array<GameplayCue> GameplayCues { get; set; } = [];

	public override void _ValidateProperty(Dictionary property)
	{
		if (property["name"].AsStringName() == PropertyName.Duration && DurationType != DurationType.HasDuration)
		{
			property["usage"] = (int)(PropertyUsageFlags.Default | PropertyUsageFlags.ReadOnly);
		}

		if (DurationType == DurationType.Instant && property["name"].AsStringName() == PropertyName.HasPeriodicApplication)
		{
			property["usage"] = (int)(PropertyUsageFlags.Default | PropertyUsageFlags.ReadOnly);
		}

		if (!HasPeriodicApplication
			&& (property["name"].AsStringName() == PropertyName.Period ||
				property["name"].AsStringName() == PropertyName.ExecuteOnApplication ||
				property["name"].AsStringName() == PropertyName.ApplicationResetPeriodPolicy ||
				property["name"].AsStringName() == PropertyName.ExecuteOnSuccessfulApplication))
		{
			property["usage"] = (int)(PropertyUsageFlags.Default | PropertyUsageFlags.ReadOnly);
		}

		if ((DurationType == DurationType.Instant || !CanStack)
			&& (property["name"].AsStringName() == PropertyName.StackLimit ||
				property["name"].AsStringName() == PropertyName.InitialStack ||
				property["name"].AsStringName() == PropertyName.SourcePolicy ||
				property["name"].AsStringName() == PropertyName.InstigatorDenialPolicy ||
				property["name"].AsStringName() == PropertyName.InstigatorOverridePolicy ||
				property["name"].AsStringName() == PropertyName.InstigatorOverrideStackCountPolicy ||
				property["name"].AsStringName() == PropertyName.LevelPolicy ||
				property["name"].AsStringName() == PropertyName.LevelDenialPolicy ||
				property["name"].AsStringName() == PropertyName.LevelOverridePolicy ||
				property["name"].AsStringName() == PropertyName.LevelOverrideStackCountPolicy ||
				property["name"].AsStringName() == PropertyName.MagnitudePolicy ||
				property["name"].AsStringName() == PropertyName.OverflowPolicy ||
				property["name"].AsStringName() == PropertyName.ExpirationPolicy ||
				property["name"].AsStringName() == PropertyName.ApplicationRefreshPolicy ||
				property["name"].AsStringName() == PropertyName.ApplicationResetPeriodPolicy ||
				property["name"].AsStringName() == PropertyName.ExecuteOnSuccessfulApplication))
		{
			property["usage"] = (int)(PropertyUsageFlags.Default | PropertyUsageFlags.ReadOnly);
		}

		if (SourcePolicy == StackPolicy.AggregateBySource
			&& (property["name"].AsStringName() == PropertyName.InstigatorDenialPolicy ||
				property["name"].AsStringName() == PropertyName.InstigatorOverridePolicy ||
				property["name"].AsStringName() == PropertyName.InstigatorOverrideStackCountPolicy))
		{
			property["usage"] = (int)(PropertyUsageFlags.Default | PropertyUsageFlags.ReadOnly);
		}

		if (InstigatorOverridePolicy != StackOwnerOverridePolicy.Override && property["name"].AsStringName() == PropertyName.InstigatorOverrideStackCountPolicy)
		{
			property["usage"] = (int)(PropertyUsageFlags.Default | PropertyUsageFlags.ReadOnly);
		}

		if (LevelPolicy == StackLevelPolicy.SegregateLevels
			&& (property["name"].AsStringName() == PropertyName.LevelDenialPolicy ||
				property["name"].AsStringName() == PropertyName.LevelOverridePolicy ||
				property["name"].AsStringName() == PropertyName.LevelOverrideStackCountPolicy))
		{
			property["usage"] = (int)(PropertyUsageFlags.Default | PropertyUsageFlags.ReadOnly);
		}

		if (LevelOverridePolicy == 0 && property["name"].AsStringName() == PropertyName.LevelOverrideStackCountPolicy)
		{
			property["usage"] = (int)(PropertyUsageFlags.Default | PropertyUsageFlags.ReadOnly);
		}

		if (DurationType != DurationType.HasDuration &&
			property["name"].AsStringName() == PropertyName.ApplicationRefreshPolicy)
		{
			property["usage"] = (int)(PropertyUsageFlags.Default | PropertyUsageFlags.ReadOnly);
		}
	}

	public ForgeGameplayEffectData GetEffectData()
	{
		if (_data.HasValue)
		{
			return _data.Value;
		}

		var modifiers = new List<ForgeModifier>();
		foreach (Modifier modifier in Modifiers)
		{
			modifiers.Add(modifier.GetModifier());
		}

		var components = new List<IGameplayEffectComponent>();
		foreach (EffectComponent component in Components)
		{
			components.Add(component.GetComponent());
		}

		var executions = new List<ForgeExecution>();
		foreach (Execution execution in Executions)
		{
			executions.Add(execution.GetExecutionClass());
		}

		var gameplayCues = new List<GameplayCueData>();
		foreach (GameplayCue gameplayCue in GameplayCues)
		{
			gameplayCues.Add(gameplayCue.GetGameplayCueData());
		}

		_data = new ForgeGameplayEffectData(
			Name,
			[.. modifiers],
			GetDurationData(),
			GetStackingData(),
			GetPeriodicData(),
			SnapshotLevel,
			[.. components],
			RequireModifierSuccessToTriggerCue,
			SuppressStackingCues,
			[.. executions],
			[.. gameplayCues]);

		return _data.Value;
	}

	private DurationData GetDurationData()
	{
		return new DurationData(DurationType, GetDuration());
	}

	private ForgeScalableFloat? GetDuration()
	{
		if (DurationType != DurationType.HasDuration)
		{
			return null;
		}

		return Duration.GetScalableFloat();
	}

	private StackingData? GetStackingData()
	{
		if (!CanStack)
		{
			return null;
		}

		return new StackingData(
			StackLimit.GetScalableInt(),
			InitialStack.GetScalableInt(),
			SourcePolicy,
			LevelPolicy,
			MagnitudePolicy,
			OverflowPolicy,
			ExpirationPolicy,
			GetOwnerDenialPolicy(),
			GetOwnerOverridePolicy(),
			GetOwnerOverrideStackCountPolicy(),
			GetLevelDenialPolicy(),
			GetLevelOverridePolicy(),
			GetLevelOverrideStackCountPolicy(),
			GetApplicationRefreshPolicy(),
			GetApplicationResetPeriodPolicy(),
			GetExecuteOnSuccessfulApplication());
	}

	private StackOwnerDenialPolicy? GetOwnerDenialPolicy()
	{
		if (SourcePolicy != StackPolicy.AggregateByTarget)
		{
			return null;
		}

		return InstigatorDenialPolicy;
	}

	private StackOwnerOverridePolicy? GetOwnerOverridePolicy()
	{
		if (SourcePolicy != StackPolicy.AggregateByTarget)
		{
			return null;
		}

		return InstigatorOverridePolicy;
	}

	private StackOwnerOverrideStackCountPolicy? GetOwnerOverrideStackCountPolicy()
	{
		if (SourcePolicy != StackPolicy.AggregateByTarget ||
			InstigatorOverridePolicy != StackOwnerOverridePolicy.Override)
		{
			return null;
		}

		return InstigatorOverrideStackCountPolicy;
	}

	private LevelComparison? GetLevelDenialPolicy()
	{
		if (LevelPolicy != StackLevelPolicy.AggregateLevels)
		{
			return null;
		}

		return LevelDenialPolicy;
	}

	private LevelComparison? GetLevelOverridePolicy()
	{
		if (LevelPolicy != StackLevelPolicy.AggregateLevels)
		{
			return null;
		}

		return LevelOverridePolicy;
	}

	private StackLevelOverrideStackCountPolicy? GetLevelOverrideStackCountPolicy()
	{
		if (LevelPolicy != StackLevelPolicy.AggregateLevels ||
			LevelOverridePolicy == 0)
		{
			return null;
		}

		return LevelOverrideStackCountPolicy;
	}

	private StackApplicationRefreshPolicy? GetApplicationRefreshPolicy()
	{
		if (DurationType != DurationType.HasDuration)
		{
			return null;
		}

		return ApplicationRefreshPolicy;
	}

	private StackApplicationResetPeriodPolicy? GetApplicationResetPeriodPolicy()
	{
		if (!HasPeriodicApplication)
		{
			return null;
		}

		return ApplicationResetPeriodPolicy;
	}

	private bool? GetExecuteOnSuccessfulApplication()
	{
		if (!HasPeriodicApplication)
		{
			return null;
		}

		return ExecuteOnSuccessfulApplication;
	}

	private PeriodicData? GetPeriodicData()
	{
		if (!HasPeriodicApplication)
		{
			return null;
		}

		return new PeriodicData(Period.GetScalableFloat(), ExecuteOnApplication, PeriodInhibitionRemovedPolicy.NeverReset);
	}
}
