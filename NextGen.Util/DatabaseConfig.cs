namespace NextGen.Util
{
    public class DatabaseConfig
    {
        public string Server { get; set; }
        public int Port { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public string Database { get; set; }
        public uint MinPoolSize { get; set; }
        public uint MaxPoolSize { get; set; }
        public int QueryCachePerClient { get; set; }
        public int OverloadFlags { get; set; }

        public string BuildConnectionString()
        {
            return $"User ID={User};Password={Password};Host={Server};Port={Port};Database={Database};Protocol=TCP;Compress=false;Pooling=true;Min Pool Size={MinPoolSize};Max Pool Size={MaxPoolSize};Connection Lifetime=0;";
        }
    }
}
