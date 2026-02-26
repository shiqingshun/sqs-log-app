namespace SqsLogApp.Infrastructure;

[Flags]
internal enum HotkeyModifiers : uint
{
    Alt = 0x0001,
    Control = 0x0002,
    Shift = 0x0004,
    Win = 0x0008
}

internal readonly record struct HotkeyDefinition(HotkeyModifiers Modifiers, Keys Key);

internal static class HotkeyParser
{
    public static bool TryParse(string value, out HotkeyDefinition definition)
    {
        definition = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var tokens = value.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length < 2)
        {
            return false;
        }

        var modifiers = (HotkeyModifiers)0;
        var key = Keys.None;

        foreach (var rawToken in tokens)
        {
            var token = rawToken.Trim();
            switch (token.ToUpperInvariant())
            {
                case "CTRL":
                case "CONTROL":
                    modifiers |= HotkeyModifiers.Control;
                    break;
                case "ALT":
                    modifiers |= HotkeyModifiers.Alt;
                    break;
                case "SHIFT":
                    modifiers |= HotkeyModifiers.Shift;
                    break;
                case "WIN":
                case "WINDOWS":
                    modifiers |= HotkeyModifiers.Win;
                    break;
                default:
                    if (key != Keys.None || !TryParseKey(token, out key))
                    {
                        return false;
                    }
                    break;
            }
        }

        if (modifiers == 0 || key == Keys.None || IsModifierKey(key))
        {
            return false;
        }

        definition = new HotkeyDefinition(modifiers, key);
        return true;
    }

    public static string ToDisplayText(HotkeyDefinition definition)
    {
        var parts = new List<string>(5);
        if (definition.Modifiers.HasFlag(HotkeyModifiers.Win))
        {
            parts.Add("Win");
        }

        if (definition.Modifiers.HasFlag(HotkeyModifiers.Control))
        {
            parts.Add("Ctrl");
        }

        if (definition.Modifiers.HasFlag(HotkeyModifiers.Alt))
        {
            parts.Add("Alt");
        }

        if (definition.Modifiers.HasFlag(HotkeyModifiers.Shift))
        {
            parts.Add("Shift");
        }

        parts.Add(GetKeyDisplayText(definition.Key));
        return string.Join("+", parts);
    }

    private static bool TryParseKey(string token, out Keys key)
    {
        key = Keys.None;
        if (token.Length == 1 && char.IsLetter(token[0]))
        {
            return Enum.TryParse(token.ToUpperInvariant(), out key);
        }

        if (token.Length == 1 && char.IsDigit(token[0]))
        {
            return Enum.TryParse($"D{token}", out key);
        }

        return Enum.TryParse(token, ignoreCase: true, out key);
    }

    private static bool IsModifierKey(Keys key)
        => key is Keys.ControlKey or Keys.ShiftKey or Keys.Menu or Keys.LWin or Keys.RWin;

    private static string GetKeyDisplayText(Keys key)
    {
        if (key is >= Keys.D0 and <= Keys.D9)
        {
            return ((int)key - (int)Keys.D0).ToString();
        }

        return key.ToString().ToUpperInvariant();
    }
}
