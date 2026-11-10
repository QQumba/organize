namespace Organizer.Desktop.Tests;

public class InstanceNameTests
{
    [Fact]
    public void Same_path_gives_same_name()
    {
        var a = Path.Combine(Path.GetTempPath(), "a", "desktop.json");
        Assert.Equal(InstanceName.MutexName(a), InstanceName.MutexName(a));
        Assert.Equal(InstanceName.MutexName(a), InstanceName.MutexName(a.ToUpperInvariant()));
        Assert.Equal(InstanceName.MutexName(null), InstanceName.MutexName(null));
    }

    [Fact]
    public void Different_paths_give_different_names()
    {
        var a = Path.Combine(Path.GetTempPath(), "a", "desktop.json");
        var b = Path.Combine(Path.GetTempPath(), "b", "desktop.json");
        Assert.NotEqual(InstanceName.MutexName(a), InstanceName.MutexName(b));
        Assert.NotEqual(InstanceName.MutexName(a), InstanceName.MutexName(null));
    }
}
