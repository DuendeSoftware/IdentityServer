using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Duende.IdentityServer
{
    public static class Tracing
    {
        private static readonly ActivitySource _source = new ActivitySource(
            ServiceName,
            Version);

        public static ActivitySource ActivitySource => _source;

        public static string ServiceName => "Duende.IdentityServer";
        public static string Version => "5.2.0";
    }
}
