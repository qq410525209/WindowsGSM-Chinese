using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WindowsGSM.Functions;

namespace WindowsGSM.DiscordBot
{
    class Commands
    {
        private readonly DiscordSocketClient _client;

        public Commands(DiscordSocketClient client)
        {
            _client = client;
            _client.MessageReceived += CommandReceivedAsync;
        }

        private async Task CommandReceivedAsync(SocketMessage message)
        {
            // The bot should never respond to itself.
            if (message.Author.Id == _client.CurrentUser.Id) { return; }

            // Return if the author is not admin
            List<string> adminIds = Configs.GetBotAdminIds();
            if (!adminIds.Contains(message.Author.Id.ToString())) { return; }

            // Return if the message is not WindowsGSM prefix
            var prefix = Configs.GetBotPrefix();
            var commandLen = prefix.Length + 4;
            if (message.Content.Length < commandLen) { return; }
            if (message.Content.Length == commandLen && message.Content == $"{prefix}wgsm")
            {
                await SendHelpEmbed(message);
                return;
            }

            if (message.Content.Length >= commandLen + 1 && message.Content.Substring(0, commandLen + 1) == $"{prefix}wgsm ")
            {
                // Remote Actions
                string[] args = message.Content.Split(new[] { ' ' }, 2);
                string[] splits = args[1].Split(' ');

                switch (splits[0])
                {
                    case "start":
                    case "stop":
                    case "restart":
                    case "send":
                    case "list":
                    case "check":
                    case "backup":
                    case "update":
                    case "stats":
                        List<string> serverIds = Configs.GetServerIdsByAdminId(message.Author.Id.ToString());
                        if (splits[0] == "check")
                        {
                            await message.Channel.SendMessageAsync(
                                serverIds.Contains("0") ?
                                "你有完整的权限.\nCommands: `check`, `list`, `start`, `stop`, `restart`, `send`, `backup`, `update`, `stats`" :
                                $"您有服务器权限 (`{string.Join(",", serverIds.ToArray())}`)\nCommands: `check`, `start`, `stop`, `restart`, `send`, `backup`, `update`, `stats`");
                            break;
                        }

                        if (splits[0] == "list" && serverIds.Contains("0"))
                        {
                            await Action_List(message);
                        }
                        else if (splits[0] != "list" && (serverIds.Contains("0") || serverIds.Contains(splits[1])))
                        {
                            switch (splits[0])
                            {
                                case "start": await Action_Start(message, args[1]); break;
                                case "stop": await Action_Stop(message, args[1]); break;
                                case "restart": await Action_Restart(message, args[1]); break;
                                case "send": await Action_SendCommand(message, args[1]); break;
                                case "backup": await Action_Backup(message, args[1]); break;
                                case "update": await Action_Update(message, args[1]); break;
                                case "stats": await Action_Stats(message); break;
                            }
                        }
                        else
                        {
                            await message.Channel.SendMessageAsync("您无权访问.");
                        }
                        break;
                    default: await SendHelpEmbed(message); break;
                }
            }
        }

        private async Task Action_List(SocketMessage message)
        {
            await Application.Current.Dispatcher.Invoke(async () =>
            {
                MainWindow WindowsGSM = (MainWindow)Application.Current.MainWindow;

                var list = WindowsGSM.GetServerList();

                string ids = string.Empty;
                string status = string.Empty;
                string servers = string.Empty;

                foreach ((string id, string state, string server) in list)
                {
                    ids += $"`{id}`\n";
                    status += $"`{state}`\n";
                    servers += $"`{server}`\n";
                }

                var embed = new EmbedBuilder { Color = Color.Teal };
                embed.AddField("ID", ids, inline: true);
                embed.AddField("状态", status, inline: true);
                embed.AddField("服务器名称", servers, inline: true);

                await message.Channel.SendMessageAsync(embed: embed.Build());
            });
        }

        private async Task Action_Start(SocketMessage message, string command)
        {
            string[] args = command.Split(' ');
            if (args.Length == 2 && int.TryParse(args[1], out int i))
            {
                await Application.Current.Dispatcher.Invoke(async () =>
                {
                    MainWindow WindowsGSM = (MainWindow)Application.Current.MainWindow;
                    if (WindowsGSM.IsServerExist(args[1]))
                    {
                        MainWindow.ServerStatus serverStatus = WindowsGSM.GetServerStatus(args[1]);
                        if (serverStatus == MainWindow.ServerStatus.Stopped)
                        {
                            bool started = await WindowsGSM.StartServerById(args[1], message.Author.Id.ToString(), message.Author.Username);
                            await message.Channel.SendMessageAsync($"Server (ID: {args[1]}) {(started ? "启动" : "无法启动")}.");
                        }
                        else if (serverStatus == MainWindow.ServerStatus.Started)
                        {
                            await message.Channel.SendMessageAsync($"Server (ID: {args[1]}) 已经启动.");
                        }
                        else
                        {
                            await message.Channel.SendMessageAsync($"Server (ID: {args[1]}) 当前处于 {serverStatus.ToString()} 状态，无法启动.");
                        }

                        await SendServerEmbed(message, Color.Green, args[1], WindowsGSM.GetServerStatus(args[1]).ToString(), WindowsGSM.GetServerName(args[1]));
                    }
                    else
                    {
                        await message.Channel.SendMessageAsync($"Server (ID: {args[1]}) 不存在.");
                    }
                });
            }
            else
            {
                await message.Channel.SendMessageAsync($"Usage: {Configs.GetBotPrefix()}wgsm start `<SERVERID>`");
            }
        }

