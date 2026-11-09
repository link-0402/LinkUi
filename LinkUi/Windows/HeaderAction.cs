using Dalamud.Interface;

namespace LinkUi;

/// <summary>
/// A plugin button in a <see cref="ToolWindow"/> header, such as support or changelog links. It shows its
/// glyph in the header, or a menu item in the options menu once the window is too narrow.
/// <paramref name="Lit"/> draws the glyph in the accent while it returns true.
/// </summary>
public sealed record HeaderAction(FontAwesomeIcon Icon, string Tooltip, Action OnClick, Func<bool>? Lit = null);
