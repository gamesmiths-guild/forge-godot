using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Enumerates comparison operations for the comparison resolver.
/// </summary>
public enum ComparisonOperation
{
	/// <summary>Left == Right.</summary>
	Equal = 0,

	/// <summary>Left != Right.</summary>
	NotEqual = 1,

	/// <summary>Left &lt; Right.</summary>
	LessThan = 2,

	/// <summary>Left &lt;= Right.</summary>
	LessThanOrEqual = 3,

	/// <summary>Left &gt; Right.</summary>
	GreaterThan = 4,

	/// <summary>Left &gt;= Right.</summary>
	GreaterThanOrEqual = 5,
}

/// <summary>
/// Resolver resource that compares two nested numeric resolvers and produces a boolean result.
/// </summary>
[Tool]
[GlobalClass]
public partial class ComparisonResolverResource : StatescriptResolverResource
{
	/// <summary>
	/// Gets or sets the left-hand operand resolver.
	/// </summary>
	[Export]
	public StatescriptResolverResource? Left { get; set; }

	/// <summary>
	/// Gets or sets the comparison operation.
	/// </summary>
	[Export]
	public ComparisonOperation Operation { get; set; }

	/// <summary>
	/// Gets or sets the right-hand operand resolver.
	/// </summary>
	[Export]
	public StatescriptResolverResource? Right { get; set; }
}
