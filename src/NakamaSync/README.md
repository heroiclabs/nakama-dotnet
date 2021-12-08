### Introduction

Synchronized variables are a quality-of-life abstraction built on top of Nakama's realtime
multiplayer. These are reactive variables that allow clients to share state with one another in
a straightforward, event-driven manner.

### Basic Usage

```csharp

// Clients A and B
SharedVar<string> helloWorld = new SharedVar<string>(opcode: 0);

var registry = new VarRegistry();
registry.Register(helloWorld);

IMatch match = await socket.JoinSyncMatch(registry)

// Client A
helloWorld.SetValue("Hello World!");

// Client B
helloWorld.OnValueChanged += evt =>
{
    System.Console.WriteLine(evt.ValueChange.NewValue); // "Hello World!"
};

```

Any serializable type can be inserted as the generic type parameter to a synchronized variable. Any exceptions
related to synchronized variables are routed through the socket's `ReceivedError` event.

### Connecting and Match Hosting

In order to use synchronized variables, you must create or join an enriched `IMatch` implementation, `ISyncMatch`. Unlike `IMatch`, `ISyncMatch` will keep its presence list up-to-date as users join and leave. `ISyncMatch` is also host-aware. The creator will be the initial host. If the host leaves or you decide to manually set a new host, `ISyncMatch` will dispatch an `OnHostChangedEvent`.


```csharp
ISyncMatch match = await socket.JoinSyncMatch();

match.OnHostChanged += evt =>
{
    System.Console.Writeline("Old Host: " + evt.OldHost.UserId);
    System.Console.Writeline("New Host: " + evt.NewHost.UserId);
};

match.SetHost(match.Self);
```

There is no need to explicitly set the match host. The initial host will be the match creator. If the match
creator leaves, the host become the first user in an alphanumeric sort on the match's user ids. However, here are a couple of cases where you may want to explicitly set the match host:

(1) The client has lower latency than the others and is therefore better suited to the increased traffic demands on the host.
(2) The client is, for some reason, trusted over the others.

### Broadcast, Validate, Acknowledge
The notion of a host is important to the security of the synchronized variables system. By default, all
synchronized values are trusted by all clients. But you may add a handler to any variable for the host to invoke
upon receiving a new value.

```csharp
// Host client

SharedVar<string> helloWorld = new SharedVar<string>(opcode: 0);

helloWorld.ValidationHandler = (source, change) =>
{
    if (!string.IsNullOrEmpty(change.ValueChange.OldValue))
    {
        // Invalid -- "Hello World" should have only been written to once!
        return false;
    }

    return true;
};
```

Invalid values will be observed by guest clients through a synchronized variable's `OnValueChange.StatusChange` member.

```csharp
// Guest client

SharedVar<string> helloWorld = new SharedVar<string>(opcode: 0);

helloWorld.OnValueChanged = (source, change) =>
{
    if (change.ValidationChange.NewStatus == ValidationStatus.Invalid)
    {
        System.Console.Writeline(change.ValueChange.OldValue); // the invalid value
        System.Console.Writeline(change.ValueChange.NewValue); // null - this is the original value of the variable.
    }
};
```

Do not check if you are the host prior to adding the validation handler. The handler should be added on all clients and will only be utilized when the client becomes the host.

When a guest broadcasts a new value to other players, the new status of those values are `ValidationStatus.Pending`. This allows clients to optimistically apply each value until receiving a `ValidationStatus.Invalid` or
`ValidationStatus.Valid`.

### Variable handshaking
All variables registered before a match join will sync their values as part of the initial sync match task:

```csharp
// Client A creates match.
SharedVar<string> helloWorld = new SharedVar<string>(opcode: 0);

var registry = new VarRegistry();
registry.Register(helloWorld);

IMatch match = await socket.CreateSyncMatch(registry);

helloWorld.SetValue("Hello World!");
System.Console.Writeline(world.GetValue()); // "world!"

// Client B joins match.
SharedVar<string> helloWorld = new SharedVar<string>(opcode: 0);

var registry = new VarRegistry();
registry.Register(helloWorld);

// The helloWorld variable syncs as Client B joins.
IMatch match = await socket.JoinSyncMatch(registry);
System.Console.Writeline(hello.GetValue()); // "Hello"

```

You may also set your variable value prior to joining a sync match. But be aware that any remote clients
that contain a conflicting value for your variable will overwrite your joining value. This is by design
to prevent bad actors from preloading an invalid match state and dumping it onto existing clients.

## Variable types

### Shared Var
The shared var is the most basic and permissive type of shared var. Anyone can write to or read from a shared var.
Any conflicting writes will be rolled back for the writing client to resolve.

### Self Var
On a user's

### Presence Var
Any other presences in the match.

You do not directly register Self and Presence variables. Instead, these are created for you by a Group variable.
This variable reflects

### Accepted var types
- Serializer

### Lock versioning and rollbacks


### Synced Collections

### Clearing the var registry or reusing it for a match

### Managing opcodes

We recommend putting each var opcode into an `enum` for readability. There is a set of Reserved opcodes on the SyncMatch. TODO add the reserved opcodes
Attempting to register duplicate opcodes will provoke an exception.

### Use in authoritative matches
They are a natural fit for relay matches but can also be used in authoritative matches

### Deferred Variable Registration

Sometimes you want to register a variable after the match has started.

In fact, `GroupVar` uses deferred variable registration under the hood in order to create and remove
presence vars as users come and go.


### Handshake process

### Await on handshake task for a deferred variable