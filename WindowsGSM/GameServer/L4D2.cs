namespace WindowsGSM.GameServer
{
    class L4D2 : Engine.Source
    {
        public const string FullName = "求生之路2 专用服务器";
        public override string Defaultmap { get { return "c1m1_hotel"; } }
        public override string Game { get { return "left4dead2"; } }
        public override string AppId { get { return "222860"; } }

        public L4D2(Functions.ServerConfig serverData) : base(serverData)
        {
            base.serverData = serverData;
        }
    }
}
