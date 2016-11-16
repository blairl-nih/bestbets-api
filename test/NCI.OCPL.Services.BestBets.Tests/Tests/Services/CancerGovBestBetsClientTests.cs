using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.Options;

using Xunit;
using Moq;
using RichardSzalay.MockHttp;

using NCI.OCPL.Utils.Testing;

using NCI.OCPL.Services.BestBets.Services;
using NCI.OCPL.Services.BestBets.Tests.CategoryTestData;

namespace NCI.OCPL.Services.BestBets.Tests
{
    public class CancerGovBestBetsClientTests
    {
        
        public static IEnumerable<object[]> XmlDeserializingData => new[] {
            new object[] { new PancoastTumorCategoryTestData() },
            new object[] { new BreastCancerCategoryTestData() }
        };

        /// <summary>
        /// Tests the correct loading of various data files.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [Theory, MemberData("XmlDeserializingData")]
        public void GetBestBetForDisplay_DataLoading(BaseCategoryTestData data)
        {
            //Setup a mock handler, which is what HttpClient uses under the hood to fetch
            //data.
            var mockHttp = new MockHttpMessageHandler();

            string filePath = data.TestFilePath;

            ByteArrayContent content = new ByteArrayContent(TestingTools.GetTestFileAsBytes(filePath));
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/xml");

            mockHttp
                .When(string.Format("https://www.cancer.gov/PublishedContent/BestBets/{0}.xml", data.ExpectedData.ID))
                .Respond(System.Net.HttpStatusCode.OK, content);

            // Setup the mocked Options
            Mock<IOptions<CancerGovBestBetsClientOptions>> bbClientOptions = new Mock<IOptions<CancerGovBestBetsClientOptions>>();
            bbClientOptions
                .SetupGet(opt => opt.Value)
                .Returns(new CancerGovBestBetsClientOptions(){
                    Host = "https://www.cancer.gov",
                    BBCategoryPathFormatter = "/PublishedContent/BestBets/{0}.xml"
                }
            );

            CancerGovBestBetsClient bbClient = new CancerGovBestBetsClient(new HttpClient(mockHttp), bbClientOptions.Object);

            IBestBetDisplay actDisplay = bbClient.GetBestBetForDisplay(data.ExpectedData.ID);

            Assert.Equal(data.ExpectedData, actDisplay, new IBestBetDisplayComparer());
        }

        /// <summary>
        /// Test for Handling HTTP status errors from the Cgov side.  (e.g. 404, 500, etc)
        /// </summary>
        public void GetBestBetForDisplay_HttpStatusError() 
        {

        }

        /// <summary>
        /// Test for bad configuration file errors.
        /// </summary>
        public void GetBestBetForDisplay_ConfigError() 
        {
        }

    }
}