# Networking

Forge for Godot plays a game over the network with one server in charge: the server simulates every entity from the players' input, and each client sends only what its player pressed. This page is the setup. The design, the protocol and what each piece does are in the core's [networking page](https://github.com/gamesmiths-guild/forge/blob/main/docs/networking/README.md).

## Setup

1. **The session node.** Add a [`ForgeNetworkSession`](nodes.md#forgenetworksession) to the scene that plays online, and point its `Local Player` at the `ForgeEntity` the player on this machine controls, the one with the [`ForgeInputCollector`](nodes.md#forgeinputcollector). One session per scene.
2. **The input actions.** Networked input travels as the project's Forge input actions (`forge/input/actions` and `forge/input/vectors` in Project Settings), so every machine must list the same ones in the same order. A client whose list differs is refused. Input events travel as Forge tags, so the registered tags must match too; a client whose tags differ is refused.
3. **The asset manifest.** Both ends must agree on which effect, ability or graph an index means. The editor plugin keeps a [`ForgeNetworkManifest`](resources.md#forgenetworkmanifest) at `res://forge_network_manifest.tres` (the path is `forge/networking/manifest` in Project Settings), regenerated when one of those resources is saved, when the project's files change, and from **Project, Tools, Forge, Regenerate network manifest**. Commit it; a client whose manifest differs is refused.
4. **Host, join, leave.** From a menu or a script:

```csharp
// Host, with a player on this machine.
session.Host();

// Or join.
session.Join("192.168.1.10");

// Later.
session.Leave();
```

`Host` returns an error when the port is taken; `Join` returns one when no client could be made, and reports the connection's fate later through `Connected`, `Refused` (with a `RefusalReason`) or `Disconnected`. The server raises `PeerJoined` and `PeerLeft` with the client's peer id.

## Dedicated servers

A dedicated server simulates the game with nothing to show. Host with `dedicated` set and the presentation passes are skipped: cues, ability presentation and everything else that only shows the game.

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

Godot's **Debug, Run Multiple Instances** runs the project several times at once. Set it to two or more, give one instance a feature tag or an argument your menu reads to host, and have the others join `127.0.0.1`.

Localhost has no latency, so the game feels nothing like a real connection. The session node's **Network Conditioner** group adds one: `Latency Ms`, `Jitter Ms` and `Loss Percent` are applied to this machine's outgoing packets, and can be changed while playing. Set them on the client instance, or on both, to try the game at 100 or 200 ms with some loss. `NetworkConditions.Poor` in the core is 200 ms with 50 ms of jitter and 5% loss.

## The overlay

Add a [`ForgeNetworkOverlay`](nodes.md#forgenetworkoverlay) anywhere in the scene. **F3** shows and hides it, and **F4** cycles the conditioner through a perfect, a good, a far and a poor link. It shows the tick and tick rate, the mode and state, and:

- On a client: the round trip, the server's estimated tick, how many ticks ahead this client runs, and how many of its input ticks the server holds in hand.
- On a server: each client's input depth and how many of its ticks were received, lost or late.
- On both: bytes up and down per second, and the conditioner's settings.

## What a client sends

Each physics step, a client sends the server the input frame its tick reads: the held, pressed and released actions, the analog vectors, the aim, and any discrete input events, for the tick its clock named. It sends the last eight ticks every time, so a lost packet costs nothing. The server keeps each client's commands by tick and, as it simulates a tick, takes that tick's command; the session node raises `InputTaken` with it, and the game feeds it to the entity the client controls, raising only the input events it allows, so a client can't trigger an ability meant for a server event:

```csharp
// The tags a client may raise as input events, children included.
var inputEvents = new TagContainer(
    ForgeManagers.Instance.TagsManager,
    [Tag.RequestTag(ForgeManagers.Instance.TagsManager, "input")]);

session.InputTaken += (peer, command, delivery) =>
{
    ForgeEntity entity = EntityOf(peer.Id);
    entity.Input?.Submit(command.Frame);
    command.RaiseEvents(entity, inputEvents);
};
```

Which entity a peer controls is the game's to decide.

## The clock

Every entity simulates the tick the session clock names (`ForgeManagers.Instance.Clock`), counted at the physics tick rate. The server's clock is the session's time; a client's is set to the server's tick when it's welcomed and kept about a round trip plus `Lead Margin Ticks` ahead of it, so its input arrives before the server needs it. The clock jumps when it drifts by more than `Lead Tolerance Ticks`. Offline, the clock simply counts from zero when the game starts, and a [`ForgeWorldHistory`](nodes.md#forgeworldhistory) that rolls back takes the clock back with it.
