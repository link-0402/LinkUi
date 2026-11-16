# LinkUi

LinkUi is a shared ImGui look for Dalamud plugins: a dense "pro tool" style with neutral greys and one accent colour per plugin. It pushes one style around the plugin's whole draw, so stock ImGui calls pick it up unchanged, and adds a small set of its own widgets and a window header that replaces the title bar.

## Add it to a plugin

1. Add the repo as a submodule: `git submodule add <url> LinkUi`.
2. Reference the library from the plugin's csproj:
   ```xml
   <ProjectReference Include="..\LinkUi\LinkUi\LinkUi.csproj" />
   ```
3. In CI, check out with submodules (`actions/checkout` with `submodules: true`).
4. `LinkUi.dll` ships in the plugin's zip next to the plugin's own DLL.

## Host setup

One `LinkUiHost` per plugin. `LinkUi.Demo/DemoPlugin.cs` is a working example.

1. Create the host first, before any window: `new LinkUiHost(pluginInterface, log, new LinkUiOptions(accent, FontAwesomeIcon.Palette))`. The icon is optional and shows in every header.
2. Route every `UiBuilder.Draw` handler through the host: `host.Draw += windowSystem.Draw` instead of `UiBuilder.Draw +=`. Only what draws through `host.Draw` gets the style.
3. Dispose the host from the plugin's `Dispose`, last, after removing the handlers and windows.

## ToolWindow

Subclass `ToolWindow` instead of Dalamud's `Window` and draw the body in `DrawContent()`.

- `Subtitle`: muted text after the title, such as the open file; null for none.
- `HeaderActions`: the plugin's header buttons, as `HeaderAction(icon, tooltip, onClick, lit)`. They move into the options menu when the window is narrow.
- `ContentFlags`: menu bar and scroll flags for the content area. Content flags set on `Flags` move here on the next frame, except `NoScrollbar` and `NoScrollWithMouse`, which stay on the outer window; with `AlwaysAutoResize` there's no content area and `ContentFlags` is ignored.
- `OnPreDraw()` and `OnPostDraw()` replace `PreDraw` and `PostDraw`, which `ToolWindow` seals.
- Override `DrawExtraOptions` to add your own rows to the options menu.
- Dalamud's `TitleBarButtons` and its own window options menu (opacity, blur override) don't show on a `ToolWindow`.

The header shows the host's icon, the title (the window name before `##`), the subtitle, the plugin actions, then pin, options and close. The options menu holds Pin window, Make click-through, Stay on top and Opacity, and a window can add its own rows. While click-through is on, a pill in the header turns it off when clicked.

Pin, click-through, stay on top and opacity are saved per window id (the name after `###`, or the whole name) in `linkui-windows.json` in the plugin's config folder. The stored values win when the window first opens, so set them through the window after that.

## Widgets

All in the static `Ui` class unless noted. Widgets need the host: call them only from inside `host.Draw`.

| Widget | What it is |
|---|---|
| `PrimaryButton` | Filled accent button for the main action, with an optional icon. |
| `DangerButton` | Red-tinted button for actions that throw work away. |
| `IconButton` | Square glyph button on a control fill. |
| `GhostIconButton` | Square glyph button with no fill until hovered, for toolbars. |
| `ToggleButton` | Button that stays pressed while on. |
| `Chip` | Filter chip with a count and optional colour swatches. |
| `Combo` | `ImGui.Combo` that Ctrl + mouse wheel also steps through. |
| `ComboWheel` | Ctrl + mouse wheel stepping for any closed combo (`Scroll`, `Steps`, `Step`, `Pick`). |
| `TabStrip` | Row of flat segments with an accent underline on the active one. |
| `Slider` | Track filled up to the value, with the value centred on it. |
| `SearchBox` | Search field with a clear button; Ctrl+F focuses it. |
| `Splitter` | Drag handle between two panes. |
| `SectionHeader` | Muted heading with an optional detail and a hairline. |
| `PropertyRow` | Muted label in the gutter; the next item fills the row. |
| `Badge` | Small tinted tag. |
| `StatusDot` | Coloured dot, then "name: value". |
| `Banner` | Tinted box with a `Severity` glyph and wrapped text. |
| `EmptyState` | Centred glyph, title and hint for an empty list. |
| `Spinner` | Turning arc for background work. |
| `Hint`, `HintWrapped`, `Muted` | Text in the hint or muted colour. |
| `Tooltip` | Tooltip for the last item, in the tooltip colours. |

Colours are on `Theme.Current.Palette` and sizes on `Theme.Current.Metrics`. For a local change, use `using var _ = Style.Override().Colour(ImGuiCol.Text, p.Muted);`. `Fonts.Title()` and `Fonts.Heading()` push the larger fonts; dispose to pop.

## Known limits

- ImGui's `Header*` colours stay grey, so selected selectables, table rows and collapsing headers are grey, not the accent.
- Plain `ImGui.SetTooltip` keeps the popup colour; use `Ui.Tooltip` for the tooltip colours.
- Style vars the host doesn't push follow the user's Dalamud style: `DisabledAlpha`, `ButtonTextAlign`, `SelectableTextAlign`, `TabBorderSize`, `WindowMinSize` and `SeparatorTextBorderSize`.
