﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NCI.OCPL.Api.Common
{
    /// <summary>
    /// Elasticsearch configuration
    /// </summary>
    public class ElasticsearchOptions
    {
        /// <summary>
        /// Gets or sets the elasticsearch servers.
        /// </summary>
        /// <value>The servers.</value>
        public string Servers { get; set; }

        /// <summary>
        /// Gets or sets the userid.
        /// </summary>
        /// <value>The userid.</value>
        public string Userid { get; set; }

        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        /// <value>The password.</value>
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets the maximum retries.
        /// </summary>
        /// <value>The maximum retries.</value>
        public int MaximumRetries { get; set; }
    }
}