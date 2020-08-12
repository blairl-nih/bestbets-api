using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;

using Nest;
using Elasticsearch.Net;
using Newtonsoft.Json.Linq;
using Xunit;

using NCI.OCPL.Api.Common.Testing;
using NCI.OCPL.Api.BestBets.Services;


namespace NCI.OCPL.Api.BestBets.Tests
{
    public class ESTokenAnalyzerServiceTests : TestServiceBase
    {

        public static IEnumerable<object[]> GetTokenCountData => new[] {
            new object[] {
                "pancoast",
                new object[] {
                    new { token = "pancoast", start_offset = 0, end_offset = 6, type = "<ALPHANUM>", position= 0 },
                },
                1
            },

            new object[] {
                "breast cancer",
                new object[] {
                    new { token = "breast", start_offset = 0, end_offset = 6, type = "<ALPHANUM>", position= 0 },
                    new { token = "cancer", start_offset = 7, end_offset = 13, type = "<ALPHANUM>", position= 1 },
                },
                2
            },
            //TODO: Add crazier tests
        };

        /// <summary>
        /// Verify the GetTokenCount() method knows how to handle responses
        /// from elastic search.
        /// </summary>
        /// <param name="searchTerm">The search term to tokenizse.</param>
        /// <param name="responseTokens">The simulated response from elasticsearch.</param>
        /// <param name="expectedCount">The expected token count.</param>
        /// <returns></returns>
        [Theory, MemberData(nameof(GetTokenCountData))]
        public async void GetTokenCount_Responses(
            string searchTerm,
            object[] responseTokens,
            int expectedCount
        )
        {

            ElasticsearchInterceptingConnection conn = new ElasticsearchInterceptingConnection();

            conn.RegisterRequestHandlerForType<AnalyzeResponse>(
                (req, res) =>
                {
                    JObject resObject = new JObject();
                    resObject["tokens"] = JArray.FromObject(responseTokens);
                    byte[] byteArray = Encoding.UTF8.GetBytes(resObject.ToString());

                    res.Stream = new MemoryStream(byteArray);
                    res.StatusCode = 200;
                }
            );

            //While this has a URI, it does not matter, an InMemoryConnection never requests
            //from the server.
            var pool = new SingleNodeConnectionPool(new Uri("http://localhost:9200"));

            var connectionSettings = new ConnectionSettings(pool, conn);
            IElasticClient client = new ElasticClient(connectionSettings);

            IOptions<CGBBIndexOptions> config = GetMockConfig();

            ESTokenAnalyzerService service = new ESTokenAnalyzerService(client, config, new NullLogger<ESTokenAnalyzerService>());
            int actualCount = await service.GetTokenCount("live", searchTerm);

            Assert.Equal(expectedCount, actualCount);

        }

        //TODO: Test failure after repeated attempts.
    }
}
