using System;
using System.Collections.Generic;

namespace SteveTalksCode.StrongTypedHeaders
{
    /// <summary>
    /// Exception thrown when a type mapping is already registered.
    /// </summary>
    public class TypeAlreadyMappedException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TypeAlreadyMappedException"/> class.
        /// </summary>
        /// <param name="registrationType">The type that has failed to be registered.</param>
        /// <param name="existingMappedHeaders">The HTTP request header names already associated with the type mapping.</param>
        /// <param name="requestedMappedHeaders">The HTTP request header names requested to be associated with the type mapping.</param>
        internal TypeAlreadyMappedException(Type registrationType, IEnumerable<string> existingMappedHeaders, IEnumerable<string> requestedMappedHeaders)
        {
            RegistrationType = registrationType;
            ExistingMappedHeaders = existingMappedHeaders ?? throw new ArgumentNullException(nameof(existingMappedHeaders));
            RequestedMappedHeaders = requestedMappedHeaders ?? throw new ArgumentNullException(nameof(requestedMappedHeaders));
        }

        /// <summary>
        /// Gets or sets the type that has failed to be registered.
        /// </summary>
        public Type RegistrationType { get; set; }

        /// <summary>
        /// Gets or sets the HTTP request header name already associated with the type mapping.
        /// </summary>
        public IEnumerable<string> ExistingMappedHeaders { get; set; }

        /// <summary>
        /// Gets or sets the HTTP request header name requested to be associated with the type mapping.
        /// </summary>
        public IEnumerable<string> RequestedMappedHeaders { get; set; }
    }
}
