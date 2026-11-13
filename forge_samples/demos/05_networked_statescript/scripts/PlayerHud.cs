// Copyright © Gamesmiths Guild.

using System.Linq;
using System.Text;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Godot.Nodes;
using Gamesmiths.Forge.Godot.Resources.Abilities;
using Gamesmiths.Forge.Tags;
using Godot;

namespace Gamesmiths.Forge.Example;

/// <summary>
/// The Statescript demo's HUD for this machine's player: the action bar, the tags and the click of a press refused for
/// cooldown or mana. It reads the entity the session names as the local player, which on a client is the one it
/// predicts, so a cooldown starts counting the moment the key is pressed.
/// </summary>
public partial class PlayerHud : CanvasLayer
{
	private const AbilityActivationFailures RefusalsWorthAClick =
		AbilityActivationFailures.Cooldown | AbilityActivationFailures.InsufficientResources;

	private ForgeEntity? _player;
	private AudioStreamPlayer3D? _activationFailedSfx;
	private SkillSlot?[] _skillSlots = [];

	[Export]
	public required ForgeNetworkSession Session { get; set; }

	[Export]
	public ForgeAbilityData? ProjectileAbility { get; set; }

	[Export]
	public ForgeAbilityData? DashAbility { get; set; }

	[Export]
	public ForgeAbilityData? ShieldAbility { get; set; }

	[Export]
	public ForgeAbilityData? ThornsAbility { get; set; }

	[Export]
	public ActionBarView? Skill1View { get; set; }

	[Export]
	public ActionBarView? Skill2View { get; set; }

	[Export]
	public ActionBarView? Skill3View { get; set; }

	[Export]
	public ActionBarView? Skill4View { get; set; }

	[Export]
	public Label? TagsView { get; set; }

	public override void _Ready()
	{
		base._Ready();

		Visible = false;
		Session.LocalPlayerChanged += OnLocalPlayerChanged;
	}

	public override void _Process(double delta)
	{
		base._Process(delta);

		if (_player is null)
		{
			return;
		}

		(ForgeAbilityData? Data, ActionBarView? View)[] configs =
		[
			(ProjectileAbility, Skill1View),
			(DashAbility, Skill2View),
			(ShieldAbility, Skill3View),
			(ThornsAbility, Skill4View),
		];

		for (int i = 0; i < configs.Length; i++)
		{
			// A client grants its player's abilities as the session starts predicting it, a moment after it appears.
			_skillSlots[i] ??= SkillSlot.Create(_player, configs[i].Data, configs[i].View);
			_skillSlots[i]?.UpdateView();
		}

		if (TagsView is not null)
		{
			var tagsText = new StringBuilder();

			foreach (Tag tag in _player.Tags.AllTags)
			{
				tagsText.Append(tag.ToString()).Append('\n');
			}

			TagsView.Text = tagsText.ToString();
		}
	}

	private void OnLocalPlayerChanged(Node? player)
	{
		if (_player is not null && IsInstanceValid(_player))
		{
			_player.Abilities.OnAbilityActivationFailed -= OnActivationFailed;
		}

		_player = player as ForgeEntity;
		_skillSlots = new SkillSlot?[4];
		Visible = _player is not null;

		if (_player is not null)
		{
			_activationFailedSfx = _player.GetParent().GetNodeOrNull<AudioStreamPlayer3D>("DryFireSfx");
			_player.Abilities.OnAbilityActivationFailed += OnActivationFailed;
		}
	}

	// Other refusals stay silent: pressing the shield's key while it is up is refused as well, and that press is the
	// toggle off.
	private void OnActivationFailed(AbilityHandle handle, AbilityActivationFailures failures)
	{
		if ((failures & RefusalsWorthAClick) != 0)
		{
			_activationFailedSfx?.Play();
		}
	}

	// The same view mirror the Statescript demo's controller keeps, so the two HUDs read identically.
	private sealed class SkillSlot(AbilityHandle handle, ActionBarView view, Tag? cooldownTag, float totalCooldownTime)
	{
		public static SkillSlot? Create(ForgeEntity forgeEntity, ForgeAbilityData? data, ActionBarView? view)
		{
			if (data is null || view is null
				|| !forgeEntity.Abilities.TryGetAbility(data.GetAbilityData(), out AbilityHandle? handle, forgeEntity))
			{
				return null;
			}

			Tag? cooldownTag = null;
			float totalCooldownTime = 0f;
			CooldownData[]? cooldownData = handle.GetCooldownData();

			if (cooldownData is { Length: > 0 })
			{
				cooldownTag = cooldownData[0].CooldownTags.First();
				totalCooldownTime = cooldownData[0].TotalTime;
			}

			return new SkillSlot(handle, view, cooldownTag, totalCooldownTime);
		}

		public void UpdateView()
		{
			if (cooldownTag is not null)
			{
				view.UpdateCooldown(handle.GetRemainingCooldownTime(cooldownTag.Value), totalCooldownTime);
			}

			CostData[]? costData = handle.GetCostData();

			if (costData is { Length: > 0 })
			{
				view.UpdateCost($"{costData[0].Cost}");
			}

			view.UpdateActive(handle.IsActive);
		}
	}
}
