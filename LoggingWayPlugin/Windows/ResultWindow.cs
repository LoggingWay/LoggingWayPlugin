using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using LoggingWayPlugin.Proto;
using LoggingWayPlugin.RPC;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace LoggingWayPlugin.Windows
{
    // ResultWindow.cs
    public class ResultWindow : Window, IDisposable
    {
        public readonly UploadView view;

        public ResultWindow(LoggingwayManager manager,Configuration config) : base("Upload Result")
        {
            view = new UploadView(manager,this,config);
            Size = new Vector2(380, 220);
            SizeCondition = ImGuiCond.FirstUseEver;
        }

        public override void Draw()
        {
            switch (view.State.Phase)
            {
                case UploadPhase.Idle:
                    ImGui.TextDisabled("No upload in progress.");
                    break;

                case UploadPhase.Queued:
                    ImGui.TextDisabled("Queued at");
                    ImGui.SameLine();
                    ImGui.Text(view.State.QueuedAt?.ToLocalTime().ToString("HH:mm:ss") ?? "-");
                    ImGui.Spacing();
                    ImGui.TextDisabled("Waiting for a worker slot...");
                    break;

                case UploadPhase.Polling:
                    ImGui.TextDisabled("Queued at");
                    ImGui.SameLine();
                    ImGui.Text(view.State.QueuedAt?.ToLocalTime().ToString("HH:mm:ss") ?? "-");
                    ImGui.Spacing();
                    // Spinning indicator
                    var t = (float)(ImGui.GetTime() % 1.0);
                    ImGui.Text($"Processing  {SpinnerFrame(t)}");
                    break;

                case UploadPhase.Done:
                    DrawResult(view.State.Result!);
                    break;

                case UploadPhase.Failed:
                    ImGui.TextColored(new Vector4(1, 0.3f, 0.3f, 1), "Upload failed");
                    ImGui.Spacing();
                    ImGui.TextWrapped(view.State.ErrorMessage ?? "Unknown error.");
                    ImGui.Spacing();
                    if (ImGui.Button("Dismiss"))
                        IsOpen = false;
                    break;
            }
        }

        private static void DrawResult(PollJobResultReply result)
        {
            ImGui.TextColored(new Vector4(0.4f, 1f, 0.4f, 1), "Upload complete");
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            DrawStatRow("Encounter ID", result.EncounterId.ToString());
            DrawStatRow("PScore", result.Pscore.ToString("F2"));
            DrawStatRow("Rank", result.Rank == 0
                                            ? "Unranked"
                                            : $"#{result.Rank} / {result.TotalRanked}");
        }

        private static void DrawStatRow(string label, string value)
        {
            ImGui.TextDisabled(label);
            ImGui.SameLine(120);
            ImGui.Text(value);
        }

        private static char SpinnerFrame(float t) => "|/-\\"[(int)(t * 4) % 4];

        public void Dispose()
        {
            view.Dispose();
        }
    }
}
