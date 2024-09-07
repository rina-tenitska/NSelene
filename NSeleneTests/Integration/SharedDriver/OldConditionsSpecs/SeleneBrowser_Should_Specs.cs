using NUnit.Framework;
using static NSelene.Selene;

namespace NSelene.Tests.Integration.SharedDriver.OldConditionsSpecs
{
    using System;
    using System.Linq;
    using System.Reflection;
    using Harness;
    using OpenQA.Selenium;

    [TestFixture]
    public class SeleneBrowser_Should_OldStyleConditions_Specs : BaseTest
    {
        // TODO: imrove coverage and consider breaking down into separate test classes

        [Test]
        public void SeleneWaitTo_HaveJsReturned_WaitsForPresenceInDom_OfInitiialyAbsent()
        {
            Configuration.Timeout = 0.6; 
            Configuration.PollDuringWaits = 0.1;
            Given.OpenedEmptyPage();
            var beforeCall = DateTime.Now;
            Given.OpenedPageWithBodyTimedOut(
                @"
                <p style='display:none'>a</p>
                <p style='display:none'>b</p>
                ",
                300
            );

            Selene.WaitTo(Have.JSReturnedTrue(
                @"
                var expectedCount = arguments[0]
                return document.getElementsByTagName('p').length == expectedCount
                "
                ,
                2
            ));

            var afterCall = DateTime.Now;
            Assert.Greater(afterCall, beforeCall.AddSeconds(0.3));
            Assert.Less(afterCall, beforeCall.AddSeconds(0.6));
        }

        [Test]
        public void SeleneWaitTo_HaveNoJsReturned_WaitsForAbsenceInDom_OfInitiialyPresent()
        {
            Configuration.Timeout = 0.6; 
            Configuration.PollDuringWaits = 0.1;
            Given.OpenedPageWithBody(
                @"
                <p style='display:none'>a</p>
                <p style='display:none'>b</p>
                "
            );
            var beforeCall = DateTime.Now;
            Given.WithBodyTimedOut(
                @"
                ",
                300
            );

            SS("p").Should(Have.No.Count(2));
            Selene.WaitTo(Have.No.JSReturnedTrue(
                @"
                var expectedCount = arguments[0]
                return document.getElementsByTagName('p').length == expectedCount
                "
                ,
                2
            ));

            var afterCall = DateTime.Now;
            Assert.Greater(afterCall, beforeCall.AddSeconds(0.3));
            Assert.Less(afterCall, beforeCall.AddSeconds(0.6));
                Assert.AreEqual(
                    0, 
                    Configuration.Driver
                    .FindElements(By.TagName("p")).Count
                );
        }
        
        [Test]
        public void SeleneWaitTo_HaveJsReturned_IsRenderedInError_OnAbsentElementTimeoutFailure()
        {
            Configuration.Timeout = 0.25;
            Configuration.PollDuringWaits = 0.1;
            Given.OpenedEmptyPage();
            var beforeCall = DateTime.Now;

            try
            {
                Selene.WaitTo(Have.JSReturnedTrue(
                    @"
                    var expectedCount = arguments[0]
                    return document.getElementsByTagName('p').length == expectedCount
                    "
                    ,
                    2
                ));
            }

            catch (TimeoutException error)
            {
                var afterCall = DateTime.Now;
                Assert.Greater(afterCall, beforeCall.AddSeconds(0.25));
                var accuracyDelta = 0.2;
                Assert.Less(afterCall, beforeCall.AddSeconds(0.25 + 0.1 + accuracyDelta));
                Assert.That(error.Message.Trim(), Does.Contain($$"""
                Timed out after {{0.25}}s, while waiting for:
                    OpenQA.Selenium.Chrome.ChromeDriver.Should(JSReturnedTrue)
                """.Trim()
                ));
            }

            // catch (TimeoutException error)
            // {
            //     var afterCall = DateTime.Now;
            //     Assert.Greater(afterCall, beforeCall.AddSeconds(0.25));
            //     var accuracyDelta = 0.2;
            //     Assert.Less(afterCall, beforeCall.AddSeconds(0.25 + 0.1 + accuracyDelta));

            //     // TODO: shoud we check timing here too?
            //     var lines = error.Message.Split("\n").Select(
            //         item => item.Trim()
            //     ).ToList();

            //     Assert.Contains("Timed out after 0.25s, while waiting for:", lines);
            //     Assert.Contains("Browser.All(p).count = 2", lines);
            //     Assert.Contains("Reason:", lines);
            //     Assert.Contains("actual: count = 0", lines);
            // }
        }
        
        [Test]
        public void SeleneWaitTo_HaveNoJsReturned_IsRenderedInError_OnInDomElementsTimeoutFailure()
        {
            Configuration.Timeout = 0.25;
            Configuration.PollDuringWaits = 0.1;
            Given.OpenedPageWithBody(
                @"
                <p style='display:none'>a</p>
                <p style='display:none'>b</p>
                "
            );
            var beforeCall = DateTime.Now;

            try 
            {
                Selene.WaitTo(Have.No.JSReturnedTrue(
                    @"
                    var expectedCount = arguments[0]
                    return document.getElementsByTagName('p').length == expectedCount
                    "
                    ,
                    2
                ));
            }

            catch (TimeoutException error)
            {
                var afterCall = DateTime.Now;
                Assert.Greater(afterCall, beforeCall.AddSeconds(0.25));
                var accuracyDelta = 0.2;
                Assert.Less(afterCall, beforeCall.AddSeconds(0.25 + 0.1 + accuracyDelta));

                Assert.That(error.Message.Trim(), Does.Contain($$"""
                Timed out after {{0.25}}s, while waiting for:
                    OpenQA.Selenium.Chrome.ChromeDriver.Should(Not.JSReturnedTrue)
                """.Trim()
                ));

            }

            // catch (TimeoutException error)
            // {
            //     var afterCall = DateTime.Now;
            //     Assert.Greater(afterCall, beforeCall.AddSeconds(0.25));
            //     var accuracyDelta = 0.2;
            //     Assert.Less(afterCall, beforeCall.AddSeconds(0.25 + 0.1 + accuracyDelta));

            //     // TODO: shoud we check timing here too?
            //     var lines = error.Message.Split("\n").Select(
            //         item => item.Trim()
            //     ).ToList();

            //     Assert.Contains("Timed out after 0.25s, while waiting for:", lines);
            //     Assert.Contains("Browser.All(p).not count = 2", lines);
            //     Assert.Contains("Reason:", lines);
            //     Assert.Contains("actual: count = 2", lines);
            // }
        }

        // [Test]
        // public void SeleneWaitTo_HaveNoJsReturned_WaitsForAsked_OfInitialyOtherResult() // NOT RELEVANT
        // {
        // }
    }
}

