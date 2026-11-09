using System.Numerics;
using Dalamud.Game.Command;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace LinkUi.Demo;

/// <summary>The LinkUi demo: one Gallery window drawn under the LinkUi host, opened with <c>/linkui</c>.</summary>
public sealed class DemoPlugin : IDalamudPlugin
{
    private const string Command = "/linkui";

    private readonly IDalamudPluginInterface pluginInterface;
    private readonly ICommandManager commands;
    private readonly LinkUiHost host;
    private readonly WindowSystem windowSystem = new("LinkUi.Demo");
    private readonly GalleryWindow gallery;
    private readonly Action toggleGallery;

    public DemoPlugin(IDalamudPluginInterface pluginInterface, ICommandManager commands, IPluginLog log)
    {
        this.pluginInterface = pluginInterface;
        this.commands = commands;
        host = new LinkUiHost(pluginInterface, log,
            new LinkUiOptions(new Vector4(224 / 255f, 164 / 255f, 58 / 255f, 1), FontAwesomeIcon.Palette));
        gallery = new GalleryWindow(log);
        windowSystem.AddWindow(gallery);
        host.Draw += windowSystem.Draw;
        toggleGallery = gallery.Toggle;
        pluginInterface.UiBuilder.OpenMainUi += toggleGallery;
        commands.AddHandler(Command, new CommandInfo(OnCommand) { HelpMessage = "Open the LinkUi Gallery." });
    }

    public void Dispose()
    {
        commands.RemoveHandler(Command);
        pluginInterface.UiBuilder.OpenMainUi -= toggleGallery;
        host.Draw -= windowSystem.Draw;
        windowSystem.RemoveAllWindows();
        host.Dispose();
    }

    private void OnCommand(string command, string args) => gallery.Toggle();
}
