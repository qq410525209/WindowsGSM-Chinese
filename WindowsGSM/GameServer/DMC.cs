namespace WindowsGSM.GameServer
{
    class DMC : Engine.GoldSource
    {
        public const string FullName = "死亡竞赛经典版 专用服务器";
        public override string Defaultmap { get { return "dcdm5"; } }
        public override string Game { get { return "dmc"; } }
        public override string AppId { get { return "40"; } }

        public DMC(Functions.ServerConfig serverData) : base(serverData)
        {
            base.serverData = serverData;
        }
    }
}
