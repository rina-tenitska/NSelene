using NUnit.Framework;
using static NSelene.Selene;

namespace NSelene.Tests.Integration.SharedDriver.SeleneSpec
{
    using System;
    using System.Linq;
    using Harness;

    [TestFixture]
    public class SeleneElement_Submit_Specs : BaseTest
    {
        [Test]
        public void Submit_WaitsForVisibility_OfInitiialyAbsent()
        {
            Configuration.Timeout = 0.6;
            Configuration.PollDuringWaits = 0.1;
            Given.OpenedEmptyPage();
            var beforeCall = DateTime.Now;
            Given.OpenedPageWithBodyTimedOut(
                @"
                <form action='#second'>go to Heading 2</form>
                <h2 id='second'>Heading 2</h2>
                ",
                300
            );

            S("form").Submit();
            var afterCall = DateTime.Now;

            Assert.IsTrue(Configuration.Driver.Url.Contains("second"));
            Assert.Greater(afterCall, beforeCall.AddSeconds(0.3));
            Assert.Less(afterCall, beforeCall.AddSeconds(0.6));
        }
        
        [Test]
        public void Submit_IsRenderedInError_OnAbsentElementFailure()
        {
            Configuration.Timeout = 0.25;
            Configuration.PollDuringWaits = 0.1;
            Given.OpenedEmptyPage();

            try 
            {
                S("form").Submit();
            }

            catch (TimeoutException error)
            {
                Assert.That(error.Message.Trim(), Does.Contain($$"""
                Timed out after {{0.25}}s, while waiting for:
                    Browser.Element(form).ActualWebElement.Submit()
                Reason:
                    no such element: Unable to locate element: {"method":"css selector","selector":"form"}
                """.Trim()
                ));
            }
        }
        
        [Test]
        public void Submit_IsRenderedInError_OnAbsentElementFailure_WhenCustomizedToWaitForNoOverlapFoundByJs()
        {
            Configuration.Timeout = 0.25;
            Configuration.PollDuringWaits = 0.1;
            Given.OpenedEmptyPage();

            try 
            {
                S("form").With(waitForNoOverlapFoundByJs: true).Submit();
            }

            catch (TimeoutException error)
            {
                Assert.That(error.Message.Trim(), Does.Contain($$"""
                Timed out after {{0.25}}s, while waiting for:
                    Browser.Element(form).ActualNotOverlappedWebElement.Submit()
                Reason:
                    no such element: Unable to locate element: {"method":"css selector","selector":"form"}
                """.Trim()
                ));
            }
        }

        [Test]
        public void Submit_Works_OnHidden_ByDefault() // TODO: but should it?
        // public void Submit_WaitsForVisibility_OfInitialyHidden()
        {
            Configuration.Timeout = 0.6;
            Configuration.PollDuringWaits = 0.1;
            Given.OpenedPageWithBody(
                @"
                <form action='#second' style='display:none'>go to Heading 2</form>
                <h2 id='second'>Heading 2</h2>
                "
            );
            var beforeCall = DateTime.Now;
            // Given.ExecuteScriptWithTimeout(
            //     @"
            //     document.getElementsByTagName('form')[0].style.display = 'block';
            //     ",
            //     300
            // );

            S("form").Submit();

            var afterCall = DateTime.Now;
            Assert.Less(afterCall, beforeCall.AddSeconds(0.3));
            // Assert.Greater(afterCall, beforeCall.AddSeconds(0.3));
            // Assert.Less(afterCall, beforeCall.AddSeconds(0.6));
            Assert.IsTrue(Configuration.Driver.Url.Contains("second"));
        }

        [Test]
        public void Submit_WaitsForVisibility_OfInitialyHidden_WhenCustomizedToWaitForNoOverlapFoundByJs()
        {
            Configuration.Timeout = 0.6;
            Configuration.PollDuringWaits = 0.1;
            Given.OpenedPageWithBody(
                @"
                <form action='#second' style='display:none'>go to Heading 2</form>
                <h2 id='second'>Heading 2</h2>
                "
            );
            var beforeCall = DateTime.Now;
            Given.ExecuteScriptWithTimeout(
                @"
                document.getElementsByTagName('form')[0].style.display = 'block';
                ",
                300
            );

            S("form").With(waitForNoOverlapFoundByJs: true).Submit();

            var afterCall = DateTime.Now;
            Assert.Greater(afterCall, beforeCall.AddSeconds(0.3));
            Assert.Less(afterCall, beforeCall.AddSeconds(0.6));

            Assert.IsTrue(Configuration.Driver.Url.Contains("second"));
        }
        
