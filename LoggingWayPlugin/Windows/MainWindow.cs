using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using LoggingWayPlugin.Proto;
using LoggingWayPlugin.RPC;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using System;
using System.Numerics;
using static FFXIVClientStructs.FFXIV.Client.LayoutEngine.LayoutManager;
using static Lumina.Data.Parsing.Layer.LayerCommon;

namespace LoggingWayPlugin.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private readonly Configuration configuration;
    private readonly LoggingwayManager loggingwayManager;
    private readonly MainView mainView;



    // State related stuff
    private static int _selectedIdx = -1;
    private static uint _selectedZoneId = 0;
    private ContentFinderCondition _selectedCfc;
    private string _filter = string.Empty;
    private ExcelSheet<ContentFinderCondition> contents = Service.DataManager.GetExcelSheet<ContentFinderCondition>();
    public MainWindow(Plugin plugin)
        : base("LoggingWayPlugin###LGMAIN1293488I", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
        this.configuration = plugin.Configuration;
        this.plugin = plugin;
        this.loggingwayManager = plugin.loggingwayManager;
        mainView = new MainView(plugin.loggingwayManager);
    }

    public void Dispose() { }

    public override void Draw()
    {
        if (ImGui.BeginTabBar("MainWindow###111dadadadad"))
        {
            DrawMain();
            DrawCharacters(mainView.Characters);
            DrawEncounterBrowser(mainView.Encounters);
            DrawLeaderboardBrowser(mainView.Leaderboard);
            ImGui.EndTabBar();
        }

    }

    public void DrawMain()
    {
        if (!ImGui.BeginTabItem("LoginStatus###LGLOGINSTATUS1111"))
            return;
        if (configuration.EnableLoggingwayIntegration)
        {
            ImGui.Indent();
            ImGui.Text("Login Status:");
            ImGui.SameLine();
            if (loggingwayManager.LoginState == LoggingwayLoginState.NotLoggedIn)
            {
                ImGui.Text("You are not logged in to Loggingway. You may log in to enable automatic report uploads.");
            }
            if (loggingwayManager.LoginState == LoggingwayLoginState.LoggedIn)
            {
                ImGui.TextColored(UIHelpers.ColGreen, "Logged in");
                if (ImGui.Button("Logout"))
                {
                    _ = loggingwayManager.Logout();
                }
            }
            else
            {
                if (ImGui.Button("Login to Loggingway"))
                {
                    _ = loggingwayManager.StartLoginProcedureAsync();
                }
            }
            if (loggingwayManager.LoginState == LoggingwayLoginState.LoggingIn)
            {
                ImGui.Text("Waiting for callback...");
            }
            if (loggingwayManager.LoginState == LoggingwayLoginState.LoggingError)
            {
                ImGui.Text("Error while trying to log in");
                ImGui.Text(loggingwayManager.LoginException);
            }
        }
        else
        {
            ImGui.Text("You've disabled Loggingway integration. Enable it in the settings to upload reports automatically and consults leaderboards");
        }
        ImGui.EndTabItem();
    }

    public void DrawCharacters(OperationState<IReadOnlyList<Character>> characters)
    {
        if (!ImGui.BeginTabItem("Associated Characters###LGASSOCIATEDCHAR222"))
            return;

        ImGui.Text("Those are the characters associated with your Loggingway account. Only reports for those characters will be accepted");
        if (characters.IsLoading)
            ImGui.BeginDisabled();

        if (ImGui.Button("Refresh###CharactersRefresh123"))
        {
            mainView.RefreshCharacters();
        }
        if (characters.IsLoading)
            ImGui.EndDisabled();

        // Status indicator
        ImGui.SameLine();
        switch (characters.Status)
        {
            case OperationStatus.Idle:
                ImGui.TextDisabled("Not loaded");
                break;
            case OperationStatus.Loading:
                ImGui.TextDisabled("Loading...");
                break;
            case OperationStatus.Success:
                ImGui.TextColored(new Vector4(0, 1, 0, 1), $"OK");
                if (characters.LastUpdated.HasValue)
                {
                    ImGui.SameLine();
                    ImGui.TextDisabled($"(updated {characters.LastUpdated.Value.ToLocalTime():HH:mm:ss})");
                }
                break;
            case OperationStatus.Error:
                ImGui.TextColored(new Vector4(1, 0, 0, 1), $"Error: {characters.Error?.Message ?? "Unknown error"}");
                break;
        }

        // Character list
        if (characters.Data != null)
        {
            foreach (var character in characters.Data)
            {
                ImGui.Text($"{character.Name}@{character.Homeworld} - {character.Datacenter}");
            }
        }

        ImGui.EndTabItem();
    }

    public void DrawEncounterBrowser(OperationState<IReadOnlyList<Encounter>> encounters)
    {
        if (!ImGui.BeginTabItem("My Encounters###LGENCOUNTERLISTTAB33333"))
            return;
        // ── Left panel: encounter list ──────────────────────────────────────
        using (ImRaii.Child("EncounterList##LGENCOUNTERLISTCHILD222", new Vector2(260, 0), true))
        {
            DrawEncounterList(encounters);
        }

        ImGui.SameLine();

        // ── Right panel: player stat card ───────────────────────────────────
        using (ImRaii.Child("EncounterDetails##LGCHILDENCOUNTERDETAIL111", new Vector2(0, 0), false))
        {

            if (_selectedIdx >= 0 && encounters.Data is not null && _selectedIdx < encounters.Data.Count)
            {
                var enc = encounters.Data[_selectedIdx];
                DrawPlayerCard(mainView.Breakdown);
            }
            else
            {
                var avail = ImGui.GetContentRegionAvail();
                ImGui.SetCursorPos(new Vector2((avail.X - 240) * 0.5f, avail.Y * 0.45f));
                ImGui.TextColored(UIHelpers.ColMuted, "Select an encounter on the left");
            }

        }

        ImGui.EndTabItem();
    }

    public void DrawEncounterList(OperationState<IReadOnlyList<Encounter>> encounters)
    {
        ImGui.Text("Select an encounter to view its details");
        if (encounters.IsLoading)
            ImGui.BeginDisabled();
        if (ImGui.Button("Refresh###EncountersRefresh123"))
        {
            mainView.RefreshEncounters(_selectedZoneId);
        }
        if (encounters.IsLoading)
            ImGui.EndDisabled();
        // Status indicator
        ImGui.SameLine();
        switch (encounters.Status)
        {
            case OperationStatus.Idle:
                ImGui.TextDisabled("Not loaded");
                break;
            case OperationStatus.Loading:
                ImGui.TextDisabled("Loading...");
                break;
            case OperationStatus.Success:
                ImGui.TextColored(new Vector4(0, 1, 0, 1), $"OK");
                if (encounters.LastUpdated.HasValue)
                {
                    ImGui.SameLine();
                    ImGui.TextDisabled($"(updated {encounters.LastUpdated.Value.ToLocalTime():HH:mm:ss})");
                }
                break;
            case OperationStatus.Error:
                ImGui.TextColored(new Vector4(1, 0, 0, 1), $"Error: {encounters.Error?.Message ?? "Unknown error"}");
                break;
        }
        if (encounters.Data != null)
        {
            for (int i = 0; i < encounters.Data.Count; i++)
            {
                var enc = encounters.Data[i];
                bool isSelected = i == _selectedIdx;
                if (ImGui.Selectable($"{UIHelpers.CfcIdToCfcName(enc.ZoneId)} ({UIHelpers.FormatUnixTime(enc.UploadedAt)})", isSelected))
                {
                    _selectedIdx = i;
                    mainView.FindEncounterBreakdown(enc.EncounterId);
                }
            }
        }
    }

    public void DrawPlayerCard(OperationState<EncounterPlayerBreakdown> breakdown)
    {
        if (breakdown.IsLoading)
            ImGui.BeginDisabled();
        // Status indicator
        switch (breakdown.Status)
        {
            case OperationStatus.Idle:
                ImGui.TextDisabled("Not loaded");
                break;
            case OperationStatus.Loading:
                ImGui.TextDisabled("Loading...");
                break;
            case OperationStatus.Success:
                ImGui.TextColored(new Vector4(0, 1, 0, 1), $"OK");
                if (breakdown.LastUpdated.HasValue)
                {
                    ImGui.SameLine();
                    ImGui.TextDisabled($"(updated {breakdown.LastUpdated.Value.ToLocalTime():HH:mm:ss})");
                }
                break;
            case OperationStatus.Error:
                ImGui.TextColored(new Vector4(1, 0, 0, 1), $"Error: {breakdown.Error?.Message ?? "Unknown error"}");
                break;
        }
        if (breakdown.Data != null)
        {
            var data = breakdown.Data;
            ImGui.Text($"{data.Name} - {UIHelpers.JobIdToClassJob(data.JobId)}");//missing job id here
            UIHelpers.StatRow("Pscore", data.Pscore.ToString("F1"), UIHelpers.ColAccent);
            UIHelpers.StatRow("DPS", data.Dps.ToString("F1"), UIHelpers.ColAccent);
            UIHelpers.StatRow("HPS", data.Hps.ToString("F1"), UIHelpers.ColAccent);
            UIHelpers.StatRow("Total Hits", $"{data.TotalHits}", UIHelpers.ColAccent);
            UIHelpers.StatRow("Duration", UIHelpers.FormatDuration(data.Duration), UIHelpers.ColAccent);
            UIHelpers.StatRow("Damage Total", $"{data.TotalDamage}", UIHelpers.ColAccent);
            UIHelpers.StatRow("Rank", $"{data.Rank} / {data.TotalRanked}", UIHelpers.ColAccent);
        }
        if (breakdown.IsLoading)
            ImGui.EndDisabled();
    }

    public void DrawLeaderboardBrowser(OperationState<IReadOnlyList<LeaderBoardEntry>> leaderboard)
    {
        if (!ImGui.BeginTabItem("Leaderboards###LGBLEADERBOARDTAB44444"))
            return;
        if (leaderboard.Data == null)
        {
            ImGui.Text("Select a duty to see the leaderboard");
        }
        switch (leaderboard.Status)
        {
            case OperationStatus.Idle:
                ImGui.TextDisabled("Not loaded");
                break;
            case OperationStatus.Loading:
                ImGui.TextDisabled("Loading...");
                break;
            case OperationStatus.Success:
                ImGui.TextColored(new Vector4(0, 1, 0, 1), $"OK");
                if (leaderboard.LastUpdated.HasValue)
                {
                    ImGui.SameLine();
                    ImGui.TextDisabled($"(updated {leaderboard.LastUpdated.Value.ToLocalTime():HH:mm:ss})");
                }
                break;
            case OperationStatus.Error:
                ImGui.TextColored(new Vector4(1, 0, 0, 1), $"Error: {leaderboard.Error?.Message ?? "Unknown error"}");
                break;
        }
        //Based on the plugin filter combo in the dalamud console
        //https://github.com/goatcorp/Dalamud/blob/master/Dalamud/Interface/Internal/Windows/ConsoleWindow.cs#L705
        string resolvedName = _selectedCfc.RowId != 0 ? _selectedCfc.Name.ToString() : "Duty name";
        if (ImGui.BeginCombo("Duty Picker", resolvedName, ImGuiComboFlags.HeightLarge))
        {
            var sourceNames = contents.Where(c => c.Name != "")//remove empty or null entries
                              .Where(c => c.Name.ToString().IndexOf(_filter, StringComparison.OrdinalIgnoreCase) != -1)
                              .ToList();
            ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X);
            ImGui.InputTextWithHint("##ContentSearchFilter", "Search duties...", ref _filter, 300);
            ImGui.Separator();

            if (!sourceNames.Any())
            {
                ImGui.Text("No matches found");
            }

            foreach (ContentFinderCondition selectable in sourceNames)
            {
                if (ImGui.Selectable(selectable.Name.ToString(), selectable.RowId == _selectedCfc.RowId))
                {
                    _selectedCfc = selectable;
                    mainView.RefreshLeaderBoard(_selectedCfc.RowId);

                }
            }
            ImGui.EndCombo();
        }
        foreach (var entry in mainView.Leaderboard.Data ?? Array.Empty<LeaderBoardEntry>())
        {
            ImGui.Text($"{entry.Char.Name} - {UIHelpers.JobIdToClassJob(entry.Jobid)} - Pscore: {entry.Psccore:F1} - Rank: {entry.Rank}");
        }
        ImGui.EndTabItem();
    }
}
