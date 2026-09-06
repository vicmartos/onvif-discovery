# Onvif Discovery

[![NuGet](https://img.shields.io/nuget/v/OnvifDiscovery.svg?style=flat-square)](https://www.nuget.org/packages/OnvifDiscovery/)
[![GitHub CI](https://github.com/vicmaeg/onvif-discovery/actions/workflows/ci.yml/badge.svg)](https://github.com/vicmaeg/onvif-discovery/actions/workflows/ci.yml)
[![Azure Pipelines](https://dev.azure.com/vmaeg/onvif-discovery/_apis/build/status/vicmaeg.onvif-discovery?branchName=master)](https://dev.azure.com/vmaeg/onvif-discovery/_build/latest?definitionId=3&branchName=master)
[![Quality Gate](https://sonarcloud.io/api/project_badges/measure?project=vicmaeg_onvif-discovery&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=vicmaeg_onvif-discovery)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=vicmaeg_onvif-discovery&metric=coverage)](https://sonarcloud.io/summary/new_code?id=vicmaeg_onvif-discovery)
[![Code Smells](https://sonarcloud.io/api/project_badges/measure?project=vicmaeg_onvif-discovery&metric=code_smells)](https://sonarcloud.io/summary/new_code?id=vicmaeg_onvif-discovery)

OnvifDiscovery is a small, cross-platform .NET library for discovering ONVIF-compliant devices with WS-Discovery. It probes every eligible IPv4 Ethernet and Wi-Fi interface and streams devices as they reply.

The package targets .NET 8 and .NET 10 and has no runtime package dependencies.

## Installation

```bash
dotnet add package OnvifDiscovery
```

## Discover devices

`DiscoverAsync` returns an asynchronous stream. The timeout is measured in seconds; reaching it completes the stream normally.

```csharp
using OnvifDiscovery;

using var cancellation = new CancellationTokenSource();
var discovery = new Discovery();

await foreach (var device in discovery.DiscoverAsync(
                   timeout: 5,
                   cancellationToken: cancellation.Token))
{
    Console.WriteLine($"{device.Mfr} {device.Model} at {device.Address}");

    foreach (var serviceAddress in device.XAddresses)
    {
        Console.WriteLine($"  {serviceAddress}");
    }
}
```

Callers that already use channels can provide their own `ChannelWriter<DiscoveryDevice>`:

```csharp
using System.Threading.Channels;
using OnvifDiscovery;
using OnvifDiscovery.Models;

using var cancellation = new CancellationTokenSource();
var discovery = new Discovery();
var channel = Channel.CreateUnbounded<DiscoveryDevice>();

var discoveryTask = discovery.DiscoverAsync(
    channel.Writer,
    timeout: 5,
    cancellationToken: cancellation.Token);

await foreach (var device in channel.Reader.ReadAllAsync(cancellation.Token))
{
    Console.WriteLine($"{device.Mfr} {device.Model} at {device.Address}");
}

await discoveryTask;
```

External cancellation stops discovery with an `OperationCanceledException`. Discovery failures complete the supplied channel with the same exception and fault the returned task.

## Discovery results

Each `DiscoveryDevice` contains:

- `Address`: IP address that sent the discovery response.
- `XAddresses`: ONVIF service URLs advertised by the device.
- `Types`: advertised ONVIF device types.
- `Mfr` and `Model`: manufacturer and model parsed from the device scopes when present.
- `Scopes`: the complete advertised scope list.

Results seen on multiple network interfaces are de-duplicated by their advertised service addresses.

## Network requirements

WS-Discovery uses IPv4 multicast address `239.255.255.250` on UDP port `3702`. The host and devices must be on a network where multicast traffic is available, and the application must be allowed to send and receive UDP traffic. Only active Ethernet and Wi-Fi interfaces with IPv4 support are used.

## Development

Build the solution:

```bash
dotnet build OnvifDiscovery.sln --configuration Release
```

Run the xUnit v3 test executable:

```bash
dotnet run --project OnvifDiscovery.Tests/OnvifDiscovery.Tests.csproj --configuration Release
```

## License

OnvifDiscovery is licensed under the [MIT License](LICENSE).
