using KianaBH.Data;
using KianaBH.Database;
using KianaBH.KianaBH.Tool;
using KianaBH.GameServer.Command;
using KianaBH.GameServer.Server;
using KianaBH.Internationalization;
using KianaBH.KcpSharp;
using KianaBH.Util;
using System.Globalization;

namespace KianaBH.KianaBH.Program;

public class KianaBH
{
    public static readonly Logger Logger = new("KianaBH");
    public static readonly DatabaseHelper DatabaseHelper = new();
    public static readonly Listener Listener = new();
    public static readonly CommandManager CommandManager = new();

    public static async Task Main()
    {
        var time = DateTime.Now;
        RegisterExitEvent();
        IConsole.InitConsole();
        LoaderManager.InitConfig();
        await LoaderManager.InitSdkServer();
        LoaderManager.InitPacket();

        LoaderManager.InitDatabase();
        if (!DatabaseHelper.LoadAllData)
        {
            var t = Task.Run(() =>
            {
                while (!DatabaseHelper.LoadAllData) // wait for all data to be loaded
                    Thread.Sleep(100);
            });

            await t.WaitAsync(new CancellationToken());

            Logger.Info(I18NManager.Translate("Server.ServerInfo.LoadedItem", I18NManager.Translate("Word.Database")));
        }

        Logger.Warn(I18NManager.Translate("Server.ServerInfo.WaitForAllDone"));

        await LoaderManager.InitResource();
        ResourceManager.IsLoaded = true;

        LoaderManager.InitCommand();
        HandbookGenerator.GenerateAll();

        var elapsed = DateTime.Now - time;
        Logger.Info(I18NManager.Translate("Server.ServerInfo.ServerStarted",
            Math.Round(elapsed.TotalSeconds, 2).ToString(CultureInfo.InvariantCulture)));

        IConsole.ListenConsole();

    }

    # region Exit

    private static void RegisterExitEvent()
    {
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            Logger.Info(I18NManager.Translate("Server.ServerInfo.Shutdown"));
            ProcessExit();
        };
        AppDomain.CurrentDomain.UnhandledException += (obj, arg) =>
        {
            Logger.Error(I18NManager.Translate("Server.ServerInfo.UnhandledException", obj.GetType().Name),
                (Exception)arg.ExceptionObject);
            Logger.Info(I18NManager.Translate("Server.ServerInfo.Shutdown"));
            ProcessExit();
            Environment.Exit(1);
        };

        Console.CancelKeyPress += (_, eventArgs) =>
        {
            Logger.Info(I18NManager.Translate("Server.ServerInfo.CancelKeyPressed"));
            eventArgs.Cancel = true;
            Environment.Exit(0);
        };
    }

    private static void ProcessExit()
    {
        KcpListener.Connections.Values.ToList().ForEach(x => x.Stop(true));
        DatabaseHelper.SaveThread?.Interrupt();
        DatabaseHelper.SaveDatabase();
    }

    # endregion
}