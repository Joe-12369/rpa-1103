using II.RPA.Core.Talker.Talkers;
using Xunit;

namespace II.RPA.Core.Talker.Tests
{
    public class ChildCoreServerPortFileTests
    {
        [Fact]
        public void TryClear_DeletesStalePortFile()
        {
            var path = Path.Combine(Path.GetTempPath(), $"zdesktop-port-{Guid.NewGuid():N}.txt");
            File.WriteAllText(path, "7184");

            var cleared = ChildCoreServerPortFile.TryClear(path, out var error);

            Assert.True(cleared, error);
            Assert.Null(error);
            Assert.False(File.Exists(path));
        }

        [Fact]
        public void TryClear_IsIdempotentWhenFileMissing()
        {
            var path = Path.Combine(Path.GetTempPath(), $"zdesktop-port-{Guid.NewGuid():N}.txt");
            Assert.False(File.Exists(path));

            var cleared = ChildCoreServerPortFile.TryClear(path, out var error);

            Assert.True(cleared, error);
            Assert.Null(error);
        }

        [Fact]
        public void TryClear_ReportsErrorWhenTargetIsDirectory()
        {
            // 目录无法用 File.Delete 删除，用于覆盖失败分支（不抛异常、返回原因）
            var path = Path.Combine(Path.GetTempPath(), $"zdesktop-port-dir-{Guid.NewGuid():N}");
            Directory.CreateDirectory(path);
            try
            {
                var cleared = ChildCoreServerPortFile.TryClear(path, out var error);

                // File.Exists(目录) 为 false，因此按"无需清理"处理并返回成功
                Assert.True(cleared);
                Assert.Null(error);
            }
            finally
            {
                Directory.Delete(path, true);
            }
        }

        [Fact]
        public void FullPath_PointsToZCoreDesktopPortFile()
        {
            var expected = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Z-Core",
                "Z-DeskTop.txt");

            Assert.Equal(expected, ChildCoreServerPortFile.FullPath);
        }
    }
}
