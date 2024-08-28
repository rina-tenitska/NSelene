using NUnit.Framework;
using static NSelene.Selene;

namespace NSelene.Tests.Integration.SharedDriver.SeleneSpec
{
    using System;
    using System.Linq;
    using System.Reflection;
    using Harness;
    using OpenQA.Selenium;

    [TestFixture]
    public class SeleneCollection_Should_Specs : BaseTest
    {
        // TODO: imrove coverage and consider breaking down into separate test classes

        [Test]
        public void Should_HaveCount_WaitsForPresenceInDom_OfInitiialyAbsent()
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

            SS("p").Should(Have.Count(2));
            var afterCall = DateTime.Now;

            Assert.Greater(afterCall, beforeCall.AddSeconds(0.3));
            Assert.Less(afterCall, beforeCall.AddSeconds(1.0));
        }

        [Test]
        public void Should_HaveNoCount_WaitsForAbsenceInDom_OfInitiialyPresent()
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
        public void Should_HaveCount_IsRenderedInError_OnAbsentElementTimeoutFailure()
        {
            Configuration.Timeout = 0.25;
            Configuration.PollDuringWaits = 0.1;
            Given.OpenedEmptyPage();
            var beforeCall = DateTime.Now;

            try 
            {
                SS("p").Should(Have.Count(2));
            }

            catch (TimeoutException error)
            {
                var afterCall = DateTime.Now;
                Assert.Greater(afterCall, beforeCall.AddSeconds(0.25));
                var accuracyDelta = 0.2;
                Assert.Less(afterCall, beforeCall.AddSeconds(0.25 + 0.1 + accuracyDelta));

                Assert.That(error.Message.Trim(), Does.Contain("""
                Timed out after 0,25s, while waiting for:
                	Browser.All(p).Should(Have.Count = 2)
                Reason:
                	actual: Have.Count = 0
                """.Trim()
                ));
            }
        }
        
        [Test]
        public void Should_HaveNoCount_IsRenderedInError_OnInDomElementsTimeoutFailure()
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
                SS("p").Should(Have.No.Count(2));
            }

            catch (TimeoutException error)
            {
                var afterCall = DateTime.Now;
                Assert.Greater(afterCall, beforeCall.AddSeconds(0.25));
                var accuracyDelta = 0.2;
                Assert.Less(afterCall, beforeCall.AddSeconds(0.25 + 0.1 + accuracyDelta));

                Assert.That(error.Message.Trim(), Does.Contain("""
                Timed out after 0,25s, while waiting for:
                	Browser.All(p).Should(Not.Have.Count = 2)
                Reason:
                	condition not matched
                """.Trim()
                ));
            }
        }

        [Test]
        public void Should_HaveCount_WaitsForAsked_OfInitialyOtherVisibleCount()
        {
            Configuration.Timeout = 0.6;
            Configuration.PollDuringWaits = 0.1;
            Given.OpenedPageWithBody(
                @"
                <p>a</p>
                "
            );
            var beforeCall = DateTime.Now;
            Given.OpenedPageWithBodyTimedOut(
                @"
                <p>a</p>
                <p>b</p>
                "
                ,
                300
            );

            SS("p").Should(Have.Count(2));

            var afterCall = DateTime.Now;
            Assert.Greater(afterCall, beforeCall.AddSeconds(0.3));
            Assert.Less(afterCall, beforeCall.AddSeconds(0.6));
            Assert.AreEqual(
                2, 
                Configuration.Driver
                .FindElements(By.TagName("p")).Count
            );
        }
    }
}

