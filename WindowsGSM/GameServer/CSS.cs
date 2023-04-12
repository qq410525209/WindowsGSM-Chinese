namespace WindowsGSM.GameServer
{
    class CSS : Engine.Source
    {
        public const string FullName = "反恐精英:起源 专用服务器";
        public override string Defaultmap { get { return "de_dust2"; } }
        public override string Additional { get { return "-tickrate 64"; } }
        public override string Game { get { return "cstrike"; } }
        public override string AppId { get { return "232330"; } }

        public CSS(Functions.ServerConfig serverData) : base(serverData)
        {
            base.serverData = serverData;
        }
    }
}
