using II.RPA.Logging;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

namespace II.RPA.Core.ProviderTests
{
    [TestFixture]
    public class TestFunction
    {
        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            Console.WriteLine("OneTimeSetUp()");
        }

        [OneTimeTearDown]
        public void TestFixtureTearDown()
        {
            Console.WriteLine("OneTimeTearDown()");
        }

        [SetUp]
        public void SetUp()
        {
            Console.WriteLine("SetUp()");
        }

        [TearDown]
        public void TearDown()
        {
            Console.WriteLine("TearDown()");
        }

        [Test]
        public void TestMethod01()
        {
            Console.WriteLine("TestMethod01()");
        }

        [Test]
        public void TestMethod02()
        {
            Console.WriteLine("TestMethod02()");
        }

        [Test]
        public void LimitTest()
        {
            var text = "12345123451234512345123451234512345123451234512345123451234512345123451234512345123451234512345123451234512345123451234512345123451234512345123451234512345123451234512345123451234512345123451234512345123451234512345123451234512345123451234512345123451234512345";
            var limited1 = text.Limit(100);
            var limited2 = text.Limit(100, "截断", false);
            var limited3 = text.Limit(100, "截断", true);

            Console.WriteLine(limited1);
            Console.WriteLine(limited2);
            Console.WriteLine(limited3);
        }

        [Test]
        public void TestProtect()
        {
            Stopwatch watch = Stopwatch.StartNew();
            JObject protectedContent = null;

            var times = 100;
            while (times-- > 0)
            {
                Console.WriteLine(times);
                protectedContent = RootDto.Demo.ProtectText(
                   null,
                   x => x.Token,
                   x => x.Sub.Password);
            }

            Console.WriteLine(watch.Elapsed);
            Console.WriteLine(protectedContent);
        }

        public class RootDto
        {
            public string Token { get; set; }

            public SubDto Sub { get; set; }

            public static RootDto Demo = new RootDto
            {
                Token = "123457",
                Sub = new SubDto
                {
                    Name = "Test",
                    Password = "Password"
                }
            };
        }

        public class SubDto
        {
            public string Name { get; set; }

            public string Password { get; set; }
        }
    }
}
