using II.RPA.WebBrowser.Common.Basic.connection;
using II.RPA.WebBrowser.Common.Basic.connection.ClientTransfer;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace II.RPA.WebBrowser.Common.Tests
{
    public class ClientTransferRequestDispatcherTests
    {
        [Fact]
        public async Task CompletesPendingRequestsOutOfOrder()
        {
            using (var dispatcher = new ClientTransferRequestDispatcher())
            {
                string firstId = Guid.NewGuid().ToString();
                string secondId = Guid.NewGuid().ToString();
                ClientTransferEntry first = dispatcher.Register(firstId, 5000);
                ClientTransferEntry second = dispatcher.Register(secondId, 5000);

                Assert.True(dispatcher.TryComplete(Response(secondId)));
                Assert.True(dispatcher.TryComplete(Response(firstId)));

                Assert.Equal(secondId, (await second.Completion).Response.MessageId);
                Assert.Equal(firstId, (await first.Completion).Response.MessageId);
                Assert.Equal(0, dispatcher.PendingCount);
            }
        }

        [Fact]
        public async Task DuplicateMessageIdDoesNotReplaceOriginalEntry()
        {
            using (var dispatcher = new ClientTransferRequestDispatcher())
            {
                string messageId = Guid.NewGuid().ToString();
                ClientTransferEntry original = dispatcher.Register(messageId, 5000);
                ClientTransferEntry duplicate = dispatcher.Register(messageId, 5000);

                Assert.Equal(ClientTransferCompletionReason.MessageIdConflict, (await duplicate.Completion).Reason);
                Assert.Equal(1, dispatcher.PendingCount);

                Assert.True(dispatcher.TryComplete(Response(messageId)));
                Assert.Equal(ClientTransferCompletionReason.Response, (await original.Completion).Reason);
                Assert.Equal(0, dispatcher.PendingCount);
            }
        }

        [Fact]
        public async Task TimeoutRemovesOnlyCurrentEntryAndLateResponseIsDiscarded()
        {
            using (var dispatcher = new ClientTransferRequestDispatcher())
            {
                string timedOutId = Guid.NewGuid().ToString();
                string activeId = Guid.NewGuid().ToString();
                ClientTransferEntry timedOut = dispatcher.Register(timedOutId, 30);
                ClientTransferEntry active = dispatcher.Register(activeId, 5000);

                Assert.Equal(ClientTransferCompletionReason.Timeout, (await timedOut.Completion).Reason);
                Assert.False(dispatcher.TryComplete(Response(timedOutId)));
                Assert.False(dispatcher.IsClosed);
                Assert.Equal(1, dispatcher.PendingCount);

                Assert.True(dispatcher.TryComplete(Response(activeId)));
                Assert.Equal(ClientTransferCompletionReason.Response, (await active.Completion).Reason);
            }
        }

        [Fact]
        public async Task CompleteAllClosesDispatcherAndCompletesEveryPendingEntry()
        {
            using (var dispatcher = new ClientTransferRequestDispatcher())
            {
                ClientTransferEntry[] entries = Enumerable.Range(0, 8)
                    .Select(_ => dispatcher.Register(Guid.NewGuid().ToString(), 5000))
                    .ToArray();
                var exception = new InvalidOperationException("stream closed");

                dispatcher.CompleteAll(ClientTransferCompletionReason.ConnectionClosed, exception);

                ClientTransferDispatchResult[] results = await Task.WhenAll(entries.Select(entry => entry.Completion));
                Assert.All(results, result =>
                {
                    Assert.Equal(ClientTransferCompletionReason.ConnectionClosed, result.Reason);
                    Assert.Same(exception, result.Exception);
                });
                Assert.True(dispatcher.IsClosed);
                Assert.Equal(0, dispatcher.PendingCount);

                ClientTransferEntry rejected = dispatcher.Register(Guid.NewGuid().ToString(), 5000);
                Assert.Equal(ClientTransferCompletionReason.ConnectionClosed, (await rejected.Completion).Reason);
            }
        }

        [Fact]
        public async Task ResponseAndConnectionCloseRaceCompletesEntryExactlyOnce()
        {
            for (int index = 0; index < 200; index++)
            {
                using (var dispatcher = new ClientTransferRequestDispatcher())
                {
                    string messageId = Guid.NewGuid().ToString();
                    ClientTransferEntry entry = dispatcher.Register(messageId, 5000);

                    await Task.WhenAll(
                        Task.Run(() => dispatcher.TryComplete(Response(messageId))),
                        Task.Run(() => dispatcher.CompleteAll(ClientTransferCompletionReason.ConnectionClosed)));

                    ClientTransferDispatchResult result = await entry.Completion;
                    Assert.True(
                        result.Reason == ClientTransferCompletionReason.Response ||
                        result.Reason == ClientTransferCompletionReason.ConnectionClosed);
                    Assert.Equal(0, dispatcher.PendingCount);
                }
            }
        }

        [Fact]
        public async Task RegisterAndCloseRaceAlwaysPublishesCloseResultBeforeClosedState()
        {
            for (int index = 0; index < 2000; index++)
            {
                using (var dispatcher = new ClientTransferRequestDispatcher())
                {
                    ClientTransferEntry entry = null;
                    await Task.WhenAll(
                        Task.Run(() => entry = dispatcher.Register(Guid.NewGuid().ToString(), 5000)),
                        Task.Run(() => dispatcher.CompleteAll(ClientTransferCompletionReason.ProtocolError)));

                    ClientTransferDispatchResult result = await entry.Completion;
                    Assert.NotNull(result);
                    Assert.Equal(ClientTransferCompletionReason.ProtocolError, result.Reason);
                    Assert.Equal(0, dispatcher.PendingCount);
                }
            }
        }

        private static TransferMessage Response(string messageId)
        {
            return new TransferMessage { MessageId = messageId, MessageType = MessageType.Collback };
        }
    }
}
