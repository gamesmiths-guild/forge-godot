// Copyright © Gamesmiths Guild.

#if TOOLS
using Gamesmiths.Forge.Godot.Resources;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Tags;

[Tool]
public partial class QueryExpressionInspectorPlugin : EditorInspectorPlugin
{
	public override bool _CanHandle(GodotObject @object)
	{
		return @object is ForgeQueryExpression;
	}

	public override void _ParseBegin(GodotObject @object)
	{
		if (@object is not ForgeQueryExpression query)
		{
			return;
		}

		var editor = new QueryExpressionEditorControl();
		editor.Setup(query, query.EmitChanged);
		AddCustomControl(editor);
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
		return name is nameof(ForgeQueryExpression.ExpressionType)
			or nameof(ForgeQueryExpression.Expressions)
			or nameof(ForgeQueryExpression.TagContainer);
	}
}
#endif
