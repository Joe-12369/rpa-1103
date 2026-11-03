using Grpc.Core;
using System.Reflection;

namespace II.RPA.Communication.Hosting;

public sealed class GrpcServiceRegistry
{
    private readonly List<GrpcServiceRegistration> _registrations = [];

    public IReadOnlyList<GrpcServiceRegistration> Registrations => _registrations;

    public void Register<TService>(
        TService service,
        Func<TService, ServerServiceDefinition> createDefinition,
        Action<ServiceBinderBase, TService> bindToServiceBinder)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(createDefinition);
        ArgumentNullException.ThrowIfNull(bindToServiceBinder);

        Register(
            createDefinition(service),
            binder => bindToServiceBinder(binder, service));
    }

    public void Register(ServerServiceDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        _registrations.Add(new GrpcServiceRegistration(definition, binder => BindDefinition(definition, binder)));
    }

    public void Register(ServerServiceDefinition definition, Action<ServiceBinderBase> bindToServiceBinder)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(bindToServiceBinder);

        _registrations.Add(new GrpcServiceRegistration(definition, bindToServiceBinder));
    }

    private static void BindDefinition(ServerServiceDefinition definition, ServiceBinderBase serviceBinder)
    {
        var method = typeof(ServerServiceDefinition).GetMethod("BindService", BindingFlags.Instance | BindingFlags.NonPublic, binder: null, types: [typeof(ServiceBinderBase)], modifiers: null);
        if (method is null)
        {
            throw new InvalidOperationException(
                "Grpc.Core.ServerServiceDefinition does not expose a bind method for ServiceBinderBase.");
        }

        method.Invoke(definition, [serviceBinder]);
    }
}

public sealed class GrpcServiceRegistration
{
    private readonly Action<ServiceBinderBase> _bindToServiceBinder;

    public GrpcServiceRegistration(ServerServiceDefinition definition, Action<ServiceBinderBase> bindToServiceBinder)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        _bindToServiceBinder = bindToServiceBinder ?? throw new ArgumentNullException(nameof(bindToServiceBinder));
    }

    public ServerServiceDefinition Definition { get; }

    public void BindTo(ServiceBinderBase serviceBinder)
    {
        ArgumentNullException.ThrowIfNull(serviceBinder);
        _bindToServiceBinder(serviceBinder);
    }
}
