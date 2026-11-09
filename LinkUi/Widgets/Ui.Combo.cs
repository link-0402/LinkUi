using Dalamud.Bindings.ImGui;

namespace LinkUi;

public static partial class Ui
{
    /// <summary><c>ImGui.Combo</c> that Ctrl + mouse wheel also steps through (<see cref="ComboWheel"/>). True when the index changed.</summary>
    public static bool Combo(string id, ref int index, string[] items)
        => ImGui.Combo(id, ref index, items, items.Length) | ComboWheel.Scroll(ref index, items.Length);
}
