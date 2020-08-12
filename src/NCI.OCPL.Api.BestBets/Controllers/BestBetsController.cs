using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Nest;
using Elasticsearch.Net;

using NCI.OCPL.Api.Common;

namespace NCI.OCPL.Api.BestBets.Controllers
{
    /// <summary>
    /// Controller for the /bestbets route.
    /// </summary>
    [Route("[controller]")]
    public class BestBetsController : Controller
    {
        private readonly IBestBetsMatchService _matchService;
        private readonly IBestBetsDisplayService _displayService;
        private readonly IHealthCheckService _healthService;
        private readonly ILogger<BestBetsController> _logger;

        /// <summary>
        /// Status to return when the system is operating correctly.
        /// </summary>
        public const string HEALTHY_STATUS = "alive!";

        /// <summary>
        /// Represents a IBestBetDisplay as returned by the get method.
        /// </summary>
        private class BestBetAPIGetResult : IBestBetDisplay
        {
            public string HTML { get; set; }
            public string ID { get; set; }

            public string Name { get; set; }

            public int Weight { get; set; }
        }

        /// <summary>
        /// Creates a new instance of a BestBetsController
        /// </summary>
        /// <param name="matchService">An IBestBetsMatchService for getting matched best bets categories</param>
        /// <param name="displayService">An IBestBetsDisplayService for getting the display HTML for matched categories</param>
        /// <param name="healthService">An IHealthCheckService for determining whether Elasticsearch can be reached</param>
        /// <param name="logger">A logger</param>
        public BestBetsController(
            IBestBetsMatchService matchService,
            IBestBetsDisplayService displayService,
            IHealthCheckService healthService,
            ILogger<BestBetsController> logger
        )
        {
            this._matchService = matchService;
            this._displayService = displayService;
            this._healthService = healthService;
            this._logger = logger;
        }

        /// <summary>
        /// Get the Best Bet Categories specified language and term.
        /// </summary>
        /// <returns>The get.</returns>
        /// <param name="collection">The collection to use. For normal operation use 'live'.</param>
        /// <param name="language">The language of the results. This should be either 'en' for English or 'es' for Spanish.</param>
        /// <param name="term">The search term to get categories for.</param>
        // GET api/values/5
        [HttpGet("{collection}/{language}/{*term}")]
        public async Task<IBestBetDisplay[]> Get(string collection, string language, string term)
        {

            if (String.IsNullOrWhiteSpace(collection))
                throw new APIErrorException(400, "You must supply a collection, language and search term");

            if (collection.ToLower() != "preview" && collection.ToLower() != "live")
                throw new APIErrorException(404, "Unsupported collection. Please try 'live'");

            if (String.IsNullOrWhiteSpace(language))
                throw new APIErrorException(400, "You must supply a language and search term");

            if (language.ToLower() != "en" && language.ToLower() != "es")
                throw new APIErrorException(404, "Unsupported Language. Please try either 'en' or 'es'");

            if (String.IsNullOrWhiteSpace(term))
                throw new APIErrorException(400, "You must supply a search term");

            // Term comes from from a catch-all parameter, so make sure it's been decoded.
            term = WebUtility.UrlDecode(term);

            // Step 1. Remove Punctuation
            string cleanedTerm = CleanTerm(term);

            string[] categoryIDs = await _matchService.GetMatches(collection.ToLower(), language.ToLower(), cleanedTerm);

            List<IBestBetDisplay> displayItems = new List<IBestBetDisplay>();

            var categoryTasks = from categoryID in categoryIDs
                                select _displayService.GetBestBetForDisplay(collection.ToLower(), categoryID);

            var categoryItems = await Task.WhenAll(categoryTasks);

            foreach (IBestBetDisplay item in categoryItems ) {
                displayItems.Add(new BestBetAPIGetResult()
                {
                    ID = item.ID,
                    Name = item.Name,
                    Weight = item.Weight,
                    HTML = item.HTML
                });
            }

            return displayItems.ToArray();
        }


        /// <summary>
        /// Provides an endpoint for checking that the various services which make up the API
        /// (and thus the API itself) are all in a state where they can return information.
        /// </summary>
        /// <returns>The contents of BestBetsController.HEALTHY_STATUS ('alive!') if
        /// all services are running. If unhealthy services are found, APIErrorException is thrown
        /// with HTTPStatusCode set to 500.</returns>
        [HttpGet("status")]
        public async Task<string> GetStatus()
        {
            IHealthCheckService[] monitoredServices = new IHealthCheckService[]
            {
                _healthService
            };

            // Check for all services so we can log the status of all failures rather than
            // than just the first one.
            bool allHealthy = true;
            //Select an async callback that keeps the name with the service.
            var healthChecks = monitoredServices.Select(async svc => new
            {
                Status = await svc.IsHealthy()
            });

            var results = await Task.WhenAll(healthChecks);

            foreach (var result in results)
            {
                if (!result.Status)
                {
                    allHealthy = false;
                    _logger.LogError("Service not healthy.");
                }
            }

            if (!allHealthy)
            {
                _logger.LogError("One or more services are not healthy.");
                throw new APIErrorException(500, "One or more services are not healthy.");
            }

            return HEALTHY_STATUS;
        }

        //TODO: Move CleanTerm to a shared class for use by the indexer as well.

        /// <summary>
        /// This will remove punctuation from a term
        /// </summary>
        /// <param name="term">The term to clean</param>
        /// <returns>The cleaned term</returns>
        private string CleanTerm(string term)
        {
            var sb = new StringBuilder();
            foreach (char c in term)
            {
                if (!char.IsPunctuation(c))
                    sb.Append(c);
            }
            term = sb.ToString();

            //TODO: Verify that this list is not actually a duplicate of
            return System.Text.RegularExpressions.Regex.Replace(term, "[-':;\"\\./]", "");
        }

    }
}
