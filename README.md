# bestbets-svc
## Build
```
cd bestbets-api

# install NuGet packages
dotnet restore 
# builds all projects (test are dependent on src)
dotnet build
# runs unit tests (only in tests folder)
dotnet test test/**
# Run code coverage 
# (../../lcov is because this will run from the test project...)
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=lcov /p:CoverletOutput=../../lcov test/**
# Publish to publish directory
dotnet publish -c Release -o ../../publish src/NCI.OCPL.Api.BestBets
```


gratuitous change 
