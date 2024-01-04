namespace WindowsGSM.GameServer
{
    class CS : Engine.GoldSource
    {
        public const string FullName = "反恐精英:1.6 专用服务器";
        public override string Defaultmap { get { return "de_dust2"; } }
        public override string Game { get { return "cstrike"; } }
        public override string AppId { get { return "10"; } }

        public CS(Functions.ServerConfig serverData) : base(serverData)
        {
            base.serverData = serverData;
        }
    }
}
