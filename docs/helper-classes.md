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

- Make sure the target node (or a child) implements `IForgeEntity`.
- For generic `ApplyEffects<TData>`, all ForgeEffect children must support the same TData type.
- Add `[Tool]` and `[GlobalClass]` to custom node scripts for editor usability.
