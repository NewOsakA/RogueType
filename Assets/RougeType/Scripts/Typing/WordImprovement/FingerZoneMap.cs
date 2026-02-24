using System.Collections.Generic;

public enum FingerZone
{
    LeftPinky,
    LeftRing,
    LeftMiddle,
    LeftIndex,
    RightIndex,
    RightMiddle,
    RightRing,
    RightPinky,
}
public static class FingerZoneMap
{
    private static readonly Dictionary<char, FingerZone> map = new Dictionary<char, FingerZone>()
    {
        // Left Pinky
        {'q', FingerZone.LeftPinky}, {'a', FingerZone.LeftPinky}, {'z', FingerZone.LeftPinky},

        // Left Ring
        {'w', FingerZone.LeftRing}, {'s', FingerZone.LeftRing}, {'x', FingerZone.LeftRing},

        // Left Middle
        {'e', FingerZone.LeftMiddle}, {'d', FingerZone.LeftMiddle}, {'c', FingerZone.LeftMiddle},

        // Left Index
        {'r', FingerZone.LeftIndex}, {'t', FingerZone.LeftIndex},
        {'f', FingerZone.LeftIndex}, {'g', FingerZone.LeftIndex},
        {'v', FingerZone.LeftIndex}, {'b', FingerZone.LeftIndex},

        // Right Index
        {'y', FingerZone.RightIndex}, {'u', FingerZone.RightIndex},
        {'h', FingerZone.RightIndex}, {'j', FingerZone.RightIndex},
        {'n', FingerZone.RightIndex}, {'m', FingerZone.RightIndex},

        // Right Middle
        {'i', FingerZone.RightMiddle}, {'k', FingerZone.RightMiddle},

        // Right Ring
        {'o', FingerZone.RightRing}, {'l', FingerZone.RightRing},

        // Right Pinky
        {'p', FingerZone.RightPinky},
    };

    public static bool TryGetZone(char c, out FingerZone zone)
    {
        c = char.ToLower(c);
        return map.TryGetValue(c, out zone);
    }
}