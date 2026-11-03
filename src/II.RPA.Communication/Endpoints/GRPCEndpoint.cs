namespace II.RPA.Communication.Endpoints;

public abstract record GRPCEndpoint;

public sealed record TcpEndpoint(string Host, int Port) : GRPCEndpoint;

public sealed record NamedPipeEndpoint(string PipeName) : GRPCEndpoint;
