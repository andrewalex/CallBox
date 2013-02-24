using System;

namespace CallBox
{
    /// <summary>
    /// This tells a CallBox not to route calls to the specified method
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Event, Inherited = false, AllowMultiple = false)]
    public sealed class CallBoxBindAttribute : Attribute    { }
}
