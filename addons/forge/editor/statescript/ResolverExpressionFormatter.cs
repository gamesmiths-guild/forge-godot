// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Globalization;
using Gamesmiths.Forge.Godot.Resources;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript;

/// <summary>
/// Renders a <see cref="StatescriptResolverResource"/> tree as a single, human-readable expression string with BBCode
/// syntax highlighting. Used by the Expression node editor to preview the whole condition as one wrapping label.
/// </summary>
/// <remarks>
/// <para>
/// Coverage is the boolean "condition set" plus a generic fallback: logical operators (<c>AND</c>/<c>OR</c>/<c>XOR</c>/
/// <c>NOT</c>), comparisons, arithmetic, attribute/variable/constant/tag-query leaves and ability entities render with
/// dedicated formatting; every other resolver falls back to a <c>Name(args)</c> function form (nested bases) or a plain
/// reference token. The output never clamps, callers should host it in a word-wrapping <see cref="RichTextLabel"/>.
/// </para>
/// </remarks>
internal static class ResolverExpressionFormatter
{
	private const string ReferenceColor = "c678dd";
	private const string ValueColor = "56b6c2";
	private const string KeywordColor = "e5c07b";
	private const string MutedColor = "7f848e";

	/// <summary>
	/// Formats a resolver tree as a BBCode expression string.
	/// </summary>
	/// <param name="resolver">The root resolver resource, or <see langword="null"/> when nothing is bound.</param>
	/// <returns>A BBCode string suitable for a <see cref="RichTextLabel"/> with <c>BbcodeEnabled</c>.</returns>
	public static string Format(StatescriptResolverResource? resolver)
	{
		return FormatNode(resolver);
	}

	private static string FormatNode(StatescriptResolverResource? resolver)
	{
		return resolver switch
		{
			null => Muted("(unset)"),
			ComparisonResolverResource comparison => FormatComparison(comparison),
			BinaryNestedResolverResourceBase binary => FormatBinary(binary),
			UnaryNestedResolverResourceBase unary => FormatUnary(unary),
			TernaryNestedResolverResourceBase ternary => FormatFunction(
				ReadableName(ternary.ResolverTypeId),
				FormatNode(ternary.First),
				FormatNode(ternary.Second),
				FormatNode(ternary.Third)),
			AttributeResolverResource attribute => FormatAttribute(attribute),
			VariableResolverResource variable => Reference(VariableLabel(variable)),
			VariantResolverResource constant => FormatConstant(constant),
			TagQueryResolverResource tagQuery => FormatTagQuery(tagQuery),
			EntityResolverResourceBase entity => Reference(EntityLabel(entity)),
			_ => Reference(ReadableName(resolver.ResolverTypeId)),
		};
	}

	private static string FormatComparison(ComparisonResolverResource comparison)
	{
		return FormatOperand(comparison.Left, 2)
			+ " " + Keyword(ComparisonSymbol(comparison.Operation)) + " "
			+ FormatOperand(comparison.Right, 2);
	}

	private static string FormatBinary(BinaryNestedResolverResourceBase binary)
	{
		string? logicalKeyword = LogicalKeyword(binary.ResolverTypeId);
		if (logicalKeyword is not null)
		{
			return FormatLogicalOperand(binary.Left)
				+ " " + Keyword(logicalKeyword) + " "
				+ FormatLogicalOperand(binary.Right);
		}

		string? arithmeticSymbol = ArithmeticSymbol(binary.ResolverTypeId);
		if (arithmeticSymbol is not null)
		{
			int precedence = Precedence(binary);

			int rightPrecedence = binary.ResolverTypeId is "Subtract" or "Divide" or "Modulo"
				? precedence + 1
				: precedence;

			return FormatOperand(binary.Left, precedence)
				+ " " + Keyword(arithmeticSymbol) + " "
				+ FormatOperand(binary.Right, rightPrecedence);
		}

		return FormatFunction(
			ReadableName(binary.ResolverTypeId),
			FormatNode(binary.Left),
			FormatNode(binary.Right));
	}

