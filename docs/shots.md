# Shots

Forge for Godot fires three kinds of shot, from graphs or from code, in 2D and 3D, offline and on the network alike:

- **Hitscans** hit what is on their line at once: one ray, or pellets spread in a cone.
- **Lightweight projectiles** fly along a path their launch decides. Nothing is sent while they fly: every machine flies them from the launch alone. See [Shots](https://github.com/gamesmiths-guild/forge/blob/main/docs/networking/shots.md) in the core docs for how.
- **Blasts** reach every entity in a radius, with effects and a push scaled by distance.

A projectile that needs effects, tags or a graph of its own is still an entity: a [`ForgeProjectile3D` or 2D](nodes.md#forgeprojectile2d--forgeprojectile3d) on a networked scene a graph instantiates.

Fire a shot from an entity's tick, as an ability's behavior runs, so the server and the client that predicts the entity fire it alike. On the server, a client's shot meets the other entities where that client was shown them ([Hits](networking.md#hits)), and what it hits shows its cues on the shooter's screen at once.

## Resources

Each kind of shot is a resource: [`ForgeHitscanData`](resources.md#forgehitscandata), [`ForgeProjectileData`](resources.md#forgeprojectiledata) and [`ForgeSplashData`](resources.md#forgesplashdata). Each holds the effects it applies. Every entity it hits gets them once, with the share of the shot's damage it had left as **float context data**, which a [custom calculator or execution](resources.md#forgecustomcalculator-abstract) reads to scale the magnitude. A projectile's resource has to be in the [network manifest](resources.md#forgenetworkmanifest), which lists every one in the project.

## Nodes

[Hitscan, Launch Projectile and Splash](statescript/nodes/shot-nodes.md), each in 2D and 3D, take the resource and the place, and fire from the ability's owner.

## From code

`ForgeShots`, in `Gamesmiths.Forge.Godot.Core.Shots`, fires them from a [`RollbackBehavior<TContext>`](https://github.com/gamesmiths-guild/forge/blob/main/docs/rollback.md#coded-behaviors), which a client predicts:

```csharp
public sealed class RifleContext : BehaviorContext
{
    public int Ticks { get; set; }
}

public sealed class Rifle(ForgeHitscanData beam) : RollbackBehavior<RifleContext>
{
    public override void OnStarted(AbilityBehaviorContext context)
    {
        base.OnStarted(context);

        var owner = (ForgeEntity)context.Owner;
        var body = (Node3D)owner.GetParent();
        System.Numerics.Vector3 aim = owner.Input!.Current.AimDirection;

        ForgeShots.Hitscan3D(owner, body.GlobalPosition + Vector3.Up, new Vector3(aim.X, aim.Y, aim.Z), beam);
    }

    public override void OnEnded(AbilityBehaviorContext context)
    {
    }
}
```

| Method | Does |
|---|---|
| `Hitscan3D`, `Hitscan2D` | Fires the hitscan: spreads its pellets from the entity's seeded random, traces each, and applies its effects to each entity a pellet hits. Adds what each pellet hit, in order, to a list you pass, and gives what the first pellet stopped on, for a tracer. |
| `Trace3D`, `Trace2D` | Traces one ray and applies nothing, for a shot that decides itself what its hits do. |
| `Spread3D`, `Spread2D` | Spreads directions over a cone or an arc from the entity's seeded random. |
| `Launch3D`, `Launch2D` | Launches a lightweight projectile in the entity's current tick. |
| `Splash3D`, `Splash2D` | Blasts from a point. |

A `ShotHit3D` or `ShotHit2D` says where the shot hit, the collider and the entity, the [hitbox](#hitboxes) if it was one, the share of damage it had left times a hitbox's `Damage Scale` (`Scale`), and the projectile if it hit one in flight. A `SplashHit3D` or `SplashHit2D` says how far the entity was, its scale and the push it gave it, which a body without a character movement doesn't take.

## Going through things

A ray, and a projectile, stops at the first collider in its collision mask it may not go through. Two settings let it through:

- **Pierce Mask.** It goes through colliders on these layers, keeping `Pierce Scale` of its damage each time, up to `Max Pierces` (negative for no limit). A cloth banner, a player a railgun goes through.
- **Power.** A collider with a [`ForgeShotSurface`](resources.md#forgeshotsurface) as its `forge_shot_surface` metadata costs the shot its `Resistance` times the thickness the shot crosses, measured from where it goes in to where it comes out, all at once as it goes in: one thicker than its power crosses stops it, however slowly it flies. A shot with `Power` above zero goes through while it has power left, and keeps the share left as its share of damage. A wooden crate with a low resistance lets a rifle through at reduced damage; a concrete wall with none set, or a high one, stops it.

Each entity on the line gets the effects once, with the scale it was hit at.

## Hitboxes

A [`ForgeHitbox3D` or 2D](nodes.md#forgehitbox2d--forgehitbox3d) is a part of an entity a shot can hit, such as a head: a static body of shapes that moves with the entity's animation, usually under a `BoneAttachment3D`. Put hitboxes on a layer of their own, which the entity's movement doesn't collide with and shots do. A hit on one reports it, scales the damage by its `Damage Scale` (2 for a head that takes double) and carries its `Tags`.

A [`ForgeHitboxes3D` or 2D](nodes.md#forgehitboxes2d--forgehitboxes3d) next to the entity's `ForgeEntity` makes them rewind with the body:

- On the server, it records where each hitbox stands against the body every tick, so a lag-compensated shot meets the hitboxes posed as the shooter's client showed them, where it showed the body.
- The animators listed in its `Animators` move hitboxes, so they're simulation: they advance in physics ticks, and a [Play Animation](statescript/nodes/presentation-nodes.md) node plays them on a dedicated server too, where nothing else is shown, seeking each tick to where the activation has played to.

```text
Player (CharacterBody3D)
├── CollisionShape3D                    layer: players
├── Model
│   └── Skeleton3D
│       ├── BoneAttachment3D (head)
│       │   └── ForgeHitbox3D           layer: hitboxes, Damage Scale = 2
│       │       └── CollisionShape3D
│       └── BoneAttachment3D (spine)
│           └── ForgeHitbox3D           layer: hitboxes
│               └── CollisionShape3D
├── AnimationPlayer
├── ForgeHitboxes3D                     Animators = [AnimationPlayer]
└── ForgeEntity
```

## Blasts and knockback

A blast's push goes to an entity whose body a [`ForgeCharacterMovement3D` or 2D](nodes.md#forgecharactermovement2d--forgecharactermovement3d) moves, as a velocity added away from the center with a little lift. It's part of the body, so it rolls back with it: the shooter's own push, as a rocket jump's, is predicted with the rest of its player, and another player's arrives with the server's correction. With `Affects Instigator` on, the shooter takes `Instigator Scale` of the effects too. With `Blocked By Walls` on, a wall between the center and an entity shields it.

For a rocket jump, give the movement a `Jump Speed` and turn on `Keep Air Speed`, so the push carries the body in the air rather than being slowed down there.

## On the network

- A launch reaches every client but the shooter's, which launched it already. A mirror flies the projectile's `Scene` at the tick the client shows the world at, so it leaves the shooter's hand there, and shows its `Impact Scene` when that tick reaches the end the server sent.
- The shooter's client predicts its projectiles, hits included, and ends each where the server's ended.
- On the server, a client's projectile meets the other entities where that client was shown them as it leaves, and less so as it flies: the compensation fades out over the shooter's `Compensation Fade Ms`, so a target that dodged it on its own screen isn't hit where it was.
- A hitscan that hits projectiles tests them where its shooter saw them. On a client, it ends only its own player's projectiles at once; another player's end when the server's word arrives.
