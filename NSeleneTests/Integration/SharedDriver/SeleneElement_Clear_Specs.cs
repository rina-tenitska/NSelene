using NUnit.Framework;
using static NSelene.Selene;

namespace NSelene.Tests.Integration.SharedDriver.SeleneSpec
{
    using System;
    using System.Linq;
    using Harness;
    using OpenQA.Selenium;

    [TestFixture]
    public class SeleneElement_Clear_Specs : BaseTest
    {
        [Test]
        public void Clear_WaitsForVisibility_OfInitiialyAbsent()
        {
            Configuration.Timeout = 0.6;
            Configuration.PollDuringWaits = 0.1;
            Given.OpenedEmptyPage();
            var beforeCall = DateTime.Now;
            Given.OpenedPageWithBodyTimedOut(
                @"
                <input value='abracadabra'></input>
                ",
                300
            );

            S("input").Clear();
            var afterCall = DateTime.Now;

            Assert.AreEqual(
                "", 
                Configuration.Driver
                .FindElement(By.TagName("input")).GetAttribute("value")
            );
            Assert.AreEqual(
                "", 
                Configuration.Driver
                .FindElement(By.TagName("input")).GetDomProperty("value")
            );
            Assert.Greater(afterCall, beforeCall.AddSeconds(0.3));
            Assert.Less(afterCall, beforeCall.AddSeconds(0.6));
        }
        
        [Test]
        public void Clear_IsRenderedInError_OnAbsentElementFailure()
        {
            Configuration.Timeout = 0.25;
            Configuration.PollDuringWaits = 0.1;
            Given.OpenedEmptyPage();

            try 
            {
                S("input").Clear();
            }

            catch (TimeoutException error)
            {
                // TODO: shoud we check timing here too?
                var lines = error.Message.Split(Environment.NewLine).Select(
                    item => item.Trim()
                ).ToList();

                Assert.That(error.Message.Trim(), Does.Contain($$"""
                Timed out after {{0.25}}s, while waiting for:
                    Browser.Element(input).ActualWebElement.Clear()
                Reason:
                    no such element: Unable to locate element: {"method":"css selector","selector":"input"}
                """.Trim()
                ));
            }
        }
        
        [Test]
        public void Clear_IsRenderedInError_OnAbsentElementFailure_WhenCustomizedToWaitForNoOverlapFoundByJs()
        {
            Configuration.Timeout = 0.25;
            Configuration.PollDuringWaits = 0.1;
            Given.OpenedEmptyPage();

            try 
            {
                S("input").With(waitForNoOverlapFoundByJs: true).Clear();
            }

            catch (TimeoutException error)
            {
                Assert.That(error.Message.Trim(), Does.Contain($$"""
                Timed out after {{0.25}}s, while waiting for:
                    Browser.Element(input).ActualNotOverlappedWebElement.Clear()
                Reason:
                    no such element: Unable to locate element: {"method":"css selector","selector":"input"}
                """.Trim()
                ));
            }
        }
        
        // [Test]
        // public void Clear_PassesOnHidden_IfAllowed() // NOT IMPLEMENTED
        // {
        //     Configuration.Timeout = 0.25;
        //     Configuration.PollDuringWaits = 0.1;
        //     Given.OpenedPageWithBody(
        //         @"
        //         <input value='abracadabra' style='display:none'></input>
        //         "
        //     );

        //     S("input").Clear();
            
        //     Assert.AreEqual(
        //         "", 
        //         Configuration.Driver
        //         .FindElement(By.TagName("input")).GetAttribute("value")
        //     );
        //     Assert.AreEqual(
        //         "", 
        //         Configuration.Driver
        //         .FindElement(By.TagName("input")).GetDomProperty("value")
        //     );
        // }

        [Test]
        public void Clear_WaitsForVisibility_OfInitialyHidden()
        {
            Configuration.Timeout = 0.6;
            Configuration.PollDuringWaits = 0.1;
            Given.OpenedPageWithBody(
                @"
                <input value='abracadabra' style='display:none'></input>
                "
            );
            var beforeCall = DateTime.Now;
            Given.ExecuteScriptWithTimeout(
                @"
                document.getElementsByTagName('input')[0].style.display = 'block';
                ",
                300
            );

            S("input").Clear();
            var afterCall = DateTime.Now;

            Assert.AreEqual(
                "", 
                Configuration.Driver
                .FindElement(By.TagName("input")).GetAttribute("value")
            );
            Assert.AreEqual(
                "", 
                Configuration.Driver
                .FindElement(By.TagName("input")).GetDomProperty("value")
            );
            Assert.Greater(afterCall, beforeCall.AddSeconds(0.3));
            Assert.Less(afterCall, beforeCall.AddSeconds(0.6));
        }
        
