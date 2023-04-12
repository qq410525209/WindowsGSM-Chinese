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
            if (server.Game != GameServer.VM.FullName)
            {
                // 如果不是英灵神殿 游戏，则不支持
                return null;
            }

            // 检查 英灵神殿 服务器中是否存在BepInEx核心插件文件
            return File.Exists(Functions.ServerPath.GetServersServerFiles(server.ID, "BepInEx", "core", "BepInEx.dll"));
        }
        public static async Task<bool> BepInExMod(Functions.ServerTable server)
        {
            try
            {
                string basePath = Functions.ServerPath.GetServersServerFiles(server.ID);
                string version = await GetBepInExModLatestVersion();
                // 下载最新版本的BepInExPack_Valheim压缩包
                string zipPath = Functions.ServerPath.GetServersServerFiles(server.ID, $"denikson-BepInExPack_Valheim-{version}.zip");

                using (WebClient webClient = new WebClient())
                {
                    await webClient.DownloadFileTaskAsync($"https://thunderstore.io/package/download/denikson/BepInExPack_Valheim/{version}/", zipPath);

                }

                // 解压 BepInExPack_Valheim文件
                bool success = await Task.Run(() =>
                {
                    try
                    {
                        using (var f = File.OpenRead(zipPath))
                        using (var a = new ZipArchive(f))
                        {
                            // 创建缺少的目录
                            a.Entries.Where(o => o.FullName.StartsWith("BepInExPack_Valheim/") && o.Name == string.Empty && !Directory.Exists(Path.Combine(basePath, o.FullName.Replace("BepInExPack_Valheim/", "")))).ToList().ForEach(o => Directory.CreateDirectory(Path.Combine(basePath, o.FullName.Replace("BepInExPack_Valheim/", ""))));

                            // 将文件解压至服务器目录中
                            a.Entries.Where(o => o.FullName.StartsWith("BepInExPack_Valheim/") && o.Name != string.Empty).ToList().ForEach(e => e.ExtractToFile(Path.Combine(basePath, e.FullName.Replace("BepInExPack_Valheim/", "")), true));
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

        private static async Task<string> GetBepInExModLatestVersion()
        {
            try
            {
                // 访问 BepInEx 的最新版本
                var webRequest = WebRequest.Create("https://valheim.thunderstore.io/api/experimental/package/denikson/BepInExPack_Valheim/") as HttpWebRequest;
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
