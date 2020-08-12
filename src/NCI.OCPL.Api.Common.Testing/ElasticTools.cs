using System;
using System.Collections.Generic;

using Elasticsearch.Net;
using Nest;

using Moq;

namespace NCI.OCPL.Api.Common.Testing
{

    /// <summary>
    /// Tools for mocking elasticsearch clients
    /// </summary>
    public static class ElasticTools {

        /// <summary>
        /// Gets an ElasticClient backed by an InMemoryConnection.  This is used to mock the
        /// JSON returned by the elastic search so that we test the Nest mappings to our models.
        /// </summary>
        /// <param name="testFile"></param>
        /// <param name="requestDataCallback"></param>
        /// <returns></returns>
        public static IElasticClient GetInMemoryElasticClient(string testFile) {

            //Get Response JSON
            byte[] responseBody = TestingTools.GetTestFileAsBytes(testFile);

            //While this has a URI, it does not matter, an InMemoryConnection never requests
            //from the server.
            var pool = new SingleNodeConnectionPool(new Uri("http://localhost:9200"));

            // Setup ElasticSearch stuff using the contents of the JSON file as the client response.
            InMemoryConnection conn = new InMemoryConnection(responseBody);

            var connectionSettings = new ConnectionSettings(pool, conn);

            return new ElasticClient(connectionSettings);
        }

        /// <summary>
        /// This function mocks the IElasticClient.SearchTemplate method and can be used to capture
        /// the requests being made to the ElasticSearch servers.
        /// </summary>
        /// <param name="requestInspectorCallback">An Action to be called once the IElasticClient.SearchTemplate
        /// method has been called.  This should be used to store off the ISearchTemplateRequest for later
        /// comparison.
        /// </param>
        /// <param name="dataFiller">This is a callback function that is used to fill in the mocked
        /// response from a searchTemplate call.  This is generic as the caller of
        /// </param>
        /// <returns></returns>
        public static IElasticClient GetMockedSearchTemplateClient<T>(
            Action<ISearchTemplateRequest> requestInspectorCallback,
            Action<Mock<ISearchResponse<T>>> dataFiller
        ) where T : class
        {

            // Setup Mocked Response
            Mock<ISearchResponse<T>> mockResponse = new Mock<ISearchResponse<T>>();

            // Call our dataFiller to setup the results to be whatever the caller needs it
            // to be.
            if (dataFiller != null)
            {
                dataFiller(mockResponse);
            }

            // Setup the client mock.
            Mock<IElasticClient> elasticClientMock = new Mock<IElasticClient>();

            /// Mock up the Search Template Function
            elasticClientMock
                // Handle the condition where this code should run
                .Setup(
                    ec => ec.SearchTemplate(
                        It.IsAny<Func<SearchTemplateDescriptor<T>, ISearchTemplateRequest>>()
                    )
                )
                // Give a callback for the mocked signature.  This will store off the request.
                // This is a little inside baseball, but the invoking of the anon function below is taken from
                // how the Nest code will execute the search based on the above mocked call.
                // https://github.com/elastic/elasticsearch-net/blob/master/src/Nest/Search/SearchTemplate/ElasticClient-SearchTemplate.cs
                .Callback<Func<SearchTemplateDescriptor<T>,ISearchTemplateRequest>>(
                    sd => {
                        ISearchTemplateRequest savedTemplateRequest;
                        savedTemplateRequest = sd?.Invoke(new SearchTemplateDescriptor<T>());
                        //throw new Exception(JObject.FromObject(savedTemplateRequest).ToString());
                        //Call the callback so that the calling function can save the searchrequest
                        //for comparing once the IElasticClient.SearchTemplate function has executed.
                        if (requestInspectorCallback != null) {
                            requestInspectorCallback((ISearchTemplateRequest)savedTemplateRequest);
                        }
                    }
                )
                // Return something from our method.
                .Returns(mockResponse.Object);

            return elasticClientMock.Object;
        }

    }
}