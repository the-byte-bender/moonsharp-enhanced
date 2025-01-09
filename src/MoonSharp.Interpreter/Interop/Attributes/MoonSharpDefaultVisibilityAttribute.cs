using System;

namespace MoonSharp.Interpreter
{
    /// <summary>
    /// Specifies the default visibility of a class's members for MoonSharp scripts.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class MoonSharpDefaultVisibilityAttribute : Attribute
    {
        /// <summary>
        /// Gets a value indicating whether the class members are visible by default.
        /// </summary>
        public bool IsVisibleByDefault { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MoonSharpDefaultVisibilityAttribute"/> class.
        /// </summary>
        /// <param name="isVisibleByDefault">if set to <c>true</c> the class members are visible by default; otherwise, <c>false</c>.</param>
        public MoonSharpDefaultVisibilityAttribute(bool isVisibleByDefault)
        {
            IsVisibleByDefault = isVisibleByDefault;
        }
    }
}