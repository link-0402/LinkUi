namespace LinkUi;

/// <summary>
/// Remembers one text's measured width. <see cref="Get"/> measures again only when the text (by ordinal
/// value) or the font size changes, so a scale or font change re-measures and an unchanged frame doesn't.
/// A mutable struct: keep it in a non-readonly field or an array element, since a copy re-measures every frame.
/// </summary>
internal struct WidthSlot
{
    private string? text;
    private float fontSize;
    private float width;

    public float Get(string text, float fontSize, Func<string, float> measure)
    {
        if (this.text == null || this.fontSize != fontSize || !string.Equals(this.text, text, StringComparison.Ordinal))
        {
            width = measure(text);
            this.text = text;
            this.fontSize = fontSize;
        }
        return width;
    }
}
