namespace WindowsGSM.GameServer
{
    class GMOD : Engine.Source
    {
        public const string FullName = "盖瑞模组 专用服务器";
        public override string Defaultmap { get { return "gm_construct"; } }
        public override string Additional { get { return "-tickrate 66 +gamemode sandbox +host_workshop_collection"; } }
        public override string Game { get { return "garrysmod"; } }
        public override string AppId { get { return "4020"; } }

        public GMOD(Functions.ServerConfig serverData) : base(serverData)
        {
            base.serverData = serverData;
        }
    }
}
