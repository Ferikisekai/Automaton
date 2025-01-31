using Automaton.FeaturesSetup;
using Automaton.IPC;
using Automaton.UI;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using ImGuiNET;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Automaton;

public class Automaton : IDalamudPlugin
{
    [PluginService] public static IAddonLifecycle AddonLifecycle { get; private set; }

    public static string Name => "Automaton";
    private const string CommandName = "/automaton";
    internal WindowSystem Ws;
    internal MainWindow MainWindow;
    internal DebugWindow DebugWindow;

    internal static Automaton P;
    internal static DalamudPluginInterface pi;
    public static Configuration Config;

    public List<FeatureProvider> FeatureProviders = [];
    private FeatureProvider provider;
    public IEnumerable<BaseFeature> Features => FeatureProviders.Where(x => !x.Disposed).SelectMany(x => x.Features).OrderBy(x => x.Name);
    internal TaskManager TaskManager;

    public Automaton(DalamudPluginInterface pluginInterface)
    {
        P = this;
        pi = pluginInterface;
        Initialize();
    }

    private void Initialize()
    {
        ECommonsMain.Init(pi, P, ECommons.Module.DalamudReflector, ECommons.Module.ObjectFunctions);

        Ws = new();
        MainWindow = new();
        DebugWindow = new();
        Ws.AddWindow(MainWindow);
        Ws.AddWindow(DebugWindow);
        TaskManager = new();
        Config = pi.GetPluginConfig() as Configuration ?? new Configuration();
        Config.Initialize(Svc.PluginInterface);

        Svc.Commands.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = $"Opens the {Name} menu.",
            ShowInHelp = true
        });

        PandorasBoxIPC.Init();
        QoLBarIPC.Init();

        Svc.PluginInterface.UiBuilder.Draw += Ws.Draw;
        Svc.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
        Common.Setup();
        provider = new FeatureProvider(Assembly.GetExecutingAssembly());
        provider.LoadFeatures();
        FeatureProviders.Add(provider);
#if DEBUG
        MainWindow.IsOpen = !MainWindow.IsOpen;
#endif
    }

    public void Dispose()
    {
        Svc.Commands.RemoveHandler(CommandName);
        foreach (var f in Features.Where(x => x is not null && x.Enabled))
        {
            f.Disable();
        }

        provider.UnloadFeatures();

        PandorasBoxIPC.Dispose();
        QoLBarIPC.Dispose();

        Svc.PluginInterface.UiBuilder.Draw -= Ws.Draw;
        Svc.PluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUI;
        Ws.RemoveAllWindows();
        MainWindow = null;
        DebugWindow = null;
        Ws = null;
        ECommonsMain.Dispose();
        FeatureProviders.Clear();
        Common.Shutdown();
        P = null;
    }

    private void OnCommand(string command, string args)
    {
        if (args is "debug" or "d" && Config.showDebugFeatures)
        {
            DebugWindow.IsOpen = !DebugWindow.IsOpen;
            return;
        }
        MainWindow.IsOpen = !MainWindow.IsOpen;
    }

    public void DrawConfigUI()
    {
        MainWindow.IsOpen = !MainWindow.IsOpen;

        if (Svc.PluginInterface.IsDevMenuOpen && (Svc.PluginInterface.IsDev || Config.showDebugFeatures))
        {
            if (ImGui.BeginMainMenuBar())
            {
                if (ImGui.MenuItem(Name))
                {
                    if (ImGui.GetIO().KeyShift)
                    {
                        DebugWindow.IsOpen = !DebugWindow.IsOpen;
                    }
                    else
                    {
                        MainWindow.IsOpen = !MainWindow.IsOpen;
                    }
                }
                ImGui.EndMainMenuBar();
            }
        }
    }
}

