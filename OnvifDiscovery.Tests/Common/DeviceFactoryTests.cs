using System.Net;
using OnvifDiscovery.Common;
using OnvifDiscovery.Models;
using OnvifDiscovery.Tests.TestHelpers;
using Shouldly;
using Xunit;

namespace OnvifDiscovery.Tests.Common;

public class DeviceFactoryTests
{
    private static readonly string[] deviceTypes = { "dn:NetworkVideoTransmitter", "tds:Device" };
    private static readonly string[] xAddresses =
    {
        "https://192.168.1.1/onvif/device_service",
        "https://192.168.1.2/onvif/device_service"
    };
    private const string IpLoopback = "127.0.0.1";

    [Theory]
    [InlineData("onvif://www.onvif.org/hardware/Camera%20One onvif://www.onvif.org/type/video_encoder", "Camera One", "")]
    [InlineData("onvif://www.onvif.org/type/video_encoder onvif://www.onvif.org/hardware/Camera%20One onvif://www.onvif.org/name/Acme%20Camera", "Camera One", "Acme")]
    [InlineData("onvif://www.onvif.org/type/video_encoder onvif://www.onvif.org/hardware/Camera%20One", "Camera One", "")]
    [InlineData("onvif://www.onvif.org/hardware/Camera%2FOne%3A2", "Camera/One:2", "")]
    [InlineData("onvif://www.onvif.org/mfr/Acme%20Systems", "", "Acme Systems")]
    [InlineData("onvif://www.onvif.org/manufacturer/Acme%20Systems", "", "Acme Systems")]
    [InlineData("onvif://www.onvif.org/acc/mfr/Acme%20Systems", "", "Acme Systems")]
    [InlineData("onvif://www.onvif.org/name/Other%20Camera onvif://www.onvif.org/mfr/Acme%20Systems", "", "Acme Systems")]
    [InlineData("onvif://www.onvif.org/name/Acme%20Camera", "", "Acme")]
    [InlineData("onvif://www.onvif.org/name/acc/Acme%20Camera", "", "Acme")]
    [InlineData("onvif://www.onvif.org/mfr/First onvif://www.onvif.org/manufacturer/Second", "", "First")]
    [InlineData("onvif://www.onvif.org/not-hardware/Wrong onvif://www.onvif.org/not-name/Wrong onvif://www.onvif.org/not-mfr/Wrong onvif://www.onvif.org/not-manufacturer/Wrong", "", "")]
    [InlineData("onvif://hardware/Wrong onvif://name/Wrong onvif://mfr/Wrong", "", "")]
    [InlineData("onvif://www.onvif.org/type/video_encoder?name/Wrong", "", "")]
    [InlineData("onvif://www.onvif.org/type/video_encoder#manufacturer/Wrong", "", "")]
    [InlineData("not-a-uri/hardware/Wrong onvif://[invalid/mfr/Wrong onvif://www.onvif.org/hardware/Valid", "Valid", "")]
    [InlineData("onvif://www.onvif.org/hardware/Bad%ZZ onvif://www.onvif.org/hardware/Valid", "Valid", "")]
    [InlineData("onvif://www.onvif.org/mfr/Bad% onvif://www.onvif.org/name/Acme%20Camera", "", "Acme")]
    [InlineData("onvif://www.onvif.org/hardware/ onvif://www.onvif.org/mfr/ onvif://www.onvif.org/name/", "", "")]
    [InlineData("onvif://www.onvif.org/type/video_encoder", "", "")]
    [InlineData("", "", "")]
    [InlineData(" \t\r\n ", "", "")]
    public void CreateDevice_Parses_Scope_Uris_Independently(string scopes, string expectedModel, string expectedMfr)
    {
        var device = DeviceFactory.CreateDevice(new ProbeMatch { Scopes = scopes }, IPEndPoint.Parse(IpLoopback));

        device.Model.ShouldBe(expectedModel);
        device.Mfr.ShouldBe(expectedMfr);
        device.Scopes.ShouldBe(scopes.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    [Fact]
    public void CreateDevice_Splits_Whitespace_And_Preserves_Original_Scopes()
    {
        var scopes = new[]
        {
            "onvif://www.onvif.org/hardware/Camera%20One",
            "onvif://www.onvif.org/manufacturer/Acme%20Systems",
            "onvif://www.onvif.org/location/Bad%ZZ",
            "not-a-uri"
        };
        var probeMatch = new ProbeMatch { Scopes = " \t" + string.Join(" \t\r\n  ", scopes) + " \n" };

        var device = DeviceFactory.CreateDevice(probeMatch, IPEndPoint.Parse(IpLoopback));

        device.Model.ShouldBe("Camera One");
        device.Mfr.ShouldBe("Acme Systems");
        device.Scopes.ShouldBe(scopes);
    }

    [Theory]
    [ClassData(typeof(ScopesTestDataGenerator))]
    public void CreateDevice_Model_And_Manufacturer_Parsed_Correctly_From_Scopes(string[] scopes,
        string expectedModel, string expectedMfr)
    {
        // Arrange
        var probeMatch = new ProbeMatch
        {
            Scopes = string.Join(" ", scopes),
            XAddrs = string.Empty,
            Types = string.Empty
        };
        var remoteEndpoint = IPEndPoint.Parse(IpLoopback);

        // Act
        var device = DeviceFactory.CreateDevice(probeMatch, remoteEndpoint);

        //Assert
        device.Model.ShouldBe(expectedModel);
        device.Mfr.ShouldBe(expectedMfr);
        device.Scopes.ShouldBe(scopes);
    }

    [Fact]
    public void CreateDevice_Types_Parsed()
    {
        // Arrange
        var types = string.Join(" ", deviceTypes);
        var probeMatch = new ProbeMatch
        {
            Scopes = string.Empty,
            XAddrs = string.Empty,
            Types = types
        };
        var remoteEndpoint = IPEndPoint.Parse(IpLoopback);

        // Act
        var device = DeviceFactory.CreateDevice(probeMatch, remoteEndpoint);

        // Assert
        device.Types.Count().ShouldBe(2);
        device.Types.ShouldBe(deviceTypes);
    }

    [Fact]
    public void CreateDevice_XAddress_Parsed()
    {
        // Arrange
        var xaddress = string.Join(" ", xAddresses);
        var probeMatch = new ProbeMatch
        {
            Scopes = string.Empty,
            XAddrs = xaddress,
            Types = string.Empty
        };
        var remoteEndpoint = IPEndPoint.Parse(IpLoopback);

        // Act
        var device = DeviceFactory.CreateDevice(probeMatch, remoteEndpoint);

        // Assert
        device.XAddresses.Count().ShouldBe(2);
        device.XAddresses.ShouldBe(xAddresses);
    }

    [Fact]
    public void CreateDevice_Address_Parsed()
    {
        // Arrange
        var probeMatch = new ProbeMatch
        {
            Scopes = string.Empty,
            XAddrs = string.Empty,
            Types = string.Empty
        };
        var remoteEndpoint = IPEndPoint.Parse(IpLoopback);

        // Act
        var device = DeviceFactory.CreateDevice(probeMatch, remoteEndpoint);

        // Assert
        device.Address.ShouldBe(IpLoopback);
    }
}