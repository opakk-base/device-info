using GetDevice.Services;

namespace GetDevice.Tests.Services;

public class SingleInstanceServiceTests
{
    [Fact]
    public void TryAcquire_FirstInstance_ReturnsTrue()
    {
        using var service = new SingleInstanceService();

        var acquired = service.TryAcquire();

        Assert.True(acquired);
        service.Release();
    }

    [Fact]
    public void TryAcquire_SecondInstance_ReturnsFalse()
    {
        using var first = new SingleInstanceService();
        using var second = new SingleInstanceService();

        Assert.True(first.TryAcquire());
        Assert.False(second.TryAcquire());

        first.Release();
    }

    [Fact]
    public void Release_CanAcquireAgainAfterRelease()
    {
        using var first = new SingleInstanceService();
        using var second = new SingleInstanceService();

        Assert.True(first.TryAcquire());
        first.Release();

        Assert.True(second.TryAcquire());
        second.Release();
    }
}
