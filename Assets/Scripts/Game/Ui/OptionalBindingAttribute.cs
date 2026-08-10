using System;

namespace GatekeeperBastion.Game.Ui
{
    /// <summary>Marks a serialized UnityEngine.Object field as legitimately allowed to be null
    /// in an authored scene (e.g. a view's unused widget slot). BindingValidator (M35 criterion
    /// 40) skips these; every other null serialized Object field is reported as a missed wire.
    /// Lives in the runtime assembly so views can annotate their own fields.</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class OptionalBindingAttribute : Attribute
    {
    }
}
