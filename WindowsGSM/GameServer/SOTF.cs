using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System;
using WindowsGSM.Functions;
using WindowsGSM.DiscordBot;

namespace WindowsGSM.GameServer
{
    internal class SOTF
    {

        // 存储服务器配置数据的对象
        private readonly ServerConfig _serverData;

        // 错误消息和通知消息
        public string Error;
        public string Notice;

        // 服务器全名、启动路径、是否允许嵌入控制台、端口号增量、查询方法等服务器信息
        public const string FullName = "森林之子 专用服务器";
        public string StartPath = "SonsOfTheForestDS.exe";
        // 是否允许嵌入式控制台，端口增量和查询方法等配置信息
        public bool AllowsEmbedConsole = true;
        public int PortIncrements = 1;
        public dynamic QueryMethod = new Query.A2S();

        // 默认配置项：默认地图、端口号、查询端口、默认地图、最大玩家数、额外参数及应用 ID
        public string Port = "8766";//端口号
        public string QueryPort = "27016";//查询端口
        public string BlobSyncPort = "9700";//BlobSync端口
        public string Defaultmap = "Normal";//默认地图
        public string Maxplayers = "8";//服务器人数
        public string Additional = $" -dedicatedserver.BlobSyncPort \"9700\" -dedicatedserver.SkipNetworkAccessibilityTest \"true\""; // 额外的服务器启动参数
        public string AppId = "2465200";//APPID

        // 构造函数，需要传入服务器配置数据对象
        public SOTF(Functions.ServerConfig serverData)
        {
            _serverData = serverData;
        }
        public async void CreateServerCFG()
        {
            // 创建steam_appid.txt文件并写入App ID
            string txtPath = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, "steam_appid.txt");
            File.WriteAllText(txtPath, "1326470");
        }

        // 启动服务器进程
        public async Task<Process> Start()
        {

            string shipExePath = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath);
            if (!File.Exists(shipExePath))
            {
                Error = $"{Path.GetFileName(shipExePath)} 未找到 ({shipExePath})";
                return null;
            }
            //string param = $"-nographics -batchmode -userdatapath \"{ServerPath.GetServersServerFiles(_serverData.ServerID)}\" -dedicatedserver.Password \"{_serverData.Password}\"  {_serverData.ServerParam}" + (!AllowsEmbedConsole ? " -log" : string.Empty);
            string param = String.Empty;
            //param += $" -nographics";//禁用图形界面
            //param += $" -batchmode";//启用批处理模式
            param += $" -batchmode -dedicatedserver.IpAddress \"{_serverData.ServerIP}\"";// 服务器绑定的IP地址
            param += $" -dedicatedserver.GamePort \"{_serverData.ServerPort}\"";// 游戏服务器运行的端口
            param += $" -dedicatedserver.QueryPort \"{_serverData.ServerQueryPort}\"";//用于查询服务器信息的端口
            //param += $" -dedicatedserver.BlobSyncPort \"9700\"";// 用于数据同步的端口，确保服务器之间的数据一致性
            param += $" -dedicatedserver.ServerName \"{_serverData.ServerName}\"";// 游戏服务器的名称
            param += $" -dedicatedserver.MaxPlayers \"{_serverData.ServerMaxPlayer}\"";// 服务器允许的最大玩家数
            param += $" -dedicatedserver.GameMode \"{_serverData.ServerMap}\"";// 服务器的游戏模式
            //param += $" -dedicatedserver.SkipNetworkAccessibilityTest \"true\"";//跳过网络可访问性测试
            //param += $" -dedicatedserver.LogFilesEnabled \"true\"";//开启日志写入
            param += $" -userdatapath \"{ServerPath.GetServersServerFiles(_serverData.ServerID) + "\\Saved"}\"";//存档路径
            param += $" {_serverData.ServerParam}" + (!AllowsEmbedConsole ? " -log" : string.Empty);



            Process p;
            if (!AllowsEmbedConsole)
            {
                p = new Process
                {
                    StartInfo =
                    {
                        WorkingDirectory = ServerPath.GetServersServerFiles(_serverData.ServerID),
                        FileName = shipExePath,
                        Arguments = param,
                        WindowStyle = ProcessWindowStyle.Minimized,
                        UseShellExecute = false
                    },
                    EnableRaisingEvents = true
                };
                p.Start();
            }
            else
            {
                p = new Process
                {
                    StartInfo =
                    {
                        WorkingDirectory = ServerPath.GetServersServerFiles(_serverData.ServerID),
                        FileName = shipExePath,
                        Arguments = param,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    },
                    EnableRaisingEvents = true
                };
                var serverConsole = new Functions.ServerConsole(_serverData.ServerID);
                p.OutputDataReceived += serverConsole.AddOutput;
                p.ErrorDataReceived += serverConsole.AddOutput;
                p.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
            }

            return p;
        }


        // - Stop server function
        public async Task Stop(Process p)
        {
            await Task.Run(() =>
            {
                Functions.ServerConsole.SetMainWindow(p.MainWindowHandle);
                Functions.ServerConsole.SendWaitToMainWindow("^c");
            });
            await Task.Delay(20000);
        }

        // 安装游戏服务端
        public async Task<Process> Install()
        {
            var steamCMD = new Installer.SteamCMD();
            // 使用 SteamCMD 安装服务端
            Process p = await steamCMD.Install(_serverData.ServerID, string.Empty, AppId);
            Error = steamCMD.Error;

            return p;
        }

        // 升级游戏服务端
        public async Task<Process> Update(bool validate = false, string custom = null)
        {
            // 使用 SteamCMD 更新服务端
            var (p, error) = await Installer.SteamCMD.UpdateEx(_serverData.ServerID, AppId, validate, custom: custom);
            Error = error;
            return p;
        }

        // 在服务器文件夹中检查游戏服务端是否已正确安装
        public bool IsInstallValid()
        {
            return File.Exists(Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, "SonsOfTheForestDS.exe"));
        }

        public bool IsImportValid(string path)
        {
            string exePath = Path.Combine(path, "SonsOfTheForestDS.exe");
            Error = $"无效路径！找不到 {Path.GetFileName(exePath)}";
            return File.Exists(exePath);
        }

        // 获取本地游戏服务端的版本号
        public string GetLocalBuild()
        {
            var steamCMD = new Installer.SteamCMD();
            return steamCMD.GetLocalBuild(_serverData.ServerID, AppId);
        }

        // 获取官方游戏服务端的版本号
        public async Task<string> GetRemoteBuild()
        {
            var steamCMD = new Installer.SteamCMD();
            return await steamCMD.GetRemoteBuild(AppId);
        }
    }
}