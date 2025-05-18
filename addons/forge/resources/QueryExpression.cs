// Copyright © Gamesmiths Guild.

using System.Diagnostics;
using Gamesmiths.Forge.GameplayTags;
using Gamesmiths.Forge.Godot.Nodes;
using Godot;
using Godot.Collections;

using static Gamesmiths.Forge.Godot.Core.Forge;

namespace Gamesmiths.Forge.Godot.Resources;

[Tool]
[GlobalClass]
[Icon("uid://dscm401i41h52")]
public partial class QueryExpression : ForgeResource
{
	private GameplayTagQueryExpressionType _expressionType;

	[Export]
	public GameplayTagQueryExpressionType ExpressionType
	{
		get => _expressionType;

		set
		{
			_expressionType = value;
			NotifyPropertyListChanged();
		}
	}

	[Export]
	public Array<QueryExpression>? Expressions { get; set; }

	[Export]
	public TagContainer? TagContainer { get; set; }

	public override void _ValidateProperty(Dictionary property)
	{
		if ((ExpressionType == GameplayTagQueryExpressionType.Undefined || IsExpressionType())
			&& property["name"].AsStringName() == PropertyName.TagContainer)
		{
			property["usage"] = (int)PropertyUsageFlags.NoEditor;
		}

		if ((ExpressionType == GameplayTagQueryExpressionType.Undefined || !IsExpressionType())
			&& property["name"].AsStringName() == PropertyName.Expressions)
		{
			property["usage"] = (int)PropertyUsageFlags.NoEditor;
		}
	}

	public bool IsExpressionType()
	{
		return ExpressionType == GameplayTagQueryExpressionType.AnyExpressionsMatch
				|| ExpressionType == GameplayTagQueryExpressionType.AllExpressionsMatch
				|| ExpressionType == GameplayTagQueryExpressionType.NoExpressionsMatch;
	}

	public GameplayTagQueryExpression GetQueryExpression()
	{
		Debug.Assert(
			TagsManager is not null,
			$"{nameof(TagsManager)} should have been initialized by the Forge plugin.");

		TagContainer ??= new();

		var expression = new GameplayTagQueryExpression(TagsManager);

		switch (_expressionType)
		{
			case GameplayTagQueryExpressionType.AnyExpressionsMatch:
				expression = expression.AnyExpressionsMatch();
				AddExpressions(expression);
				break;

			case GameplayTagQueryExpressionType.AllExpressionsMatch:
				expression = expression.AllExpressionsMatch();
				AddExpressions(expression);
				break;

			case GameplayTagQueryExpressionType.NoExpressionsMatch:
				expression = expression.NoExpressionsMatch();
				AddExpressions(expression);
				break;

			case GameplayTagQueryExpressionType.AnyTagsMatch:
				expression = expression.AnyTagsMatch();
				expression.AddTags(TagContainer.GetTagContainer());
				break;

			case GameplayTagQueryExpressionType.AllTagsMatch:
				expression = expression.AllTagsMatch();
				expression.AddTags(TagContainer.GetTagContainer());
				break;

			case GameplayTagQueryExpressionType.NoTagsMatch:
				expression = expression.NoTagsMatch();
				expression.AddTags(TagContainer.GetTagContainer());
				break;

			case GameplayTagQueryExpressionType.AnyTagsMatchExact:
				expression = expression.AnyTagsMatchExact();
				expression.AddTags(TagContainer.GetTagContainer());
				break;

			case GameplayTagQueryExpressionType.AllTagsMatchExact:
				expression = expression.AllTagsMatchExact();
				expression.AddTags(TagContainer.GetTagContainer());
				break;

			case GameplayTagQueryExpressionType.NoTagsMatchExact:
				expression = expression.NoTagsMatchExact();
				expression.AddTags(TagContainer.GetTagContainer());
				break;
		}

		return expression;
	}

	private void AddExpressions(GameplayTagQueryExpression expression)
	{
		Debug.Assert(
			Expressions is not null,
			$"{nameof(Expressions)} reference is missing.");

		foreach (QueryExpression innerExpression in Expressions)
		{
			expression.AddExpression(innerExpression.GetQueryExpression());
		}
	}
}
