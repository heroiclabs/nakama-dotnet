Nakama .NET
===========

# Nakama

> .NET client for Nakama server written in C#.

[Nakama](https://github.com/heroiclabs/nakama) is an open-source server designed to power modern games and apps. Features include user accounts, chat, social, matchmaker, realtime multiplayer, and much [more](https://heroiclabs.com).

This client implements the full API and socket options with the server. It's written in C# with minimal dependencies to support Unity, Xamarin, Godot, XNA, and other engines and frameworks.

Full documentation is online - https://heroiclabs.com/docs

## Getting Started

You'll need to setup the server and database before you can connect with the client. The simplest way is to use Docker but have a look at the [server documentation](https://github.com/heroiclabs/nakama#getting-started) for other options.

1. Install and run the servers. Follow these [instructions](https://heroiclabs.com/docs/install-docker-quickstart).

2. Download the client from the [releases page](https://github.com/heroiclabs/nakama-dotnet/releases) and import it into your project. You can also [build from source](#source-builds).

3. Use the connection credentials to build a client object.

    ```csharp
    // using Nakama;
    const string scheme = "http";
    const string host = "127.0.0.1";
    const int port = 7350;
    const string serverKey = "defaultkey";
    var client = new Client(scheme, host, port, serverKey);
    ```

## Usage

The client object has many methods to execute various features in the server or open realtime socket connections with the server.

### Authenticate

There's a variety of ways to [authenticate](https://heroiclabs.com/docs/authentication) with the server. Authentication can create a user if they don't already exist with those credentials. It's also easy to authenticate with a social profile from Google Play Games, Facebook, Game Center, etc.

```csharp
var email = "super@heroes.com";
var password = "batsignal";
var session = await client.AuthenticateEmailAsync(email, password);
System.Console.WriteLine(session);
```

### Sessions

When authenticated the server responds with an auth token (JWT) which contains useful properties and gets deserialized into a `Session` object.

```csharp
System.Console.WriteLine(session.AuthToken); // raw JWT token
System.Console.WriteLine(session.RefreshToken); // raw JWT token.
System.Console.WriteLine(session.UserId);
System.Console.WriteLine(session.Username);
System.Console.WriteLine("Session has expired: {0}", session.IsExpired);
System.Console.WriteLine("Session expires at: {0}", session.ExpireTime);
```

It is recommended to store the auth token from the session and check at startup if it has expired. If the token has expired you must reauthenticate. The expiry time of the token can be changed as a setting in the server.

```csharp
var authToken = "restored from somewhere";
var refreshToken = "restored from somewhere";
var session = Session.Restore(authToken, refreshToken);

// Check whether a session is close to expiry.
if (session.HasExpired(DateTime.UtcNow.AddDays(1)))
{
    try
    {
        session = await client.SessionRefreshAsync(session);
    }
    catch (ApiResponseException e)
    {
        System.Console.WriteLine("Session can no longer be refreshed. Must reauthenticate!");
    }
}
```

:warning: NOTE: The length of the lifetime of a session can be set on the server with the "--session.token_expiry_sec" command flag argument. The lifetime of the refresh token for a session can be set on the server with the "--session.refresh_token_expiry_sec" command flag.

### Requests

The client includes lots of builtin APIs for various features of the game server. These can be accessed with the async methods. It can also call custom logic in RPC functions on the server. These can also be executed with a socket object.

All requests are sent with a session object which authorizes the client.

```csharp
var account = await client.GetAccountAsync(session);
System.Console.WriteLine(account.User.Id);
System.Console.WriteLine(account.User.Username);
System.Console.WriteLine(account.Wallet);
```

Requests can be supplied with a retry configurations in cases of transient network or server errors.

A single configuration can be used to control all request retry behavior:

```csharp
var retryConfiguration = new RetryConfiguration(baseDelayMs: 1000, maxRetries: 5, delegate { System.Console.Writeline("about to retry."); });

client.GlobalRetryConfiguration = retryConfiguration;
var account = await client.GetAccountAsync(session);
```

Or, the configuration can be supplied on a per-request basis:

```csharp

var retryConfiguration = new RetryConfiguration(baseDelayMs: 1000, maxRetries: 5, delegate { System.Console.Writeline("about to retry."); });

var account = await client.GetAccountAsync(session, retryConfiguration);

```

Per-request retry configurations override the global retry
configuration.

Requests also can be supplied with a cancellation token if you need to cancel them mid-flight:

```csharp
var canceller = new CancellationTokenSource();
var account = await client.GetAccountAsync(session, retryConfiguration: null, canceller);

await Task.Delay(25);

canceller.Cancel(); // will raise a TaskCanceledException
```

### Socket

The client can create one or more sockets with the server. Each socket can have it's own event listeners registered for responses received from the server.

```csharp
var socket = Socket.From(client);
socket.Connected += () =>
{
    System.Console.WriteLine("Socket connected.");
};
socket.Closed += () =>
{
    System.Console.WriteLine("Socket closed.");
};
socket.ReceivedError += e => System.Console.WriteLine(e);
await socket.ConnectAsync(session);
```

# Satori

Satori is a liveops server for games that powers actionable analytics, A/B testing and remote configuration. Use the Satori .NET Client to coomunicate with Satori from within your .NET game.

Full documentation is online - https://heroiclabs.com/docs/satori/client-libraries/unity/index.html

## Getting Started

Create a client object that accepts the API you were given as a Satori customer.

```csharp
using Satori;

const string scheme = "https";
const string host = "127.0.0.1"; // add your host here
const int port = 443;
const string apiKey = "apiKey"; // add the api key that was given to you as a Satori customer.

var client = new Client(scheme, host, port, apiKey);
```

Then authenticate with the server to obtain your session.


```csharp
// Authenticate with the Satori server.
try
{
    session = await client.AuthenticateAsync(id);
    Debug.Log("Authenticated successfully.");
}
catch(ApiResponseException ex)
{
    Debug.LogFormat("Error authenticating: {0}", ex.Message);
}
```

Using the client you can get any experiments or feature flags, the user belongs to.

```csharp
var experiments = await client.GetExperimentsAsync(session);
var flag = await client.GetFlagAsync(session, "FlagName");
```

You can also send arbitrary events to the server:

```csharp

await client.EventAsync(session, new Event("gameLaunched", DateTime.UtcNow));

```
This is only a subset of the Satori client API, so please see the documentation link listed earlier for the [full API](https://dotnet.docs.heroiclabs.com/html/namespace_satori.html).


# Contribute

The development roadmap is managed as GitHub issues and pull requests are welcome. If you're interested to improve the code please open an issue to discuss the changes or drop in and discuss it in the [community forum](https://forum.heroiclabs.com).

## Source Builds

The codebase can be built with the [Dotnet CLI](https://docs.microsoft.com/en-us/dotnet/core/tools). All dependencies are downloaded at build time with Nuget.

```shell
dotnet build Nakama/Nakama.csproj
```

```shell
dotnet build Satori/Satori.csproj
```

For release builds see [our instructions](./RELEASEINST.md):

## Run Tests

To run tests you will need to run the server and database. Most tests are written as integration tests which execute against the server. A quick approach we use with our test workflow is to use the Docker compose file described in the [documentation](https://heroiclabs.com/docs/install-docker-quickstart).

```shell
docker-compose -f ./docker-compose-postgres.yml up
dotnet test Nakama.Tests/Nakama.Tests.csproj
```

To run a specific test, pass the fully qualified name of the method to `dotnet test --filter`:

```shell
dotnet test --filter "Nakama.Tests.Api.GroupTest.ShouldPromoteAndDemoteUsers"
```

If you'd like to attach a Visual Studio debugger to a test, set `VSTEST_HOST_DEBUG` to `true` in your shell environment and run `dotnet test`. Attach the debugger to the process identified by the console.

In order to pass tests for Satori, the Satori console must be populated with sample data available via a button in its GUI.
Then you can test the SDK with `dotnet test Satori.Tests/Satori.Tests.csproj`.

## Generate Docs

nakama-dotnet API docs are generated with Doxygen and deployed to GitHub pages.

When changing the API comments, rerun Doxygen and commit the changes in `docs/*`.

To run Doxygen:

```
brew install doxygen
cd docs/
doxygen
```

# Licenses

This project is licensed under the [Apache-2 License](https://github.com/heroiclabs/nakama-dotnet/blob/master/LICENSE).

# Special Thanks

Thanks to Alex Parker (@zanders3) for the excellent [json](https://github.com/zanders3/json) library and David Haig (@ninjasource) for [Ninja.WebSockets](https://github.com/ninjasource/Ninja.WebSockets).