        private async Task Action_Stop(SocketMessage message, string command)
        {
            string[] args = command.Split(' ');
            if (args.Length == 2 && int.TryParse(args[1], out int i))
            {
                await Application.Current.Dispatcher.Invoke(async () =>
                {
                    MainWindow WindowsGSM = (MainWindow)Application.Current.MainWindow;
                    if (WindowsGSM.IsServerExist(args[1]))
                    {
                        MainWindow.ServerStatus serverStatus = WindowsGSM.GetServerStatus(args[1]);
                        if (serverStatus == MainWindow.ServerStatus.Started)
                        {
                            bool started = await WindowsGSM.StopServerById(args[1], message.Author.Id.ToString(), message.Author.Username);
                            await message.Channel.SendMessageAsync($"Server (ID: {args[1]}) {(started ? "停止" : "停止失败")}.");
                        }
                        else if (serverStatus == MainWindow.ServerStatus.Stopped)
                        {
                            await message.Channel.SendMessageAsync($"Server (ID: {args[1]}) 已经停止.");
                        }
                        else
                        {
                            await message.Channel.SendMessageAsync($"Server (ID: {args[1]}) 当前处于 {serverStatus.ToString()} 状态，无法停止.");
                        }

                        await SendServerEmbed(message, Color.Orange, args[1], WindowsGSM.GetServerStatus(args[1]).ToString(), WindowsGSM.GetServerName(args[1]));
                    }
                    else
                    {
                        await message.Channel.SendMessageAsync($"Server (ID: {args[1]}) 不存在.");
                    }
                });
            }
            else
            {
                await message.Channel.SendMessageAsync($"Usage: {Configs.GetBotPrefix()}wgsm stop `<SERVERID>`");
            }
        }

        private async Task Action_Restart(SocketMessage message, string command)
        {
            string[] args = command.Split(' ');
            if (args.Length == 2 && int.TryParse(args[1], out int i))
            {
                await Application.Current.Dispatcher.Invoke(async () =>
                {
                    MainWindow WindowsGSM = (MainWindow)Application.Current.MainWindow;
                    if (WindowsGSM.IsServerExist(args[1]))
                    {
                        MainWindow.ServerStatus serverStatus = WindowsGSM.GetServerStatus(args[1]);
                        if (serverStatus == MainWindow.ServerStatus.Started)
                        {
                            bool started = await WindowsGSM.RestartServerById(args[1], message.Author.Id.ToString(), message.Author.Username);
                            await message.Channel.SendMessageAsync($"Server (ID: {args[1]}) {(started ? "重新启动" : "无法重新启动")}.");
                        }
                        else
                        {
                            await message.Channel.SendMessageAsync($"Server (ID: {args[1]}) 目前处于 {serverStatus.ToString()} 状态, 无法重新启动.");
                        }

                        await SendServerEmbed(message, Color.Blue, args[1], WindowsGSM.GetServerStatus(args[1]).ToString(), WindowsGSM.GetServerName(args[1]));
                    }
                    else
                    {
                        await message.Channel.SendMessageAsync($"Server (ID: {args[1]}) 不存在.");
                    }
                });
            }
            else
            {
                await message.Channel.SendMessageAsync($"Usage: {Configs.GetBotPrefix()}wgsm restart `<SERVERID>`");
            }
        }

