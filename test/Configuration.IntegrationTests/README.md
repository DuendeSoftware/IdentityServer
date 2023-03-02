# Configuration.IntegrationTests README
This file describes how to run the IdentityServer.Configuration integration tests.

## Run Tests
To run tests, execute the following from the `~\test\Configuration.IntegrationTests` directory:

```sh
dotnet test --collect:"XPlat Code Coverage" --settings coverlet.runsettings
```

Each run of the tests will generate cobertura and lcov results in the
`~\test\Configuration.IntegrationTests\TestResults\{guid}\` directory,
generating a new guid for each run.



## Generate Coverage Report
To generate an html coverage report, run the tests and then, again from the
`~\test\Configuration.IntegrationTests` directory, execute the following:

This will generate html in the `~\test\Configuration.IntegrationTests\CoverageReports` directory.

```sh
dotnet build /t:Coverage
```

## Further Work
- Run in the build pipeline

- So far, cleaning up test results hasn't been necessary. ReportGenerator happily
consumes multiple coverage reports without an issue. Will this become an issue
when number of reports grows?
