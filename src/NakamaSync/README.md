### Introduction

Synchronized variables ("sync vars") are a quality-of-life abstraction built on top of Nakama's realtime
matchmaking. Sync vars are reactive variables that allow clients to share state with one another in
a straightforward, event-driven manner.

### Basic Usage

```
SharedVar<string> hello = new SharedVar<string>(opcode: 0);
SharedVar<string> world = new SharedVar<string>(opcode: 1);

var registry = new VarRegistry();
registry.Register(hello);
registry.Register(world);

IMatch match = await socket.JoinSyncMatch(registry)

// Client A
hello.SetValue("hello");

world.OnValueChanged += evt =>
{
    System.Console.WriteLine(evt.ValueChange.NewValue); // world!
};

// Client B

hello.OnValueChanged += evt =>
{
    System.Console.WriteLine(evt.ValueChange.NewValue); // hello
};

world.SetValue("world!");
```


### TODO WRITE THIS ATTRIBUTE CODE
We provide a `[SyncVar]` attribute to allow users to easily register variables at compile time without manually
interacting with a `VarRegistry`.

```

class SyncTest
{
    [SyncVar]
    private readonly SharedVar<string> hello = new SharedVar<string>(opcode: 0);

    public void Start()
    {
        await socket.JoinSyncMatch();
        hello.SetValue("hello world!");
    }
}

```


### Connecting

An enriched `IMatch` object called an `ISyncMatch`. Unlike `IMatch`, it will keep its presence list up-to-date with as users join and leave the match. `ISyncMatch` is also host-aware. The user who created the match will be the initial host. If that user leaves or you decide to manually set a new host, `ISyncMatch` will dispatch an `OnHostChangedEvent`.


```

ISyncMatch match = await socket.JoinSyncMatch();

match.OnHostChanged += evt =>
{
    System.Console.Writeline("Old host was " + evt.OldHost.UserId);
    System.Console.Writeline("New host is " + evt.Oldhost.UserId);

};

match.SetHost(match.Self);

```


### Host validation

### Handshaking

## Variable types



### SharedVar
The shared var is the most basic and permissive type of shared var. Anyone

### Accepted var types
- Serializer

### Lock versioning and rollbacks


### Synced Collections


### Managing opcodes

We recommend putting each var opcode into an `enum` for readability. There is a set of Reserved opcodes on the SyncMatch. TODO add the reserved opcodes

### Use in authoritative matches
They are a natural fit for relay matches but can also be used in authoritative matches

### Deferred Variable Registration

Sometimes you want to register a variable after the match has started

In fact, `GroupVar` uses deferred variable registration under the hood in order to create and remove
presence vars as users come and go.


### Handshake process

### Await on handshake task for a deferred variable