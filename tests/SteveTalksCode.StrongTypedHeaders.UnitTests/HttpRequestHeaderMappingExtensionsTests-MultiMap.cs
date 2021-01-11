// ReSharper disable StringLiteralTypo

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using NSubstitute;
using Xunit;

namespace SteveTalksCode.StrongTypedHeaders.UnitTests
{
    public class HttpRequestHeaderMappingExtensionsMultiMapTests
    {
        private const string CorroIdHeaderName1 = "X-Correlation-Id1";
        private const string CorroIdHeaderName2 = "X-Correlation-Id2";
        private const string CorroIdHeaderName3 = "X-Correlation-Id3";
        private const string ApiKeyValue = "ApiKey";

        [ExcludeFromCodeCoverage]
        private static void AddNullMapping(IHeaderMappingBuilder builder)
        {
            builder.AddMapping<ICorrelationId>(new[] { CorroIdHeaderName1, CorroIdHeaderName2, CorroIdHeaderName3 }, null!);
        }

        [ExcludeFromCodeCoverage]
        private static void NonExecutingMultiValueMappingToCorrelationId(string[]? headers, IHeaderMappingBuilder builder)
        {
            builder.AddMapping(headers!,
                collection => new CorrelationId(
                    collection[CorroIdHeaderName1]?.LastOrDefault(),
                    collection[CorroIdHeaderName2]?.LastOrDefault(),
                    collection[CorroIdHeaderName3]?.LastOrDefault()));
        }

        [Fact]
        public void Ensure_AddRequestHeaderMappings_With_One_Builder_Registrations_Adds_Scoped_Instance()
        {
            var services = new ServiceCollection()
                .AddRequestHeaderMappings(b =>
                    NonExecutingMultiValueMappingToCorrelationId(
                        new[] {CorroIdHeaderName1, CorroIdHeaderName2, CorroIdHeaderName3}, b));
            Assert.NotNull(services.SingleOrDefault(sd => sd.ServiceType == typeof(CorrelationId)));
        }
        
        [Fact]
        public void Ensure_AddRequestHeaderMappings_Builder_Registration_Throws_Exception_On_Null_Headers()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => new ServiceCollection()
                .AddRequestHeaderMappings(b =>
                    NonExecutingMultiValueMappingToCorrelationId(
                        null!, b)));

