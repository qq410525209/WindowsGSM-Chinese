namespace WindowsGSM.GameServer
{
    class ZPS : Engine.Source
    {
        public const string FullName = "僵尸恐慌 专用服务器";
        public override string Defaultmap { get { return "zps_cinema"; } }
        public override string Game { get { return "zps"; } }
        public override string AppId { get { return "17505"; } }

        public ZPS(Functions.ServerConfig serverData) : base(serverData)
        {
            base.serverData = serverData;
        }
    }
}