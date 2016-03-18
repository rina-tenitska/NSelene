﻿using NUnit.Framework;
using NSelene;
using static NSelene.Utils;
using OpenQA.Selenium;

namespace NSeleneTests.WithManagedBrowserBeforAndAfterAllTests
{
    [TestFixture]
    public class InnerSElementSearchTests
    {

        [TearDown]
        public void TeardownTest()
        {
            Config.Timeout = 4;
        }
        
        [Test]
        public void InnerSElementSearchIsLazyAndDoesNotStartOnCreation()
        {
            Given.OpenedPageWithBody("<p id='#existing'>Hello!</p>");
            var nonExistentElement = S("#existing").Find("#not-existing-inner");
        }

        [Test]
        public void SElementSearchIsLazyAndDoesNotStartEvenOnFollowingInnerSearch()
        {
            Given.OpenedEmptyPage();
            var nonExistentElement = S("#not-existing").Find("#not-existing-inner");
        }

        [Test]
        public void InnerSElementSearchIsPostponedUntilActualActionLikeQuestioiningValue()
        {
            Given.OpenedPageWithBody("<p id='existing'>Hello!</p>");
            var element = S("#existing").Find("#will-exist");
            When.WithBody(@"
                <p id='existing'>Hello! 
                    <input id='will-exist' type='submit' value='How r u?'></input>
                </p>"
            );
            Assert.AreEqual("How r u?", element.GetValue());
        }

        [Test]
        public void InnerSElementSearchFindsExactlyInsideParentElement()
        {
            Given.OpenedPageWithBody(@"
                <a href='#first' style='display:none'>go to Heading 2</a>
                <p>
                    <a href='#second'>go to Heading 2</a>
                    <h1 id='first'>Heading 1</h1>
                    <h2 id='second'>Heading 2</h2>
                </p>"
            );
            S("p").S("a").Click();
            Assert.IsTrue(Utils.GetDriver().Url.Contains("second"));
        }

        [Test]
        public void InnerSElementSearchWaitsForVisibilityOnActionsLikeClick()
        {
            Given.OpenedPageWithBody(@"
                <p>
                    <a href='#second' style='display:none'>go to Heading 2</a>
                    <h2 id='second'>Heading 2</h2>
                </p>"
            );
            Utils.ExecuteScript(@"
                setTimeout(
                    function(){
                        document.getElementsByTagName('a')[0].style = 'display:block';
                    }, 
                    1000);"
            );
            S("p").S("a").Click();
            Assert.IsTrue(Utils.GetDriver().Url.Contains("second"));
        }

        [Test]
        public void BothNormalAndInnerSElementSearchWaitsForVisibilityOnActionsLikeClick()
        {
            Given.OpenedPageWithBody(@"
                <p style='display:none'>
                    <a href='#second' style='display:none'>go to Heading 2</a>
                    <h2 id='second'>Heading 2</h2>
                </p>"
            );
            Utils.ExecuteScript(@"
                setTimeout(
                    function(){
                        document.getElementsByTagName('p')[0].style = 'display:block';
                    }, 
                    500);
               setTimeout(
                    function(){
                        document.getElementsByTagName('a')[0].style = 'display:block';
                    }, 
                    1000);"
            );
            S("p").S("a").Click();
            Assert.IsTrue(Utils.GetDriver().Url.Contains("second"));
        }

        [Test]
        public void InnerSElementSearchFailsOnTimeoutDuringWaitingForVisibilityOnActionsLikeClick()
        {
            Config.Timeout = 0.5;
            Given.OpenedPageWithBody(@"
                <p>
                    <a href='#second' style='display:none'>go to Heading 2</a>
                    <h2 id='second'>Heading 2</h2>
                </p>"
            );
            Utils.ExecuteScript(@"
                setTimeout(
                    function(){
                        document.getElementsByTagName('a')[0].style = 'display:block';
                    }, 
                    1000);"
            );
            try {
                S("p").S("a").Click();
                Assert.Fail("should fail on timeout before can be clicked");
            } catch (WebDriverTimeoutException ex) {
                Assert.IsFalse(Utils.GetDriver().Url.Contains("second"));
            }
        }
    }
}

