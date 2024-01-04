namespace WindowsGSM.GameServer
{
    class CSGO : Engine.Source
    {
        public const string FullName = "反恐精英:全球攻势 专用服务器";
        public override string Defaultmap { get { return "de_dust2"; } }
        public override string Additional { get { return "-tickrate 64 -usercon +game_type 0 +game_mode 0 +mapgroup mg_active"; } }
        public override string Game { get { return "csgo"; } }
        public override string AppId { get { return "740"; } }

        public CSGO(Functions.ServerConfig serverData) : base(serverData)
        {
            base.serverData = serverData;
        }
    }
}
