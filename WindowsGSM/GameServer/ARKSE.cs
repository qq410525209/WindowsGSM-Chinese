using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace WindowsGSM.GameServer
{
    // 继承自 UnrealEngine 引擎
    class ARKSE : Engine.UnrealEngine
    {
        // 存储服务器配置数据的对象
        private readonly Functions.ServerConfig _serverData;

        // 错误消息和通知消息
        public string Error;
        public string Notice;

        // 服务器全名、启动路径、是否允许嵌入控制台、端口号增量、查询方法等服务器信息
        public const string FullName = "方舟:生存计划 专用服务器";
        public string StartPath = @"ShooterGame\Binaries\Win64\ShooterGameServer.exe";
        public bool AllowsEmbedConsole = false;
        public int PortIncrements = 2;
        public dynamic QueryMethod = new Query.A2S();

        // 默认配置项：端口号、查询端口、默认地图、最大玩家数、额外参数及应用 ID
        public string Port = "7777";
        public string QueryPort = "27015";
        public string Defaultmap = "TheIsland";
        public string Maxplayers = "16";
        public string Additional = string.Empty;
        public string AppId = "376030";

        // 构造函数，需要传入服务器配置数据对象
        public ARKSE(Functions.ServerConfig serverData)
        {
            _serverData = serverData;
        }

        // 创建服务器配置文件，但ARK似乎没有配置文件
        public async void CreateServerCFG()
        {
            //No config file seems
        }

        // 启动服务器进程
        public async Task<Process> Start()
        {
            // 获取游戏服务器程序路径
            string shipExePath = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath);
            // 检查文件是否存在
            if (!File.Exists(shipExePath))
            {
                Error = $"{Path.GetFileName(shipExePath)} 未找到 ({shipExePath})";
                return null;
            }

            // 拼接启动参数
            string param = string.IsNullOrWhiteSpace(_serverData.ServerMap) ? string.Empty : _serverData.ServerMap;
            param += "?listen";
            param += string.IsNullOrWhiteSpace(_serverData.ServerName) ? string.Empty : $"?SessionName=\"{_serverData.ServerName}\"";
            param += string.IsNullOrWhiteSpace(_serverData.ServerIP) ? string.Empty : $"?MultiHome={_serverData.ServerIP}";
            param += string.IsNullOrWhiteSpace(_serverData.ServerPort) ? string.Empty : $"?Port={_serverData.ServerPort}";
            param += string.IsNullOrWhiteSpace(_serverData.ServerMaxPlayer) ? string.Empty : $"?MaxPlayers={_serverData.ServerMaxPlayer}";
            param += string.IsNullOrWhiteSpace(_serverData.ServerQueryPort) ? string.Empty : $"?QueryPort={_serverData.ServerQueryPort}";
            param += $"{_serverData.ServerParam} -server -log";

            // 创建进程，并设置启动参数
            Process p = new Process
            {
                StartInfo =
            {
                FileName = shipExePath,
                Arguments = param,
                WindowStyle = ProcessWindowStyle.Minimized,
                UseShellExecute = false
            },
                EnableRaisingEvents = true
            };
            p.Start();

            return p;
        }

        // 停止服务器进程
        public async Task Stop(Process p)
        {
            await Task.Run(() =>
            {
                p.Kill();
            });
        }

        // 安装游戏服务端
        public async Task<Process> Install()
        {
            var steamCMD = new Installer.SteamCMD();
            // 使用 SteamCMD 安装 ARK 服务端
            Process p = await steamCMD.Install(_serverData.ServerID, string.Empty, AppId);
            Error = steamCMD.Error;

            return p;
        }

        // 升级游戏服务端
        public async Task<Process> Update(bool validate = false, string custom = null)
        {
            // 使用 SteamCMD 更新 ARK 服务端
            var (p, error) = await Installer.SteamCMD.UpdateEx(_serverData.ServerID, AppId, validate, custom: custom);
            Error = error;
            return p;
        }

        // 在服务器文件夹中检查游戏服务端是否已正确安装
        public bool IsInstallValid()
        {
            return File.Exists(Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath));
        }

        // 在导入服务器时检查游戏服务端安装文件夹是否有效（有 PackageInfo.bin 文件）
        public bool IsImportValid(string path)
        {
            string exePath = Path.Combine(path, "PackageInfo.bin");
            Error = $"路径无效！ 找不到 {Path.GetFileName(exePath)}";
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