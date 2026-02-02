using Serilog;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace QuickShare.Services
{
    public class CertificateService(ILogger logger, AppConfigService appConfigService)
    {
        private readonly string certPath = Path.Combine(AppContext.BaseDirectory, "server.pfx");
        private readonly string certPassword = appConfigService.AesKeyBase64;
        private const string SubjectName = "CN=quickshare.local";
        private static readonly TimeSpan ValidityPeriod = TimeSpan.FromDays(3650);
        private const int MinValidityDays = 7;

        public void EnsureCertificateExists()
        {
            try
            {
                if (File.Exists(certPath) && IsCertificateValid())
                {
                    logger.Information($"Existing certificate is valid at: {certPath}");
                    return;
                }

                logger.Warning("No valid certificate found. Generating new self-signed certificate...");
                GenerateSelfSignedCertificate();
                logger.Information($"New certificate generated at: {certPath}");
            }
            catch (CryptographicException ex)
            {
                logger.Error($"Certificate cryptographic error: {ex.Message}. Attempting to regenerate.");
                try
                {
                    if (File.Exists(certPath))
                    {
                        File.Delete(certPath);
                    }
                    GenerateSelfSignedCertificate();
                    logger.Information($"Certificate regenerated successfully at: {certPath}");
                }
                catch (Exception regenEx)
                {
                    logger.Error($"Failed to regenerate certificate: {regenEx.Message}");
                    throw new InvalidOperationException("Failed to generate or regenerate certificate.", regenEx);
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to handle certificate: {ex.Message}");
                throw;
            }
        }

        private bool IsCertificateValid()
        {
            try
            {
                var cert = new X509Certificate2(certPath, certPassword);
                return cert.NotBefore <= DateTime.Now &&
                       cert.NotAfter >= DateTime.Now.AddDays(MinValidityDays) &&
                       cert.Subject == SubjectName;
            }
            catch (CryptographicException ex)
            {
                logger.Warning($"Certificate validation failed (cryptographic error): {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                logger.Warning($"Certificate validation failed: {ex.Message}");
                return false;
            }
        }

        private void GenerateSelfSignedCertificate()
        {

            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest(
                SubjectName,
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            request.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(false, false, 0, false));

            request.CertificateExtensions.Add(
                new X509KeyUsageExtension(
                    X509KeyUsageFlags.DigitalSignature |
                    X509KeyUsageFlags.KeyEncipherment,
                    false));

            request.CertificateExtensions.Add(
                new X509EnhancedKeyUsageExtension(
                    new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") },
                    false));

            var sanBuilder = new SubjectAlternativeNameBuilder();
            sanBuilder.AddDnsName($"{Environment.MachineName.ToLower()}.local");
            sanBuilder.AddDnsName("quickshare.local");
            sanBuilder.AddDnsName("localhost");
            sanBuilder.AddIpAddress(System.Net.IPAddress.Loopback);

            request.CertificateExtensions.Add(sanBuilder.Build());

            using var certificate = request.CreateSelfSigned(
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow + ValidityPeriod);

            byte[] pfxData;
            try
            {
                pfxData = certificate.Export(
                    X509ContentType.Pfx,
                    certPassword);
            }
            catch (CryptographicException ex)
            {
                throw new InvalidOperationException("Failed to export certificate to PFX format.", ex);
            }

            var certDir = Path.GetDirectoryName(certPath);
            if (!string.IsNullOrEmpty(certDir))
            {
                Directory.CreateDirectory(certDir);
            }
            File.WriteAllBytes(certPath, pfxData);

            logger.Information("Certificate generated with SANs: quickshare.local, localhost, 127.0.0.1");
        }
    }
}
