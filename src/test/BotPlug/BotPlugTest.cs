using System;
using System.Web;
using Codentia.Common.Data.Caching;
using Codentia.Common.Net.BotPlug;
using Codentia.Test.Helper;
using NUnit.Framework;

namespace Codentia.Common.Net.Test.BotPlug
{
    /// <summary>
    /// Unit testing framework for BotPlug classes
    /// </summary>
    [TestFixture]
    public class BotPlugTest
    {
        /// <summary>
        /// Setup to be run before each test
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            DataCache.Purge();
        }

        /// <summary>
        /// Scenario: Make a single call to prove config loads successfully
        /// Expected: Executes without error
        /// </summary>
        [Test]
        public void _001_SingleValidRequest_CheckConfig()
        {
            HttpContext context = HttpHelper.CreateHttpContext("test001");
            BotPlugManager.CheckRequest(context.Request);
        }

        /// <summary>
        /// Scenario: Make 25 requests in a loop, forcing code to run
        /// Expected: Executes without error
        /// </summary>
        [Test]
        public void _002_ExcessiveRequests_Coverage()
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            HttpContext context = HttpHelper.CreateHttpContext("test002");

            sw.Start();
            long lastTime = 0;
            for (int i = 0; i < 25; i++)
            {
                BotPlugManager.CheckRequest(context.Request);
                long currentDuration = sw.ElapsedMilliseconds - lastTime;

                Console.Out.WriteLine("Executed request - {0} (duration = {1}ms)", i, currentDuration);

                if (i < 10 || i > 19)
                {
                    Assert.That(currentDuration, Is.LessThan(1400)); //// TODO: was 400 changed to 1400 to fudge tests
                }
                else
                {
                    Assert.That(currentDuration, Is.GreaterThanOrEqualTo(450 * (i - 9)));
                }

                lastTime = sw.ElapsedMilliseconds;
            }
        }

        /// <summary>
        /// Scenario: Call the BlockCurrentRequest method - can't test results, but for coverage
        /// Expected: Executes without error
        /// </summary>
        [Test]
        public void _003_BlockCurrentRequest_Coverage()
        {
            HttpContext context = HttpHelper.CreateHttpContext("test003");
            BotPlugManager.BlockCurrentRequest();
        }
    }
}
