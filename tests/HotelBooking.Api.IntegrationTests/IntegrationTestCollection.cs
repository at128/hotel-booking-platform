using HotelBooking.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace HotelBooking.Api.IntegrationTests;

/// <summary>
/// xUnit Collection Definition — ensures ALL test classes share a SINGLE WebAppFactory instance.
/// This means only ONE SQL Server container is created for the entire test suite.
/// </summary>
[CollectionDefinition("Integration")]
public class IntegrationTestCollection : ICollectionFixture<WebAppFactory>
{
    // This class has no code and is never created.
    // Its purpose is to be the place to apply [CollectionDefinition]
    // and all the ICollectionFixture<> interfaces.
}
