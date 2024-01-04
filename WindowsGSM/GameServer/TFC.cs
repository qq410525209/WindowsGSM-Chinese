namespace WindowsGSM.GameServer
{
    class TFC : Engine.GoldSource
    {
        public const string FullName = "军团要塞:经典 专用服务器";
        public override string Defaultmap { get { return "dustbowl"; } }
        public override string Game { get { return "tfc"; } }
        public override string AppId { get { return "20"; } }

        public TFC(Functions.ServerConfig serverData) : base(serverData)
        {
            base.serverData = serverData;
        }
    }
}
