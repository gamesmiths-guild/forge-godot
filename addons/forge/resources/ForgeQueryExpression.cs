// Copyright © Gamesmiths Guild.

using System.Diagnostics;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Tags;
using Godot;
using Godot.Collections;

namespace Gamesmiths.Forge.Godot.Resources;

[Tool]
[GlobalClass]
[Icon("uid://dscm401i41h52")]
public partial class QueryExpression : Resource
{
	private TagQueryExpressionType _expressionType;

	[Export]
	public TagQueryExpressionType ExpressionType
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
	public ForgeTagContainer? TagContainer { get; set; }

	public override void _ValidateProperty(Dictionary property)
	{
		if ((ExpressionType == TagQueryExpressionType.Undefined || IsExpressionType())
			&& property["name"].AsStringName() == PropertyName.TagContainer)
		{
			property["usage"] = (int)PropertyUsageFlags.NoEditor;
		}

		if ((ExpressionType == TagQueryExpressionType.Undefined || !IsExpressionType())
			&& property["name"].AsStringName() == PropertyName.Expressions)
		{
			property["usage"] = (int)PropertyUsageFlags.NoEditor;
		}
	}

	public bool IsExpressionType()
	{
		return ExpressionType == TagQueryExpressionType.AnyExpressionsMatch
				|| ExpressionType == TagQueryExpressionType.AllExpressionsMatch
				|| ExpressionType == TagQueryExpressionType.NoExpressionsMatch;
	}

	public TagQueryExpression GetQueryExpression()
	{
		TagContainer ??= new();

		var expression = new TagQueryExpression(ForgeManagers.Instance.TagsManager);

		switch (_expressionType)
		{
			case TagQueryExpressionType.AnyExpressionsMatch:
				expression = expression.AnyExpressionsMatch();
				AddExpressions(expression);
				break;

			case TagQueryExpressionType.AllExpressionsMatch:
				expression = expression.AllExpressionsMatch();
				AddExpressions(expression);
				break;

			case TagQueryExpressionType.NoExpressionsMatch:
				expression = expression.NoExpressionsMatch();
				AddExpressions(expression);
				break;

			case TagQueryExpressionType.AnyTagsMatch:
				expression = expression.AnyTagsMatch();
				expression.AddTags(TagContainer.GetTagContainer());
				break;

			case TagQueryExpressionType.AllTagsMatch:
				expression = expression.AllTagsMatch();
				expression.AddTags(TagContainer.GetTagContainer());
				break;

			case TagQueryExpressionType.NoTagsMatch:
				expression = expression.NoTagsMatch();
				expression.AddTags(TagContainer.GetTagContainer());
				break;

			case TagQueryExpressionType.AnyTagsMatchExact:
				expression = expression.AnyTagsMatchExact();
				expression.AddTags(TagContainer.GetTagContainer());
				break;

			case TagQueryExpressionType.AllTagsMatchExact:
				expression = expression.AllTagsMatchExact();
				expression.AddTags(TagContainer.GetTagContainer());
				break;

			case TagQueryExpressionType.NoTagsMatchExact:
				expression = expression.NoTagsMatchExact();
				expression.AddTags(TagContainer.GetTagContainer());
				break;
		}

		return expression;
	}

	private void AddExpressions(TagQueryExpression expression)
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
