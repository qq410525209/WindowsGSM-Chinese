using System;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.IO.Compression;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace WindowsGSM.Functions
{
    class InstallAddons
    {
        public static bool? IsBepInExModExists(Functions.ServerTable server)
        {
            if (server.Game != GameServer.VM.FullName && server.Game != GameServer.SOTF.FullName)
            {
                // 如果不是英灵神殿或森林之子游戏，则不支持
                return null;
            }
            // 检查是否存在BepInEx核心插件文件
            if (server.Game == GameServer.VM.FullName)
            {
                // 英灵神殿游戏检查BepInEx.dll
                return File.Exists(Functions.ServerPath.GetServersServerFiles(server.ID, "BepInEx", "core", "BepInEx.dll"));
            }
            else if (server.Game == GameServer.SOTF.FullName)
            {
                // 森林之子游戏检查BepInEx.Core.dll
                return File.Exists(Functions.ServerPath.GetServersServerFiles(server.ID, "BepInEx", "core", "BepInEx.Core.dll"));
            }
            // 不支持的游戏类型
            return null;
        }
        public static async Task<bool> BepInExMod(Functions.ServerTable server)
        {
            try
            {
                string basePath = Functions.ServerPath.GetServersServerFiles(server.ID);
                string version = await GetBepInExModLatestVersion(server);
                string zipPath;
                string downloadUrl;

                if (server.Game == GameServer.VM.FullName)
                {
                    // 英灵神殿游戏使用 BepInExPack_Valheim
                    zipPath = Functions.ServerPath.GetServersServerFiles(server.ID, $"denikson-BepInExPack_Valheim-{version}.zip");
                    downloadUrl = $"https://thunderstore.io/package/download/denikson/BepInExPack_Valheim/{version}/";
                }
                else if (server.Game == GameServer.SOTF.FullName)
                {
                    // 森林之子游戏使用 BepInExPack_IL2CPP
                    zipPath = Functions.ServerPath.GetServersServerFiles(server.ID, $"BepInEx-BepInExPack_IL2CPP-{version}.zip");
                    downloadUrl = $"https://thunderstore.io/package/download/BepInEx/BepInExPack_IL2CPP/{version}/";
                }
                else
                {
                    // 不支持的游戏类型
                    return false;
                }

                using (WebClient webClient = new WebClient())
                {
                    await webClient.DownloadFileTaskAsync(downloadUrl, zipPath);
                }
                // 解压 BepInExMod 压缩包
                bool success = await Task.Run(() =>
                {
                    try
                    {
                        using (var f = File.OpenRead(zipPath))
                        using (var a = new ZipArchive(f))
                        {
                            // 创建缺少的目录
                            a.Entries.Where(o => o.FullName.StartsWith(GetModStartPath(server.Game)) && o.Name == string.Empty && !Directory.Exists(Path.Combine(basePath, o.FullName.Replace(GetModStartPath(server.Game), "")))).ToList().ForEach(o => Directory.CreateDirectory(Path.Combine(basePath, o.FullName.Replace(GetModStartPath(server.Game), ""))));

                            // 将文件解压至服务器目录中
                            a.Entries.Where(o => o.FullName.StartsWith(GetModStartPath(server.Game)) && o.Name != string.Empty).ToList().ForEach(e => e.ExtractToFile(Path.Combine(basePath, e.FullName.Replace(GetModStartPath(server.Game), "")), true));
                        }
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                });

                // 删除 BepInEx 压缩包
                await Task.Run(() => { try { File.Delete(zipPath); } catch { } });
                return success;
            }
            catch
            {
                return false; // 安装失败
            }
        }
        private static string GetModStartPath(string game)
        {
            if (game == GameServer.VM.FullName)
            {
                // 英灵神殿游戏使用 BepInExPack_Valheim 的起始路径
                return "BepInExPack_Valheim/";
            }
            else if (game == GameServer.SOTF.FullName)
            {
                // 森林之子游戏使用 BepInExPack_IL2CPP 的起始路径
                return "BepInExPack/";
            }

            // 不支持的游戏类型
            return string.Empty;
        }
        private static async Task<string> GetBepInExModLatestVersion(Functions.ServerTable server)
        {
            string url;
            if (server.Game == GameServer.VM.FullName)
            {
                url = "https://thunderstore.io/api/experimental/package/denikson/BepInExPack_Valheim/";
            }
            else if (server.Game == GameServer.SOTF.FullName)
            {
                url = "https://thunderstore.io/api/experimental/package/BepInEx/BepInExPack_IL2CPP/";
            }
            else
            {
                // 不支持的游戏类型
                return null;
            }
            try
            {
                // 访问 BepInEx 的最新版本
                var webRequest = WebRequest.Create(url) as HttpWebRequest;
                webRequest.Method = "GET";
                webRequest.UserAgent = "Anything";
                webRequest.ServicePoint.Expect100Continue = false;
                var response = await webRequest.GetResponseAsync();

                // 将服务器响应读取为字符串，并获取版本号
                using (var responseReader = new StreamReader(response.GetResponseStream()))
                {
                    string responseString = responseReader.ReadToEnd();
                    JObject responseJson = JObject.Parse(responseString);
                    string versionNumber = responseJson["latest"]["version_number"].ToString();
                    Console.WriteLine($"Response: {responseString}");
                    Console.WriteLine($"Version Number: {versionNumber}");
                    return versionNumber;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                return null; // 获取失败，返回 null
            }
        }
        public static bool? IsAMXModXAndMetaModPExists(Functions.ServerTable server)
        {
            dynamic gameServer = GameServer.Data.Class.Get(server.Game);
            if (!(gameServer is GameServer.Engine.GoldSource))
            {
                // Game Type not supported
                return null;
            }

            string MMPath = Functions.ServerPath.GetServersServerFiles(server.ID, gameServer.Game, "addons\\metamod.dll");
            return Directory.Exists(MMPath);
        }

        public static async Task<bool> AMXModXAndMetaModP(Functions.ServerTable server)
        {
            try
            {
                dynamic gameServer = GameServer.Data.Class.Get(server.Game);
                return await GameServer.Addon.AMXModX.Install(server.ID, modFolder: gameServer.Game);
            }
            catch
            {
                return false;
            }
        }

        public static bool? IsSourceModAndMetaModExists(Functions.ServerTable server)
        {
            dynamic gameServer = GameServer.Data.Class.Get(server.Game);
            if (!(gameServer is GameServer.Engine.Source))
            {
                // Game Type not supported
                return null;
            }

            string SMPath = Functions.ServerPath.GetServersServerFiles(server.ID, gameServer.Game, "addons\\sourcemod");
            return Directory.Exists(SMPath);
        }

        public static async Task<bool> SourceModAndMetaMod(Functions.ServerTable server)
        {
            try
            {
                dynamic gameServer = GameServer.Data.Class.Get(server.Game);
                return await GameServer.Addon.SourceMod.Install(server.ID, modFolder: gameServer.Game);
            }
            catch
            {
                return false;
            }
        }

        public static bool? IsDayZSALModServerExists(Functions.ServerTable server)
        {
            if (server.Game != GameServer.DAYZ.FullName)
            {
                // Game Type not supported
                return null;
            }

            string exePath = Functions.ServerPath.GetServersServerFiles(server.ID, "DZSALModServer.exe");
            return File.Exists(exePath);
        }

        public static async Task<bool> DayZSALModServer(Functions.ServerTable server)
        {
            try
            {
                string zipPath = Functions.ServerPath.GetServersServerFiles(server.ID, "dzsalmodserver.zip");
                using (WebClient webClient = new WebClient())
                {
                    await webClient.DownloadFileTaskAsync("http://dayzsalauncher.com/releases/dzsalmodserver.zip", zipPath);
                    await Task.Run(() => { try { ZipFile.ExtractToDirectory(zipPath, Functions.ServerPath.GetServersServerFiles(server.ID)); } catch { } });
                    await Task.Run(() => { try { File.Delete(zipPath); } catch { } });
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool? IsOxideModExists(Functions.ServerTable server)
        {
            if (server.Game != GameServer.RUST.FullName)
            {
                // 如果不是 Rust 游戏，则不支持
                return null;
            }

            // 检查 Rust 服务器中是否存在 Oxide 核心插件文件
            return File.Exists(Functions.ServerPath.GetServersServerFiles(server.ID, "RustDedicated_Data", "Managed", "Oxide.Core.dll"));
        }

        public static async Task<bool> OxideMod(Functions.ServerTable server)
        {
            try
            {
                string basePath = Functions.ServerPath.GetServersServerFiles(server.ID);

                // 下载最新版本的 Oxide.Rust 压缩包
                string zipPath = Functions.ServerPath.GetServersServerFiles(server.ID, "Oxide.Rust.zip");
                using (WebClient webClient = new WebClient())
                {
                    await webClient.DownloadFileTaskAsync("https://github.com/OxideMod/Oxide.Rust/releases/latest/download/Oxide.Rust.zip", zipPath);
                }

                // 解压 Oxide.Rust 文件
                bool success = await Task.Run(() =>
                {
                    try
                    {
                        using (var f = File.OpenRead(zipPath))
                        using (var a = new ZipArchive(f))
                        {
                            // 创建缺少的目录
                            a.Entries.Where(o => o.Name == string.Empty && !Directory.Exists(Path.Combine(basePath, o.FullName))).ToList().ForEach(o => Directory.CreateDirectory(Path.Combine(basePath, o.FullName)));

                            // 将文件解压至服务器目录中
                            a.Entries.Where(o => o.Name != string.Empty).ToList().ForEach(e => e.ExtractToFile(Path.Combine(basePath, e.FullName), true));
                        }
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                });

                // 删除 Oxide.Rust 压缩包
                await Task.Run(() => { try { File.Delete(zipPath); } catch { } });
                return success;
            }
            catch
            {
                return false; // 安装失败
            }
        }

        private static async Task<string> GetOxideModLatestVersion()
        {
            try
            {
                // 访问 Oxide.Rust 的最新版本
                var webRequest = WebRequest.Create("https://api.github.com/repos/OxideMod/Oxide.Rust/releases/latest") as HttpWebRequest;
                webRequest.Method = "GET";
                webRequest.UserAgent = "Anything";
                webRequest.ServicePoint.Expect100Continue = false;
                var response = await webRequest.GetResponseAsync();

                // 将服务器响应读取为字符串，并获取版本号
                using (var responseReader = new StreamReader(response.GetResponseStream()))
                {
                    return JObject.Parse(responseReader.ReadToEnd())["tag_name"].ToString();
                }
            }
            catch
            {
                return null; // 获取失败，返回 null
            }
        }

    }
}
