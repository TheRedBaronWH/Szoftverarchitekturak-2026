using UnityEngine.InputSystem;

// One mapping for both the keyboard handler and the visible platform badges.
public static class PlatformShortcuts
{
    public static readonly Key[] Keys = {
        Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6,
        Key.Digit7, Key.Digit8, Key.Digit9, Key.Digit0, Key.Minus, Key.Equals,
        Key.Q, Key.E, Key.T, Key.Y, Key.U, Key.I, Key.O, Key.P
    };
    private static readonly string[] Captions = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "−", "=", "Q", "E", "T", "Y", "U", "I", "O", "P" };
    public static readonly Key[] NumberPadKeys = { Key.Numpad1,Key.Numpad2,Key.Numpad3,Key.Numpad4,Key.Numpad5,
        Key.Numpad6,Key.Numpad7,Key.Numpad8,Key.Numpad9,Key.Numpad0 };

    public static string Caption(string platform)
    {
        return platform != null && platform.StartsWith("P") && int.TryParse(platform.Substring(1), out int number)
            && number > 0 && number <= Captions.Length ? Captions[number - 1] : platform;
    }

    public static string DisplayCaption(string platform)
    {
        if (Keyboard.current != null && platform != null && platform.StartsWith("P")
            && int.TryParse(platform.Substring(1), out int number) && number > 0 && number <= Keys.Length)
        {
            string text = Keyboard.current[Keys[number-1]].displayName;
            if (!string.IsNullOrEmpty(text)) return text.ToUpperInvariant();
        }
        return Caption(platform);
    }
}
