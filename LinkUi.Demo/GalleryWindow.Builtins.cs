using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace LinkUi.Demo;

/// <summary>Every built-in ImGui widget the plugins use, drawn live and again disabled.</summary>
internal sealed partial class GalleryWindow
{
    private static readonly string[] ComboItems = ["First", "Second", "Third"];
    private static readonly string[] TableRows = ["Alpha", "Bravo", "Charlie", "Delta", "Echo"];
    private static readonly string[] TableKinds = ["Model", "Texture", "Material", "Model", "Texture"];
    private static readonly string[] TableSizes = ["12 KB", "2 MB", "3 KB", "48 KB", "512 KB"];
    private static readonly Vector2 ProgressSize = new(240, 0);
    private static readonly Vector2 PanelSize = new(0, 80);

    private bool check = true;
    private int radio;
    private string text = "";
    private float number = 1.5f;
    private int comboIndex;
    private float slider = 0.4f;
    private float drag = 12f;
    private int selectedItem = 1;
    private int selectedRow = 2;
    private Vector3 colour = new(224 / 255f, 164 / 255f, 58 / 255f);

    private void DrawBuiltinsSection()
    {
        SectionTitle("Built-ins");
        ImGui.PushID("live");
        try
        {
            DrawBuiltins();
        }
        finally
        {
            ImGui.PopID();
        }

        ImGui.Spacing();
        ImGui.TextUnformatted("Disabled");
        ImGui.Separator();
        ImGui.PushID("disabled");
        ImGui.BeginDisabled();
        try
        {
            DrawBuiltins();
        }
        finally
        {
            ImGui.EndDisabled();
            ImGui.PopID();
        }
    }

    private void DrawBuiltins()
    {
        ImGui.Button("Button");
        ImGui.SameLine();
        ImGui.SmallButton("Small button");

        ImGui.Checkbox("Checkbox", ref check);
        ImGui.RadioButton("Radio one", ref radio, 0);
        ImGui.SameLine();
        ImGui.RadioButton("Radio two", ref radio, 1);

        ImGui.InputTextWithHint("Input text", "Type here", ref text, 128);
        ImGui.InputFloat("Input float", ref number);

        if (ImGui.BeginCombo("Combo", ComboItems[comboIndex]))
        {
            try
            {
                for (var i = 0; i < ComboItems.Length; i++)
                {
                    if (ImGui.Selectable(ComboItems[i], i == comboIndex))
                        comboIndex = i;
                }
            }
            finally
            {
                ImGui.EndCombo();
            }
        }

        ImGui.SliderFloat("Slider", ref slider, 0f, 1f);
        ImGui.DragFloat("Drag", ref drag, 0.1f, 0f, 100f);

        if (ImGui.BeginTabBar("tabs"))
        {
            try
            {
                DrawTab("Tab one", "The first tab.");
                DrawTab("Tab two", "The second tab.");
                DrawTab("Tab three", "The third tab.");
            }
            finally
            {
                ImGui.EndTabBar();
            }
        }

        if (ImGui.CollapsingHeader("Collapsing header"))
            ImGui.TextUnformatted("Inside the header.");

        if (ImGui.TreeNode("Tree node"))
        {
            try
            {
                ImGui.TextUnformatted("Inside the tree node.");
            }
            finally
            {
                ImGui.TreePop();
            }
        }

        for (var i = 0; i < ComboItems.Length; i++)
        {
            if (ImGui.Selectable(ComboItems[i], i == selectedItem))
                selectedItem = i;
        }

        DrawTable();

        if (ImGui.Button("Open popup"))
            ImGui.OpenPopup("popup");
        if (ImGui.BeginPopup("popup"))
        {
            try
            {
                ImGui.TextUnformatted("A popup.");
            }
            finally
            {
                ImGui.EndPopup();
            }
        }

        ImGui.SameLine();
        ImGui.Button("Hover for a tooltip");
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip("A tooltip.");

        ImGui.ProgressBar(0.6f, ProgressSize, "60%");
        ImGui.ColorEdit3("Colour", ref colour);
        ImGui.Separator();

        var visible = ImGui.BeginChild("panel", PanelSize, true);
        try
        {
            if (visible)
                ImGui.TextUnformatted("A child region in the Panel colour.");
        }
        finally
        {
            ImGui.EndChild();
        }
    }

    private static void DrawTab(string label, string body)
    {
        if (!ImGui.BeginTabItem(label))
            return;
        try
        {
            ImGui.TextUnformatted(body);
        }
        finally
        {
            ImGui.EndTabItem();
        }
    }

    private void DrawTable()
    {
        if (!ImGui.BeginTable("table", 3, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders))
            return;
        try
        {
            ImGui.TableSetupColumn("Name");
            ImGui.TableSetupColumn("Kind");
            ImGui.TableSetupColumn("Size");
            ImGui.TableHeadersRow();
            for (var i = 0; i < TableRows.Length; i++)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                if (ImGui.Selectable(TableRows[i], i == selectedRow, ImGuiSelectableFlags.SpanAllColumns))
                    selectedRow = i;
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(TableKinds[i]);
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(TableSizes[i]);
            }
        }
        finally
        {
            ImGui.EndTable();
        }
    }
}
