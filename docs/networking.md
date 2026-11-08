# Networking

Forge for Godot plays a game over the network with one server in charge: the server simulates every entity from the players' input, each client sends only what its player pressed, and the server sends the world back down to every client. This page is the setup. The design, the protocol and what each piece does are in the core's [networking](https://github.com/gamesmiths-guild/forge/blob/main/docs/networking/README.md), [replication](https://github.com/gamesmiths-guild/forge/blob/main/docs/networking/replication.md) and [effects on the network](https://github.com/gamesmiths-guild/forge/blob/main/docs/networking/effects.md) pages. The Networked Statescript sample in `forge_samples/demos/05_networked_statescript/` puts it together.

## Setup

1. **The session node.** Add a [`ForgeNetworkSession`](nodes.md#forgenetworksession) to the scene that plays online. One session per scene.
2. **Networked entities.** Turn on `Networked` on every [`ForgeEntity`](nodes.md#forgeentity) the server should simulate for everyone: players, enemies, anything whose state matters to more than one machine. See [Networked entities](#networked-entities).
3. **The input actions.** Networked input travels as the project's Forge input actions (`forge/input/actions` and `forge/input/vectors` in Project Settings), so every machine must list the same ones in the same order. A client whose list differs is refused. Input events travel as Forge tags, so the registered tags must match too; a client whose tags differ is refused.
4. **The asset manifest.** Both ends must agree on which effect, ability, graph or networked scene an index means. The editor plugin keeps a [`ForgeNetworkManifest`](resources.md#forgenetworkmanifest) at `res://forge_network_manifest.tres` (the path is `forge/networking/manifest` in Project Settings), regenerated when one of those resources is saved, when the project's files change, and from **Project, Tools, Forge, Regenerate network manifest**. It lists every scene whose own `ForgeEntity` is networked. Commit it; a client whose manifest differs is refused.
5. **Host, join, leave.** From a menu or a script:

```csharp
// Host, with a player on this machine.
session.Host();

// Or join.
session.Join("192.168.1.10");

// Later.
session.Leave();
```

`Host` returns an error when the port is taken; `Join` returns one when no client could be made, and reports the connection's fate later through `Connected`, `Refused` (with a `RefusalReason`) or `Disconnected`. The server raises `PeerJoined` and `PeerLeft` with the client's peer id.

## Networked entities

A [`ForgeEntity`](nodes.md#forgeentity) with `Networked` on is simulated by the server and shown on every client. Offline it plays like any other entity.

- **Placed or spawned.** One that is in the scene when the server starts hosting is **placed**: every machine loads the same scene, before hosting or joining, so every client has it too, and each machine numbers its placed entities the same way, by their place in the scene. One that enters the tree on the server later is **spawned**: the server sends it to every client, which makes it from the same scene. A spawned entity's scene must be listed in the manifest, which it is when the scene's own `ForgeEntity` is networked. Host before spawning anything networked.
- **Spawning one.** On the server, instantiate the scene and add it to the tree as usual. Set `NetworkOwner` first when it belongs to a player:

```csharp
var player = playerScene.Instantiate<Node3D>();
player.GetNode<ForgeEntity>("Forge Entity").NetworkOwner = (int)peerId;
level.AddChild(player);
```

  The [Instantiate Scene](statescript/nodes/scene-nodes.md) nodes do the same from a graph, and what an entity spawns belongs to that entity's owner. On a client they don't instantiate networked scenes: the server spawns them for everyone.
- **Despawning one.** Free it on the server. Every client frees its copy.
- **One to a scene.** A spawned scene holds one networked entity, and hosting again after leaving needs a fresh level: whatever is in the tree as hosting starts counts as placed.
- **Owners.** `NetworkOwner` is the multiplayer peer id whose player the entity is: 1 for the server's own player, 0 for the world's. A placed entity can only be the server's or the world's, since it's registered before any client has a peer id, and a client's copy of it keeps 0. A [`ForgeInputCollector`](nodes.md#forgeinputcollector) reads the devices only on the machine of the entity's owner, and the server hands each client's input to the entities that client owns.

## What a client sees

A client's copy of a networked entity is a **mirror** (`ForgeEntity.IsMirror`). It simulates nothing: it doesn't advance, starts none of its own effects or abilities, and holds what the server sends. Only its copies of the server's graphs run. `Join` makes mirrors of the level's placed entities at once and for good, even if the connection then fails, so reload the level on `Disconnected` before playing it again.

- **Attributes, tags and cues** are the server's: health bars, tag checks and cue handlers work on a client as they do on the server. Persistent cues start and stop as the server's do, and executed cues, such as a hit flash, play once, unless they're more than 0.2 s old by the time they arrive. On the entity a client owns, an attribute's base value is the server's too; everywhere else a mirror's base value is its current value.
- **Effects** are the server's too, as far as [each one's visibility](#effects-on-a-client) allows: the entity's `EffectsManager` answers queries, a handle tells its stacks and time left, and the `OnActiveEffect` events follow them. They run nothing: no modifiers, periodic executions, components or cues.
- **Graphs** show what the server's show, as far as [clients run them](#graphs-on-a-client): a remote player rolls and its dash whooshes, a brazier's flame lights up.
- **Bodies** are shown between the states the server sent, `Interpolation Delay Ms` (100 ms) behind the newest one, which is itself about half a round trip old, whether a movement component or a graph moves them on the server.
- **Everything shows at the body's moment.** What the server sent of a mirror, its spawn, attributes, tags, cues, effects, graphs and despawn, waits until its body is shown at the tick it's of, so an animation starts as the body moves and a burst plays where it fell. The moment shown runs with the client's own time, a little faster or slower as the estimates of the link move, so bodies don't hitch when they do.
- **Its own player** is a mirror too, and answers its input a round trip late: nothing is predicted yet. The session sets `Local Player` to the networked entity with an input that this machine's peer owns as it appears, and raises `LocalPlayerChanged`, so a camera can follow it.
- **Late joiners** get the world as it is, and **relevancy** is everything by default; set `Relevancy Radius` for a client to hear only about entities within that distance of one it owns.
- **Where spawns go.** A client adds what the server spawns under `Spawn Parent`, or the current scene.

## Effects on a client

A mirror holds the effects active on the server's entity, so a client can show what's on anyone: read them from `EffectsManager` as you would offline.

```csharp
foreach (ActiveEffectHandle effect in entity.EffectsManager.GetActiveEffects())
{
    label.Text += $"{effect.Effect!.EffectData.Name} x{effect.StackCount}, {effect.RemainingDuration:0.0}s\n";
}
```

A [`ForgeEffectData`](resources.md#forgeeffectdata)'s `Visibility` says who hears about it while it's active:

- **Public**: every client that has the entity.
- **Owner Only**: only the client that owns the entity, which suits a cooldown only its player reads.
- **Server Only**: no client, which suits an effect that only grants abilities.
- **Default**, for a new effect: what `forge/networking/default_effect_visibility` says in Project Settings, which is Public.

This decides who gets the effect. What it did is the entity's state, and reaches every client that has the entity whatever this says: attribute values, tags and cues.

- **Listing them.** An effect travels as its index in the manifest, which lists every effect saved as a resource of its own. One saved inside a scene or another resource can't be listed, so it stays on the server, and the session warns once unless it's Server Only. An instant effect never travels: what it did arrives as attribute values and cues.
- **Custom data.** An effect applied with custom data, through `ApplyEffect<TData>`, takes it along when both ends say how the type travels, before hosting or joining. A client that registers other keys is refused; only the keys are compared, so keep what's behind each key the same on both ends. Data of a type neither says anything about stays on the server.

```csharp
session.RegisterEffectContext<HitData>(
    "hit",
    static (writer, hit) => writer.WriteVarInt(hit.Damage),
    static reader => new HitData((int)reader.ReadVarInt()));

// Later, on a client too.
if (handle.TryGetContextData(out HitData? hit))
{
    ShowDamage(hit.Damage);
}
```

## Graphs on a client

A mirror runs a copy of every graph its server entity's abilities run, as far as clients run it, with nothing to set up: presentation nodes show what the server's show, and no link is followed. Presentation state nodes, such as Play Animation, Play Audio and Node Property Override, start and end as the server's do, and presentation actions, such as Play Audio One Shot and Instantiate Scene, run when the server's ran. Simulation and authority nodes don't run: what they did arrives as the entity's state.

- **Scene nodes** are presentation, so a cosmetic scene shows on every client, made by each machine itself. A networked scene is the server's alone to make: a client's graph makes none, and gets the server's entity instead.
- **Nodes acting on what presentation made** are presentation in their graph, such as a Move To flying the ember a Scene node spawned. The node's badge in the editor shows its kind in the graph, and says so.
- **Variables** a graph keeps as values travel as they change, before the states that read them. Object variables stay on each machine, and so do shared variables and activation data: copy what clients need into a graph variable.
- **Joining late**, a client sees what runs as it is: a lit brazier is lit, and an animation already playing starts part way through.
- **A death burst** a graph makes as its entity dies still shows: the entity's last events go with its despawn.
- **Listing them.** An ability's graph travels as the ability's index in the manifest. An ability saved inside another resource can't be listed, and the session warns once when its graph has something clients would show.

### Regions

In the Statescript editor, select nodes and pick **Region, Client Only** or **Region, Server Only** to frame them in a region, blue or purple. Drop a node on a frame to add it, pick **Region, Leave Region** to take selected nodes out, and delete a frame to delete its region. The regions are saved in the graph.

- **Client only**: never sent. Every machine runs the region by itself, the server as part of the graph, and a client from the presentation state, or the entry, that leads into it, for as long as that state runs. For presentation with logic of its own, such as a flame's flicker.
- **Server only**: never runs on a client, and nothing of it is sent. For work that's the server's alone, such as spawning a networked enemy.

## Dedicated servers

A dedicated server simulates the game with nothing to show. Host with `dedicated` set and the presentation passes are skipped: cues, ability presentation and everything else that only shows the game. Cues still reach every client.

```csharp
if (ForgeNetworkSession.DedicatedRequested)
{
    session.Host(dedicated: true);
}
```

`DedicatedRequested` is true when the game was started with the user argument `--dedicated`, after Godot's own `--`:

```bash
godot --headless --path . -- --dedicated
```

## Trying it on one machine

Godot's **Debug, Customize Run Instances** runs the project several times at once. Enable multiple instances, give one instance an argument your menu reads to host, and have the others join `127.0.0.1`. The Networked Statescript sample reads `--host`, `--join` and `--dedicated`.

Localhost has no latency, so the game feels nothing like a real connection. The session node's **Network Conditioner** group adds one: `Latency Ms`, `Jitter Ms` and `Loss Percent` are applied to this machine's outgoing packets, and can be changed while playing. Set them on the client instance, or on both, to try the game at 100 or 200 ms with some loss. `NetworkConditions.Poor` in the core is 200 ms with 50 ms of jitter and 5% loss.

## The overlay

Add a [`ForgeNetworkOverlay`](nodes.md#forgenetworkoverlay) anywhere in the scene. **F3** shows and hides it, and **F4** cycles the conditioner through a perfect, a good, a far and a poor link. It shows the tick and tick rate, the mode and state, and:

- On a client: the round trip, the server's estimated tick, how many ticks ahead this client runs, how many of its input ticks the server holds in hand, how many entities it mirrors, the tick of the newest state it has, and how many state packets came late.
- On a server: how many entities it replicates, and each client's input depth and how many of its ticks were received, lost or late.
- On both: bytes up and down per second, and the conditioner's settings.

## What a client sends

Each physics step, a client sends the server the input frame its tick reads: the held, pressed and released actions, the analog vectors, the aim, and any discrete input events, for the tick its clock named. It sends the last eight ticks every time, so a lost packet costs nothing. The server keeps each client's commands by tick and, as it simulates a tick, hands that tick's command to every entity the client owns that has an input. The session node then raises `InputTaken` with it. A game that raises input events does it there, raising only the ones it allows, so a client can't trigger an ability meant for a server event:

```csharp
// The tags a client may raise as input events, children included.
var inputEvents = new TagContainer(
    ForgeManagers.Instance.TagsManager,
    [Tag.RequestTag(ForgeManagers.Instance.TagsManager, "input")]);

session.InputTaken += (peer, command, delivery) =>
{
    command.RaiseEvents(PlayerOf(peer.Id), inputEvents);
};
```

## The clock

Every entity simulates the tick the session clock names (`ForgeManagers.Instance.Clock`), counted at the physics tick rate. The server's clock is the session's time; a client's is set to the server's tick when it's welcomed and kept about a round trip plus `Lead Margin Ticks` ahead of it, so its input arrives before the server needs it. The clock jumps when it drifts by more than `Lead Tolerance Ticks`. Offline, the clock simply counts from zero when the game starts, and a [`ForgeWorldHistory`](nodes.md#forgeworldhistory) that rolls back takes the clock back with it.
