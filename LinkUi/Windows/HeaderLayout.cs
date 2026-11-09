namespace LinkUi;

/// <summary>What the header has to fit, in pixels at the metrics' scale.</summary>
internal readonly record struct HeaderInput(float Width, float ButtonSize, float IconWidth, bool HasIcon, float TitleWidth, float SubtitleWidth,
    int PluginActions, bool ShowPin, bool ShowClose, bool ClickThrough, float PillWidth, Metrics M);

/// <summary>Where the plugin actions go and how much of the title, subtitle and click-through pill shows.</summary>
internal readonly record struct HeaderResult(bool ActionsInHeader, bool ActionsInMenu, float TitleShown, float SubtitleShown, bool ShowButtons, bool ShowPill, float PillShown);

/// <summary>
/// The header's fold and clip rules. Pin, options and close always fit; the plugin actions fold into the
/// options menu once they would squeeze the title below its minimum, and the subtitle clips before the title.
/// In click-through mode only the pill shows.
/// </summary>
internal static class HeaderLayout
{
    public static HeaderResult Compute(in HeaderInput input)
    {
        var m = input.M;
        var left = m.HeaderPadLeft + (input.HasIcon ? input.IconWidth + m.HeaderGap : 0);
        bool actionsInHeader = false, actionsInMenu = false, showButtons, showPill;
        float pillShown, space;
        if (input.ClickThrough)
        {
            showButtons = false;
            showPill = true;
            pillShown = MathF.Min(input.PillWidth, MathF.Max(0, input.Width - left - m.HeaderPadRight));
            space = input.Width - left - pillShown - m.HeaderGap - m.HeaderPadRight;
        }
        else
        {
            showButtons = true;
            showPill = false;
            pillShown = 0;
            var buttons = (input.ShowPin ? 1 : 0) + 1 + (input.ShowClose ? 1 : 0);
            var fixedWidth = buttons * input.ButtonSize + (buttons - 1) * m.HeaderButtonGap + m.HeaderPadRight;
            var actions = input.PluginActions > 0 ? input.PluginActions * (input.ButtonSize + m.HeaderButtonGap) + 2 * m.SeparatorMargin + 1 : 0;
            var withActions = input.Width - left - fixedWidth - actions - m.HeaderGap;
            actionsInHeader = input.PluginActions > 0 && withActions >= MathF.Min(input.TitleWidth, m.MinTitleWidth);
            actionsInMenu = input.PluginActions > 0 && !actionsInHeader;
            space = actionsInHeader ? withActions : input.Width - left - fixedWidth - m.HeaderGap;
        }
        space = MathF.Max(0, space);
        var titleShown = MathF.Min(input.TitleWidth, space);
        var subtitleShown = input.SubtitleWidth > 0 ? Math.Clamp(space - titleShown - m.HeaderGap, 0, input.SubtitleWidth) : 0;
        return new HeaderResult(actionsInHeader, actionsInMenu, titleShown, subtitleShown, showButtons, showPill, pillShown);
    }

    /// <summary>
    /// The narrowest width at which <see cref="Compute"/> shows the full title and subtitle and keeps every
    /// plugin action in the header (in click-through mode, the full pill). <see cref="HeaderInput.Width"/> is ignored.
    /// </summary>
    public static float NaturalWidth(in HeaderInput input)
    {
        var m = input.M;
        var left = m.HeaderPadLeft + (input.HasIcon ? input.IconWidth + m.HeaderGap : 0);
        var text = input.TitleWidth + (input.SubtitleWidth > 0 ? m.HeaderGap + input.SubtitleWidth : 0);
        if (input.ClickThrough)
            return left + text + m.HeaderGap + input.PillWidth + m.HeaderPadRight;
        var buttons = (input.ShowPin ? 1 : 0) + 1 + (input.ShowClose ? 1 : 0);
        var fixedWidth = buttons * input.ButtonSize + (buttons - 1) * m.HeaderButtonGap + m.HeaderPadRight;
        var actions = input.PluginActions > 0 ? input.PluginActions * (input.ButtonSize + m.HeaderButtonGap) + 2 * m.SeparatorMargin + 1 : 0;
        return left + text + m.HeaderGap + actions + fixedWidth;
    }
}