            Assert.Equal("headerNames", ex.ParamName);
        }

        [Fact]
        public void Ensure_AddRequestHeaderMappings_Builder_Registration_Throws_Exception_On_Null_Mapper()
        {

            var ex = Assert.Throws<ArgumentNullException>(() => new ServiceCollection()
                .AddRequestHeaderMappings(AddNullMapping));
            Assert.Equal("mapping", ex.ParamName);
        }



        [Fact]
        public void Ensure_AddRequestHeaderMappings_Builder_Registration_Resolves_As_Scoped_Instance_Even_When_No_HttpContext()
        {
            var services = new ServiceCollection()
                .AddRequestHeaderMappings(builder =>
                {
                    builder.AddMapping(new[] {CorroIdHeaderName1, CorroIdHeaderName2, CorroIdHeaderName3},

                        collection =>

                            new CorrelationId(
                                collection.TryGetValue(CorroIdHeaderName1, out var corro1)
                                    ? corro1.LastOrDefault()
                                    : null!,
                                collection.TryGetValue(CorroIdHeaderName2, out var corro2)
                                    ? corro2.LastOrDefault()
                                    : null!,
                                collection.TryGetValue(CorroIdHeaderName3, out var corro3)
                                    ? corro3.LastOrDefault()
                                    : null!)
                    );
                });
                
            var sp = services.BuildServiceProvider(true);
            var scopedSp = sp.CreateScope().ServiceProvider;
            var foundService = scopedSp.GetRequiredService<CorrelationId>();
            Assert.Null(foundService.CorroId1);
            Assert.Null(foundService.CorroId2);
            Assert.Null(foundService.CorroId3);
        }


        [Fact]
        public void Ensure_AddRequestHeaderMappings_Builder_Registration_Resolves_Via_Accessor_Even_When_No_HttpContext()
        {
            var services = new ServiceCollection()
                .AddRequestHeaderMappings(builder =>
                {
                    builder.AddMapping<ICorrelationId>(new[] { CorroIdHeaderName1, CorroIdHeaderName2, CorroIdHeaderName3 },

                        collection =>

                            new CorrelationId(
                                collection.TryGetValue(CorroIdHeaderName1, out var corro1)
                                    ? corro1.LastOrDefault()
                                    : null!,
                                collection.TryGetValue(CorroIdHeaderName2, out var corro2)
                                    ? corro2.LastOrDefault()
                                    : null!,
                                collection.TryGetValue(CorroIdHeaderName3, out var corro3)
                                    ? corro3.LastOrDefault()
                                    : null!)
                    );
                });

            var sp = services.BuildServiceProvider(true);
            var scopedSp = sp.CreateScope().ServiceProvider;
            var foundService = scopedSp.GetRequiredService<IHttpRequestHeadersAccessor>();
            var corroIdInstance = foundService.GetHeader<ICorrelationId>();
            Assert.NotNull(corroIdInstance);
            Assert.Null(corroIdInstance!.CorroId1);
            Assert.Null(corroIdInstance!.CorroId2);
            Assert.Null(corroIdInstance!.CorroId3);
            corroIdInstance = foundService.GetRequiredHeader<ICorrelationId>();
            Assert.NotNull(corroIdInstance);
            Assert.Null(corroIdInstance!.CorroId1);
            Assert.Null(corroIdInstance!.CorroId2);
            Assert.Null(corroIdInstance!.CorroId3);
            corroIdInstance = (CorrelationId)foundService.GetRequiredHeader(typeof(ICorrelationId));
            Assert.NotNull(corroIdInstance);
            Assert.Null(corroIdInstance!.CorroId1);
            Assert.Null(corroIdInstance!.CorroId2);
            Assert.Null(corroIdInstance!.CorroId3);
            Assert.Throws<InvalidOperationException>(() => foundService.GetRequiredHeader<NoSuchTypeRegistered>());
            Assert.Throws<InvalidOperationException>(() => foundService.GetRequiredHeader(typeof(NoSuchTypeRegistered)));
        }

        [Fact]
        public void Ensure_AddRequestHeaderMappings_Throws_Exception_With_Duplicate_Target_Types()
        {
            const string headerA = CorroIdHeaderName1 + "A";
            const string headerB = CorroIdHeaderName2 + "B";
            const string headerC = CorroIdHeaderName1 + "C";
            const string headerD = CorroIdHeaderName2 + "D";

            var exThrows = Assert.Throws<TypeAlreadyMappedException>(() =>

                _ = new ServiceCollection()
                    .AddRequestHeaderMappings(builder =>
                        AddMappingWhichThrowsException(builder, headerA, headerB, headerC, headerD)));
            

            Assert.Equal(new [] {headerA, headerB}, exThrows.ExistingMappedHeaders);
            Assert.Equal(new []{ headerC, headerD}, exThrows.RequestedMappedHeaders);
            Assert.Equal(typeof(CorrelationId), exThrows.RegistrationType);
        }

        [ExcludeFromCodeCoverage]
        private static void AddMappingWhichThrowsException(IHeaderMappingBuilder builder, string headerA,
            string headerB, string headerC, string headerD)
        {
            builder.AddMapping(new[] { headerA, headerB},

                    NonExecutedCorrelationIdFactory1(headerA, headerB)
                )
                .AddMapping(new[] { headerC, headerD },

                    NonExecutedCorrelationFactory2(headerC, headerD)
                );
        }

        [ExcludeFromCodeCoverage]
        private static Func<IReadOnlyDictionary<string, IReadOnlyCollection<string>>, CorrelationId> NonExecutedCorrelationFactory2(string headerC, string headerD)
        {
            return collection =>

                new CorrelationId(
                    collection.TryGetValue(headerC, out var corro1)
                        ? corro1.LastOrDefault()
                        : null!,
                    collection.TryGetValue(headerD, out var corro2)
                        ? corro2.LastOrDefault()
                        : null!,
                    null!);
        }

        [ExcludeFromCodeCoverage]
        private static Func<IReadOnlyDictionary<string, IReadOnlyCollection<string>>, CorrelationId> NonExecutedCorrelationIdFactory1(string headerA, string headerB)
        {
            return collection =>

                new CorrelationId(
                    collection.TryGetValue(headerA, out var corro1)
                        ? corro1.LastOrDefault()
                        : null!,
                    collection.TryGetValue(headerB, out var corro2)
                        ? corro2.LastOrDefault()
                        : null!,
                    null!);
        }


        [Fact]
        public void Ensure_AddRequestHeaderMappings_Throws_Exception_From_Builder_Lambda_On_Invalid_Cast()
        {
            var services = new ServiceCollection()
                .AddRequestHeaderMappings(builder =>
                {
                    // ReSharper disable once SuspiciousTypeConversion.Global // This is deliberate for the test.
                    builder.AddMapping(new[] { CorroIdHeaderName1 }, _ => (ICorrelationId)new Dummy());
                });

            var sp = services.BuildServiceProvider(true);
            var scopedSp = sp.CreateScope().ServiceProvider;

            Assert.Throws<InvalidCastException>(() => scopedSp.GetRequiredService<ICorrelationId>());
        }

        [Fact]
        public void Ensure_AddRequestHeaderMappings_Builder_Registration_Resolves_As_Scoped_Instance_For_Single_Mapping()
        {

            var headers = new HeaderDictionary
            {
                { CorroIdHeaderName1, "This is header 1" },
                { CorroIdHeaderName2, "This is header 2" },
                { CorroIdHeaderName3, "This is header 3" }
            };
            var services = new ServiceCollection()
                .AddSingleton<IHttpContextAccessor>(_ => new MockHttpContextAccessor(headers))
                .AddRequestHeaderMappings(builder =>
                {

                    builder.AddMapping(new[] {CorroIdHeaderName1, CorroIdHeaderName2, CorroIdHeaderName3},

                        collection =>

                            new CorrelationId(
                                collection.TryGetValue(CorroIdHeaderName1, out var corro1)
                                    ? corro1.LastOrDefault()
                                    : null!,
                                collection.TryGetValue(CorroIdHeaderName2, out var corro2)
                                    ? corro2.LastOrDefault()
                                    : null!,
                                collection.TryGetValue(CorroIdHeaderName3, out var corro3)
                                    ? corro3.LastOrDefault()
                                    : null!)
                    );
                });
            
            var sp = services.BuildServiceProvider(true);
            var scopedSp = sp.CreateScope().ServiceProvider;
            var foundService = scopedSp.GetRequiredService<CorrelationId>();
            Assert.Equal(headers[CorroIdHeaderName1], foundService.CorroId1);
            Assert.Equal(headers[CorroIdHeaderName2], foundService.CorroId2);
            Assert.Equal(headers[CorroIdHeaderName3], foundService.CorroId3);
        }

        [Fact]
        public void Ensure_AddRequestHeaderMappings_Builder_Registration_Resolves_As_Scoped_Instance_For_Multiple_Mappings_With_Many_Values()
        {

            var headers = new HeaderDictionary
            {
                { CorroIdHeaderName1, new StringValues(new [] {"Header 1a", "Header 1b"})  },
                { CorroIdHeaderName2, new StringValues(new [] {"Header 2a", "Header 2b", "Header2c"})  },
                { CorroIdHeaderName3, new StringValues(new [] {"Header 3a", "Header 3b", "Header3c", "Header3d"})},
                { ApiKeyValue, new StringValues(new [] {"FirstApiKey", "LastApiKey"}) },

            };

            var services = new ServiceCollection()
                .AddSingleton<IHttpContextAccessor>(_ => new MockHttpContextAccessor(headers))
                .AddRequestHeaderMappings(builder =>
                {
                    builder.AddMapping(headers.Keys, collection =>
                    {
                        var retVal = new MultiValuesCorrelationId
                        {
                            Corro1Values = collection.TryGetValue(CorroIdHeaderName1, out var values1)
                                ? values1.ToArray()
                                : null!,
                            Corro2Values = collection.TryGetValue(CorroIdHeaderName2, out var values2)
                                ? values2.ToArray()
                                : null!,
                            Corro3Values = collection.TryGetValue(CorroIdHeaderName3, out var values3)
                                ? values3.ToArray()
                                : null!
                        };
                        return retVal;
                    });
                    builder.AddMapping(ApiKeyValue, collection => new MultiValueApiKey(collection.ToArray()));
                }
                );

            var sp = services.BuildServiceProvider(true);
            var scopedSp = sp.CreateScope().ServiceProvider;

            var foundService1 = scopedSp.GetRequiredService<MultiValuesCorrelationId>();
            Assert.Equal(headers[CorroIdHeaderName1], foundService1.Corro1Values);
            Assert.Equal(headers[CorroIdHeaderName2], foundService1.Corro2Values);
            Assert.Equal(headers[CorroIdHeaderName3], foundService1.Corro3Values);

            var foundService2 = scopedSp.GetRequiredService<MultiValueApiKey>();
            Assert.Equal(headers[ApiKeyValue], foundService2.Value);
        }

        [Fact]
        public void Ensure_AddRequestHeaderMappings_Builder_Registration_Resolves_From_Accessor_For_Multiple_Mappings_With_Many_Values()
        {

            var headers = new HeaderDictionary
            {
                { CorroIdHeaderName1, new StringValues(new [] {"Header 1a", "Header 1b"})  },
                { CorroIdHeaderName2, new StringValues(new [] {"Header 2a", "Header 2b", "Header2c"})  },
                { CorroIdHeaderName3, new StringValues(new [] {"Header 3a", "Header 3b", "Header3c", "Header3d"})},
                { ApiKeyValue, new StringValues(new [] {"FirstApiKey", "LastApiKey"}) },

            };

            var services = new ServiceCollection()
                .AddSingleton<IHttpContextAccessor>(_ => new MockHttpContextAccessor(headers))
                .AddRequestHeaderMappings(builder =>
                {
                    builder.AddMapping(headers.Keys, collection =>
                    {
                        var retVal = new MultiValuesCorrelationId
                        {
                            Corro1Values = collection.TryGetValue(CorroIdHeaderName1, out var values1)
                                ? values1.ToArray()
                                : null!,
                            Corro2Values = collection.TryGetValue(CorroIdHeaderName2, out var values2)
                                ? values2.ToArray()
                                : null!,
                            Corro3Values = collection.TryGetValue(CorroIdHeaderName3, out var values3)
                                ? values3.ToArray()
                                : null!
                        };
                        return retVal;
                    });
                    builder.AddMapping(ApiKeyValue, collection => new MultiValueApiKey(collection.ToArray()));
                }
                );

            var sp = services.BuildServiceProvider(true);
            
            var accessorService = sp.GetRequiredService<IHttpRequestHeadersAccessor>();

            var foundService1 = accessorService.GetRequiredHeader<MultiValuesCorrelationId>();
            Assert.Equal(headers[CorroIdHeaderName1], foundService1.Corro1Values);
            Assert.Equal(headers[CorroIdHeaderName2], foundService1.Corro2Values);
            Assert.Equal(headers[CorroIdHeaderName3], foundService1.Corro3Values);

            var foundService2 = accessorService.GetRequiredHeader<MultiValueApiKey>();
            Assert.Equal(headers[ApiKeyValue], foundService2.Value);
        }


        [Fact]
        public void Ensure_AddRequestHeaderMappings_Builder_Registration_Throws_When_Mapping_Not_Registered()
        {

            var headers = new HeaderDictionary
            {
                { CorroIdHeaderName1, new StringValues(new [] {"Header 1a", "Header 1b"})  },
                { CorroIdHeaderName2, new StringValues(new [] {"Header 2a", "Header 2b", "Header2c"})  },
                { CorroIdHeaderName3, new StringValues(new [] {"Header 3a", "Header 3b", "Header3c", "Header3d"})},
                { ApiKeyValue, new StringValues(new [] {"FirstApiKey", "LastApiKey"}) },

            };

            var services = new ServiceCollection()
                .AddSingleton<IHttpContextAccessor>(_ => new MockHttpContextAccessor(headers))
                .AddRequestHeaderMappings(builder =>
                {
                    builder.AddMapping(ApiKeyValue, collection => new MultiValueApiKey(collection.ToArray()));
                }
                );

            var sp = services.BuildServiceProvider(true);

            var accessorService = sp.GetRequiredService<IHttpRequestHeadersAccessor>();

            var foundService1 = accessorService.GetRequiredHeader<MultiValueApiKey>();
            Assert.Equal(headers[ApiKeyValue], foundService1.Value);

            Assert.Throws<InvalidOperationException>( () => accessorService.GetRequiredHeader<MultiValuesCorrelationId>());
        }


        public record NoSuchTypeRegistered;

        private class MockHttpContextAccessor : IHttpContextAccessor
        {
            public MockHttpContextAccessor(IHeaderDictionary headers)
            {
                var subst = Substitute.For<HttpContext>();
                var fakeRequest = Substitute.For<HttpRequest>();
                fakeRequest.Headers.Returns(headers);
                subst.Request.Returns(fakeRequest);
                HttpContext = subst;
            }

            public HttpContext HttpContext { get; set; }
        }


        public interface ICorrelationId
        {
            string? CorroId1 { get; } 
            string? CorroId2 { get; }
            string? CorroId3 { get; }
        }
        
        [ExcludeFromCodeCoverage]
        public record MultiValueApiKey(string[]? Value)
        {
        }
        
        [ExcludeFromCodeCoverage]
        public record CorrelationId(string? CorroId1, string? CorroId2, string? CorroId3) : ICorrelationId
        {
        }
        
        [ExcludeFromCodeCoverage]
        public class Dummy
        {
        }

        [ExcludeFromCodeCoverage]
        public class MultiValuesCorrelationId
        {
            public string[]? Corro1Values { get; set; }

            public string[]? Corro2Values { get; set; }

            public string[]? Corro3Values { get; set; }
        }
    }
}