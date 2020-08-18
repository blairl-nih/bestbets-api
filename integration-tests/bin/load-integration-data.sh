#!/bin/bash

# Commit hash of the http://github.com/nciocpl/bestbets-content respository to use
# when loading data into elastic search.
BESTBETS_CONTENT_COMMIT=0032561df90ba71f4c19e955bcfc1ddb6cab8b85

## Use the Environment var or the default
if [[ -z "${ELASTIC_SEARCH_HOST}" ]]; then
    ELASTIC_HOST="http://localhost:9200"
else
    ELASTIC_HOST=${ELASTIC_SEARCH_HOST}
fi

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null 2>&1 && pwd )"

## Wait until docker is up.
echo "Waiting for ES Service at ${ELASTIC_HOST} to Start"
until $(curl --output /dev/null --silent --head --fail "${ELASTIC_HOST}"); do
    printf '.'
    sleep 1
done
echo "ES Service is up"

# First wait for ES to start...
response=$(curl --write-out %{http_code} --silent --output /dev/null "${ELASTIC_HOST}")

until [ "$response" = "200" ]; do
    response=$(curl --write-out %{http_code} --silent --output /dev/null "${ELASTIC_HOST}")
    >&2 echo "Elastic Search is unavailable - sleeping"
    sleep 1
done

# next wait for ES status to turn to Green
health_check="curl -fsSL ${ELASTIC_HOST}/_cat/health?h=status"
health=$(eval $health_check)
echo "Waiting for ES status to be ready"
until [[ "$health" = 'green' ]]; do
    >&2 echo "Elastic Search is unavailable - sleeping"
    sleep 10
    health=$(eval $health_check)
done
echo "ES status is green"

## retrieve bestbets-loader submodule
git submodule update --init
pushd "${DIR}/bestbets-loader"
## Override bestbets loader config to use the specified content commit
## as well as the dynamic elasticsearch port.
export NODE_CONFIG="{ \"pipeline\": { \"source\": { \"config\": { \"resourcesPath\": \"/tree/${BESTBETS_CONTENT_COMMIT}/content\" } }, \"transformers\": [ { \"module\": \"./lib/transformers/category-to-match-transformer\", \"config\": { \"eshosts\": [\"${ELASTIC_HOST}\"], \"settingsPath\": \"es-mappings/settings.json\", \"analyzer\": \"nostem\"} } ], \"loader\": { \"config\": { \"eshosts\": [ \"${ELASTIC_HOST}\" ] } } } }"
npm install
node index.js
popd
