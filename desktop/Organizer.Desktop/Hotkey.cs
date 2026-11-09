using System.Globalization;

namespace Organizer.Desktop;

[Flags]
public enum HotkeyModifiers : uint { None = 0, Alt = 1, Ctrl = 2, Shift = 4, Win = 8 }

/// <summary>A global shortcut: modifiers plus one key, as a Win32 virtual-key code (so the keyboard layout does not matter).</summary>
public sealed record Hotkey(HotkeyModifiers Modifiers, uint VirtualKey, string Text)
{
    public const string DefaultText = "Alt+Q";

    /// <summary>Parses "Ctrl+Alt+Space". Null or blank gives the default. Needs at least one modifier and exactly one key.</summary>
    public static bool TryParse(string? text, out Hotkey? hotkey, out string? error)
    {
        hotkey = null;
        error = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            text = DefaultText;
        }

        var modifiers = HotkeyModifiers.None;
        uint? key = null;
        var valid = true;
        foreach (var raw in text.Split('+'))
        {
            var part = raw.Trim();
            switch (part.ToLowerInvariant())
            {
                case "ctrl" or "control": modifiers |= HotkeyModifiers.Ctrl; continue;
                case "alt": modifiers |= HotkeyModifiers.Alt; continue;
                case "shift": modifiers |= HotkeyModifiers.Shift; continue;
                case "win": modifiers |= HotkeyModifiers.Win; continue;
            }
            if (key is not null || KeyCode(part) is not { } code)
            {
                valid = false;
                break;
            }
            key = code;
        }

        if (!valid || key is null || modifiers == HotkeyModifiers.None)
        {
            error = $"Invalid Hotkey \"{text}\": expected at least one modifier (Ctrl, Alt, Shift, Win) plus one key, for example \"Ctrl+Alt+Space\".";
            return false;
        }
        hotkey = new Hotkey(modifiers, key.Value, text.Trim());
        return true;
    }

    private static uint? KeyCode(string name)
    {
        if (name.Length == 1)
        {
            var c = char.ToUpperInvariant(name[0]);
            if (c is (>= 'A' and <= 'Z') or (>= '0' and <= '9'))
            {
                return c;
            }
        }
        if (name.Length is 2 or 3 && name[0] is 'F' or 'f' &&
            int.TryParse(name.AsSpan(1), NumberStyles.None, CultureInfo.InvariantCulture, out var n) && n is >= 1 and <= 24)
        {
            return (uint)(0x70 + n - 1);
        }
        return name.ToLowerInvariant() switch
        {
            "space" => 0x20,
            "enter" or "return" => 0x0D,
            "tab" => 0x09,
            "esc" or "escape" => 0x1B,
            "backspace" => 0x08,
            _ => null,
        };
    }
}
