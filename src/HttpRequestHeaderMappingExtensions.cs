using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Primitives;

namespace SteveTalksCode.StrongTypedHeaders
{
    /// <summary>
    /// An extension class to add dependency injection registrations that map HTTP Request headers to types.
    /// </summary>
    public static class HttpRequestHeaderMappingExtensions
    {
        /// <summary>
        /// Add a set of one or more functions to map HTTP Request Header values into typed instances.
        /// </summary>
        /// <param name="services">The service collection to add the registrations to.</param>
        /// <param name="mappingBuilder">The routine to append mappings.</param>
        /// <returns>The instance of <see cref="IServiceCollection"/> that was passed into <param name="services"></param>.</returns>
        public static IServiceCollection AddRequestHeaderMappings(
            this IServiceCollection services,
            Action<IHeaderMappingBuilder> mappingBuilder)
        {
            if (services is null) throw new ArgumentNullException(nameof(services));
            if (mappingBuilder is null) throw new ArgumentNullException(nameof(mappingBuilder));
            var mappings = new HttpRequestHeaderMappingCollection(services);
            mappingBuilder(mappings);
            mappings.Build();
            return services;
        }

        private interface IHttpRequestHeadersObjectAccessor
        {
            object GetMappedValue(Type headerType);
        }

        private class HttpRequestHeaderMappingCollection : IHeaderMappingBuilder
        {
            private readonly Dictionary<Type, TypedHeaderWrapper> _mappingDictionary = new();

            private readonly IServiceCollection _services;

            public HttpRequestHeaderMappingCollection(IServiceCollection services) => _services = services;
            
            IHeaderMappingBuilder IHeaderMappingBuilder.AddMapping<T>(
                string headerName,
                Func<IReadOnlyCollection<string>, T> mapping)
            {
                if (headerName is null) throw new ArgumentNullException(nameof(headerName));
                if (mapping is null) throw new ArgumentNullException(nameof(mapping));

                var typeOfT = typeof(T);
                var factory = new SingleValueTypedHeader(typeOfT, headerName, mapping);

                if (_mappingDictionary.ContainsKey(typeOfT))
                    throw new TypeAlreadyMappedException(typeOfT, _mappingDictionary[typeOfT].HeaderKeys, new[] { headerName });

                _mappingDictionary.Add(typeof(T), factory);
                return this;
            }

            IHeaderMappingBuilder IHeaderMappingBuilder.AddMapping<T>(IEnumerable<string> headerNames, Func<IReadOnlyDictionary<string, IReadOnlyCollection<string>>, T> mapping)
            {
                if (mapping is null) throw new ArgumentNullException(nameof(mapping));

                var headers = headerNames?.ToArray() ?? throw new ArgumentNullException(nameof(headerNames));

                var typeOfT = typeof(T);

                var factory = new MultiValuedTypedHeader(typeOfT, headers!, collection => mapping!(collection));

                if (_mappingDictionary.ContainsKey(typeOfT))
                    throw new TypeAlreadyMappedException(typeOfT, _mappingDictionary[typeOfT].HeaderKeys, headers!);

                _mappingDictionary.Add(typeof(T), factory);
                return this;
            }

            internal void Build()
            {
                _services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

                // ReSharper disable once UnusedParameter.Local
                _services.TryAddSingleton(sp => this);
                _services.TryAddSingleton<HttpRequestHeadersAccessor>();
                _services.TryAddSingleton<IHttpRequestHeadersAccessor>(sp => sp.GetRequiredService<HttpRequestHeadersAccessor>());
                _services.TryAddSingleton<IHttpRequestHeadersObjectAccessor>(sp => sp.GetRequiredService<HttpRequestHeadersAccessor>());

                foreach (var item in _mappingDictionary.Values)
                {
                    _services.TryAddScoped(item.HeaderType, provider =>
                        provider.GetRequiredService<IHttpRequestHeadersObjectAccessor>()
                            .GetMappedValue(item.HeaderType));
                }
            }
            private bool TryGetValue(Type key, out TypedHeaderWrapper value)
            {
                if (_mappingDictionary.TryGetValue(key, out var tempValue))
                {
                    value = tempValue;
                    return true;
                }

                value = null!;
                return false;
            }

            private abstract class TypedHeaderWrapper
            {
                protected TypedHeaderWrapper(Type type)
                {
                    HeaderType = type;
                }

