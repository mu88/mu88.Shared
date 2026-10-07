using FluentAssertions;
using mu88.Shared.Testing.SystemTests;

namespace Tests.Unit;

[TestFixture]
[Category("Unit")]
public class SystemTestsBaseTests
{
    [Test]
    public void Setup_ShouldInitializeACancellableToken()
    {
        // Arrange
        var sut = new TestSystemTests();

        // Act
        sut.Setup();

        // Assert
        sut.PublicCancellationToken.CanBeCanceled.Should().BeTrue();
        sut.PublicCancellationToken.IsCancellationRequested.Should().BeFalse();
    }

    [Test]
    public void Timeout_ShouldDefaultToOneMinute() => new TestSystemTests().PublicTimeout.Should().Be(TimeSpan.FromMinutes(1));

    [Test]
    public void BuildAppBaseAddress_ShouldCombineHostnamePortAndSubPath() =>
        new TestSystemTests().PublicBuildAppBaseAddress("localhost", 12345).Should().Be(new Uri("http://localhost:12345/sub-path"));

    [Test]
    public async Task CleanupAdditionalResourcesAsync_ShouldDoNothing_ByDefault() =>
        await FluentActions.Awaiting(() => new TestSystemTests().PublicCleanupAdditionalResourcesAsync(CancellationToken.None)).Should().NotThrowAsync();

    private sealed class TestSystemTests : SystemTestsBase
    {
        public CancellationToken PublicCancellationToken => CancellationToken;

        public TimeSpan PublicTimeout => Timeout;

        protected override string SubPath => "/sub-path";

        public Uri PublicBuildAppBaseAddress(string hostname, int mappedPort) => BuildAppBaseAddress(hostname, mappedPort);

        public Task PublicCleanupAdditionalResourcesAsync(CancellationToken cancellationToken) => CleanupAdditionalResourcesAsync(cancellationToken);
    }
}
