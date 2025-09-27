codegen
=======

> A util tool to generate a client from the Swagger spec of Nakama's server API.

### Nakama API

To generate a client for Nakama, run the following:

```shell
task -v generate-nakama
```

### Nakama Console API

To generate a client for the Nakama Console, run the following:

```shell
task -v generate-nakamaconsole
```

### Satori API

To generate a client for Satori, run the following:

```shell
task -v generate-satori
```

### Satori Console API

To generate a client for the Satori Console, run the following:

```shell
task -v generate-satoriconsole
```

### Rationale

We want to maintain a simple lean low level client within our C# client which has minimal dependencies so we built our own. This gives us complete control over the dependencies required and structure of the code generated.

The generated code is designed to be supported within Unity engine, Xamarin, Godot engine, and other projects. It requires .NET4.5 framework, TinyJson, and uses `System.Threading.Tasks`.

### Limitations

The code generator has __only__ been checked against the Swagger specification generated for Nakama server. YMMV.
