namespace WindowsGSM.GameServer
{
    class NMRIH : Engine.Source
    {
        public const string FullName = "地狱已满 专用服务器";
        public override string Defaultmap { get { return "nmo_broadway"; } }
        public override string Game { get { return "nmrih"; } }
        public override string AppId { get { return "317670"; } }

        public NMRIH(Functions.ServerConfig serverData) : base(serverData)
        {
            base.serverData = serverData;
        }
    }
}
