using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SagaUtil
{
    public class SystemVariables
    {
        private static readonly SystemVariables instance = new SystemVariables();

        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static SystemVariables()
        {
        }

        private SystemVariables()
        {
        }

        public string BookDb { get; private set; }
        public string UserDb { get; private set; }
        public string JwtSigningKey { get; private set; }
        public string JwtIssuer => "SagaServer";
        public string JwtAudience => "SagaServer";
        public string Protocol => "http";

        public void UpdateState(string userDb, string bookDb, string signingKey)
        {
            this.BookDb = bookDb;
            this.UserDb = userDb;
            this.JwtSigningKey = signingKey;
        }

        public static SystemVariables Instance
        {
            get
            {
                return instance;
            }
        }
    }
}
