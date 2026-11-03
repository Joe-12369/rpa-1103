using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.IPC.NamedPipe.Abstractions
{
    public interface INamedPipeRequestHandler
    {
        Task<byte[]> HandleRequestAsync(NamedPipeRequestContext context, byte[] request, CancellationToken cancellationToken);
    }
}
