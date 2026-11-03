using II.RPA.WebBrowser.Common.Basic.connection;
using System;
using Xunit;

namespace II.RPA.WebBrowser.Common.Tests
{
    public class NmhConnectionCapabilityResolverTests
    {
        [Theory]
        [InlineData("7.3.6.9", 1)]
        [InlineData("7.3.7.0", 2)]
        [InlineData("7.3.7.1", 2)]
        [InlineData("8.0.0.0", 2)]
        public void ResolvesProtocolAtVersionBoundary(
            string fileVersion,
            int expectedProtocolValue)
        {
            var reader = new StubMetadataReader("C:\\nmh.exe", fileVersion);

            NmhConnectionCapability capability =
                new NmhConnectionCapabilityResolver(reader).Resolve(1234);

            Assert.Equal((NmhConnectionProtocol)expectedProtocolValue, capability.Protocol);
            Assert.Equal(1234, capability.ProcessId);
            Assert.Equal("C:\\nmh.exe", capability.ExeFilePath);
            Assert.Equal(new Version(fileVersion), capability.FileVersion);
            Assert.Null(capability.Error);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("not-a-version")]
        public void InvalidVersionFailsClosed(string fileVersion)
        {
            var reader = new StubMetadataReader("C:\\nmh.exe", fileVersion);

            NmhConnectionCapability capability =
                new NmhConnectionCapabilityResolver(reader).Resolve(1234);

            Assert.Equal(NmhConnectionProtocol.Failure, capability.Protocol);
            Assert.NotNull(capability.Error);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void EmptyExePathFailsClosed(string exeFilePath)
        {
            var reader = new StubMetadataReader(exeFilePath, "7.3.7.0");

            NmhConnectionCapability capability =
                new NmhConnectionCapabilityResolver(reader).Resolve(1234);

            Assert.Equal(NmhConnectionProtocol.Failure, capability.Protocol);
            Assert.NotNull(capability.Error);
            Assert.Equal(0, reader.FileVersionReadCount);
        }

        [Fact]
        public void MetadataReadExceptionFailsClosed()
        {
            var expected = new InvalidOperationException("process exited");
            var reader = new StubMetadataReader(expected);

            NmhConnectionCapability capability =
                new NmhConnectionCapabilityResolver(reader).Resolve(1234);

            Assert.Equal(NmhConnectionProtocol.Failure, capability.Protocol);
            Assert.Same(expected, capability.Error);
        }

        [Fact]
        public void ResolveReadsMetadataAgainForSameProcessId()
        {
            var reader = new StubMetadataReader("C:\\nmh.exe", "7.3.7.0");
            var resolver = new NmhConnectionCapabilityResolver(reader);

            resolver.Resolve(1234);
            resolver.Resolve(1234);

            Assert.Equal(2, reader.ExePathReadCount);
            Assert.Equal(2, reader.FileVersionReadCount);
        }

        private sealed class StubMetadataReader : INmhProcessMetadataReader
        {
            private readonly string _exeFilePath;
            private readonly string _fileVersion;
            private readonly Exception _exePathException;

            internal StubMetadataReader(string exeFilePath, string fileVersion)
            {
                _exeFilePath = exeFilePath;
                _fileVersion = fileVersion;
            }

            internal StubMetadataReader(Exception exePathException)
            {
                _exePathException = exePathException;
            }

            internal int ExePathReadCount { get; private set; }

            internal int FileVersionReadCount { get; private set; }

            public string GetExeFilePath(int processId)
            {
                ExePathReadCount++;
                if (_exePathException != null)
                {
                    throw _exePathException;
                }

                return _exeFilePath;
            }

            public string GetFileVersion(string exeFilePath)
            {
                FileVersionReadCount++;
                return _fileVersion;
            }
        }
    }
}
