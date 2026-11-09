using System.Runtime.InteropServices;

namespace LinkUi;

/// <summary>Win32 calls for input ImGui doesn't see, such as clicks on a click-through window.</summary>
internal static partial class Native
{
    /// <summary>The left mouse button's virtual key.</summary>
    internal const int VkLButton = 0x01;

    /// <summary>The key's state; the high bit (<c>0x8000</c>) is set while it's held.</summary>
    [LibraryImport("user32.dll")]
    internal static partial short GetKeyState(int virtualKey);

    /// <summary>True while the left mouse button is held.</summary>
    internal static bool LeftButtonDown() => (GetKeyState(VkLButton) & 0x8000) != 0;
}
