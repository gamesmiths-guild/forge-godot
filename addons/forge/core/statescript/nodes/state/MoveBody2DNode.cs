// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Ports;
using Godot;
using GodotNode = Godot.Node;
using NumericsVector2 = System.Numerics.Vector2;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;

/// <summary>
/// State node that moves a body to a destination against the world, emitting <see cref="OnArrivedPort"/> when it gets
/// there and <see cref="OnBlockedPort"/> when something stops it.
/// </summary>
/// <remarks>
/// <para>The solving counterpart of Move To 2D, and the pair are opposites on purpose. Move To drives the transform
/// along a straight line and passes through geometry, which is what a leap or a forced reposition wants; this sweeps
/// the body and meets what is in the way, which is what a dash, a charge or a shoulder barge wants. The port that
/// makes it worth a node of its own is <see cref="OnBlockedPort"/>: knowing a charge connected, and with what, is the
/// half a non-solving move cannot report.</para>
/// <para>It moves with <c>MoveAndCollide</c> rather than <c>MoveAndSlide</c>, which is not an implementation detail.
/// Move and slide is the character controller's own entry point - it reads and writes the body's velocity and applies
/// floor snapping and slope detection - so an ability calling it would move the body a second time each step and fight
/// the game's controller for ownership of that velocity. Move and collide is one swept step that touches nothing else,
/// which is what an ability-scoped move is.</para>
/// <para><b>The move is time-bounded either way.</b> It ends at the destination, or at the duration it would have
/// taken unobstructed, whichever comes first - so a slide that grinds along a wall forever is not a thing a graph can
/// author by accident. Running out of time reports <see cref="OnBlockedPort"/>, because not arriving is what being
/// blocked means; under <see cref="BlockedResponse.Slide"/> that is the only way it fires.</para>
/// <para><b>The game's own movement code still runs.</b> This adds displacement to a body; it does not take the body
/// over, and Forge has no way to tell a character controller to stand down. On a character whose script moves itself
/// every physics step the two motions combine, which is rarely what a dash wants. Suspending the controller for the
/// duration is the ability's job and composes out of what is already here - Node Enabled Override on the script that
/// moves it, or a tag the controller checks before moving.</para>
/// <para>A rigid body is better driven by Force Override 2D or Set Velocity 2D. Sweeping one with move and collide
/// teleports it past the simulation rather than pushing it through, and the result fights whatever else is acting on
/// it - but it is allowed here rather than refused, because a game that has made a rigid body kinematic for the
/// duration of an ability has a real use for it.</para>
/// <para>Configuration is captured in field initializers rather than a constructor body, because the base node
/// constructor calls <see cref="DefinePorts"/> and <see cref="DefineParameters"/> before a body would run.</para>
/// </remarks>
/// <param name="mode">Whether the value input is a duration or a speed.</param>
/// <param name="blocked">What the move does with a step something refused.</param>
/// <param name="nodePath">Optional path to a descendant body to move instead of the entity's own spatial node.</param>
[StatescriptCategory("Spatial")]
public class MoveBody2DNode(
	MoveToMode mode = MoveToMode.Duration,
	BlockedResponse blocked = BlockedResponse.Stop,
	string nodePath = "") : StateNode<MoveBody2DNodeContext>
{
	/// <summary>
	/// Input property index for the entity to move. Unbound means the ability's owner.
	/// </summary>
	public const byte EntityInput = 0;

	/// <summary>
	/// Input property index for the destination.
	/// </summary>
	public const byte DestinationInput = 1;

	/// <summary>
	/// Input property index for the duration in seconds, or the speed in pixels per second.
	/// </summary>
	public const byte ValueInput = 2;

	/// <summary>
	/// Output variable index for the entity that stopped the move, when it belongs to one.
	/// </summary>
	public const byte BlockerEntityOutput = 0;

	/// <summary>
	/// Output variable index for the node that stopped the move.
	/// </summary>
	public const byte BlockerNodeOutput = 1;

	/// <summary>
	/// Output port index for the event emitted when the body reaches the destination.
	/// </summary>
	public const byte OnArrivedPort = 4;

	/// <summary>
	/// Output port index for the event emitted when something stops the move.
	/// </summary>
	public const byte OnBlockedPort = 5;

	private const double MinimumDuration = 0.0001;

	private readonly MoveToMode _mode = mode;
	private readonly BlockedResponse _blocked = blocked;
	private readonly string _nodePath = nodePath ?? string.Empty;

	/// <inheritdoc/>
	public override string Description =>
		"Moves a body to a destination against the world, emitting OnArrived on completion and OnBlocked when "
		+ "something stops it.";

	/// <inheritdoc/>
	protected override void DefinePorts(List<InputPort> inputPorts, List<OutputPort> outputPorts)
	{
		base.DefinePorts(inputPorts, outputPorts);
		outputPorts.Add(CreatePort<EventPort>(OnArrivedPort, "OnArrived"));
		outputPorts.Add(CreatePort<EventPort>(OnBlockedPort, "OnBlocked"));
	}

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Entity", typeof(IForgeEntity), IsOptional: true));
		inputProperties.Add(new InputProperty("Destination", typeof(NumericsVector2)));
		inputProperties.Add(new InputProperty("Value", typeof(double)));

		outputVariables.Add(new OutputVariable("Blocker Entity", typeof(IForgeEntity)));
		outputVariables.Add(new OutputVariable("Blocker Node", typeof(GodotNode)));
	}

	/// <inheritdoc/>
	protected override void OnActivate(GraphContext graphContext)
	{
		MoveBody2DNodeContext nodeContext = graphContext.GetNodeContext<MoveBody2DNodeContext>(NodeID);
		nodeContext.Body = null;
		nodeContext.ElapsedTime = 0;
		nodeContext.LastBlocker = null;

		IForgeEntity? entity = ResolveEntityOrOwner(graphContext);

		if (!ForgeEntityBridge.TryGetSpatialNode2D(entity, _nodePath, out Node2D? spatialNode))
		{
			GD.PushWarning(
				"Statescript: Move Body 2D found no Node2D for its entity" +
				(_nodePath.Length == 0 ? "." : $" at [{_nodePath}].") +
				" The move was skipped.");
			return;
		}

		if (spatialNode is not PhysicsBody2D body)
		{
			GD.PushWarning(
				$"Statescript: Move Body 2D resolved [{spatialNode.Name}], which is not a PhysicsBody2D and has "
				+ "nothing to sweep. Use Move To 2D for a node that only has a transform. The move was skipped.");
			return;
		}

		if (!graphContext.TryResolve(InputProperties[DestinationInput].BoundName, out NumericsVector2 destination))
		{
			GD.PushWarning("Statescript: Move Body 2D could not resolve a destination. The move was skipped.");
			return;
		}

		graphContext.TryResolve(InputProperties[ValueInput].BoundName, out double value);

		var target = new Vector2(destination.X, destination.Y);
		float distance = body.GlobalPosition.DistanceTo(target);

		nodeContext.Destination = target;

		nodeContext.Duration = _mode == MoveToMode.Speed ? ResolveDurationFromSpeed(distance, value) : value;

		// A move with no time to run is a teleport, and a teleport that solves is the whole step taken at once - which
		// can still be stopped short, and that is the difference between this instant case and Move To 2D's.
		if (nodeContext.Duration < MinimumDuration)
		{
			if (TryMove(body, target - body.GlobalPosition, out GodotNode? blocker) && blocker is null)
			{
				DeactivateNodeAndEmitMessage(graphContext, OnArrivedPort);
			}
			else
			{
				ReportBlocked(graphContext, blocker);
			}

			return;
		}

		nodeContext.Speed = (float)(distance / nodeContext.Duration);
		nodeContext.Body = body;
	}

	/// <inheritdoc/>
	protected override void OnDeactivate(GraphContext graphContext)
	{
		graphContext.GetNodeContext<MoveBody2DNodeContext>(NodeID).Body = null;
	}

	/// <inheritdoc/>
	protected override void OnFixedUpdate(double deltaTime, GraphContext graphContext)
	{
		MoveBody2DNodeContext nodeContext = graphContext.GetNodeContext<MoveBody2DNodeContext>(NodeID);
		PhysicsBody2D? body = nodeContext.Body;

		// A move whose body could not be found, or has been freed since, is over rather than pending: nothing gives it
		// one back, so holding the node open would stall every graph waiting on either port. Blocked rather than
		// arrived, because it did not get there.
		if (body is null || !GodotObject.IsInstanceValid(body))
		{
			ReportBlocked(graphContext, null);
			return;
		}

		nodeContext.ElapsedTime += deltaTime;

		Vector2 offset = nodeContext.Destination - body.GlobalPosition;
		float step = nodeContext.Speed * (float)deltaTime;

		// The last step is the one that would overshoot: taken at exactly the remaining distance, it lands on the
		// destination rather than past it, and a collision on the way still stops it short.
		bool finalStep = offset.Length() <= step;
		Vector2 motion = finalStep ? offset : offset.Normalized() * step;

		if (!TryMove(body, motion, out GodotNode? blocker))
		{
			ReportBlocked(graphContext, blocker);
			return;
		}

		// Remembered rather than read back off whichever step ends the move: a slide meets a wall, clears it, and can
		// then time out several steps later having touched nothing that step.
		nodeContext.LastBlocker = blocker ?? nodeContext.LastBlocker;

		// Arrival is the step that would have landed on the destination meeting nothing on the way, rather than a
		// distance compared against an epsilon: under Slide a collision redirects that step somewhere else entirely,
		// and the next one measures again from wherever it ended up.
		if (finalStep && blocker is null)
		{
			DeactivateNodeAndEmitMessage(graphContext, OnArrivedPort);
			return;
		}

		// Bounded by the time the move would have taken unobstructed. Under Stop the first collision already ended it,
		// so this is what ends a Slide that is still going nowhere - and what stops a move whose destination drifted
		// out of reach from running for the rest of the ability.
		if (nodeContext.ElapsedTime >= nodeContext.Duration)
		{
			ReportBlocked(graphContext, nodeContext.LastBlocker);
		}
	}

	// A speed of zero resolves to no duration rather than to an endless move, matching Move To 2D: an authored zero
	// reads as "get there now", and a graph waiting on either port is answered on the activation step either way.
	private static double ResolveDurationFromSpeed(float distance, double speed)
	{
		return speed <= 0 ? 0 : distance / speed;
	}

	private static void WriteObject(GraphContext graphContext, OutputVariable output, object? value)
	{
		if (output.BoundName == StringKey.Empty)
		{
			return;
		}

		Variables? variables = output.Scope == VariableScope.Shared
			? graphContext.SharedVariables
			: graphContext.GraphVariables;

		if (variables?.TryGetObjectVariableType(output.BoundName, out _) == true)
		{
			variables.SetObject(output.BoundName, value);
		}
	}

	// Returns whether the move may carry on. The blocker is set whenever something was met, under either response, so
	// a slide that runs out of time still reports the last thing it touched rather than nothing at all.
	private bool TryMove(PhysicsBody2D body, Vector2 motion, out GodotNode? blocker)
	{
		blocker = null;

		KinematicCollision2D? collision = body.MoveAndCollide(motion);

		if (collision is null)
		{
			return true;
		}

		blocker = collision.GetCollider() as GodotNode;

		if (_blocked == BlockedResponse.Stop)
		{
			return false;
		}

		// One redirect, not a loop: the part of the step the surface refused, turned along it. A second collision in
		// the same step is left for the next one, which keeps a body wedged in a corner from spending the frame
		// bouncing between two walls it cannot leave - but it is still the most recent thing touched, so it is what
		// gets reported if this turns out to be the step the move ends on.
		KinematicCollision2D? redirected = body.MoveAndCollide(collision.GetRemainder().Slide(collision.GetNormal()));

		blocker = redirected?.GetCollider() as GodotNode ?? blocker;

		return true;
	}

	private void ReportBlocked(GraphContext graphContext, GodotNode? blocker)
	{
		WriteObject(
			graphContext,
			OutputVariables[BlockerEntityOutput],
			ForgeEntityBridge.TryGetEntityInHierarchy(blocker, out IForgeEntity? entity) ? entity : null);

		WriteObject(graphContext, OutputVariables[BlockerNodeOutput], blocker);

		DeactivateNodeAndEmitMessage(graphContext, OnBlockedPort);
	}

	private IForgeEntity? ResolveEntityOrOwner(GraphContext graphContext)
	{
		StringKey boundName = InputProperties[EntityInput].BoundName;

		if (boundName != StringKey.Empty
			&& graphContext.TryResolveObject(boundName, out IForgeEntity? entity)
			&& entity is not null)
		{
			return entity;
		}

		return graphContext.TryGetActivationContext(out AbilityBehaviorContext? abilityContext)
			? abilityContext.Owner
			: null;
	}
}