                public Type HeaderType { get; }

                public abstract IEnumerable<string> HeaderKeys { get; }

                public abstract object? GetMappedValue(IDictionary<string, StringValues> headerValues);
            }

            private class HttpRequestHeadersAccessor : IHttpRequestHeadersAccessor, IHttpRequestHeadersObjectAccessor
            {
                private readonly IHttpContextAccessor _httpAccessor;
                private readonly HttpRequestHeaderMappingCollection _mappings;

                public HttpRequestHeadersAccessor(IHttpContextAccessor ctxAccessor, HttpRequestHeaderMappingCollection mappings)
                {
                    _httpAccessor = ctxAccessor;
                    _mappings = mappings;
                }

                object IHttpRequestHeadersAccessor.GetRequiredHeader(Type headerType) => GetCheckedMappedValue(headerType);

                object IHttpRequestHeadersObjectAccessor.GetMappedValue(Type headerType) => GetCheckedMappedValue(headerType);

                T? IHttpRequestHeadersAccessor.GetHeader<T>()
                    where T : class => GetMappedValue(typeof(T)) is T val ? val : default;

                T IHttpRequestHeadersAccessor.GetRequiredHeader<T>()
                    where T : class
                {
                    if (GetMappedValue(typeof(T)) is T val) return val;
                    throw GetException(typeof(T));
                }

                [ExcludeFromCodeCoverage(Justification = "These checks are to future proof against a non-generic addition to the registration.")]
                private object GetCheckedMappedValue(Type headerType)
                {
                    var retVal = GetMappedValue(headerType);

                    Type retValType = retVal?.GetType() ?? throw GetException(headerType);

                    if (retValType!.IsAssignableTo(headerType))
                    {
                        return retVal;
                    }

                    throw GetException(headerType);
                }

                private object? GetMappedValue(Type headerType)
                {
                    var headers = _httpAccessor.HttpContext?.Request?.Headers;
                    var values = GetFlattenedHeaderDictionary(headers);
                    return _mappings.TryGetValue(headerType, out var foundMapper) ? foundMapper.GetMappedValue(values) : null;
                }

                private static Dictionary<string, StringValues> GetFlattenedHeaderDictionary(IHeaderDictionary? headers) =>
                    headers?.Keys.ToDictionary(
                        k => k.ToLower(),
                        k => new StringValues(headers.GetCommaSeparatedValues(k).Select(s => s).ToArray()))
                    ?? new Dictionary<string, StringValues>();

                private static InvalidOperationException GetException(Type type) => new ($"Unable to retrieve {type.FullName}. Check that a mapping has been registered.");
            }

            private class SingleValueTypedHeader : TypedHeaderWrapper
            {
                private readonly Func<IReadOnlyCollection<string>, object?> _mapper;

                public SingleValueTypedHeader(Type type, string header, Func<IReadOnlyCollection<string>, object?> mapper)
                    : base(type)
                {
                    HeaderKeys = new[] { header };
                    _mapper = mapper;
                }

                public override IEnumerable<string> HeaderKeys { get; }

                public override object? GetMappedValue(IDictionary<string, StringValues> headerValues) => _mapper(headerValues.TryGetValue(HeaderKeys.Single().ToLower(), out var foundValues) ? foundValues : new StringValues());
            }

            private class MultiValuedTypedHeader : TypedHeaderWrapper
            {
                private readonly Func<IReadOnlyDictionary<string, IReadOnlyCollection<string>>, object?> _mapper;

                public MultiValuedTypedHeader(Type type, IEnumerable<string> headers, Func<IReadOnlyDictionary<string, IReadOnlyCollection<string>>, object?> mapper)
                    : base(type)
                {
                    HeaderKeys = headers;
                    _mapper = mapper;
                }

                public override IEnumerable<string> HeaderKeys { get; }

                public override object? GetMappedValue(IDictionary<string, StringValues> headerValues)
                {
                    var d = new Dictionary<string, IReadOnlyCollection<string>>();

                    foreach (var key in HeaderKeys.Select(hk => hk))
                    {
                        if (headerValues.TryGetValue(key.ToLower(), out var foundValue)) d.Add(key, foundValue);
                    }

                    return _mapper(d);
                }
            }
        }
    }
}