        [Test]
        public void Clear_IsRenderedInError_OnHiddenElementFailure()
        {
            Configuration.Timeout = 0.25;
            Configuration.PollDuringWaits = 0.1;
            Given.OpenedPageWithBody(
                @"
                <input value='abracadabra' style='display:none'></input>
                "
            );

            try 
            {
                S("input").Clear();
            }

            catch (TimeoutException error)
            {
                Assert.That(error.Message.Trim(), Does.Contain($$"""
                Timed out after {{0.25}}s, while waiting for:
                    Browser.Element(input).ActualWebElement.Clear()
                Reason:
                    element not interactable
                """.Trim()
                ));

                Assert.AreEqual(
                    "abracadabra", 
                    Configuration.Driver
                    .FindElement(By.TagName("input")).GetAttribute("value")
                );
                Assert.AreEqual(
                    "abracadabra", 
                    Configuration.Driver
                    .FindElement(By.TagName("input")).GetDomProperty("value")
                );
            }
        }

        [Test]
        public void Clear_IsRenderedInError_OnHiddenElementFailure_WhenCustomizedToWaitForNoOverlapFoundByJs()
        {
            Configuration.Timeout = 0.25;
            Configuration.PollDuringWaits = 0.1;
            Given.OpenedPageWithBody(
                @"
                <input value='abracadabra' style='display:none'></input>
                "
            );

            try 
            {
                S("input").With(waitForNoOverlapFoundByJs: true).Clear();
            }

            catch (TimeoutException error)
            {
                Assert.That(error.Message.Trim(), Does.Contain($$"""
                Timed out after {{0.25}}s, while waiting for:
                    Browser.Element(input).ActualNotOverlappedWebElement.Clear()
                Reason:
                    javascript error: element is not visible
                """.Trim()
                ));

                Assert.AreEqual(
                    "abracadabra", 
                    Configuration.Driver
                    .FindElement(By.TagName("input")).GetAttribute("value")
                );
                Assert.AreEqual(
                    "abracadabra", 
                    Configuration.Driver
                    .FindElement(By.TagName("input")).GetDomProperty("value")
                );
            }
        }

        [Test]
        public void Clear_WorksUnderOverlay_ByDefault()
        {
            Configuration.Timeout = 1.0;
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

                <input value='abracadabra'></input>
                "
            );
            var beforeCall = DateTime.Now;

            S("input").Clear();
            
            var afterCall = DateTime.Now;
            Assert.Less(afterCall, beforeCall.AddSeconds(0.5));
            Assert.AreEqual(
                "", 
                Configuration.Driver
                .FindElement(By.TagName("input")).GetAttribute("value")
            );
            Assert.AreEqual(
                "", 
                Configuration.Driver
                .FindElement(By.TagName("input")).GetDomProperty("value")
            );
        }

        [Test]
        public void Clear_Waits_For_NoOverlay_WhenCustomized()
        {
            Configuration.Timeout = 1.0;
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

                <input value='abracadabra'></input>
                "
            );
            var beforeCall = DateTime.Now;
            Given.ExecuteScriptWithTimeout(
                @"
                document.getElementById('overlay').style.display = 'none';
                ",
                300
            );

            S("input").With(waitForNoOverlapFoundByJs: true).Clear(); // TODO: this overlay works only for "overlayying at center of element", handle the "partial overlay" cases too!
            
            var afterCall = DateTime.Now;
            Assert.Greater(afterCall, beforeCall.AddSeconds(0.3));
            Assert.Less(afterCall, beforeCall.AddSeconds(1.0));
            Assert.AreEqual(
                "", 
                Configuration.Driver
                .FindElement(By.TagName("input")).GetAttribute("value")
            );
            Assert.AreEqual(
                "", 
                Configuration.Driver
                .FindElement(By.TagName("input")).GetDomProperty("value")
            );
        }

        [Test]
        public void Clear_IsRenderedInError_OnOverlappedWithOverlayFailure_WhenCustomizedToWaitForNoOverlapFoundByJs()
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


                <input value='abracadabra'></input>
                "
            );

            try 
            {
                S("input").With(waitForNoOverlapFoundByJs: true).Clear();
            }

            catch (TimeoutException error)
            {
                Assert.That(error.Message.Trim(), Does.Contain($$"""
                Timed out after {{0.25}}s, while waiting for:
                    Browser.Element(input).ActualNotOverlappedWebElement.Clear()
                Reason:
                    Element: <input value="abracadabra">
                    is overlapped by: <div id="overlay" style="
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
                                    ">
                                </div>
                """.Trim()
                ));
            }
        }
    }
}

