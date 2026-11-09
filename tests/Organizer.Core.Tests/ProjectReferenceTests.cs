using System.Xml.Linq;

namespace Organizer.Core.Tests;

public class ProjectReferenceTests
{
    [Fact]
    public void Core_references_only_Contracts()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Organizer.sln")))
            dir = dir.Parent;
        Assert.NotNull(dir);

        var csproj = Path.Combine(dir.FullName, "core", "Organizer.Core", "Organizer.Core.csproj");
        var project = XDocument.Load(csproj);

        var projectRefs = project.Descendants("ProjectReference")
            .Select(e => Path.GetFileNameWithoutExtension(((string?)e.Attribute("Include") ?? "").Replace('\\', '/')))
            .ToList();
        Assert.Equal(new[] { "Organizer.Contracts" }, projectRefs);

        var organizerPackages = project.Descendants("PackageReference")
            .Select(e => (string?)e.Attribute("Include") ?? "")
            .Where(n => n.StartsWith("Organizer.", StringComparison.Ordinal));
        Assert.Empty(organizerPackages);
    }
}
