namespace WindowsGSM.GameServer
{
    class CSCZ : Engine.GoldSource
    {
        public const string FullName = "反恐精英-零点行动 专用服务";
        public override string Defaultmap { get { return "de_dust2"; } }
        public override string Game { get { return "czero"; } }
        public override string AppId { get { return "80"; } }

        public CSCZ(Functions.ServerConfig serverData) : base(serverData)
        {
            base.serverData = serverData;
        }
    }
}
