
## Steps to Run Locally
1. Have docker installed
2. Open a command prompt
3. `cd <REPO_ROOT>/integration-tests/docker-bestbets-api`
4. `docker-compose up --force-recreate`
   * This is being run without the detached (-d) option so that it can be easier to stop. You can choose however you want to run it.
5. `cd <REPO_ROOT>/integration-tests`
6. `./bin/load-integration-data.sh` -- this loads the test data
7. `./bin/karate ./features` -- This runs the tests
   * `./bin/karate -w ./features` will watch the feature files and rerun when they are changed. So good for devving tests

## Dependencies

The integration tests download test data from a sepcific commit hash in the [bestbets-content GitHub repository](https://github.com/nciocpl/bestbets-content). To retrieve data from a different commit, edit `bin/load-integration-data.sh` and modify the value of the `BESTBETS_CONTENT_COMMIT` environment variable.

the integration data is loaded by referencing the [bestbets-loader](https://github.com/nciocpl/bestbets-loader) as a git submodule. It is downloaded, installed, and executed via `load-integration-data.sh`. To update the loader's version, go to the `bin/bestbets-loader` directory, fetch and checkout the newer version, and commit the change.  (See also: [the submodule reference in the git manual](https://git-scm.com/book/en/v2/Git-Tools-Submodules).)

## Notes
* [Docs for understanding how to run Karate standalone](https://github.com/intuit/karate/blob/6de466bdcf105d72450a40cf31b8adb5c043037d/karate-netty/README.md#standalone-jar)
   * Specifically this has to do with the magic naming of the logging config which is really why I am posting this here!
* We have docker for dev testing because ES will no longer run on higher Java versions, this is the easiest way to get it up and running.
* .NET running locally on a Mac cannot talk to ES because of how NEST always uses the host name to connect to ES and ES exposes the Virtual Machine's hostname/IP that runs Linux on the Mac.
* You need to use the `--force-recreate` option to `docker-compose up` or run `docker-compose rm` after shutting down the cluster. If the elasticsearch container is not removed, it keeps its data, and any restarts will leave the cluster in a bad state.
