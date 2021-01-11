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
    public class HttpRequestHeaderMappingExtensionsTests
    {
        private const string CorroIdHeaderName = "X-Correlation-Id";
        private const string ApiKeyValue = "ApiKey";
        
        private static readonly string[] DefaultServiceTypes = new[]
        {
            "IHttpContextAccessor",
            "HttpRequestHeaderMappingCollection",
            "HttpRequestHeadersAccessor",
            "IHttpRequestHeadersAccessor",
            "IHttpRequestHeadersObjectAccessor"
        }.OrderBy(s => s).ToArray();

        private static string[] GetExpectedServiceTypes(params string[] types)
        {
            return DefaultServiceTypes.Concat(types).OrderBy(s => s).ToArray();
        }

        [ExcludeFromCodeCoverage]
        private static void ExceptionThrowingMappingRegistration(IHeaderMappingBuilder builder, string header1, string header2)
        {
            builder.AddMapping(header1, NonExecutingCorrelationFactory);
            builder.AddMapping(header2, NonExecutingCorrelationFactory);
        }

        [ExcludeFromCodeCoverage]
        private static CorrelationId NonExecutingCorrelationFactory(IReadOnlyCollection<string> collection)
            => new(collection.LastOrDefault());


        [Fact]
        public void Ensure_AddRequestHeaderMappings_With_Null_Service_Param_Throws_ArgNullException()
        {
            var exThrown = Assert.Throws<ArgumentNullException>(() => _ =
                HttpRequestHeaderMappingExtensions.AddRequestHeaderMappings(null!, null!));

            Assert.Contains("services", exThrown.Message);
        }

        [Fact]
        public void Ensure_AddRequestHeaderMappings_With_Null_MappingBuilder_Param_Throws_ArgNullException()
        {
            var exThrown = Assert.Throws<ArgumentNullException>(() => _ =
                new ServiceCollection().AddRequestHeaderMappings(null!));

            Assert.Contains("mappingBuilder", exThrown.Message);
        }

        [Fact]
        public void Ensure_AddRequestHeaderMappings_With_No_Builder_Registrations_Still_Adds_Common_Registrations()
        {
            var registeredServices = new ServiceCollection()
                // ReSharper disable once UnusedParameter.Local
                .AddRequestHeaderMappings(builder => { })
                .Select(s => s.ServiceType.Name).OrderBy(s => s);

            Assert.True(DefaultServiceTypes.SequenceEqual(registeredServices));
        }

        [Fact]
        public void Ensure_AddRequestHeaderMappings_With_One_Builder_Registrations_Adds_Scoped_Instance()
        {
            var services = new ServiceCollection();

            var registeredServices = services
                .AddRequestHeaderMappings(builder => builder.AddMapping(CorroIdHeaderName, NonExecutingCorrelationMapping()))
                .Select(s => s.ServiceType.Name).OrderBy(s => s);

            Assert.True(GetExpectedServiceTypes(nameof(CorrelationId)).SequenceEqual(registeredServices));
            Assert.NotNull(services.SingleOrDefault(sd => sd.ServiceType == typeof(CorrelationId)));
        }

        [ExcludeFromCodeCoverage]
        private static Func<IReadOnlyCollection<string>, CorrelationId> NonExecutingCorrelationMapping()
        {
            return collection => new CorrelationId(collection.LastOrDefault());
        }

        [Fact]
        public void Ensure_AddRequestHeaderMappings_Builder_Registration_Throws_Exception_On_Null_Header()
        {
            Func<IEnumerable<string>, CorrelationId> mapper = null!;

            var ex = Assert.Throws<ArgumentNullException>(() => new ServiceCollection()
                .AddRequestHeaderMappings(builder => builder.AddMapping(null!, mapper)));
            
            Assert.Equal("headerName", ex.ParamName);
        }

        [Fact]
        public void Ensure_AddRequestHeaderMappings_Builder_Registration_Throws_Exception_On_Null_Mapper()
        {
            Func<IEnumerable<string>, CorrelationId> mapper = null!;

            var ex = Assert.Throws<ArgumentNullException>(() => new ServiceCollection()
                .AddRequestHeaderMappings(builder => builder.AddMapping(CorroIdHeaderName, mapper)));

            Assert.Equal("mapping", ex.ParamName);
        }


        [Fact]
        public void Ensure_AddRequestHeaderMappings_Builder_Registration_Resolves_As_Scoped_Instance_Even_When_No_HttpContext()
        {
            var services = new ServiceCollection()
                .AddRequestHeaderMappings(builder =>
                {
                    builder.AddMapping(CorroIdHeaderName, collection => new CorrelationId(collection.LastOrDefault()));
                });

            var sp = services.BuildServiceProvider(true);
            var scopedSp = sp.CreateScope().ServiceProvider;
            var foundService = scopedSp.GetRequiredService<CorrelationId>();
            Assert.Null(foundService.Value);
        }


        [Fact]
        public void Ensure_AddRequestHeaderMappings_Builder_Registration_Resolves_Via_Accessor_Even_When_No_HttpContext()
        {
            var services = new ServiceCollection()
                .AddRequestHeaderMappings(builder =>
                {
                    builder.AddMapping(CorroIdHeaderName, collection => new CorrelationId(collection.LastOrDefault()));
                });

            var sp = services.BuildServiceProvider(true);
            var scopedSp = sp.CreateScope().ServiceProvider;
            var foundService = scopedSp.GetRequiredService<IHttpRequestHeadersAccessor>();
            var corroIdInstance = foundService.GetHeader<CorrelationId>();
            Assert.NotNull(corroIdInstance);
            Assert.Null(corroIdInstance!.Value);
            corroIdInstance = foundService.GetRequiredHeader<CorrelationId>();
            Assert.NotNull(corroIdInstance);
            Assert.Null(corroIdInstance!.Value);
            Assert.Throws<InvalidOperationException>(() => foundService.GetRequiredHeader<NoSuchTypeRegistered>());
            Assert.Throws<InvalidOperationException>(() => foundService.GetRequiredHeader(typeof(NoSuchTypeRegistered)));
            Assert.Null(foundService.GetHeader<NoSuchTypeRegistered>());
        }
        
        [Fact]
        public void Ensure_AddRequestHeaderMappings_Throws_Exception_With_Duplicate_Target_Types()
        {
            var header1 = CorroIdHeaderName + "1";
            var header2 = CorroIdHeaderName + "2";

            var exThrows = Assert.Throws<TypeAlreadyMappedException>(() =>
                new ServiceCollection().AddRequestHeaderMappings(builder =>
                    ExceptionThrowingMappingRegistration(builder, header1, header2)));

            Assert.Equal(header1, exThrows.ExistingMappedHeaders.Single());
            Assert.Equal(header2, exThrows.RequestedMappedHeaders.Single());
            Assert.Equal(typeof(CorrelationId), exThrows.RegistrationType);
        }


        [Fact]
        public void Ensure_AddRequestHeaderMappings_Throws_Exception_From_Builder_Lambda_On_Invalid_Cast()
        {
            var services = new ServiceCollection()
                .AddRequestHeaderMappings(builder =>
                {
                    // ReSharper disable once SuspiciousTypeConversion.Global // This is deliberate for the test.
                    builder.AddMapping(CorroIdHeaderName, _ => (ICorrelationId)new Dummy());
                });

            var sp = services.BuildServiceProvider(true);
            var scopedSp = sp.CreateScope().ServiceProvider;

            Assert.Throws<InvalidCastException>(() => scopedSp.GetRequiredService<ICorrelationId>());
        }

        [Fact]
        public void Ensure_AddRequestHeaderMappings_Builder_Registration_Resolves_As_Scoped_Instance_For_Single_Mapping()
        {

            var headers = new HeaderDictionary {{CorroIdHeaderName, "12131232131123"}};
            var services = new ServiceCollection()
                .AddSingleton<IHttpContextAccessor>(_ => new MockHttpContextAccessor(headers))
                .AddRequestHeaderMappings(builder =>
                {
                    builder.AddMapping(CorroIdHeaderName, collection => new CorrelationId(collection.LastOrDefault()));
                });

            var sp = services.BuildServiceProvider(true);
            var scopedSp = sp.CreateScope().ServiceProvider;
            var foundService = scopedSp.GetRequiredService<CorrelationId>();
            Assert.Equal(headers[CorroIdHeaderName],foundService.Value);
        }

        [Fact]
        public void Ensure_AddRequestHeaderMappings_Builder_Registration_Resolves_As_Scoped_Instance_For_Multiple_Mappings_With_Many_Values()
        {

            var headers = new HeaderDictionary
            {
                {CorroIdHeaderName, new StringValues(new [] {"FirstCorroId", "LastCorroId"})},
                {ApiKeyValue, new StringValues(new [] {"FirstApiKey", "LastApiKey"}) },
                
            };

            var capturedCorroHeaderValues = new List<string>();
            var capturedApiKeyHeaderValues = new List<string>();

            var services = new ServiceCollection()
                .AddSingleton<IHttpContextAccessor>(_ => new MockHttpContextAccessor(headers))
                .AddRequestHeaderMappings(builder =>
                {
                    builder.AddMapping(CorroIdHeaderName, collection =>
                    {
                        capturedCorroHeaderValues.AddRange(collection);
                        return new CorrelationId(collection.LastOrDefault());
                    });
                    builder.AddMapping(ApiKeyValue, collection =>
                    {
                        capturedApiKeyHeaderValues.AddRange(collection);
                        return new ApiKey(collection.LastOrDefault());
                    });
                }
                );

            var sp = services.BuildServiceProvider(true);
            var scopedSp = sp.CreateScope().ServiceProvider;
            
            var foundService1 = scopedSp.GetRequiredService<CorrelationId>();
            Assert.Equal(headers[CorroIdHeaderName].Last(), foundService1.Value);
            
            var foundService2 = scopedSp.GetRequiredService<ApiKey>();
            Assert.Equal(headers[ApiKeyValue].Last(), foundService2.Value);
            
            Assert.True(headers[CorroIdHeaderName].OrderBy(s=>s).SequenceEqual(capturedCorroHeaderValues.OrderBy(s => s)));
            Assert.True(headers[ApiKeyValue].OrderBy(s => s).SequenceEqual(capturedApiKeyHeaderValues.OrderBy(s => s)));
        }

        [Fact]
        public void Ensure_AddRequestHeaderMappings_Builder_Registration_Resolves_From_Accessor_For_Multiple_Mappings_With_Many_Values()
        {

            var headers = new HeaderDictionary
            {
                {CorroIdHeaderName, new StringValues(new [] {"FirstCorroId", "LastCorroId"})},
                {ApiKeyValue, new StringValues(new [] {"FirstApiKey", "LastApiKey"}) },

            };

            var capturedCorroHeaderValues = new List<string>();
            var capturedApiKeyHeaderValues = new List<string>();

            var services = new ServiceCollection()
                .AddSingleton<IHttpContextAccessor>(_ => new MockHttpContextAccessor(headers))
                .AddRequestHeaderMappings(builder =>
                {
                    builder.AddMapping(CorroIdHeaderName, collection =>
                    {
                        capturedCorroHeaderValues.AddRange(collection);
                        return new CorrelationId(collection.LastOrDefault());
                    });
                    builder.AddMapping(ApiKeyValue, collection =>
                    {
                        capturedApiKeyHeaderValues.AddRange(collection);
                        return new ApiKey(collection.LastOrDefault());
                    });
                }
                );

            var sp = services.BuildServiceProvider(true);
            
            var foundService1 = sp.GetRequiredService<IHttpRequestHeadersAccessor>().GetRequiredHeader<CorrelationId>();
            Assert.Equal(headers[CorroIdHeaderName].Last(), foundService1.Value);

            var foundService2 = sp.GetRequiredService<IHttpRequestHeadersAccessor>().GetRequiredHeader<ApiKey>();
            Assert.Equal(headers[ApiKeyValue].Last(), foundService2.Value);

            Assert.True(headers[CorroIdHeaderName].OrderBy(s => s).SequenceEqual(capturedCorroHeaderValues.OrderBy(s => s)));
            Assert.True(headers[ApiKeyValue].OrderBy(s => s).SequenceEqual(capturedApiKeyHeaderValues.OrderBy(s => s)));
        }

        [Fact]
        public void Ensure_AddRequestHeaderMappings_Builder_Registration_Resolves_Null_From_Accessor_For_Multiple_Mappings_With_Many_Values()
        {

            var headers = new HeaderDictionary
            {
                {CorroIdHeaderName, new StringValues(new [] {"FirstCorroId", "LastCorroId"})},
                {ApiKeyValue, new StringValues(new [] {"FirstApiKey", "LastApiKey"}) },

            };

            var capturedCorroHeaderValues = new List<string>();
            var capturedApiKeyHeaderValues = new List<string>();

            var services = new ServiceCollection()
                .AddSingleton<IHttpContextAccessor>(_ => new MockHttpContextAccessor(headers))
                .AddRequestHeaderMappings(builder =>
                {
                    builder.AddMapping(CorroIdHeaderName, collection =>
                    {
                        capturedCorroHeaderValues.AddRange(collection);
                        return new CorrelationId(collection.LastOrDefault());
                    });
                    builder.AddMapping(ApiKeyValue, collection =>
                    {
                        capturedApiKeyHeaderValues.AddRange(collection);
                        return new ApiKey(collection.LastOrDefault());
                    });
                }
                );

            var sp = services.BuildServiceProvider(true);

            var foundService1 = sp.GetRequiredService<IHttpRequestHeadersAccessor>().GetRequiredHeader<CorrelationId>();
            Assert.Equal(headers[CorroIdHeaderName].Last(), foundService1.Value);

            var foundService2 = sp.GetRequiredService<IHttpRequestHeadersAccessor>().GetRequiredHeader<ApiKey>();
            Assert.Equal(headers[ApiKeyValue].Last(), foundService2.Value);

            Assert.True(headers[CorroIdHeaderName].OrderBy(s => s).SequenceEqual(capturedCorroHeaderValues.OrderBy(s => s)));
            Assert.True(headers[ApiKeyValue].OrderBy(s => s).SequenceEqual(capturedApiKeyHeaderValues.OrderBy(s => s)));
        }


        [Fact]
        public void Ensure_AddRequestHeaderMappings_Builder_Registration_Resolves_As_Scoped_Instance_For_Multiple_Mappings()
        {

            var headers = new HeaderDictionary
            {
                {CorroIdHeaderName, "12131232131123"}, {ApiKeyValue, "z8dsdyzgcubasduicb77q8e89w0eqr789chsdauihfff"}
            };
            var services = new ServiceCollection()
                .AddSingleton<IHttpContextAccessor>(_ => new MockHttpContextAccessor(headers))
                .AddRequestHeaderMappings(builder =>
                    {
                        builder.AddMapping(CorroIdHeaderName, collection => new CorrelationId(collection.LastOrDefault()));
                        builder.AddMapping(ApiKeyValue, collection => new ApiKey(collection.LastOrDefault()));
                    }
                );

            var sp = services.BuildServiceProvider(true);
            var scopedSp = sp.CreateScope().ServiceProvider;
            var foundService1 = scopedSp.GetRequiredService<CorrelationId>();
            Assert.Equal(headers[CorroIdHeaderName], foundService1.Value);
            var foundService2 = scopedSp.GetRequiredService<ApiKey>();
            Assert.Equal(headers[ApiKeyValue], foundService2.Value);
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
        }

        [ExcludeFromCodeCoverage]
        public record ApiKey(string? Value)
        {
        }

        [ExcludeFromCodeCoverage]
        public record CorrelationId(string? Value) : ICorrelationId
        {
        }
        
        [ExcludeFromCodeCoverage]
        public class Dummy
        {
        }
    }
}
