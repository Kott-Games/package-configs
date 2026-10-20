using System;

namespace Kott.Configs
{
    /// <summary>Invalid config data or a failed config load. The message names the template, type or loader involved.</summary>
    public class ConfigException : Exception
    {
        /// <summary>Creates the exception with a message.</summary>
        public ConfigException(string message) : base(message)
        { }

        /// <summary>Creates the exception with a message and the exception that caused it.</summary>
        public ConfigException(string message, Exception innerException) : base(message, innerException)
        { }
    }
}
