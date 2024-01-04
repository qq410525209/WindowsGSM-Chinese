namespace WindowsGSM.GameServer
{
    class DODS : Engine.Source
    {
        public const string FullName = "胜利之日:起源 专用服务器";
        public override string Defaultmap { get { return "dod_anzio"; } }
        public override string Game { get { return "dod"; } }
        public override string AppId { get { return "232290"; } }

        public DODS(Functions.ServerConfig serverData) : base(serverData)
        {
            base.serverData = serverData;
        }
    }
}