	private static string FormatUnary(UnaryNestedResolverResourceBase unary)
	{
		string typeId = unary.ResolverTypeId;
		if (typeId == "Not")
		{
			return Keyword("NOT") + " " + FormatLogicalOperand(unary.Operand);
		}

		if (typeId == "NegatePassthrough")
		{
			return Keyword("-") + FormatOperand(unary.Operand, 5);
		}

		return FormatFunction(ReadableName(typeId), FormatNode(unary.Operand));
	}

	private static string FormatAttribute(AttributeResolverResource attribute)
	{
		string set = string.IsNullOrEmpty(attribute.AttributeSetClass)
			? "?"
			: ShortTypeName(attribute.AttributeSetClass);
		string name = string.IsNullOrEmpty(attribute.AttributeName) ? "?" : attribute.AttributeName;
		return Reference(EntityLabel(attribute.EntityResolver))
			+ Muted(".") + Reference(set)
			+ Muted(".") + Reference(name);
	}

	private static string FormatConstant(VariantResolverResource constant)
	{
		if (!constant.IsArray)
		{
			return Value(FormatVariantValue(constant.Value, constant.ValueType));
		}

		string[] parts = new string[constant.ArrayValues.Count];
		for (int i = 0; i < parts.Length; i++)
		{
			parts[i] = FormatVariantValue(constant.ArrayValues[i], constant.ValueType);
		}

		return Value("[" + string.Join(", ", parts) + "]");
	}

	private static string FormatVariantValue(Variant value, StatescriptVariableType valueType)
	{
		return valueType switch
		{
			StatescriptVariableType.Bool => value.AsBool() ? "true" : "false",
			StatescriptVariableType.Float
				or StatescriptVariableType.Double
				or StatescriptVariableType.Decimal => value.AsDouble().ToString(CultureInfo.InvariantCulture),
			StatescriptVariableType.Byte
				or StatescriptVariableType.SByte
				or StatescriptVariableType.Int
				or StatescriptVariableType.UInt
				or StatescriptVariableType.Long
				or StatescriptVariableType.ULong
				or StatescriptVariableType.Short
				or StatescriptVariableType.UShort => value.AsInt64().ToString(CultureInfo.InvariantCulture),
			_ => value.ToString(),
		};
	}

	private static string FormatTagQuery(TagQueryResolverResource tagQuery)
	{
		string source = tagQuery.QuerySource switch
		{
			TagQuerySource.BaseTags => "BaseTags",
			TagQuerySource.ModifierTags => "ModifierTags",
			_ => "AllTags",
		};

		return Reference(EntityLabel(tagQuery.EntityResolver) + "." + source)
			+ " " + FormatQueryExpression(tagQuery.Query);
	}

	private static string FormatQueryExpression(ForgeQueryExpression? expression)
	{
		if (expression is null)
		{
			return Muted("(no query)");
		}

		string keyword = Keyword(QueryKeyword(expression.ExpressionType));

		if (expression.IsExpressionType())
		{
			int count = expression.Expressions?.Count ?? 0;
			string[] parts = new string[count];
			for (int i = 0; i < count; i++)
			{
				parts[i] = FormatQueryExpression(expression.Expressions![i]);
			}

			return keyword + " " + Muted("(") + string.Join(Muted(", "), parts) + Muted(")");
		}

		int tagCount = expression.TagContainer?.ContainerTags?.Count ?? 0;
		if (tagCount == 0)
		{
			return keyword + " " + Muted("(no tags)");
		}

		string[] tags = new string[tagCount];
		for (int i = 0; i < tagCount; i++)
		{
			tags[i] = Value("\"" + expression.TagContainer!.ContainerTags![i] + "\"");
		}

		return keyword + " " + string.Join(Muted(", "), tags);
	}

	private static string QueryKeyword(TagQueryExpressionType expressionType)
	{
		return expressionType switch
		{
			TagQueryExpressionType.AnyTagsMatch => "has any",
			TagQueryExpressionType.AllTagsMatch => "has all",
			TagQueryExpressionType.NoTagsMatch => "has none",
			TagQueryExpressionType.AnyTagsMatchExact => "has any exact",
			TagQueryExpressionType.AllTagsMatchExact => "has all exact",
			TagQueryExpressionType.NoTagsMatchExact => "has no exact",
			TagQueryExpressionType.AnyExpressionsMatch => "any of",
			TagQueryExpressionType.AllExpressionsMatch => "all of",
			TagQueryExpressionType.NoExpressionsMatch => "none of",
			_ => "matches",
		};
	}

