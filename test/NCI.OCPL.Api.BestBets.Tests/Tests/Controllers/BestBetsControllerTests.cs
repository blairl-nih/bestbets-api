using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Microsoft.Extensions.Logging.Testing;

using Elasticsearch.Net;
using Nest;

using Newtonsoft.Json.Linq;

using Moq;
using Xunit;

using NCI.OCPL.Utils.Testing;

using NCI.OCPL.Api.BestBets;
using NCI.OCPL.Api.BestBets.Controllers;
using NCI.OCPL.Api.BestBets.Tests.CategoryTestData;
using NCI.OCPL.Api.BestBets.Tests.ESHealthTestData;
using System.Threading.Tasks;

namespace NCI.OCPL.Api.BestBets.Tests
{
    // see example explanation on xUnit.net website:
    // https://xunit.github.io/docs/getting-started-dotnet-core.html
    public class BestBetsControllerTests
    {
        public static IEnumerable<object[]> XmlDeserializingData => new[] {
            new object[] {
                "pancoast", 
                new PancoastTumorCategoryTestData() 
            }//,
            //new object[] {
            //    "breast cancer", 
            //    new BreastCancerCategoryTestData() 
            //}
        };

        [Fact]
        public async void Get_Error_LanguageEmpty() 
        {
            Mock<IBestBetsDisplayService> displayService = new Mock<IBestBetsDisplayService>();
            Mock<IBestBetsMatchService> matchService = new Mock<IBestBetsMatchService>();

            // Create instance of controller
            BestBetsController controller = new BestBetsController(                
                matchService.Object,
                displayService.Object,
                NullLogger<BestBetsController>.Instance
            );

            APIErrorException ex = await Assert.ThrowsAsync<APIErrorException>( () => controller.Get(null, null) );
        }

        [Fact]
        public async void Get_Error_LanguageBad() 
        {
            Mock<IBestBetsDisplayService> displayService = new Mock<IBestBetsDisplayService>();
            Mock<IBestBetsMatchService> matchService = new Mock<IBestBetsMatchService>();

            // Create instance of controller
            BestBetsController controller = new BestBetsController(                
                matchService.Object,
                displayService.Object,
                NullLogger<BestBetsController>.Instance
            );

            await Assert.ThrowsAsync<APIErrorException>(() => controller.Get("Chicken", null));
        }

        [Fact]
        public async void Get_Error_SearchTermBad() 
        {
            Mock<IBestBetsDisplayService> displayService = new Mock<IBestBetsDisplayService>();
            Mock<IBestBetsMatchService> matchService = new Mock<IBestBetsMatchService>();

            // Create instance of controller
            BestBetsController controller = new BestBetsController(                
                matchService.Object,
                displayService.Object,
                NullLogger<BestBetsController>.Instance
            );

            await Assert.ThrowsAsync<APIErrorException>( () => controller.Get("en", null) );
        }


        [Theory, MemberData(nameof(XmlDeserializingData))]
        public async void Get_EnglishTerm(string searchTerm, BaseCategoryTestData data) 
        {
            Mock<IBestBetsDisplayService> displayService = new Mock<IBestBetsDisplayService>(); 
            displayService
                .Setup(
                    dispSvc => dispSvc.GetBestBetForDisplay(
                        It.Is<string>(catID => catID == data.ExpectedData.ID)
                    )
                )
                .Returns(Task.FromResult<IBestBetDisplay>(TestingTools.DeserializeXML<CancerGovBestBet>(data.TestFilePath)));

            Mock<IBestBetsMatchService> matchService = new Mock<IBestBetsMatchService>();
            matchService
                .Setup(
                    matchSvc => matchSvc.GetMatches(                        
                        It.Is<string>(lang => lang == "en"),
                        It.Is<string>(term => term == searchTerm)
                    )
                )
                .Returns(Task.FromResult(new string[] { data.ExpectedData.ID }));
            
            // Create instance of controller
            BestBetsController controller = new BestBetsController(                
                matchService.Object,
                displayService.Object,
                NullLogger<BestBetsController>.Instance
            );

            IBestBetDisplay[] actualItems = await controller.Get("en", searchTerm);

            Assert.Equal(actualItems, new IBestBetDisplay[] { data.ExpectedData }, new IBestBetDisplayComparer());
        }

        /// <summary>
        /// Verify that Status always returns successfully when the services it depends
        /// on are healthy.
        /// </summary>
        [Fact]
        public async void IsHealthy_Healthy()
        {
            IBestBetsDisplayService displayService = GetMockedHealthSvc<IBestBetsDisplayService>(true);
            IBestBetsMatchService matchService = GetMockedHealthSvc<IBestBetsMatchService>(true);

            // Create instance of controller
            BestBetsController controller = new BestBetsController(
                matchService,
                displayService,
                NullLogger<BestBetsController>.Instance
                );

            var actual = await controller.GetStatus(); 

            Assert.Equal(BestBetsController.HEALTHY_STATUS, actual, ignoreCase: true);
        }


        private static T GetMockedHealthSvc<T>( bool status) where T : class, IHealthCheckService {
            var mock = new Mock<T>();
            mock.Setup(svc => svc.IsHealthy())
                .Returns(Task.FromResult(status));

            return mock.Object;
        }

        /// <summary>
        /// Combinations of one or more unhealthy services which are supposed to result in
        /// BestBetsController.GetStatus() reporting an error condition.
        /// </summary>
        public static IEnumerable<object[]> UnhealthyServiceCombinations => new[]
        {
            new object[] { GetMockedHealthSvc<IBestBetsDisplayService>(false), GetMockedHealthSvc<IBestBetsMatchService>(false) },
            new object[] { GetMockedHealthSvc<IBestBetsDisplayService>(true), GetMockedHealthSvc<IBestBetsMatchService>(false) },
            new object[] { GetMockedHealthSvc<IBestBetsDisplayService>(false), GetMockedHealthSvc<IBestBetsMatchService>(true) }
        };

        /// <summary>
        /// Verify that Status fails for the various combinations of unhealthy services.
        /// </summary>
        [Theory, MemberData(nameof(UnhealthyServiceCombinations))]
        public async void IsHealthy_Unhealthy(IBestBetsDisplayService displayService, IBestBetsMatchService matchService)
        {
            BestBetsController controller = new BestBetsController(
                matchService,
                displayService,
                NullLogger<BestBetsController>.Instance
                );

            // If any of the services are unhealthy, verify that GetStatus() throws APIErrorException
            // with a status of 500.
            APIErrorException ex = await Assert.ThrowsAsync<APIErrorException>(() => controller.GetStatus());

            Assert.Equal(500, ex.HttpStatusCode);
        }
    }
}
