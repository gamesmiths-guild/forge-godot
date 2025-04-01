// Copyright © 2025 Gamesmiths Guild.

#if TOOLS
using Gamesmiths.Forge.GameplayTags;
using Gamesmiths.Forge.Godot.GameplayTags;
using Godot;

namespace Gamesmiths.Forge.Godot;

[Tool]
public partial class ForgePluginLoader : EditorPlugin
{
	private const string AutoloadPath = "res://addons/forge/Forge.cs";
	private const string PluginScenePath = "res://addons/forge/gameplay_tags/GameplayTags.tscn";

	private GameplayTagsUI _dockedScene;

	/// <summary>
	/// Gets or sets private TagsInspectorPlugin _tagsInspectorPlugin.
	/// </summary>
	public PackedScene PluginScene { get; set; }

	/// <inheritdoc/>
	public override void _EnterTree()
	{
		RegisteredTags registeredTags =
			ResourceLoader.Load<RegisteredTags>("res://addons/forge/gameplay_tags/registered_tags.tres");
		Forge.TagsManager = new GameplayTagsManager([.. registeredTags.Tags]);

		PluginScene = ResourceLoader.Load<PackedScene>(PluginScenePath);

		_dockedScene = (GameplayTagsUI)PluginScene.Instantiate();
		_dockedScene.Name = "Gameplay Tags";
		_dockedScene.IsPluginInstance = true;
		AddControlToDock(DockSlot.RightUl, _dockedScene);

		// _tagsInspectorPlugin = new TagsInspectorPlugin();
		// AddInspectorPlugin(_tagsInspectorPlugin);
		Script baseScript = GD.Load<Script>("res://addons/forge/ForgeEntity.cs");
		Texture2D checkedIcon = GD.Load<Texture2D>("res://addons/forge/anvil.svg");
		AddCustomType("Forge Entity", "Node", baseScript, checkedIcon);

		AddAutoload();
	}

	/// <inheritdoc/>
	public override void _ExitTree()
	{
		RemoveControlFromDocks(_dockedScene);
		_dockedScene.Free();

		// RemoveInspectorPlugin(_tagsInspectorPlugin);
		RemoveCustomType("Forge Entity");

		RemoveAutoload();

		Forge.TagsManager?.DestroyTagTree();
	}

	private static void AddAutoload()
	{
		var config = ProjectSettings.LoadResourcePack(AutoloadPath);

		if (config)
		{
			GD.PrintErr("Failed to load script at res://addons/forge/Forge.cs");
			return;
		}

		if (!ProjectSettings.HasSetting("autoload/Forge"))
		{
			ProjectSettings.SetSetting("autoload/Forge", AutoloadPath);
			ProjectSettings.Save();
		}
	}

	private static void RemoveAutoload()
	{
		if (ProjectSettings.HasSetting("autoload/Forge"))
		{
			ProjectSettings.Clear("autoload/Forge");
			ProjectSettings.Save();
		}
	}
}
#endif
