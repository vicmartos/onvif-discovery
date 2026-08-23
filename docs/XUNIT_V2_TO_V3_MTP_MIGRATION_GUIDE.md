# Migration Guide: xUnit v2 with VSTest to xUnit v3 with Microsoft Testing Platform (MTP) v2

## Table of Contents

- [Overview](#overview)
- [Why Migrate?](#why-migrate)
- [Target Configuration](#target-configuration)
- [Migration Steps](#migration-steps)
  - [1. Project File Changes](#1-project-file-changes)
  - [2. Code Changes](#2-code-changes)
  - [3. CI/CD Pipeline Changes](#3-cicd-pipeline-changes)
- [Package Release 4.0.0 Considerations](#package-release-400-considerations)
- [Command-Line Reference](#command-line-reference)
- [Troubleshooting](#troubleshooting)

---

## Overview

This guide migrates a test project from **xUnit v2** with VSTest to **xUnit v3** with Microsoft Testing Platform (MTP) v2. The recommended target package is:

```xml
<PackageReference Include="xunit.v3" Version="4.0.0" />
```

`4.0.0` is a major package release of **xUnit v3**; it is not a new xUnit v4 framework generation. The migration still changes the test framework from xUnit v2 to xUnit v3.

The migration includes package and project configuration updates, any required xUnit v3 code adaptations, and MTP-compatible CI/CD changes.

## Why Migrate?

Microsoft Testing Platform (MTP) is the modern .NET test platform. It supports `dotnet test`, Test Explorer, standalone test executables, coverage extensions, and reporting extensions.

xUnit v3 provides standalone test projects, modern async support, improved test configuration, and native MTP support. Targeting package release 4.0.0 also makes MTP v2 the default implementation.

## Target Configuration

For a .NET 10 MTP-only test project, use an executable test project and select MTP in `global.json`:

```json
{
  "test": {
    "runner": "Microsoft.Testing.Platform"
  }
}
```

The relevant package names are:

| Purpose | xUnit v2 | xUnit v3 target |
|---|---|---|
| Test framework | `xunit` | `xunit.v3` 4.0.0 |
| Explicit MTP v2 selection | N/A | `xunit.v3.mtp-v2` 4.0.0 (optional) |
| VSTest adapter | `xunit.runner.visualstudio` | Only retain when VSTest fallback is required |
| VSTest SDK support | `Microsoft.NET.Test.Sdk` | Only retain when VSTest fallback is required |
| Coverage | `coverlet.msbuild` | `Microsoft.Testing.Extensions.CodeCoverage` |

In `xunit.v3` 3.x, MTP v1 was the default, so `xunit.v3.mtp-v2` was necessary to choose MTP v2 explicitly. Starting with package release 4.0.0, MTP v1 support was removed and `xunit.v3` defaults to MTP v2. `xunit.v3.mtp-v2` 4.0.0 remains valid when the explicit package name is preferred.

MTP support is native to xUnit v3. Remove `xunit.runner.visualstudio` and `Microsoft.NET.Test.Sdk` only after confirming every supported developer environment and CI runner supports MTP. Keep both packages if older VSTest-only tooling must continue to work.

## Migration Steps

### 1. Project File Changes

Update the test project to be an executable and reference xUnit v3 4.0.0:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <OutputType>Exe</OutputType>
    <IsPackable>false</IsPackable>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="xunit.v3" Version="4.0.0" />
    <PackageReference Include="Microsoft.Testing.Extensions.CodeCoverage" Version="18.*" />
  </ItemGroup>
</Project>
```

For SDK 10 and later, the `global.json` setting shown above enables MTP for `dotnet test`. For SDK 8 or 9, set `TestingPlatformDotnetTestSupport` to `true`; `UseMicrosoftTestingPlatformRunner` is optional when the MTP command-line experience is wanted.

```xml
<PropertyGroup>
  <TestingPlatformDotnetTestSupport>true</TestingPlatformDotnetTestSupport>
  <UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>
</PropertyGroup>
```

### 2. Code Changes

Apply the xUnit v2-to-v3 changes described in the official migration guide. Common changes include removing `Xunit.Abstractions`, updating `IAsyncLifetime`, using `TestContext.Current` for cancellation, modernizing theory data, and removing `async void` tests.

Before updating to package release 4.0.0, review code that implements xUnit extensibility APIs, custom runners, discoverers, orderers, result writers, or assertion extensions. Ordinary `[Fact]`, `[Theory]`, `TheoryDataRow`, and `TestContext.Current.CancellationToken` usage does not require a source change for this package upgrade.

### 3. CI/CD Pipeline Changes

Use MTP coverage and xUnit report options in CI:

```yaml
- task: DotNetCoreCLI@2
  displayName: Test
  inputs:
    command: custom
    custom: test
    arguments: >-
      --configuration $(buildConfiguration)
      --results-directory $(Agent.TempDirectory)
      --coverage
      --coverage-output-format xml
      --coverage-output coverage.xml
      --report-xunit-trx
      --report-xunit-trx-filename test-results.trx

- task: PublishTestResults@2
  inputs:
    testResultsFormat: VSTest
    testResultsFiles: '$(Agent.TempDirectory)/*.trx'
    mergeTestResults: true
```

For SonarCloud/SonarQube, point `sonar.cs.vscoveragexml.reportsPaths` at the generated XML coverage file.

## Package Release 4.0.0 Considerations

Package release 4.0.0 updates the MTP v2 packages to MTP 2.3.3 and removes MTP v1 support. It also adds Native AOT support, full test parallelization, class and method orderers, fixture lifecycle notifications, new filtering options, and assertion improvements. These capabilities are optional; do not enable Native AOT or full test parallelization as part of a dependency-only migration.

Most breaking changes affect extensibility code. Review the following only if the test suite uses the associated APIs:

- Replace obsolete `CollectionBehavior` parallelization properties with `ParallelizationAttribute` properties (`Mode`, `MaxThreads`, and `Algorithm`).
- Update custom runner, discoverer, orderer, result-writer, and assertion-extension implementations for their revised or obsolete xUnit APIs.
- Replace the removed XSL-T `TransformFactory` path with registered console or MTP result writers when custom reports are used.
- Update custom theory discoverers and test-runner constructors for their new parallelization and scheduling parameters.

For a conventional test suite without custom xUnit extensibility, package release 4.0.0 should require no test-source migration beyond the existing xUnit v2-to-v3 changes.

## Command-Line Reference

```bash
# Run all tests through MTP
dotnet test

# Run with coverage
dotnet test -- --coverage --coverage-output-format xml

# Run a specific class
dotnet test -- --filter-class YourNamespace.YourTestClass

# Run with a TRX report
dotnet test -- --report-xunit-trx --report-xunit-trx-filename results.trx
```

Reports are written to the MTP results directory. With package release 4.0.0, xUnit-owned report switches use an `xunit` prefix to avoid collisions:

| Before 4.0.0 | In 4.0.0 |
|---|---|
| `--report-ctrf` | `--report-xunit-ctrf` |
| `--report-ctrf-filename` | `--report-xunit-ctrf-filename` |
| `--report-junit` | `--report-xunit-junit` |
| `--report-junit-filename` | `--report-xunit-junit-filename` |
| `--report-nunit` | `--report-xunit-nunit` |
| `--report-nunit-filename` | `--report-xunit-nunit-filename` |
| `--report-xunit` | `--report-xunit-xml` |
| `--report-xunit-filename` | `--report-xunit-xml-filename` |

`--report-xunit-html` and `--report-xunit-trx` (and their filename switches) are unchanged.

## Troubleshooting

### `dotnet test` still uses VSTest

For .NET 10 and later, ensure `global.json` selects `Microsoft.Testing.Platform`. For SDK 8 or 9, set `TestingPlatformDotnetTestSupport` to `true` in the project.

If an older IDE or tool requires VSTest, restore the `xunit.runner.visualstudio` and `Microsoft.NET.Test.Sdk` package references instead of disabling MTP.

### Coverage report is not generated

Verify that `Microsoft.Testing.Extensions.CodeCoverage` is referenced, `--coverage` is passed, and `--results-directory` is set when CI expects a specific output location.

### MTP v1 versus MTP v2 confusion

Use `xunit.v3` 4.0.0 for the default MTP v2 configuration. Use `xunit.v3.mtp-v2` 4.0.0 only when the MTP v2 selection must be explicit. Do not attempt to use MTP v1 with package release 4.0.0; it is unsupported.

## References

- [Migrating Unit Tests from xUnit.net v2 to v3](https://xunit.net/docs/getting-started/v3/migration)
- [Microsoft Testing Platform with xUnit.net v3](https://xunit.net/docs/getting-started/v3/microsoft-testing-platform)
- [xUnit.net v3 package release 4.0.0](https://xunit.net/releases/v3/4.0.0)
- [Code Coverage with MTP](https://xunit.net/docs/getting-started/v3/code-coverage-with-mtp)
- [Microsoft Testing Platform extensions](https://learn.microsoft.com/dotnet/core/testing/unit-testing-platform-extensions)
