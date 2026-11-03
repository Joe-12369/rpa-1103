using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Deeps;
using II.RPA.Plugin.GUI.Deeps.Models;
using Newtonsoft.Json;
using System.Diagnostics;
using Xunit.Abstractions;

namespace Test.II.RPA
{
    public class AutomationElementSerializerTests
    {
        private readonly ITestOutputHelper _testOutputHelper;
        public AutomationElementSerializerTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }
        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(10)]
        public void ToDetailJson_PerformanceTest(int iterations)
        {
            var targetElement = UiCommon.UIAAutomation.FromHandle(new IntPtr(2039330));

            if (targetElement == null)
            {
                _testOutputHelper.WriteLine("Target element not found, skipping test.");
                return;
            }

            long totalMilliseconds = 0;

            for (int i = 0; i < iterations; i++)
            {
                var sw = Stopwatch.StartNew();
                var json = "";// ElementPropertiesProvider.ToPropertiesJson(targetElement.ToModel());
                sw.Stop();

                totalMilliseconds += sw.ElapsedMilliseconds;
                _testOutputHelper.WriteLine($"Iteration {i + 1}/{iterations}: {sw.ElapsedMilliseconds} ms");

                // 验证输出
                Assert.False(string.IsNullOrWhiteSpace(json));
                var dtoList = JsonConvert.DeserializeObject<ElementPropertyDto[]>(json);
                Assert.NotNull(dtoList);
                Assert.Contains(dtoList, d => d.Key == "Name");
                Assert.Contains(dtoList, d => d.Key == "ControlType");
            }

            _testOutputHelper.WriteLine($"Average time: {totalMilliseconds / (double)iterations:F2} ms");
        }
    }
}
