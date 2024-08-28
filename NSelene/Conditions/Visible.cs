namespace NSelene
{
    namespace Conditions
    {
        public class Visible : Condition<SeleneElement>
        {
            public override void Invoke(SeleneElement entity)
            {
                var webelement = entity.ActualWebElement;
                if (!webelement.Displayed)
                {
                    throw new ConditionNotMatchedException(() =>
                        "Found element is not visible" + (
                            (entity.Config.LogOuterHtmlOnFailure ?? false)
                            ? $": {webelement.GetAttribute("outerHTML")}"
                            : ""
                        )
                    );
                }
            }

            // TODO: consider rendeing this Should part in some other place...
            // TODO: consider renaming class to BeVisible ;)
            public override string ToString() => "Be.Visible";
        }

    }

    public static partial class Be
    {
        public static Conditions.Condition<SeleneElement> Visible
            => new Conditions.Visible();

        static partial class Not
        {
            public static Conditions.Condition<SeleneElement> Visible
                => Be.Visible.Not;
        }
    }

}
