using Dalamud.Bindings.ImGui;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using LoggingWayPlugin.Parser;
using LoggingWayPlugin.Parsers;
using LoggingWayPlugin.Providers;
using LoggingWayPlugin.RPC;
using LoggingWayPlugin.Windows;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using static FFXIVClientStructs.ThisAssembly.Git;

namespace LoggingWayPlugin;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;

    private const string CommandName = "/pmycommand";
    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("SamplePlugin");
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }

    private ParsingWindow ParsingWindow { get; init; }

    public readonly PacketHandlersHooks packetHandlersHooks;
    public readonly DamageParser parser = null!;
    public readonly LoggingParser loggingParser = null!;
    public readonly DebugParser debugParser = null!;
    public readonly LoggingwayManager loggingwayManager = null!;
    //public ZoneDownHookManager ZoneDownHooks { get; }
    public Plugin()
    {
        Service.Initialize(PluginInterface);
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Service.Log.Verbose("Initializing Loggingway client...");
        loggingwayManager = new LoggingwayManager(new LoggingwayClientWrapper("http://localhost:8085"));
        // You might normally want to embed resources and load them from the manifest stream
        var goatImagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png");

        Service.Log.Verbose("Initializing Packet Handlers hooks...");
        packetHandlersHooks = new PacketHandlersHooks();
        Service.Log.Verbose("Initializing Parsing module...");
        parser = new DamageParser(packetHandlersHooks,Configuration);
        Service.Log.Verbose("Initializing Logging module...");
        
        debugParser = new DebugParser(packetHandlersHooks);
        
        MainWindow = new MainWindow(this, goatImagePath);
        ParsingWindow = new ParsingWindow(parser,Configuration);
        
        ConfigWindow = new ConfigWindow(this);
        loggingParser = new LoggingParser(packetHandlersHooks, Configuration,loggingwayManager);
        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);
        WindowSystem.AddWindow(ParsingWindow);
        Service.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "A useful message to display in /xlhelp"
        });

        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;


        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;


        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;



    }
    public void Dispose()
    {
        // Unregister all actions to not leak anything during disposal of plugin
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;
        


        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        MainWindow.Dispose();
        debugParser.Dispose();
        ParsingWindow.Dispose();
        parser.Dispose();
        packetHandlersHooks.Dispose();
        loggingParser.Dispose();

        Service.CommandManager.RemoveHandler(CommandName);
    }

    private void OnCommand(string command, string args)
    {
        // In response to the slash command, toggle the display status of our main ui
        MainWindow.Toggle();
    }
    
    public void ToggleConfigUi() => ConfigWindow.Toggle();
    public void ToggleMainUi() => ParsingWindow.Toggle();
}
