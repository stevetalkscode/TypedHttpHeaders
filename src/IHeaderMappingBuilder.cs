using System;
using System.Collections.Generic;

namespace SteveTalksCode.StrongTypedHeaders
{
    /// <summary>
    /// Interface that allows type mappings from HTTP Request headers to be built.
    /// </summary>
    public interface IHeaderMappingBuilder
    {
        /// <summary>
        /// Adds a mapping to associate type <typeparamref name="T"/> with a single HTTP Request header entry.
        /// </summary>
        /// <typeparam name="T">The class type to add a mapping for.</typeparam>
        /// <param name="headerName">The HTTP Request header to map to the specified type.</param>
        /// <param name="mapping">A factory function to map the the values within the header to the type.</param>
        /// <returns>The same <see cref="IHeaderMappingBuilder"/> to allow other types to be added.</returns>
        IHeaderMappingBuilder AddMapping<T>(string headerName, Func<IReadOnlyCollection<string>, T> mapping)
            where T : class;

        /// <summary>
        /// Adds a mapping to associate type <typeparamref name="T"/> with a multiple HTTP Request headers entries.
        /// </summary>
        /// <typeparam name="T">The class type to add a mapping for.</typeparam>
        /// <param name="headerNames">The HTTP Request headers to map to the specified type.</param>
        /// <param name="mapping">A factory function to map the the values within the header to the type.</param>
        /// <returns>The same <see cref="IHeaderMappingBuilder"/> to allow other types to be added.</returns>
        IHeaderMappingBuilder AddMapping<T>(IEnumerable<string> headerNames, Func<IReadOnlyDictionary<string, IReadOnlyCollection<string>>, T> mapping)
            where T : class;
    }
}