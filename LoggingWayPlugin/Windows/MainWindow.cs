using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using LoggingWayPlugin.Proto;
using LoggingWayPlugin.RPC;
using Lumina.Excel.Sheets;
using System;
using System.Numerics;
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
        }
        if (breakdown.IsLoading)
            ImGui.EndDisabled();
    }
}
