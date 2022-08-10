codegen
=======

> A util tool to generate a client from the Swagger spec of Nakama's server API.

## Usage

```shell
go run main.go "$GOPATH/src/github.com/heroiclabs/nakama/apigrpc/apigrpc.swagger.json" "Nakama" > ../Nakama/ApiClient.gen.cs
```

### Console API

To generate a client for the Nakama Console, run the following:

```shell
go run main.go "$GOPATH/src/github.com/heroiclabs/nakama/console/console.swagger.json" "Console" > ../Nakama/Console/ConsoleClient.gen.cs
```

### Satori API

To generate a client for Satori, run the following:
```shell
go run main.go "$GOPATH/src/github.com/heroiclabs/satori/api/satori.swagger.json" "Satori" > ../Satori/ApiClient.gen.cs
```


### Rationale

We want to maintain a simple lean low level client within our C# client which has minimal dependencies so we built our own. This gives us complete control over the dependencies required and structure of the code generated.

The generated code is designed to be supported within Unity engine, Xamarin, Godot engine, and other projects. It requires .NET4.5 framework, TinyJson, and uses `System.Threading.Tasks`.

### Limitations

The code generator has __only__ been checked against the Swagger specification generated for Nakama server. YMMV.