	private static string EntityLabel(EntityResolverResourceBase? entity)
	{
		return entity switch
		{
			null => "Owner",
			AbilityOwnerResolverResource => "Owner",
			AbilitySourceResolverResource => "Source",
			AbilityTargetResolverResource => "Target",
			VariableResolverResource variable => VariableLabel(variable),
			_ => ReadableName(entity.ResolverTypeId),
		};
	}

	private static string VariableLabel(VariableResolverResource variable)
	{
		return string.IsNullOrEmpty(variable.VariableName) ? "?" : variable.VariableName;
	}

	private static string FormatLogicalOperand(StatescriptResolverResource? operand)
	{
		return Precedence(operand) < 10 ? Paren(FormatNode(operand)) : FormatNode(operand);
	}

	private static string FormatOperand(StatescriptResolverResource? operand, int parentPrecedence)
	{
		return Precedence(operand) < parentPrecedence ? Paren(FormatNode(operand)) : FormatNode(operand);
	}

	private static int Precedence(StatescriptResolverResource? resolver)
	{
		return resolver switch
		{
			ComparisonResolverResource => 2,
			BinaryNestedResolverResourceBase binary => binary.ResolverTypeId switch
			{
				"And" or "Or" or "Xor" => 1,
				"Add" or "Subtract" => 3,
				"Multiply" or "Divide" or "Modulo" => 4,
				_ => 10,
			},
			UnaryNestedResolverResourceBase unary => unary.ResolverTypeId switch
			{
				"Not" => 1,
				"NegatePassthrough" => 5,
				_ => 10,
			},
			_ => 10,
		};
	}

	private static string ComparisonSymbol(ComparisonOperation operation)
	{
		return operation switch
		{
			ComparisonOperation.Equal => "=",
			ComparisonOperation.NotEqual => "≠",
			ComparisonOperation.LessThan => "<",
			ComparisonOperation.LessThanOrEqual => "≤",
			ComparisonOperation.GreaterThan => ">",
			ComparisonOperation.GreaterThanOrEqual => "≥",
			_ => "?",
		};
	}

	private static string? LogicalKeyword(string typeId)
	{
		return typeId switch
		{
			"And" => "AND",
			"Or" => "OR",
			"Xor" => "XOR",
			_ => null,
		};
	}

	private static string? ArithmeticSymbol(string typeId)
	{
		return typeId switch
		{
			"Add" => "+",
			"Subtract" => "-",
			"Multiply" => "*",
			"Divide" => "/",
			"Modulo" => "%",
			_ => null,
		};
	}

	private static string ReadableName(string typeId)
	{
		return typeId;
	}

	private static string ShortTypeName(string typeName)
	{
		int lastDot = typeName.LastIndexOf('.');
		return lastDot >= 0 && lastDot < typeName.Length - 1 ? typeName[(lastDot + 1)..] : typeName;
	}

	private static string FormatFunction(string name, params string[] arguments)
	{
		return Keyword(name) + Muted("(") + string.Join(Muted(", "), arguments) + Muted(")");
	}

	private static string Paren(string inner)
	{
		return Muted("(") + inner + Muted(")");
	}

	private static string Reference(string text)
	{
		return Colorize(ReferenceColor, text);
	}

	private static string Value(string text)
	{
		return Colorize(ValueColor, text);
	}

	private static string Keyword(string text)
	{
		return Colorize(KeywordColor, text);
	}

	private static string Muted(string text)
	{
		return Colorize(MutedColor, text);
	}

	private static string Colorize(string color, string text)
	{
		return "[color=#" + color + "]" + Escape(text) + "[/color]";
	}

	private static string Escape(string text)
	{
		return text.Replace("[", "[lb]", System.StringComparison.Ordinal);
	}
}
#endif
