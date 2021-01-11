using System;

namespace SteveTalksCode.StrongTypedHeaders
{
    /// <summary>
    /// Interface to access registered typed headers.
    /// </summary>
    public interface IHttpRequestHeadersAccessor
    {
        /// <summary>
        /// Retrieves a class instance of type <typeparam name="T"></typeparam> when registered to map to an HTTP request header.
        /// </summary>
        /// <typeparam name="T">The type to retrieve an instance of.</typeparam>
        /// <returns>
        /// If <typeparam name="T"></typeparam> has been registered; An instance of <typeparam name="T"></typeparam> populated with an associated header value.
        /// Otherwise, null.
        /// </returns>
        T? GetHeader<T>()
            where T : class;

        /// <summary>
        /// Retrieves a class instance of type <typeparam name="T"></typeparam> when registered to map to an HTTP request header.
        /// </summary>
        /// <typeparam name="T">The type to retrieve an instance of.</typeparam>
        /// <returns>
        /// If <typeparam name="T"></typeparam> has been registered; An instance of <typeparam name="T"></typeparam> populated with an associated header value.
        /// Otherwise, throws an exception.
        /// </returns>
        /// <exception cref="InvalidOperationException">Thrown when the mapping cannot be found.</exception>
        T GetRequiredHeader<T>()
            where T : class;

        /// <summary>
        /// Retrieves a class instance of type <paramref name="type"></paramref> when registered to map to an HTTP request header.
        /// </summary>
        /// <param name="type">The type to retrieve an instance of.</param>
        /// If <paramref name="type"/> has been registered; An instance of <paramref name="type"></paramref> populated with an associated header value.
        /// Otherwise, throws an exception.
        /// <exception cref="InvalidOperationException">Thrown when the mapping cannot be found.</exception>
        object GetRequiredHeader(Type type);
    }
}