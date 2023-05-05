using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Common.NetworkHelper
{
	public static class NetworkHttpConnectionExtensions
	{
		public static void TrustSSL(string? ServerName = null)
		{
			//Trust all certificates
			//ServicePointManager.ServerCertificateValidationCallback = ((sender, certificate, chain, sslPolicyErrors) => true);

			// trust sender
			//ServicePointManager.ServerCertificateValidationCallback = ((sender, cert, chain, errors) => ValidateRemoteCertificate(sender, cert, chain, errors));

			// validate cert by calling a function
			ServicePointManager.ServerCertificateValidationCallback += new RemoteCertificateValidationCallback(ValidateRemoteCertificate);

			// callback used to validate the certificate in an SSL conversation
			bool ValidateRemoteCertificate(object sender, X509Certificate? cert, X509Chain? chain, SslPolicyErrors policyErrors)
			{
				if (cert == null) return false;
				return ServerName == null ? true : cert.Subject.Contains(ServerName);
			}
		}
	}
}