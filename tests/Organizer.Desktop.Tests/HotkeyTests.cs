namespace Organizer.Desktop.Tests;

public class HotkeyTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("Alt+Q")]
    [InlineData("alt + q")]
    public void Default_is_Alt_Q(string? text)
    {
        Assert.True(Hotkey.TryParse(text, out var h, out var error), error);
        Assert.Equal(HotkeyModifiers.Alt, h!.Modifiers);
        Assert.Equal((uint)'Q', h.VirtualKey);
    }

    [Fact]
    public void Parses_several_modifiers_and_named_keys()
    {
        Assert.True(Hotkey.TryParse("Ctrl+Alt+Space", out var h, out _));
        Assert.Equal(HotkeyModifiers.Ctrl | HotkeyModifiers.Alt, h!.Modifiers);
        Assert.Equal(0x20u, h.VirtualKey);

        Assert.True(Hotkey.TryParse("Win+Shift+F12", out h, out _));
        Assert.Equal(HotkeyModifiers.Win | HotkeyModifiers.Shift, h!.Modifiers);
        Assert.Equal(0x7Bu, h.VirtualKey);
    }

    [Theory]
    [InlineData("Q")]            // no modifier
    [InlineData("Alt")]          // no key
    [InlineData("Alt+Q+W")]      // two keys
    [InlineData("Alt+Banana")]   // unknown key
    [InlineData("Alt+")]
    [InlineData("Alt+F25")]
    [InlineData("Alt+Й")]        // not a virtual-key name
    public void Invalid_text_gives_an_error(string text)
    {
        Assert.False(Hotkey.TryParse(text, out var h, out var error));
        Assert.Null(h);
        Assert.Contains("Invalid Hotkey", error);
    }

    [Fact]
    public void Reads_the_optional_Hotkey_key_from_the_config_file()
    {
        var dir = Directory.CreateTempSubdirectory("organizer-desktop-test-").FullName;
        try
        {
            var path = Path.Combine(dir, "desktop.json");
            Assert.Null(DesktopSettings.ReadHotkey(path)); // missing file
            File.WriteAllText(path, "{ \"Address\": \"http://x\", \"Secret\": \"s\" }");
            Assert.Null(DesktopSettings.ReadHotkey(path));
            File.WriteAllText(path, "{ \"Hotkey\": \"Ctrl+Alt+Space\" }");
            Assert.Equal("Ctrl+Alt+Space", DesktopSettings.ReadHotkey(path));
            File.WriteAllText(path, "not json");
            Assert.Null(DesktopSettings.ReadHotkey(path));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