        [Test]
        public void Submit_IsRenderedInError_OnHiddenElementFailure_WhenCustomizedToWaitForNoOverlapFoundByJs()
        {
            Configuration.Timeout = 0.25;
            Configuration.PollDuringWaits = 0.1;
            Given.OpenedPageWithBody(
                @"
                <form action='#second' style='display:none'>go to Heading 2</form>
                <h2 id='second'>Heading 2</h2>
                "
            );

            try 
            {
                S("form").With(waitForNoOverlapFoundByJs: true).Submit();
            }

            catch (TimeoutException error)
            {
                Assert.That(error.Message.Trim(), Does.Contain($$"""
                Timed out after {{0.25}}s, while waiting for:
                    Browser.Element(form).ActualNotOverlappedWebElement.Submit()
                Reason:
                    javascript error: element is not visible
                """.Trim()
                ));
            }
        }

        [Test]
        public void Submit_Works_UnderOverlay()
        {
            Configuration.Timeout = 0.6;
            Configuration.PollDuringWaits = 0.1;
            Given.OpenedPageWithBody(
                @"
                <div 
                    id='overlay' 
                    style='
                        display:block;
                        position: fixed;
                        display: block;
                        width: 100%;
                        height: 100%;
                        top: 0;
                        left: 0;
                        right: 0;
                        bottom: 0;
                        background-color: rgba(0,0,0,0.1);
                        z-index: 2;
                        cursor: pointer;
                    '
                >
                </div>

                <form action='#second'>go to Heading 2</form>
                <h2 id='second'>Heading 2</h2>
                "
            );
            var beforeCall = DateTime.Now;

            S("form").Submit(); // TODO: this overlay works only for "overlayying at center of element", handle the "partial overlay" cases too!

            var afterCall = DateTime.Now;
            Assert.Less(afterCall, beforeCall.AddSeconds(0.3));
            Assert.IsTrue(Configuration.Driver.Url.Contains("second"));
        }

        [Test]
        public void Submit_Waits_For_NoOverlay_WhenCustomized()
        {
            Configuration.Timeout = 0.6;
            Configuration.PollDuringWaits = 0.1;
            Given.OpenedPageWithBody(
                @"
                <div 
                    id='overlay' 
                    style='
                        display:block;
                        position: fixed;
                        display: block;
                        width: 100%;
                        height: 100%;
                        top: 0;
                        left: 0;
                        right: 0;
                        bottom: 0;
                        background-color: rgba(0,0,0,0.1);
                        z-index: 2;
                        cursor: pointer;
                    '
                >
                </div>

                <form action='#second'>go to Heading 2</form>
                <h2 id='second'>Heading 2</h2>
                "
            );
            var beforeCall = DateTime.Now;
            Given.ExecuteScriptWithTimeout(
                @"
                document.getElementById('overlay').style.display = 'none';
                ",
                300
            );

            S("form").With(waitForNoOverlapFoundByJs: true).Submit(); // TODO: this overlay works only for "overlayying at center of element", handle the "partial overlay" cases too!
            
            var afterCall = DateTime.Now;
            Assert.Greater(afterCall, beforeCall.AddSeconds(0.3));
            Assert.Less(afterCall, beforeCall.AddSeconds(0.6));
            Assert.IsTrue(Configuration.Driver.Url.Contains("second"));
        }

        [Test]
        public void Submit_IsRenderedInError_OnOverlappedWithOverlayFailure_WhenCustomizedToWaitForNoOverlapFoundByJs()
        {
            Configuration.Timeout = 0.25;
            Configuration.PollDuringWaits = 0.1;
            Given.OpenedPageWithBody(
                @"
                <div 
                    id='overlay' 
                    style='
                        display: block;
                        position: fixed;
                        display: block;
                        width: 100%;
                        height: 100%;
                        top: 0;
                        left: 0;
                        right: 0;
                        bottom: 0;
                        background-color: rgba(0,0,0,0.1);
                        z-index: 2;
                        cursor: pointer;
                    '
                >
                </div>

                <form action='#second'>go to H2</form>
                <h2 id='second'>Heading 2</h2>
                "
            );

            try 
            {
                S("form").With(waitForNoOverlapFoundByJs: true).Submit();
            }

            catch (TimeoutException error)
            {
                Assert.That(error.Message.Trim(), Does.Contain($$"""
                Timed out after {{0.25}}s, while waiting for:
                    Browser.Element(form).ActualNotOverlappedWebElement.Submit()
                Reason:
                    Element: <form action="#second">go to H2</form>
                    is overlapped by: <div id="overlay"
                """.Trim()
                ));
            }
        }

        [Test]
        public void Submit_Fails_OnNonFormElement()
        {
            Configuration.Timeout = 0.6;
            Configuration.PollDuringWaits = 0.1;
            Given.OpenedEmptyPage();
            Given.OpenedPageWithBodyTimedOut(
                @"
                <a href='#second'>go to Heading 2</a>
                <h2 id='second'>Heading 2</h2>
                ",
                300
            );

            try 
            {
                S("a").Submit();
            }

            catch (TimeoutException error)
            {
                Assert.That(error.Message.Trim(), Does.Contain(
                    $$"""
                    Reason:
                        javascript error: Unable to find owning document
                    """
                ));

                Assert.IsFalse(Configuration.Driver.Url.Contains("second"));
            }
        }
    }
}

