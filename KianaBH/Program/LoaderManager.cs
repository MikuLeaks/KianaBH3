using System.Reflection;
using KianaBH.Data;
using KianaBH.Database;
using KianaBH.KianaBH.Tool;
using KianaBH.GameServer.Command;
using KianaBH.GameServer.Server;
using KianaBH.GameServer.Server.Packet;
using KianaBH.Internationalization;
using KianaBH.KcpSharp;
using KianaBH.Proto;
using KianaBH.Util;
using KianaBH.Util.Security;

namespace KianaBH.KianaBH.Program;

public class LoaderManager : KianaBH
{
    public static void InitConfig()
    {
        // Initialize log
        var counter = 0;
        FileInfo file;
        while (true)
        {
            file = new FileInfo(ConfigManager.Config.Path.LogPath + $"/{DateTime.Now:yyyy-MM-dd}-{++counter}.log");
            if (file is not { Exists: false, Directory: not null }) continue;
            file.Directory.Create();
            break;
        }
        Logger.SetLogFile(file);

        // Init all directories
        try
        {
            ConfigManager.InitDirectories();
        }
        catch (Exception e)
        {
            Logger.Error(I18NManager.Translate("Server.ServerInfo.FailedToLoadItem", I18NManager.Translate("Word.Config")), e);
            Console.ReadLine();
            return;
        }

        // Starting the server
        Logger.Info(I18NManager.Translate("Server.ServerInfo.StartingServer"));

        Logger.Info(@"
    __ __ _                   ____  __  __
   / //_/(_)___ _____  ____ _/ __ )/ / / /
  / ,<  / / __ `/ __ \/ __ `/ __  / /_/ / 
 / /| |/ / /_/ / / / / /_/ / /_/ / __  /  
/_/ |_/_/\__,_/_/ /_/\__,_/_____/_/ /_/   
                                          ");

        // Load the config
        Logger.Info(I18NManager.Translate("Server.ServerInfo.LoadingItem", I18NManager.Translate("Word.Config")));
        try
        {
            ConfigManager.LoadConfig();
        }
        catch (Exception e)
        {
            Logger.Error(
                I18NManager.Translate("Server.ServerInfo.FailedToLoadItem", I18NManager.Translate("Word.Config")), e);
            Console.ReadLine();
            return;
        }

        // Load the language
        Logger.Info(I18NManager.Translate("Server.ServerInfo.LoadingItem", I18NManager.Translate("Word.Language")));
        try
        {
            I18NManager.LoadLanguage();
        }
        catch (Exception e)
        {
            Logger.Error(
                I18NManager.Translate("Server.ServerInfo.FailedToLoadItem", I18NManager.Translate("Word.Language")), e);
            Console.ReadLine();
            return;
        }
    }

    public static void InitDatabase()
    {
        // Initialize the database
        try
        {
            _ = Task.Run(DatabaseHelper.Initialize); // do not wait

            while (!DatabaseHelper.LoadAccount) Thread.Sleep(100);

            Logger.Info(I18NManager.Translate("Server.ServerInfo.LoadedItem",
                I18NManager.Translate("Word.DatabaseAccount")));
        }
        catch (Exception e)
        {
            Logger.Error(
                I18NManager.Translate("Server.ServerInfo.FailedToLoadItem", I18NManager.Translate("Word.Database")), e);
            Console.ReadLine();
            return;
        }
    }

    public static async Task InitSdkServer()
    {

        SdkServer.SdkServer.Main([]);
        Logger.Info(I18NManager.Translate("Server.ServerInfo.ServerRunning", I18NManager.Translate("Word.Dispatch"),
            ConfigManager.Config.HttpServer.GetDisplayAddress()));

        KcpListener.BaseConnection = typeof(Connection);
        KcpListener.StartListener();

        await Task.CompletedTask;
    }

    public static void InitPacket()
    {
        // get opcode from CmdIds
        var opcodes = typeof(CmdIds).GetFields().Where(x => x.FieldType == typeof(int)).ToList();
        foreach (var opcode in opcodes)
        {
            var name = opcode.Name;
            var value = (int)opcode.GetValue(null)!;
            KcpConnection.LogMap.TryAdd(value, name);
        }

        HandlerManager.Init(); 
    }

    public static async Task InitResource()
    {
        // Init custom files
        Logger.Info(I18NManager.Translate("Server.ServerInfo.GeneratingItem", I18NManager.Translate("Word.CustomData")));
        try
        {
            await AssemblyGenerater.LoadCustomData(Assembly.GetExecutingAssembly());
        }
        catch (Exception e)
        {
            Logger.Error(
                I18NManager.Translate("Server.ServerInfo.FailedToLoadItem", I18NManager.Translate("Word.CustomData")), e);
            Console.ReadLine();
            return;
        }

        // Load the game data
        try
        {
            Logger.Info(I18NManager.Translate("Server.ServerInfo.LoadingItem", I18NManager.Translate("Word.GameData")));
            ResourceManager.LoadGameData();
        }
        catch (Exception e)
        {
            Logger.Error(
                I18NManager.Translate("Server.ServerInfo.FailedToLoadItem", I18NManager.Translate("Word.GameData")), e);
            Console.ReadLine();
            return;
        }
    }

    /// <summary>
    /// Register all commands and add listeners
    /// </summary>
    public static void InitCommand()
    {
        // Register the command handlers
        try
        {
            CommandManager.RegisterCommands();
        }
        catch (Exception e)
        {
            Logger.Error(
                I18NManager.Translate("Server.ServerInfo.FailedToInitializeItem",
                    I18NManager.Translate("Word.Command")), e);
            Console.ReadLine();
            return;
        }
        IConsole.OnConsoleExcuteCommand += CommandExecutor.ConsoleExcuteCommand;
        CommandExecutor.OnRunCommand += (sender, e) => { CommandManager.HandleCommand(e, sender); };

    }
}