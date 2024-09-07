using OpenQA.Selenium;
using NSelene.Conditions;
using System.Drawing;
using OpenQA.Selenium.Interactions;
using System.Collections.ObjectModel;
using System;
using NSelene.Support.SeleneElementJsExtensions;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace NSelene
{
    public interface WrapsWebElement
    {
        IWebElement ActualWebElement { get; }
    }

    // TODO: consider extracting SElement as interface... 
    public sealed class SeleneElement 
    : WrapsWebElement, IWebElement, ISearchContext, SeleneContext
    {
        readonly SeleneLocator<IWebElement> locator;

        public readonly _SeleneSettings_ Config; // TODO: remove this
        // private readonly _SeleneSettings_ config;

        [Obsolete("SeleneElement#config is obsolete, use SeleneElement.Config instead")]
        public _SeleneSettings_ config
        {
            get
            {
                return this.Config;
            }
        }
        
        internal SeleneElement(
            SeleneLocator<IWebElement> locator, 
            _SeleneSettings_ config
        ) 
        {
            this.locator = locator;
            this.Config = config;
        }        
        
        internal SeleneElement(
            By locator, 
            _SeleneSettings_ config
        ) 
        : this (
            new SearchContextWebElementSLocator(
                locator, 
                config
            ),
            config
        ) {}

        internal SeleneElement(IWebElement elementToWrap, _SeleneSettings_ config)
        : this(new WrappedWebElementSLocator(elementToWrap), config) {}

        public SeleneElement With(
            IWebDriver driver = null,
            double? timeout = null,
            double? pollDuringWaits = null,
            bool? setValueByJs = null,
            bool? typeByJs = null,
            bool? clickByJs = null,
            bool? waitForNoOverlapFoundByJs = null,
            bool? logOuterHtmlOnFailure = null,
            Action<object, Func<string>, Action> _hookWaitAction = null
        )
        {
            _SeleneSettings_ customized = new Configuration();

            customized.Driver = driver;
            customized.Timeout = timeout;
            customized.PollDuringWaits = pollDuringWaits;
            customized.SetValueByJs = setValueByJs;
            customized.TypeByJs = typeByJs;
            customized.ClickByJs = clickByJs;
            customized.WaitForNoOverlapFoundByJs = waitForNoOverlapFoundByJs;
            customized.LogOuterHtmlOnFailure = logOuterHtmlOnFailure;
            customized._HookWaitAction = _hookWaitAction;

            return new SeleneElement(
                this.locator, 
                this.Config.With(customized)
            );
        }

        public SeleneElement _With_(_SeleneSettings_ config)
        {
            return new SeleneElement(
                this.locator, 
                config
            );
        }

        // TODO: consider making it Obsolete, actions is an object with broader context than Element
        Actions Actions => new Actions(this.Config.Driver);

        internal Wait<SeleneElement> Wait // TODO: Consider making it public
        {
            get
            {
                var paramsAndTheirUsagePattern = new Regex(@"\(?(\w+)\)?\s*=>\s*?\1\.");
                return new Wait<SeleneElement>(
                    entity: this,
                    timeout: this.Config.Timeout ?? Configuration.Timeout,
                    polling: this.Config.PollDuringWaits ?? Configuration.PollDuringWaits,
                    _describeComputation: it => paramsAndTheirUsagePattern.Replace(
                        it, 
                        ""
                    ),
                    _hookAction: this.Config._HookWaitAction
                );
            }
        }

        // TODO: consider renaming it to something more concise and handy in use...
        //       take into account that maybe it's good to just add an alias
        //       because in failures it looks pretty good now:
        //       > Timed out after 0.25s, while waiting for:
        //       > Browser.Element(a).ActualWebElement.Click()
        //       
        //       some alias candidates:
        //       * Browser.Element(...).Get()
        //         - kind of tells that we get Element, not raw WebElement
        //         + one of the concisest
        //       * Browser.Element(...).Find()
        //         - not consistent with Find(selector), 
        //         + but tells that we actually finding something
        //       * Browser.Element(...).Locate()
        //         + like above but does not interfere with other names
        //         + consistent with Element.locator
        //         - not the concisest
        //       * Browser.Element(...).Raw
        //         + !!! in fact it's "raw" in its nature, and the most concise
        //         - maybe a bit "too technical", but for tech-guys probably pretty obvious
        //           yeah, Selene is for users not for coders, 
        //           + but actual raw webelement is also not for users;)
        //       - Browser.Element(...).Invoke()
        //       - Browser.Element(...).Call()
        public IWebElement ActualWebElement {
            get {
                return locator.Find();
            }
        }

        private IWebElement ActualVisibleWebElement {
            get {
                var webElement = locator.Find();
                if (!webElement.Displayed)
                {
                    throw new SeleneException(
                        () 
                        => 
                        "Element not visible" // TODO: should we render here also the current element locator? (will be redundant for S but might help in S.S, SS.S.S)
                        + (
                            (this.Config.LogOuterHtmlOnFailure ?? false)
                            ? $":{Environment.NewLine}{webElement.GetAttribute("outerHTML")}"
                            : ""
                        )
                    );
                }
                return webElement;
            }
        }

        /// 
        /// Returns:
        ///     actual not overlapped (and so visible too) webelement
        ///     or...
        /// 
        /// Throws:
        ///     WebDriverException if overlapped
        ///     Or whatever ActualVisibleWebElementAndMaybeItsCover throws
        ///
        private IWebElement ActualNotOverlappedWebElement {
            get {
                var (webElement, cover) = this.ActualVisibleWebElementAndMaybeItsCover();
                if (cover != null)
                {
                    throw new SeleneException(
                        ()
                        =>
                        $"Element" 
                        + (
                            // TODO: do we actually need to use it here? ...
                            (this.Config.LogOuterHtmlOnFailure ?? false)
                            ? $": {webElement.GetAttribute("outerHTML")}"
                            : ""
                        )   // TODO: ... while not applied here?
                        + $"{Environment.NewLine}    is overlapped by: {cover.GetAttribute("outerHTML")}"
                    );
                }
                return webElement;
            }
        }

        /// 
        /// Summary:
        ///     Checks wether visible element is covered/overlapped 
        ///     by another element at point from element's center with...
        ///
        /// Parameters:
        ///     centerXOffset
        ///     centerYOffset
        ///
        /// Returns:
        ///     Tuple<IWebElement> with:
        ///         [webelement, null] if not overlapped else [webelement, coveredWebElement]
        /// Throws: 
        ///     if element is not visible: 
        ///         javascript error: element is not visible
        private (IWebElement, IWebElement) ActualVisibleWebElementAndMaybeItsCover(
            int centerXOffset = 0, 
            int centerYOffset = 0
        )
        {
            // TODO: will it work if element is not in view but is not covered?
            // check in https://developer.mozilla.org/en-US/docs/Web/API/Document/elementFromPoint:
            // > If the specified point is outside the visible bounds of the document 
            // > or either coordinate is negative, the result is null.
            // TODO: cover it by tests (also the iframe case (check the docs by above link))
            var results = this.ExecuteScript(
                @"
                var centerXOffset = args[0];
                var centerYOffset = args[1];

                var isVisible = !!( 
                    element.offsetWidth 
                    || element.offsetHeight 
                    || element.getClientRects().length 
                ) && window.getComputedStyle(element).visibility !== 'hidden'

                if (!isVisible) {
                    throw 'element is not visible'
                }

                var rect = element.getBoundingClientRect();
                var x = rect.left + rect.width/2 + centerXOffset;
                var y = rect.top + rect.height/2 + centerYOffset;

                // TODO: now we return [element, null] in case of elementFromPoint returns null
                //       (kind of – if we don't know what to do, let's at least not block the execution...)
                //       rethink this... and handle the iframe case
                //       read more in https://developer.mozilla.org/en-US/docs/Web/API/Document/elementFromPoint

                var elementByXnY = document.elementFromPoint(x,y);
                if (elementByXnY == null) {
                    return [element, null];
                }

                var isNotOverlapped = element.isSameNode(elementByXnY);

                return isNotOverlapped 
                       ? [element, null]
                       : [element, elementByXnY];
                "
                , centerXOffset
                , centerYOffset
            );
            if (results.GetType() == typeof(ReadOnlyCollection<IWebElement>))
            {
                var webelements = (ReadOnlyCollection<IWebElement>) results;
                return (webelements[0], webelements[1]);
            }
            var objects = (ReadOnlyCollection<object>) results;
            return ((IWebElement) objects[0], objects[1] as IWebElement);
        }

        public override string ToString()
        {
            return this.locator.Description;
        }

        public SeleneElement Should(Condition<SeleneElement> condition)
        {
            var wait = this.Wait.With(
                _describeComputation: (name => $"Should({name})")
            );
            wait.For(condition);
            return this;
        }

        [Obsolete("Use the negative condition instead")]
        public SeleneElement ShouldNot(Condition<SeleneElement> condition)
        => this.Should(condition.Not);

        public bool Matching(Condition<SeleneElement> condition)
        => condition._Predicate(this);

        public bool WaitUntil(Condition<SeleneElement> condition)
        {
            try 
            {
                this.Should(condition);
            }
            catch
            {
                return false;
            }
            return true;
        }

        //
        // SeleneElement element builders
        // 

        public SeleneElement Element(By locator)
        {
            return new SeleneElement(
                new SearchContextWebElementSLocator(locator, this), 
                this.Config
            );
        }

        public SeleneElement Find(By locator)
        {
            return this.Element(locator);
        }

        public SeleneElement Element(string cssOrXPathSelector)
        {
            return this.Element(Utils.ToBy(cssOrXPathSelector));
        }

        public SeleneElement Find(string cssOrXPathSelector)
        {
            return this.Element(cssOrXPathSelector);
        }

        public SeleneCollection All(By locator)
        {
            return new SeleneCollection(
                new SearchContextWebElementsCollectionSLocator(locator, this), 
                this.Config
            );
        }

        public SeleneCollection FindAll(By locator)
        {
            return this.All(locator);
        }

        public SeleneCollection All(string cssOrXPathSelector)
        {
            return this.All(Utils.ToBy(cssOrXPathSelector));
        }

        public SeleneCollection FindAll(string cssOrXPathSelector)
        {
            return this.All(cssOrXPathSelector);
        }

        //
        // SeleneElement element commands
        // 

        public SeleneElement PressEnter()
        {
            // TODO: can find better synonym instead of Lambda to make it more obvious and self-explanatory when reading?
            //       like Operation? DescribedComputation? NamedComputation? NamedLambda?
            //       actually maybe lambda already pretty common for everybody term...
            //       but should we add Named prefix to it?
            if (this.Config.WaitForNoOverlapFoundByJs ?? false)
            {
                this.Wait.For(new _Lambda<SeleneElement, object>(
                    $"ActualNotOverlappedWebElement.SendKeys(Enter)", // TODO: should we render it as PressEnter()?
                    self => self.ActualNotOverlappedWebElement.SendKeys(Keys.Enter)
                ));
            }
            else
            {
                this.Wait.For(new _Lambda<SeleneElement, object>(
                    $"ActualWebElement.SendKeys(Enter)", // TODO: should we render it as PressEnter()?
                    self => self.ActualWebElement.SendKeys(Keys.Enter)
                ));
            }
            return this;
        }

        public SeleneElement PressTab()
        {
            if (this.Config.WaitForNoOverlapFoundByJs ?? false)
            {
                this.Wait.For(new _Lambda<SeleneElement, object>(
                    $"ActualNotOverlappedWebElement.SendKeys(Tab)", // TODO: should we render it as PressEnter()?
                    self => self.ActualNotOverlappedWebElement.SendKeys(Keys.Tab)
                ));
            }
            else
            {
                this.Wait.For(new _Lambda<SeleneElement, object>(
                    $"ActualWebElement.SendKeys(Tab)", // TODO: should we render it as PressEnter()?
                    self => self.ActualWebElement.SendKeys(Keys.Tab)
                ));
            }
            return this;
        }

        public SeleneElement PressEscape()
        {
            // TODO: do we need PressEscapeByJs ? o_O do we need so much of this ByJs?
            //       do we need something more general like ActByJs?
            //       yeah, almost useless when set globally on Configuration.ActByJs
            //       but pretty usefull when called like element.With(actByJs: true) ! 
            //       can this be implemented easy without interfering with all other like TypeByJs? o_O
            //       but should they be different in context of waiting?
            //       i mean... TypeByJs - should mean that we want to type with hack...
            //                 i.e. allowing everything that js allow... so sometimes for overlapped elements too
            //                 ActByJs - and this is something like... act ~ simulate by js... 
            //                 i.e. we should simulate the real behaviour in context of waiting, but do the action via js...
            //                 no?
            //                 is it too much? :D
            if (this.Config.WaitForNoOverlapFoundByJs ?? false)
            {
                this.Wait.For(new _Lambda<SeleneElement, object>(
                    $"ActualNotOverlappedWebElement.SendKeys(Escape)", // TODO: should we render it as PressEnter()?
                    self => self.ActualNotOverlappedWebElement.SendKeys(Keys.Escape)
                ));
            }
            else
            {
                this.Wait.For(new _Lambda<SeleneElement, object>(
                    $"ActualWebElement.SendKeys(Escape)", // TODO: should we render it as PressEnter()?
                    self => self.ActualWebElement.SendKeys(Keys.Escape)
                ));
            }
            return this;
        }

        // TODO: Do we need an alias to Type(keys) – Press(keys) ?
        //       kind of for better logging in report (when we have full support for that)

        public SeleneElement SetValue(string keys) // TODO: why the param is named keys o_O :) Do we have time to rename it? )
        {
            if (this.Config.SetValueByJs ?? Configuration.SetValueByJs) 
            {
                // TODO: should we check here for NotOverlappedWebElement too?
                //       i.e. should we consider a setValueByJs 
                //       just as a faster clear + set alternative to SendKeys
                //       or additional a kind of hacky workaround to set value ...       
                //       probably just a first...
                this.JsSetValue(keys);
            }
            else
            {
                if (this.Config.WaitForNoOverlapFoundByJs ?? false)
                {
                    this.Wait.For(new _Lambda<SeleneElement, object>(
                        $"ActualNotOverlappedWebElement.Clear().SendKeys({keys})", // TODO: should we render it as SetValue({keys})?
                        self =>
                        {
                            var webelement = self.ActualNotOverlappedWebElement;
                            webelement.Clear();
                            webelement.SendKeys(keys);
                        }
                    ));
                }
                else
                {
                    this.Wait.For(new _Lambda<SeleneElement, object>(
                        $"ActualWebElement.Clear().SendKeys({keys})", // TODO: should we render it as SetValue({keys})?
                        self =>
                        {
                            var webelement = self.ActualWebElement;
                            webelement.Clear();
                            webelement.SendKeys(keys);
                        }
                    ));
                }
            }
            return this;
        }

        // TODO: consider moving to Extensions or even deprecate
        public SeleneElement Set(string value)
        {
            return SetValue(value);
        }

        public SeleneElement Hover()
        {
            if (this.Config.WaitForNoOverlapFoundByJs ?? false)
            {
                this.Wait.For(
                    self
                    =>
                    self.Actions.MoveToElement(self.ActualNotOverlappedWebElement).Perform()
                );
            }
            else
            {
                this.Wait.For(
                    self
                    =>
                    self.Actions.MoveToElement(self.ActualWebElement).Perform()
                );
            }
            return this;
        }

        public SeleneElement DoubleClick()
        {
            if (this.Config.WaitForNoOverlapFoundByJs ?? false)
            {
                this.Wait.For(
                    self
                    =>
                    self.Actions.DoubleClick(self.ActualNotOverlappedWebElement).Perform()
                );
            }
            else
            {
                this.Wait.For(
                    self
                    =>
                    self.Actions.DoubleClick(self.ActualWebElement).Perform()
                );
            }
            return this;
        }

        //
        // SeleneElement Commands
        // (chainable alternatives to IWebElement void methods)
        //

        public SeleneElement Clear()
        {
            // TODO: consider something like AllowActionOnHidden
            //       is it enough? should we separate AllowActionOnOverlapped and AllowActionOnHidden?
            //       (overlapped is is also kind of "hidden" in context of normal meaning...)
            //       why we would want to allow clear on hidden? for example to clear upload file hidden input;)
            //       (TODO: by the way clear is not allowed on input[type=file] while sendKeys is, why?)
            /*
            if (this.config.AllowActionOnHidden ?? Configuration.AllowActionOnHidden) 
            {
                // this.Wait.For(self => self.ActualWebElement.Clear()); // this will yet fail with ElementNotInteractableException
                // so to really allow, we should do here something like:
                this.Wait.For(self => self.JsClear());
                // should we?
                // - use AllowActionOnHidden for this, or use ClearByJs? (consistent with other...)

            } else 
            {
                this.Wait.For(self => self.ActualNotOverlappedWebElement.Clear());
            }
             */

            if (this.Config.WaitForNoOverlapFoundByJs ?? false)
            {
                this.Wait.For(self => self.ActualNotOverlappedWebElement.Clear());
            }
            else
            {
                this.Wait.For(self => self.ActualWebElement.Clear());
            }
            return this;
        }

        public SeleneElement Type(string keys)
        {
            if (this.Config.TypeByJs ?? Configuration.TypeByJs) 
            {
                this.JsType(keys);
            } else
            {
                if (this.Config.WaitForNoOverlapFoundByJs ?? false)
                {
                    this.Wait.For(new _Lambda<SeleneElement, object>(
                        $"ActualNotOverlappedWebElement.SendKeys({keys})", // TODO: should we render it as Type({keys})?
                        self => self.ActualNotOverlappedWebElement.SendKeys(keys)
                    ));
                }
                else
                {
                    this.Wait.For(new _Lambda<SeleneElement, object>(
                        $"ActualNotOverlappedWebElement.SendKeys({keys})", // TODO: should we render it as Type({keys})?
                        self => self.ActualWebElement.SendKeys(keys)
                    ));
                }
            }
            return this;
        }

        /// 
        /// Summary:
        ///     A low level method similar to raw selenium webdriver one, 
        ///     with similar behaviour in context of "hidden" elements.
        ///     Waits till "Be.InDom" for input fields with type="file".
        ///     Hence is useful to send keys to hidden elements, like "upload file input"
        ///     Also it is chainable, like other SeleneElement's methods.
        ///
        public SeleneElement SendKeys(string keys)
        {
            // TODO: should we deprecate it? and keep just something like:
            //           element.With(allowActionOnHidden: true).Type(keys)
            //       ?
            //       should we consider adding Upload(string file)?
            //       * to cover the corresponding case...
            //       * yet be valid only for web... not for mobile :(

            // TODO: consider failing fast (skip waiting) if got something like "WebDriverException : invalid argument: File not found"
            //       how would we implement it by the way? :D
            this.Wait.For(new _Lambda<SeleneElement, object>(
                $"ActualWebElement.SendKeys({keys})",
                self => self.ActualWebElement.SendKeys(keys)
            ));
            return this;
        }

        public SeleneElement Submit()
        {
            if (this.Config.WaitForNoOverlapFoundByJs ?? false)
            {
                this.Wait.For(self => self.ActualNotOverlappedWebElement.Submit());
            }
            else
            {
                // this will make to pass submit on hidden element
                // TODO: maybe this is ok, because Submit is kind of low level method...
                //       but should it then be a part of high level SeleneElement API?
                this.Wait.For(self => self.ActualWebElement.Submit());
            }
            return this;
        }

        public SeleneElement Click()
        {
            if (this.Config.ClickByJs ?? Configuration.ClickByJs)
            {
                // TODO: should we incorporate wait into this.ExecuteScript ? maybe make it configurable (Configuration.WaitForExecuteScript, false by default)?
                // TODO: to keep here just this.JsClick(); ?
                this.JsClick(0, 0);
            }
            else 
            {
                this.Wait.For(self => self.ActualWebElement.Click());
            }
            return this;
        }

        //
        // Queries
        //

        public string Value => GetAttribute("value");

        //
        // IWebElement Properties
        //

        public bool Enabled
        {
            get {
                Should(Be.Visible);
                return this.ActualWebElement.Enabled;
            }
        }

        public Point Location
        {
            get {
                Should(Be.Visible);
                return this.ActualWebElement.Location;
            }
        }

        public bool Selected
        {
            get {
                Should(Be.Visible);
                return this.ActualWebElement.Selected;
            }
        }

        public Size Size
        {
            get {
                Should(Be.Visible);
                return this.ActualWebElement.Size;
            }
        }

        public string TagName
        {
            get {
                Should(Be.Visible);
                return this.ActualWebElement.TagName;
            }
        }

        public string Text
        {
            get {
                Should(Be.Visible);
                return this.ActualWebElement.Text;
            }
        }

        public bool Displayed
        {
            get {
                Should(Be.InDom);  // todo: probably we should not care in dom it or not...
                return this.ActualWebElement.Displayed;
            }
        }

        //
        // IWebElement Methods
        // 
        // TODO: do we really need these explicit interface implemantations? why?
        //       we might need only GetDomAttribute and GetDomProperty
        //       because we want to hide them from SeleneElement public API...
        //       so more lacontic versions (GetAttribute, GetProperty) can be used
        //

        void IWebElement.Clear()
        {
            Clear();
        }

        void IWebElement.SendKeys(string keys)
        {
            SendKeys(keys);
        }

        void IWebElement.Submit()
        {
            Submit();
        }

        void IWebElement.Click()
        {
            Click();
        }

        string IWebElement.GetDomAttribute(string attributeName)
        {
            return this.ActualWebElement.GetDomAttribute(attributeName);
        }

        string IWebElement.GetDomProperty(string propertyName)
        {
            return this.ActualWebElement.GetDomProperty(propertyName);
        }

        public string GetAttribute(string name)
        {
            Should(Be.InDom);
            return this.ActualWebElement.GetAttribute(name);
        }

        public string GetProperty (string propertyName)
        {
            Should(Be.InDom);
            return this.ActualWebElement.GetDomProperty(propertyName);
        }

        public ISearchContext GetShadowRoot ()
        {
            Should(Be.InDom);
            return this.ActualWebElement.GetShadowRoot();
        }

        public string GetCssValue(string property)
        {
            Should(Be.InDom);
            return this.ActualWebElement.GetCssValue(property);
        }

        //
        // ISearchContext methods
        //

        IWebElement ISearchContext.FindElement (By by)
        {
            return new SeleneElement(
                new SearchContextWebElementSLocator(by, this),
                this.Config
            );
        }

        ReadOnlyCollection<IWebElement> ISearchContext.FindElements (By by)
        {
            return new SeleneCollection(
                new SearchContextWebElementsCollectionSLocator(by, this), 
                this.Config
            ).ToReadOnlyWebElementsCollection();
        }

        //
        // SContext methods
        //

        IWebElement SeleneContext.FindElement (By by)
        {
            // TODO: calling here ActualVisibleWebElement will result in extra action, how can we improve performance here?
            return this.ActualVisibleWebElement.FindElement(by);
        }

        ReadOnlyCollection<IWebElement> SeleneContext.FindElements (By by)
        {
            // Should(Be.Visible); // not needed here cause we can find inside not visible elements... but TODO: will everytying be ok with rendering in error?
            return this.ActualWebElement.FindElements(by);
        }

        /// <remarks>
        ///     This method executes JavaScript in the context of the currently selected frame or window.
        ///     This means that "document" will refer to the current document and "element" will refer to this element
        /// </remarks>
        public object ExecuteScript(string scriptOnElementAndArgs, params object[] args)
        {
            // TODO: this method fails if this.ActualWebElement failed – this is pretty not in NSelene style!
            //       probably we have to  wrap it inside wait!
            IJavaScriptExecutor js = (IJavaScriptExecutor)this.Config.Driver;
            return js.ExecuteScript(
                $@"
                return (function(element, args) {{
                    {scriptOnElementAndArgs}
                }})(arguments[0], arguments[1])
                ", 
                new object[] { this.ActualWebElement, args }
            );
        }
    }
}