        private async Task Action_SendCommand(SocketMessage message, string command)
        {
            string[] args = command.Split(' ');
            if (args.Length >= 2 && int.TryParse(args[1], out int i))
            {
                await Application.Current.Dispatcher.Invoke(async () =>
                {
                    MainWindow WindowsGSM = (MainWindow)Application.Current.MainWindow;
                    if (WindowsGSM.IsServerExist(args[1]))
                    {
                        MainWindow.ServerStatus serverStatus = WindowsGSM.GetServerStatus(args[1]);
                        if (serverStatus == MainWindow.ServerStatus.Started)
                        {
                            string sendCommand = command.Substring(args[1].Length + 6);
                            bool sent = await WindowsGSM.SendCommandById(args[1], sendCommand, message.Author.Id.ToString(), message.Author.Username);
                            await message.Channel.SendMessageAsync($"Server (ID: {args[1]}) {(sent ? "Command sent" : "命令发送失败")}. | `{sendCommand}`");
                        }
                        else
                        {
                            await message.Channel.SendMessageAsync($"Server (ID: {args[1]}) 目前处于 {serverStatus.ToString()} 状态,无法发送命令.");
                        }
                    }
                    else
                    {
                        await message.Channel.SendMessageAsync($"Server (ID: {args[1]}) 不存在.");
                    }
                });
            }
            else
            {
                await message.Channel.SendMessageAsync($"Usage: {Configs.GetBotPrefix()}wgsm send `<SERVERID>` `<COMMAND>`");
            }
        }

        private async Task Action_Backup(SocketMessage message, string command)
        {
            string[] args = command.Split(' ');
            if (args.Length >= 2 && int.TryParse(args[1], out int i))
            {
                await Application.Current.Dispatcher.Invoke(async () =>
                {
                    MainWindow WindowsGSM = (MainWindow)Application.Current.MainWindow;
                    if (WindowsGSM.IsServerExist(args[1]))
                    {
                        MainWindow.ServerStatus serverStatus = WindowsGSM.GetServerStatus(args[1]);
                        if (serverStatus == MainWindow.ServerStatus.Stopped)
                        {
                            await message.Channel.SendMessageAsync($"Server (ID: {args[1]}) 备份启动 - 这需要一些时间.");
                            bool backuped = await WindowsGSM.BackupServerById(args[1], message.Author.Id.ToString(), message.Author.Username);
                            await message.Channel.SendMessageAsync($"Server (ID: {args[1]}) {(backuped ? "备份完成" : "备份失败")}.");
                        }
                        else if (serverStatus == MainWindow.ServerStatus.Backuping)
                        {
                            await message.Channel.SendMessageAsync($"Server (ID: {args[1]}) 已经备份.");
                        }
                        else
                        {
                            await message.Channel.SendMessageAsync($"Server (ID: {args[1]}) 目前处于 {serverStatus.ToString()} 状态, 无法备份.");
                        }
                    }
                    else
                    {
                        await message.Channel.SendMessageAsync($"Server (ID: {args[1]}) 不存在.");
                    }
                });
            }
            else
            {
                await message.Channel.SendMessageAsync($"Usage: {Configs.GetBotPrefix()}wgsm backup `<SERVERID>`");
            }
        }

        private async Task Action_Update(SocketMessage message, string command)
        {
            string[] args = command.Split(' ');
            if (args.Length >= 2 && int.TryParse(args[1], out int i))
            {
                await Application.Current.Dispatcher.Invoke(async () =>
                {
                    MainWindow WindowsGSM = (MainWindow)Application.Current.MainWindow;
                    if (WindowsGSM.IsServerExist(args[1]))
                    {
                        MainWindow.ServerStatus serverStatus = WindowsGSM.GetServerStatus(args[1]);
                        if (serverStatus == MainWindow.ServerStatus.Stopped)
                        {
                            await message.Channel.SendMessageAsync($"Server (ID: {args[1]}) 更新启动 - 这需要一些时间.");
                            bool updated = await WindowsGSM.UpdateServerById(args[1], message.Author.Id.ToString(), message.Author.Username);
                            await message.Channel.SendMessageAsync($"Server (ID: {args[1]}) {(updated ? "更新" : "更新失败")}.");
                        }
                        else if (serverStatus == MainWindow.ServerStatus.Updating)
                        {
                            await message.Channel.SendMessageAsync($"Server (ID: {args[1]}) 已经更新.");
                        }
                        else
                        {
                            await message.Channel.SendMessageAsync($"Server (ID: {args[1]}) 目前处于 {serverStatus} 状态, 无法更新.");
                        }
                    }
                    else
                    {
                        await message.Channel.SendMessageAsync($"Server (ID: {args[1]}) 不存在.");
                    }
                });
            }
            else
            {
                await message.Channel.SendMessageAsync($"Usage: {Configs.GetBotPrefix()}wgsm update `<SERVERID>`");
            }
        }

        private async Task Action_Stats(SocketMessage message)
        {
            var system = new SystemMetrics();
            await Task.Run(() => system.GetCPUStaticInfo());
            await Task.Run(() => system.GetRAMStaticInfo());
            await Task.Run(() => system.GetDiskStaticInfo());

            await message.Channel.SendMessageAsync(embed: (await GetMessageEmbed(system)).Build());
        }

