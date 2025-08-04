namespace Nexo.Core.Application.Enums
{
    /// <summary>
    /// Represents a placeholder type intended for integer values.
    /// </summary>
    public enum PlaceholderType
    {
        /// <summary>
        /// Represents a placeholder type intended for string values.
        /// </summary>
        String = 0,

        /// <summary>
        /// Represents a placeholder type intended for integer values.
        /// </summary>
        Integer = 1,

        /// <summary>
        /// Represents a placeholder type that holds a Boolean value (true or false).
        /// </summary>
        Boolean = 2,

        /// <summary>
        /// Represents a placeholder type that holds date and time values.
        /// </summary>
        DateTime = 3,

        /// <summary>
        /// Represents a placeholder type intended for list values.
        /// </summary>
        List = 4,

        /// <summary>
        /// Represents a placeholder type intended for object values.
        /// </summary>
        Object = 5,

        /// <summary>
        /// Represents a placeholder type intended for enumerated values.
        /// </summary>
        Enum = 6,

        /// <summary>
        /// Represents a custom type of placeholder that can be defined and extended by the user beyond the predefined types.
        /// </summary>
        Custom = 7
    }
}