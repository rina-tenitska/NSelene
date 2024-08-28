# Changelog

## NEXT

- should we unmark ShouldNot as deprecated?
- do we need SeleneDriver anymore? (if we go the direction of SeleneBrowser)

## 1.0.0-alpha1x (to be released on 2021.05.??)

- deprecate the majority of `Selene.*` (except S, SS) when providing alternative API via `Browser.*`

## 1.0.0-alpha14 (to be released on 2024.08.28)

- add Configuration.LogOuterHtmlOnFailure (false by default)
  - to disable previously mandatory logging of outer html of elements in error messages
    that was the reason of failures when working with Appium

## 1.0.0-alpha13 (released on 2024.02.13)

- fix element.GetAttribute to call webelement.GetAttribute instead of webelement.GetDomAttribute
  - correspondingly the behavior of element.Value is also fixed, becaused is based on element.GetAttribute

## 1.0.0-alpha12 (released on 2024.02.09)

- upgraded Selenium.WebDriver to 4.* (i.e. to 4.17.0 as of 2024.02.09)
  - WDM is not used anymore in NSelene examples and removed from README
    since Selenium Manager is in charge of managing drivers now
- added Configuration.BaseUrl, thanks to [@davespoon](https://github.com/davespoon)
- added Have.AttributeWithValue* conditions, thanks to [@davespoon](https://github.com/davespoon)

## 1.0.0-alpha11 (released on 2023.05.16)

- upgraded Selenium.WebDriver from from 4.2.0 to 4.5.1
- `collection.ElementBy(condition)` alias to `collection.FindBy(condition)`
  - `FindBy` will be probably deprecated in future
- `collection.By(condition)` alias to `collection.FilterBy(condition)`
  - `FilterBy` will be probably deprecated in future
- `element.Element(locator | selector)` alias to `element.Find(locator | selector)`
  - `Find` will be probably deprecated in future
- `element.All(locator | selector)` alias to `element.FindAll(locator | selector)`
  - `FindAll` will be probably deprecated in future
- `Should(driverCondition)` alias to `WaitTo(driverCondition)`
- conditions: Have.Url, .UrlContaining, Title, TitleContaining
- collection.ElementByIts(locator | selector, condition)
  - so you can implement something like this:
    ```csharp
    SS(".table-row").ElementByIts(".table-cell[role=name]", Have.Text("John")).Element("[role=remove-user]").Click()
    ```

## 1.0.0-alpha10 (released on 2022.05.27)

- upgraded Selenium.WebDriver from 3.141.0 to 4.2.0
  - kept old-fashioned names for SeleneElement.GetAttribute & .GetProperty
    but now under the hood they use new GetDomAttribute & GetDomProperty correspondingly
  - added SeleneElement.GetShadowRoot as wrapper over WebElement.GetShadowRoot

## 1.0.0-alpha09 (to be released on 2021.05.19)

- improved error messages for cases of inner element search
  - like error on S(".parent").Find(".child").Click() when .parent is absent or not visible
- FIXED experimental Configuration._HookWaitAction application to Should methods on SeleneElement and SeleneCollection
  (was not working, just being skipped)

## 1.0.0-alpha08 (released on 2021.05.18)

- added waiting to SeleneElementJsExtensions:
  - JsClick
  - JsType
  - JsSetValue

## 1.0.0-alpha07 (released on 2021.05.13)

- improved error messsages
  - now condition in Should method will be rendered like:
    `... .Should(Be.Visible)` over just `... .Visible`
- deprecated SeleneElement#config, use SeleneElement#Config instead (same for SeleneCollection)
  - yet be attentive... the fate of keeping SeleneElement#Config as public is also vague...
- added experimental feature: Configuration._HookWaitAction
  - prefixed with underscore implies that this feature is kind of "publically available private property that might be changed/renamed/removed/etc in future;)", so use it on your own risk!!!
  - by default equals to null, making internally call waiting algorithm for actions as it is, without additional customization
  - specified to something like:
    ```cs
    Configuration._HookWaitAction = (entityObject, describeComputation, wait) => {
        Console.WriteLine($"STARTED WAITING FOR: {entityObject}.{describeComputation()}");
        try
        {
            wait();
            Console.WriteLine($"FINISHED WAITING FOR: {entityObject}.{describeComputation()}");
        }
        catch (Exception error)
        {
            Console.WriteLine($"FAILED WAITING FOR: {entityObject}.{describeComputation()}");
            throw error;
        }
    };
    ```
    - should provide some additinal logging for all Selene actions, called on entities (like SeleneElement, SeleneCollection)
    - under actions we mean "void commands" not "queries returning result". 
      - void command like element.Should(condition) will return the element itself instead of void ;)


## 1.0.0-alpha06 (released on 2021.05.12)
- turned off "waiting for no overlay" that was built-in in alpha05 in all actions. ([#85](https://github.com/yashaka/NSelene/issues/85))
  - cause it could break some NSelene + Appium mobile tests that can't use JS, while this waiting was built on top of JS so it's relevant only for web... 
  - yet the Configuration.WaitForNoOverlapFoundByJs was added (false by default)
    - so you can turn it on globally by Configuration.WaitForNoOverlapFoundByJs = true
    - or per element by element.With(waitForNoOverlapFoundByJs: true)
  - in future we might made this waiting enabled by default, when we provide better docs and plugins to work with Appium
- added rendering of elements under overlay into errors ([#84](https://github.com/yashaka/NSelene/issues/84))
  - made rendering elements HTML – lazy (when waiting for no overlap)
    - should improve performance in context of polling during waiting

## 1.0.0-alpha05 (released on 2021.04.28)

### SUMMARY:
  * upgraded waiting of commands, error messages, thread local configuration, etc. (see CHANGELOG for more details)
    * it should be 
      * faster, 
      * more stable/less-flaky (with implicit waiting till no overlay-style pre-loaders)
      * more friendly to parallelisation in context of configuration, 
      * more customizable on elements level (not just global)

### Migrating from 1.0.0-alpha03 guide
* upgrade and check your build
  * refactor your custom conditions:
    * if you have implemented your own custom conditions by extending e.g. `Condition<SeleneElement>`
      * you will get a compilation error – to fix it:
        * change base class from `Condition<SeleneElement>` to `DescribedCondition<SeleneElement>`
        * remove `public override string Explain()` and leave `public override string ToString()` instead
        * if you use anywehere in your code an `Apply` method on condition of type `Condition<TEntity>`
          * you will get an obsolete warning
            * refactor your code to use `Invoke` method, taking into account that
              * anytime Apply throws exception - Invoke also throws exception
              * anytime Apply returns false - Invoke throws exception
              * anytime Apply returns true - Invoke just passes (it's of void type)
  * refactor obsolete things, like:
    * `Configuration.WebDriver` => `Configuration.Driver`
    * `S("#element").ShouldNot(Be.*)` to `S("#element").Should(Be.Not.*)`
    * `S("#element").ShouldNot(yourCustomCondition)` to `S("#element").Should(yourCustomCondition.Not)`
    * etc
* take into account, that some "internal" methods of 1.0.0-alpha05 were made public for easiser experimental testing in prod:), 
  but still considered as internal, that might change in future
  such methods are named with `_` prefix, 
  following kind of Python lang style of "still publically accessible private" methods:)
  use such methods on your own risk, take into account that they might be marked as obsolete in future
  yet, they will be eather renamed or made completely internal till stable 1.0 release;)
  read CHANGELOG for more details.

### Details
- added `Be.Not.*` and `Have.No.*` as entry points to "negated conditions"
- `.ShouldNot` is obsolete now, use `.Should(Be.Not.*)` or `.Should(Have.No.*)` instead
- added `Condition#Not` property, `Condition#Or(condition)`, `Condition#And(condition)`
  - added condition-builder classes, yet marked as internal
    - `Not<TEntity> : Condition<TEntity>`
    - `Or<TEntity> : Condition<TEntity>`
    - `And<TEntity> : Condition<TEntity>`
    - yet they might be renamed in future... to something like NotCondition, OrConditioin, AndCondition
    - let's finalize the naming in [#53](https://github.com/yashaka/NSelene/issues/53)
- added SeleneElement extensions
  - `.JsScrollIntoView()`
  - `.JsClick(centerXOffset=0, centerYOffset=0)`
    - proper tests coverage is yet needed
    - the same can be achieved through (can be handy when storing element in var)
      `element.With(clickByJs: true).Click()`
  - `.JsSetValue(value)`
    - the same can be achieved through 
      `element.With(setValueByJs: true).SetValue(value)`
  - `.JsType(value)`
    - the same can be achieved through 
      `element.With(typeByJs: true).Type(value)`
- made Configuration.* ThreadLocal
- added SeleneElement methods:
  - `WaitUntil(Condition)` – like Should, but returns false on failure
  - `Matching(Condition)` - the predicate, like WaitUntil but without waiting
  - `With([driver], [timeout], [pollDuringWaits], [setValueByJs], [typeByJs], [clickByJs])` - to override corresponding selene setting from Configuration
    - usage: `element.With(timeout: 2.0)`
  - `_With_(_SeleneSettings_)` option to fully disconnect element config from shared Configuration
    - underscores mean that method signature might change...
    - usage: `element._With_(Configuration.New(timeout: 2.0))`
- added SeleneCollection methods:
  - `WaitUntil(Condition)` – like Should, but returns false on failure
  - `Matching(Condition)` - the predicate, like WaitUntil but without waiting
  - `With([driver], [timeout], [pollDuringWaits], [setValueByJs], [typeByJs], [clickByJs])` - to override corresponding selene setting from Configuration
  - `_With_(_SeleneSettings_)` option to fully disconnect element config from shared Configuration
    - underscores mean that method signature might change...
    - usage: `elements._With_(Configuration.New(timeout: 2.0))`
- added SeleneDriver methods:
  - `WaitUntil(Condition)` – like Should, but returns false on failure
  - `Matching(Condition)` - the predicate, like WaitUntil but without waiting
  - `With([driver], [timeout], [pollDuringWaits], [setValueByJs], [typeByJs], [clickByJs])` - to override corresponding selene setting from Configuration
  - `_With_(_SeleneSettings_)` option to fully disconnect element config from shared Configuration
    - underscores mean that method signature might change...
    - usage: `elements._With_(Configuration.New(timeout: 2.0))`
- tuned selene elements representation in error messages
  - now code like `SS(".parent").FilterBy(Be.Visible)[0].SS(".child").FindBy(Have.CssClass("special")).S("./following-sibling::*")`
  - renders to: `Browser.All(.parent).By(Visible)[0].All(.child).FirstBy(has CSS class 'special').Element(./following-sibling::*)`
- improved waiting (waits not just for visibility but till "being passed") at SeleneElement's:
  - (... wait till visible and not overlapped)
    - Click()
    - Hover()
    - DoubleClick()
    - Submit()
    - Clear()
    - SetValue(keys)
    - Type(keys)
    - PressEnter()
    - PressEscape()
    - PressTab()
  - (... wait till visible for all but `input[type=file]`)
    - SendKeys(keys)
- upgraded waiting to new engine in asserts (.Should(condition)) of
  - SeleneElement
  - SeleneCollection
- *Deprecated (Marked as Obsolete)*
  - `Configuration.WebDriver` (use `Configuration.Driver` instead)
    - it also becomes a recommended wa
      - to set driver: `Configuration.Driver = myDriverInstance`
        - over `Selene.SetWebDriver(myDriverInstance)`
          - that might be deprecated in future
      - also take into account that in frameworks like NUnit3, 
        when you tear down driver in OneTimeTearDown (that will be executed after all test methods)
        ensure you do this by calling `Quit` on your own instance like `myDriverInstance.Quit()`
        DON'T do it like `Configuration.Driver.Quit()` or `Selene.GetWebDriver().Quit()`
        cause this will lead in memory leaked driver, this is NUnit thing, not NSelene:)
        (you still can do the latter in TearDown method that will be executed after each test method)
- **potential breaking changes**:
  - Switched to System.TimeoutException in some waits (instead of WebDriverTimeoutException)

## 1.0.0-alpha03 (to be released on 2020.06.03)
- added `SeleneElement#Type(string keys)`, i.e. `S(selector).Type(text)`
  - with wait for visibility built in
- changed in `SeleneElement#SendKeys(string keys)`, i.e. `S(selector).SendKeys(keys)`
  - the wait from Be.Visible to Be.InDom 
  - to enable its usage for file upload
  - but can break some tests, where the "wait for visibility" is needed in context of "typing text"
    - this should be fixed in further versions

## 1.0.0-alpha02 (released on 2020.05.26)
- added `Configuration.SetValueByJs`, `false` by default

## 1.0.0-alpha01 (released on 2020.05.21)

- reformatted project to the SDK-style
- **switched target framework from net45 to netstandard2.0**
  - adding support of net45 is considered to be added soon

- removed all obsolete things deprecated till 0.0.0.7 inclusive

- removed dependency to Selenium.Support 
  - it's not used anymore anywhere in NSelene
- updated Selenium.Webdriver dependency to 3.141.0

- added Have.No.CssClass and Have.No.Attribute

- `S(selector)` and other similar methods now also accepts string with xpath

- removed from API (marked internal) yet unreleased:
  - NSelene.With.*

- kept deprecated:
  - NSelene.Selectors.ByCss
  - NSelene.Selectors.ByLinkText

- other
  - restructured tests a bit
  - removed NSeleneExamples from the solution
    - left a few examples in NSeleneTests 

## 0.0.0.8 (skipped for now)

- updated selenium version to 3.141
- deprecated:
  - Selectors.ByCss
  - Selectors.ByLinkText
- added:
  - With.Type
  - With.Value
  - With.IdContains
  - With.Text
  - With.ExactText
  - With.Id
  - With.Name
  - With.ClassName
  - With.XPath
  - With.Css
  - With.Attribute
  - With.AttributeContains


## 0.0.0.7 (released May 28, 2018)

### Summary
- Upgraded selenium support to >= 3.5.2. 

### New 
- added
  - Selene.WaitTo(Condition<IWebDriver>) method and SeleneDriver#Should correspondingly
  - Conditions.JSReturnedTrue (Have.JSReturnedTrue) 
  - SeleneElement#GetProperty (from IWebElement of 3.5.2 version)

## 0.0.0.6 (released Aug 1, 2016)

### API changes 
Should not break anything in this version (because "old names" was just marked as deprecated and will be removed completely in next version):
- renamed 
  - Config to Configuration
  - Browser to SeleneDriver
  - SElement to SeleneElement
  - SCollection to SeleneCollection
  - Utils to Selene
    to be more "selenide like" (which has com.codeborn.selenide.Selenide class as a container for utility methods)
    and make name more conceptual
  - Selene.SActions() to Selene.Actions and make it property
  - Be.InDOM to Be.InDom (according to standard naming convention)
- closed access to (made private/or internal)
  - SElement#Actions
  - SeleneElement and SeleneCollection constructors (internal) 
    In order to leave ability to rename classes, 
    if one day we extract SeleneElement/SeleneCollection as interfaces. 
    It's ok because we do not create their objects via constructors, but via Selene.S/Selene.SS, etc.
- removed 
  - SElement#SLocator property
  
Breaking changes:
- Left only the following aliasses: 
  - SeleneElement: Find, FindAll, Should, ShouldNot; 
  - SeleneCollection: Should, ShouldNot
  - SeleneDriver: Find, FindAll
  Everything else moved to NSelene.Support.Extensions, so to fix code: you have to add additional "using" statement
  It is recommended though to use these extensions only as "examples", because there are too much of them. The latter may lead to confusion in usage. Usually the user will need only some of them. So better to "copy&paste" needed ones to user's project namespace.
- changed Be.Blank() to Be.Blank (refactored to property);

### New
- enhanced interoperability with raw selenium. Now implicit waits for visibility can be added to all PageFactory webelements just via decorating new SDriver(driver); And all explicit driver calls for finding both IWebElement and IList<IWebElement> will produce NSelene proxy alternatives with both implicit waits for visibility and for indexed webelements of collections.

### Refactoring
- refactored all "static variable" conditions to be "static properties", which should ensure stability for parallel testing

### License
- Changed License to "MIT License"

## 0.0.0.5 (released May 29, 2016)
- added object oriented wrapper over WebDriver - implemented in Browser class
  - which makes it much easier to integrate NSelene to existing selenium based frameworks
  - and supports "creating several drivers per test"

## 0.0.0.4 (released May 12, 2016)  
- enhanced error messages via adding locators of elements and some other useful info when describing errors of failed search by condition in a list

## 0.0.0.3 (released May 11, 2016)  
- Added parallel drivers support and upgraded Selenium to 2.53.0

## 0.0.0.2 (released March 20, 2016)
- Stabilized version with main functionality, not optimised for speed, but yet fast enough and you will hardly notice the difference:)

## 0.0.0.1 (released December 30, 2015)
- Initial "pretty draft" version with basic features ported
- Published under Apache License 2.0
