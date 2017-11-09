# WSr

WSr is a server that serves requests for [websocketconnections]( https://tools.ietf.org/html/rfc6455). 

## Getting Started

1. If you want to specify what ip and port the server will listen to you can edit [Program.cs](src/App.WSr/Program.cs)
2. In Program.cs you can also experiment with supplying you own logic to the serve call.
```c#
    public static IObservable<Unit> Serve(
            IConnectedSocket socket,
            Func<byte[]> bufferfactory,
            Action<string> log,
            Func<IObservable<Message>, IObservable<Message>> app,
            IScheduler s = null)
```
|parameter|purpose|
|-|-|
|bufferfactory|experiment with different way of allocating bytes coming in from connected sockets. Each receive from this socket will be allocated to the bytearray produced by this function.|
|log|the action will receive string logmessages from the run|
|app|the server wil run this function on every dataframe it receives on this socket.|
|scheduler|provide a scheduler (the IO-operations will be scheduled on it)|

3. `dotnet run`
4. The server will begin listening for websocket handshake-requests
5. If it receives a handshake-request it will maintain the connection with the client and interpret the following transmission as [websocketframes](https://tools.ietf.org/html/rfc6455#section-5.1)
6. Find [a way](https://www.websocket.org/echo.html) to connect to WSr yourself or test the server with [a thorough fuzzer](https://github.com/crossbario/autobahn-testsuite)
7. Have fun!

### Docker
Assuming your working directory is where [the docker file is at](.):
```docker build -t tag/name . && docker run -d -p 127.0.0.1:9001:80 tag/name```

### Prerequisites

WSr requires [dotnet SDK](https://www.microsoft.com/net/download/core). Also it requires you to be able to download nuget packages. See csproj for dependencies `dotnet restore` should do the trick if not just running it doesn´t. 


### Installing

There's not really so much you need to do to install WSr. The only required step is to clone the repo:

```
git clone https://github.com/goekboet/wsr.git
```

Downloading a fuzzer good for testing. I use [autobahn](https://github.com/crossbario/autobahn-testsuite) and there are some simple macros for easy test from the commandline [included](wsr/test/Fuzzer) in the repo.


## Running the tests

The project has unit-tests that aim to cover the core logic. Each function that transforms the observable should have a corresponding test. 

```dotnet test```

For integration testing I use [autobahn](https://github.com/crossbario/autobahn-testsuite) and there are some simple macros for easy test from the commandline [included](wsr/test/Fuzzer) in the repo.

#### Questions? 
If you have them you can [open an issue](https://github.com/goekboet/wsr/issues) and I'll get back to you asap.
