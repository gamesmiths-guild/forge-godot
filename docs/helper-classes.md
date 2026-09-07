# Helper Classes

## ForgeBootstrap & ForgeManagers

An autoload singleton that initializes the Forge system.

**Description:**

`ForgeBootstrap` is automatically registered as an autoload when the plugin is enabled. It initializes the core managers (`TagsManager`, `CuesManager`) and makes them available through the `ForgeManagers.Instance` static property.

**Usage:**

```csharp
// Access managers from anywhere in your code
var tagsManager = ForgeManagers.Instance.TagsManager;
var cuesManager = ForgeManagers.Instance.CuesManager;

// Fetch a tag (ensures it's registered in the system)
var tag = Tag.RequestTag(tagsManager, "ability.damage.fire");
```

## ForgeEntityBridge

The canonical translation between Godot scene nodes and `IForgeEntity`.

**Description:**

Forge supports two authoring patterns, and this static class is the only place that knows about both:

- **Composition**: a plain `ForgeEntity` node parented under the visual or physics root. The spatial node is an *ancestor* of the entity.
- **Direct**: a body script that implements `IForgeEntity` itself. The spatial node *is* the entity.

Every spatial node, Statescript resolver, and cue handler resolves through here instead of guessing inline, so both patterns keep working and the guess only ever has to be fixed in one place. Use it in your own code for the same reason.

**Lookups:**

| Method | Answers |
|---|---|
| `TryGetEntity(node, out entity)` | The entity for a node, checking the node itself then its direct children. The narrow case: a collider that was just hit. |
| `TryGetEntityInHierarchy(node, out entity)` | The same, widening up the ancestor chain. Physics reports the collider it hit, which is often a hurtbox nested under the body that owns the entity. |
| `TryGetEntityNode(entity, out node)` | The Godot node backing an entity. |
| `TryGetOwningNode(entity, out node)` | "The node this entity lives on", with no dimension: the nearest spatial ancestor of *either* kind, falling back to the entity's own node. |
| `TryGetSpatialNode3D` / `TryGetSpatialNode2D` | The spatial node of a given dimension, with `nodePath` overloads so `%CastPoint` markers work everywhere. |
| `TryGetEntityChild(entity, nodePath, out child)` | A child that is neither spatial nor the entity itself — an animation player, an audio player, an emitter. **An empty path means the first matching child**, one level deep, first match wins, which is not what an empty path means in the spatial lookups. |

```csharp
if (ForgeEntityBridge.TryGetEntityInHierarchy(hit.GetCollider() as Node, out IForgeEntity? target))
{
    _effectApplier.ApplyEffects(target, effectOwner: Owner, effectSource: this);
}
```

## IInstantiationReceiver

Implement this on the root script of a scene that Forge instantiates, and it is handed its ownership when it enters the tree:

```csharp
void OnInstantiated(IForgeEntity? owner, IForgeEntity? source);
```

`owner` is the entity that owns the effects the instance applies, usually the ability's owner; `source` is what is credited as causing them, which can be the instance's own entity when the spawned scene is a Forge entity in its own right.

Both [`InstantiateScene`/`Scene` nodes](statescript/nodes/scene-nodes.md#the-instantiating-pair) (when **Pass ownership** is on) and [`InstantiateSceneCueHandler`](nodes.md#instantiatescenecuehandler) call it. It is what replaces a bespoke `Launch` method on a projectile: `ForgeProjectile3D` implements it, so a graph aims a projectile by spawning it rotated and nothing has to be called afterwards.

## AudioPlayers

A switch over Godot's three audio player types, which share no base class.

Anything that means "the entity's audio player" without caring whether it is an `AudioStreamPlayer`, `AudioStreamPlayer2D` or `AudioStreamPlayer3D` goes through here. It is shared by the [Play Audio nodes](statescript/nodes/presentation-nodes.md#audio) and the [`AudioCueHandler`](nodes.md#audiocuehandler), so a path resolves the same way in a graph and in a cue.

## EffectApplier

A helper class for applying effects from child effect nodes to target entities.

**Description:**

`EffectApplier` streamlines applying effects from child `ForgeEffect` nodes to target `IForgeEntity` nodes. Construct and use an `EffectApplier` to handle both single-use and persistent effect application patterns, using optional contextual data.

**How it works:**

- Attach one or more `ForgeEffect` children to any node (e.g., a projectile, trap, or environmental hazard).
- Initialize the `EffectApplier` with the parent node.
- On collision or similar interaction, call an apply method on the target entity node.

**Usage Example:**

**Example:** Projectile Applying Effects on Hit

Suppose you have a projectile scene structured as:

```
MyProjectile (Node3D)
├── ForgeEffect_Fire (ForgeEffect)
├── ForgeEffect_Knockback (ForgeEffect)
```

Attach a script to `MyProjectile`:

```csharp
using Godot;
using Gamesmiths.Forge.Godot.Core;

public partial class MyProjectile : Node3D
{
    private EffectApplier _effectApplier;

    // The entity (e.g. player) who fired or owns this projectile
    public IForgeEntity? Owner { get; set; }

    public override void _Ready()
    {
        // Collect all ForgeEffect children
        _effectApplier = new EffectApplier(this);
    }

    // Call this when the projectile collides with something
    public void OnProjectileHit(Node3D targetNode)
    {
        // targetNode must implement IForgeEntity or have a child that does
        _effectApplier.ApplyEffects(
            targetNode,
            effectOwner: Owner,     // The player or entity that fired/owns the projectile
            effectSource: this,     // The projectile itself
            level: 2);
    }
}
```

And when spawning the projectile in your player/weapon code:

```csharp
// ... inside firing logic:
var projectile = ProjectileScene.Instantiate<MyProjectile>();
projectile.Owner = this; // Set the player (or relevant entity) as owner
projectile.GlobalTransform = muzzle.GlobalTransform;
GetTree().Root.AddChild(projectile);
```

**Key Usage:**
- `effectOwner` is always the ultimate entity responsible (e.g., the player).
- `effectSource` is the thing that directly causes the effect (e.g., this projectile node).
- `level` is the desired level for the effect to be applied with.

**Using context data:**

```csharp
object attackData = /* ... */;
_effectApplier.ApplyEffects(targetNode, attackData, Owner, this, level: 2);
```

**Notes:**

- Make sure the target node (or a child) implements `IForgeEntity`. `EffectApplier` resolves it through [`ForgeEntityBridge`](#forgeentitybridge), so both authoring patterns work.
- For generic `ApplyEffects<TData>`, all ForgeEffect children must support the same TData type.
- Add `[Tool]` and `[GlobalClass]` to custom node scripts for editor usability.
- Before writing a projectile script like the one above, check whether [`ForgeProjectile3D`](nodes.md#forgeprojectile2d--forgeprojectile3d) already covers it — it does all of this, plus sweeping, piercing and distance falloff.
