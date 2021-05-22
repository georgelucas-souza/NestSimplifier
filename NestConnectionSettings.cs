using System;
using System.Collections.Generic;
using System.Text;

namespace NestSimplifier
{
    public class NestConnectionSettings
    {
        public Uri[] Uris { get; private set; }
        public string UserName { get; private set; }
        public string Password { get; private set; }
        public TimeSpan? ConnectionTimeOut { get; private set; }
        public string CertificateFilePath { get; private set; }

        public NestConnectionSettings(Uri[] uris, string userName, string password, TimeSpan? connectionTimeOut = null, string certificateFilePath = null)
        {
            Uris = uris;
            UserName = userName;
            Password = password;
            ConnectionTimeOut = connectionTimeOut;
            CertificateFilePath = certificateFilePath;
        }
    }
}
