using Dalamud.Bindings.ImGui;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace LoggingWayPlugin.Windows
{
    internal static class UIHelpers
    {
        // ── Palette ────────────────────────────────────────────────────────────────
        public static readonly Vector4 ColHeader = new(0.13f, 0.14f, 0.18f, 1f);
        public static readonly Vector4 ColRowEven = new(0.11f, 0.12f, 0.16f, 1f);
        public static readonly Vector4 ColRowOdd = new(0.09f, 0.10f, 0.13f, 1f);
        public static readonly Vector4 ColRowHovered = new(0.20f, 0.22f, 0.30f, 1f);
        public static readonly Vector4 ColAccent = new(0.33f, 0.68f, 1.00f, 1f);   // cyan-blue
        public static readonly Vector4 ColGreen = new(0.35f, 0.85f, 0.55f, 1f);
        public static readonly Vector4 ColYellow = new(1.00f, 0.82f, 0.30f, 1f);
        public static readonly Vector4 ColRed = new(1.00f, 0.38f, 0.38f, 1f);
        public static readonly Vector4 ColMuted = new(0.55f, 0.58f, 0.65f, 1f);
        public static readonly Vector4 ColWhite = new(0.95f, 0.95f, 0.97f, 1f);
        public static string FormatUnixTime(long unixSeconds)
        {
            try
            {
                var dt = DateTimeOffset.FromUnixTimeSeconds(unixSeconds).LocalDateTime;
                return dt.ToString("yyyy-MM-dd HH:mm");
            }
            catch { return unixSeconds.ToString(); }
        }

        /// <summary>A centred metric tile: small label above a large coloured value.</summary>
        public static void BigMetric(string label, string value, Vector4 valueCol, float width)
        {
            Vector2 pos = ImGui.GetCursorScreenPos();

            ImGui.GetWindowDrawList().AddRectFilled(
                pos, pos + new Vector2(width, 56),
                ImGui.ColorConvertFloat4ToU32(ColHeader), 4f);

            float labelW = ImGui.CalcTextSize(label).X;
            ImGui.SetCursorScreenPos(pos + new Vector2((width - labelW) * 0.5f, 6));
            ImGui.TextColored(ColMuted, label);

            float valW = ImGui.CalcTextSize(value).X;
            ImGui.SetCursorScreenPos(pos + new Vector2((width - valW) * 0.5f, 28));
            ImGui.TextColored(valueCol, value);

            // Advance cursor to the right of the tile
            ImGui.SetCursorScreenPos(pos + new Vector2(width + 8, 0));
        }

        /// <summary>Label on the left, right-aligned coloured value.</summary>
        public static void StatRow(string label, string value, Vector4 valueCol)
        {
            float availW = ImGui.GetContentRegionAvail().X;
            ImGui.TextColored(ColMuted, label);
            ImGui.SameLine(availW - ImGui.CalcTextSize(value).X);
            ImGui.TextColored(valueCol, value);
        }

        /// <summary>Colour-coded progress bar for a 0–1 rate value.</summary>
        public static void RateBar(string label, float rate)
        {
            Vector4 barCol = rate >= 0.25f ? ColGreen
                           : rate >= 0.10f ? ColYellow
                           : ColMuted;

            float availW = ImGui.GetContentRegionAvail().X;
            float barW = availW - 110;
            float barH = ImGui.GetTextLineHeight();
            string pctText = $"{rate * 100f:F1}%";

            ImGui.TextColored(ColMuted, label);
            ImGui.SameLine(100);

            Vector2 barPos = ImGui.GetCursorScreenPos();
            float fill = barW * Math.Clamp(rate, 0f, 1f);

            // Track
            ImGui.GetWindowDrawList().AddRectFilled(
                barPos, barPos + new Vector2(barW, barH),
                ImGui.ColorConvertFloat4ToU32(new Vector4(0.18f, 0.18f, 0.22f, 1f)), 3f);

            // Fill
            if (fill > 0f)
                ImGui.GetWindowDrawList().AddRectFilled(
                    barPos, barPos + new Vector2(fill, barH),
                    ImGui.ColorConvertFloat4ToU32(barCol with { W = 0.75f }), 3f);

            // Percentage centred over bar
            float textX = barPos.X + (barW - ImGui.CalcTextSize(pctText).X) * 0.5f;
            ImGui.GetWindowDrawList().AddText(
                new Vector2(textX, barPos.Y),
                ImGui.ColorConvertFloat4ToU32(ColWhite),
                pctText);

            ImGui.SetCursorScreenPos(barPos + new Vector2(0, barH + 2));
        }

        public static string FormatLargeNumber(long n) =>
            n >= 1_000_000 ? $"{n / 1_000_000.0:F2}M"
          : n >= 1_000 ? $"{n / 1_000.0:F1}K"
          : $"{n}";

        public static string FormatDuration(float seconds)
        {
            int m = (int)(seconds / 60);
            int s = (int)(seconds % 60);
            return m > 0 ? $"{m}m{s:D2}s" : $"{s}s";
        }

        public static string JobIdToClassJob(uint jobId)
        {
            var res = Service.DataManager.GetExcelSheet<Lumina.Excel.Sheets.ClassJob>()?.GetRow(jobId).Name.ToString() ?? "JobID not found";
            return res;
        }
        public static string CfcIdToCfcName(uint cfcId)
        {
            var res = Service.DataManager.GetExcelSheet<Lumina.Excel.Sheets.ContentFinderCondition>()?.GetRow(cfcId).Name.ToString() ?? "CFCID not found";
            return res;
        }
    }
}