        private async Task SendServerEmbed(SocketMessage message, Color color, string serverId, string serverStatus, string serverName)
        {
            var embed = new EmbedBuilder { Color = color };
            embed.AddField("ID", serverId, inline: true);
            embed.AddField("状态", serverStatus, inline: true);
            embed.AddField("服务器名称", serverName, inline: true);

            await message.Channel.SendMessageAsync(embed: embed.Build());
        }

        private async Task SendHelpEmbed(SocketMessage message)
        {
            var embed = new EmbedBuilder
            {
                Title = "可用命令:",
                Color = Color.Teal
            };

            string prefix = Configs.GetBotPrefix();
            embed.AddField("Command", $"{prefix}wgsm check\n{prefix}wgsm list\n{prefix}wgsm start <SERVERID>\n{prefix}wgsm stop <SERVERID>\n{prefix}wgsm restart <SERVERID>\n{prefix}wgsm update <SERVERID>\n{prefix}wgsm send <SERVERID> <COMMAND>\n{prefix}wgsm backup <SERVERID>\n{prefix}wgsm stats", inline: true);
            embed.AddField("Usage", "检查权限\n打印服务器列表，包括 id、状态和名称\n通过serverId远程启动服务器\n通过serverId远程停止服务器\n通过serverId远程重启服务器\n向服务器控制台发送命令\n通过serverId远程备份服务器\n通过serverId远程更新服务器", inline: true);

            await message.Channel.SendMessageAsync(embed: embed.Build());
        }

        private string GetProgressBar(double progress)
        {
            // ▌ // ▋ // █ // Which one is the best?
            const int MAX_BLOCK = 23;
            string display = $" {(int)progress}% ";

            int startIndex = MAX_BLOCK / 2 - display.Length / 2;
            string progressBar = string.Concat(Enumerable.Repeat("█", (int)(progress / 100 * MAX_BLOCK))).PadRight(MAX_BLOCK).Remove(startIndex, display.Length).Insert(startIndex, display);

            return $"**`{progressBar}`**";
        }

        private string GetActivePlayersString(int activePlayers)
        {
            const int MAX_BLOCK = 23;
            string display = $" {activePlayers} ";

            int startIndex = MAX_BLOCK / 2 - display.Length / 2;
            string activePlayersString = string.Concat(Enumerable.Repeat(" ", MAX_BLOCK)).Remove(startIndex, display.Length).Insert(startIndex, display);

            return $"**`{activePlayersString}`**";
        }

        private async Task<(int, int, int)> GetGameServerDashBoardDetails()
        {
            if (Application.Current != null)
            {
                return await Application.Current.Dispatcher.Invoke(async () =>
                {
                    MainWindow WindowsGSM = (MainWindow)Application.Current.MainWindow;
                    return (WindowsGSM.GetServerCount(), WindowsGSM.GetStartedServerCount(), WindowsGSM.GetActivePlayers());
                });
            }

            return (0, 0, 0);
        }

        private async Task<EmbedBuilder> GetMessageEmbed(SystemMetrics system)
        {
            var embed = new EmbedBuilder
            {
                Title = ":small_orange_diamond: 系统信息",
                Description = $"服务器名称: {Environment.MachineName}",
                Color = Color.Blue
            };

            embed.AddField("CPU", GetProgressBar(await Task.Run(() => system.GetCPUUsage())), true);
            double ramUsage = await Task.Run(() => system.GetRAMUsage());
            embed.AddField("内存: " + SystemMetrics.GetMemoryRatioString(ramUsage, system.RAMTotalSize), GetProgressBar(ramUsage), true);
            double diskUsage = await Task.Run(() => system.GetDiskUsage());
            embed.AddField("磁盘: " + SystemMetrics.GetDiskRatioString(diskUsage, system.DiskTotalSize), GetProgressBar(diskUsage), true);

            (int serverCount, int startedCount, int activePlayers) = await GetGameServerDashBoardDetails();
            embed.AddField($"服务器: {serverCount}/{MainWindow.MAX_SERVER}", GetProgressBar(serverCount * 100 / MainWindow.MAX_SERVER), true);
            embed.AddField($"在线: {startedCount}/{serverCount}", GetProgressBar((serverCount == 0) ? 0 : startedCount * 100 / serverCount), true);
            embed.AddField("活跃玩家", GetActivePlayersString(activePlayers), true);

            embed.WithFooter(new EmbedFooterBuilder().WithIconUrl("https://github.com/WindowsGSM/WindowsGSM/raw/master/WindowsGSM/Images/WindowsGSM.png").WithText($"WindowsGSM {MainWindow.WGSM_VERSION} | 系统信息"));
            embed.WithCurrentTimestamp();

            return embed;
        }
    }
}
