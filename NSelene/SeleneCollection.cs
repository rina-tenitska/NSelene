using OpenQA.Selenium;
using System.Collections.Generic;
using System.Linq;
using NSelene.Conditions;
using System.Collections.ObjectModel;
using System;
using System.Collections;
using System.Text.RegularExpressions;

namespace NSelene
{
    public interface WrapsWebElementsCollection
    {
        ReadOnlyCollection<IWebElement> ActualWebElements { get; }
    }

    public sealed class SeleneCollection 
        :  WrapsWebElementsCollection
        , IReadOnlyList<SeleneElement>
        , IReadOnlyCollection<SeleneElement>
        , IList<SeleneElement>
        , IList<IWebElement>
        , ICollection<SeleneElement>
        , IEnumerable<SeleneElement>
        , IEnumerable
    {
        readonly SeleneLocator<ReadOnlyCollection<IWebElement>> locator;

        public readonly _SeleneSettings_ Config; // TODO: remove
        
        [Obsolete("SeleneCollection#config is obsolete, use SeleneCollection.Config instead")]
        public _SeleneSettings_ config
        {
            get
            {
                return this.Config;
            }
        }
        
        internal SeleneCollection(
            SeleneLocator<ReadOnlyCollection<IWebElement>> locator, 
            _SeleneSettings_ config
        ) 
        {
            this.locator = locator;
            this.Config = config;
        }

        internal SeleneCollection(
            SeleneLocator<ReadOnlyCollection<IWebElement>> locator
        )
        : this(locator, Configuration.Shared) {}        
        
        internal SeleneCollection(
            By locator, 
            _SeleneSettings_ config
        ) 
        : this (
            new SearchContextWebElementsCollectionSLocator(
                locator, 
                config
            ),
            config
        ) {}

        internal SeleneCollection(
            IList<IWebElement> elementsListToWrap, 
            _SeleneSettings_ config
        )
        : this(
            new WrappedWebElementsCollectionSLocator(elementsListToWrap), 
            config
        ) 
        {}

        public SeleneCollection With(
            IWebDriver driver = null,
            double? timeout = null,
            double? pollDuringWaits = null,
            bool? setValueByJs = null,
            bool? typeByJs = null,
            bool? clickByJs = null,
            bool? waitForNoOverlapFoundByJs = null,
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
            customized._HookWaitAction = _hookWaitAction;

            /* same but another style and not so obvious with harder override logic: 
            // mentioned here just for an example, to think about later on API improvements

            _SeleneSettings_ customized = Configuration._With_(
                driver: driver ?? this.config.Driver,
                timeout: timeout ?? this.config.Timeout,
                pollDuringWaits: pollDuringWaits ?? this.config.PollDuringWaits,
                setValueByJs: setValueByJs ?? this.config.SetValueByJs
            );
            */

            return new SeleneCollection(
                this.locator, 
                this.Config.With(customized)
            );
        }

        public SeleneCollection _With_(_SeleneSettings_ config)
        {
            return new SeleneCollection(
                this.locator, 
                config
            );
        }
        
        public ReadOnlyCollection<IWebElement> ActualWebElements
        {
            get {
                return locator.Find();
            }
        }
        Wait<SeleneCollection> Wait
        {
            get
            {
                var paramsAndTheirUsagePattern = new Regex(@"\(?(\w+)\)?\s*=>\s*?\1\.");
                return new Wait<SeleneCollection>(
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

        // TODO: consider depracating
        SeleneLocator<ReadOnlyCollection<IWebElement>> SLocator 
        {
            get {
                return this.locator;
            }
        }

        public override string ToString()
        {
            return this.locator.Description;
        }

        public SeleneCollection Should(Condition<SeleneCollection> condition)
        {
            var wait = this.Wait.With(
                _describeComputation: (name => $"Should({name})")
            );
            wait.For(condition);
            return this;
        }

        [Obsolete("Use the negative condition instead, e.g. Should(Have.No.Count(0))")]
        public SeleneCollection ShouldNot(Condition<SeleneCollection> condition)
        {
            return this.Should(condition.Not);
        }

        public bool Matching(Condition<SeleneCollection> condition)
        => condition._Predicate(this);

        public bool WaitUntil(Condition<SeleneCollection> condition)
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

        public SeleneElement ElementBy(Condition<SeleneElement> condition)
        {
            return new SeleneElement(
                new SCollectionWebElementByConditionSLocator(condition, this, this.Config), 
                this.Config
            );
        }

        public SeleneElement FindBy(Condition<SeleneElement> condition)
        {
            return this.ElementBy(condition);
        }

        public SeleneCollection By(Condition<SeleneElement> condition)
        {
            return new SeleneCollection(
                new SCollectionFilteredWebElementsCollectionSLocator(
                    condition, this, this.Config
                ), 
                this.Config
            );
        }

        public SeleneCollection FilterBy(Condition<SeleneElement> condition)
        {
            return this.By(condition);
        }

        public ReadOnlyCollection<IWebElement> ToReadOnlyWebElementsCollection()
        {
            return new ReadOnlyCollection<IWebElement>(this);
        }

        //
        // IReadOnlyList
        //

        public SeleneElement this [int index] {
            get {
                return new SeleneElement(
                    new SCollectionWebElementByIndexSLocator(index, this), 
                    this.Config
                );
            }
        }

        //
        // IReadOnlyCollection
        //

        public int Count
        {
            get {
                return this.ActualWebElements.Count;
            }
        }

        //
        // IEnumerator
        //

        //TODO: is it stable enought in context of "ajax friendly"?
        public IEnumerator<SeleneElement> GetEnumerator ()
        {
            //TODO: is it lazy? seems like not... because of ToList() conversion? should it be lazy?
            return new ReadOnlyCollection<SeleneElement>(
                this.ActualWebElements.Select(
                    webelement 
                    => 
                    new SeleneElement(webelement, this.Config)).ToList()
                ).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator ()
        {
            return GetEnumerator();
        }

        //
        // IList<IWebElement> methods
        //

        int ICollection<IWebElement>.Count {
            get {
                return this.Count;
            }
        }

        bool ICollection<IWebElement>.IsReadOnly {
            get {
                return true;
            }
        }

        IWebElement IList<IWebElement>.this [int index] {
            get {
                return this[index];
            }

            set {
                throw new NotImplementedException ();
            }
        }

        int IList<IWebElement>.IndexOf (IWebElement item)
        {
            throw new NotImplementedException ();
        }

        void IList<IWebElement>.Insert (int index, IWebElement item)
        {
            throw new NotImplementedException ();
        }

        void IList<IWebElement>.RemoveAt (int index)
        {
            throw new NotImplementedException ();
        }

        void ICollection<IWebElement>.Add (IWebElement item)
        {
            throw new NotImplementedException ();
        }

        void ICollection<IWebElement>.Clear ()
        {
            throw new NotImplementedException ();
        }

        bool ICollection<IWebElement>.Contains (IWebElement item)
        {
            throw new NotImplementedException ();
        }

        void ICollection<IWebElement>.CopyTo (IWebElement [] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException("arrayIndex");
            if (array.Rank > 1)
                throw new ArgumentException("array is multidimensional.");
            if (array.Length - arrayIndex < Count)
                throw new ArgumentException("Not enough elements after index in the destination array.");

            for (int i = 0; i < Count; ++i)
                array.SetValue(this[i], i + arrayIndex);
        }

        bool ICollection<IWebElement>.Remove (IWebElement item)
        {
            throw new NotImplementedException ();
        }

        IEnumerator<IWebElement> IEnumerable<IWebElement>.GetEnumerator ()
        {
            return this.GetEnumerator();
        }

        //
        // IList<SElement> methods
        //

        bool ICollection<SeleneElement>.IsReadOnly {
            get {
                return true;
            }
        }

        SeleneElement IList<SeleneElement>.this [int index] {
            get {
                return this[index];
            }

            set {
                throw new NotImplementedException ();
            }
        }

        int IList<SeleneElement>.IndexOf (SeleneElement item)
        {
            throw new NotImplementedException ();
        }

        void IList<SeleneElement>.Insert (int index, SeleneElement item)
        {
            throw new NotImplementedException ();
        }

        void IList<SeleneElement>.RemoveAt (int index)
        {
            throw new NotImplementedException ();
        }

        void ICollection<SeleneElement>.Add (SeleneElement item)
        {
            throw new NotImplementedException ();
        }

        void ICollection<SeleneElement>.Clear ()
        {
            throw new NotImplementedException ();
        }

        bool ICollection<SeleneElement>.Contains (SeleneElement item)
        {
            throw new NotImplementedException ();
        }

        void ICollection<SeleneElement>.CopyTo (SeleneElement [] array, int arrayIndex)
        {
            throw new NotImplementedException ();
        }

        bool ICollection<SeleneElement>.Remove (SeleneElement item)
        {
            throw new NotImplementedException ();
        }
    }



    namespace Support.Extensions 
    {
        public static class SeleneCollectionExtensions 
        {
            public static SeleneCollection AssertTo(this SeleneCollection selements, Condition<SeleneCollection> condition)
            {
                return selements.Should(condition);
            }

            [Obsolete("Use the negative condition instead, e.g. AssertTo(Have.No.Count(0))")]
            public static SeleneCollection AssertToNot(this SeleneCollection selements, Condition<SeleneCollection> condition)
            {
                return selements.ShouldNot(condition);
            }
        }
    }
}
