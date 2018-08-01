function() {    
  var env = karate.env; // get system property 'karate.env'
  
  karate.log('karate.env system property was:', env);
  
  if (!env) {
    env = 'local';
  }

  var config = {
    env: env,
	  apihost: 'http://bestbets-api:5000'
  };

  if (env == 'local') {
    // customize
    // e.g. config.foo = 'bar';
    config.apihost = 'http://localhost:5000';
  }
  return config;
}