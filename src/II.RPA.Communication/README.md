# II.RPA.Communication

该项目只负责 gRPC 的基础通信能力：TCP/命名管道端点托管、服务注册、客户端调用器、超时处理，以及与消息类型无关的请求-响应双向流会话。

项目本身不包含任何 `.proto` 文件，也不生成或内置任何业务消息、服务或路由约定。调用方应在自己的项目中引用任意 `.proto`，生成 C# 类型，并在创建 Server/Client 时把生成的绑定传给本框架。

## Server

假设调用方自己的 proto 生成了 `Greeter.GreeterBase` 和 `Greeter.BindService`：

```csharp
var service = new MyGreeterService();

await using var server = new GrpcServer(
    new ServerOptions
    {
        Endpoint = new TcpEndpoint("127.0.0.1", 50051)
    },
    services => services.Register(
        service,
        Greeter.BindService,
        Greeter.BindService));

await server.StartAsync();
```

也可以直接传入任意 `ServerServiceDefinition`：

```csharp
services.Register(serviceDefinition);
```

若命名管道使用的服务定义无法通过反射绑定，请显式提供绑定委托：

```csharp
services.Register(
    Greeter.BindService(service),
    binder => Greeter.BindService(binder, service));
```

## Client

请求默认不设置超时，以保持旧调用方未传 deadline 时的行为。需要有限超时时，可显式传入 RPC deadline、`CreateCallOptions(timeout)` 或配置 `TimeoutOptions.DefaultRequestTimeout`。`Timeout.InfiniteTimeSpan` 表示无限等待，调用者取消和连接断开仍会结束请求。流式请求/应答会话采用相同默认策略；命名管道连接超时独立配置。

调用方把自己 proto 生成的 Client 构造工厂传入：

```csharp
await using var connection = new GrpcClient(
    new ClientOptions
    {
        Endpoint = new TcpEndpoint("127.0.0.1", 50051)
    });

var client = connection.CreateClient(invoker => new Greeter.GreeterClient(invoker));
var reply = await client.SayHelloAsync(new HelloRequest { Name = "RPA" });
```

也可直接使用 `connection.CallInvoker` 发起调用。框架不检查 proto 的 package、service、method、message、字段、流式类型或命名方式。

对于调用方自定义的双向流请求-响应协议，可把生成 Client 返回的 `AsyncDuplexStreamingCall<TRequest, TResponse>` 传给 `CreateRequestReplyStreamSession`，并由调用方提供关联 ID 的提取规则；框架不会要求消息中存在固定字段。
