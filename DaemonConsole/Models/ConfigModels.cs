using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace daemon_console.Models
{
    internal class ConfigModel
    {
        /// <summary>
        /// Graph API endpoint, could be public Azure (default) or a Sovereign cloud (US government, etc ...)
        /// </summary>
        public string ApiUrl { get; set; } = "https://graph.microsoft.com/";

        public string ElasticUri { get; set; }

        public string EncryptionKey { get; set; }

        public Entity Alerts { get; set; }

        public Entity AuditLogs { get; set; }

        public Entity Users { get; set; }
                
        public Entity Chat { get; set; }

        public Entity ChatMembers { get; set; }

        public Entity ChatMessages { get; set; }

        public IEnumerable<GraphTenant> Tenants { get; set; }
        
        /// <summary>
        /// Reads the configuration from a json file
        /// </summary>
        /// <param name="path">Path to the configuration json file</param>
        /// <returns>ConfigModel read from the json file</returns>
        public static ConfigModel ReadFromJsonFile(string path)
        {
            IConfigurationRoot Configuration;

            var builder = new ConfigurationBuilder()
             .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(path);

            Configuration = builder.Build();
            return Configuration.Get<ConfigModel>();
        }
    }

    public class Entity
    {
        public string IndexName { get; set; }

        public string Endpoint { get; set; }

        public IEnumerable<string> ActivitiesToEmail { get; set; }

        public string EmailTemplate { get; set; }
    }

    public class GraphTenant
    {
        /// <summary>
        /// instance of Azure AD, for example public Azure or a Sovereign cloud (Azure China, Germany, US government, etc ...)
        /// </summary>
        public string Instance { get; set; } = "https://login.microsoftonline.com/{0}";

        /// <summary>
        /// The Tenant is:
        /// - either the tenant ID of the Azure AD tenant in which this application is registered (a guid)
        /// or a domain name associated with the tenant
        /// - or 'organizations' (for a multi-tenant application)
        /// </summary>
        public string Tenant { get; set; }

        /// <summary>
        /// Guid used by the application to uniquely identify itself to Azure AD
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Client secret (application password)
        /// </summary>
        /// <remarks>Daemon applications can authenticate with AAD through two mechanisms: ClientSecret
        /// (which is a kind of application password: this property)
        /// or a certificate previously shared with AzureAD during the application registration 
        /// (and identified by the CertificateName property belows)
        /// <remarks> 
        public string ClientSecret { get; set; }

        /// <summary>
        /// Name of a certificate in the user certificate store
        /// </summary>
        /// <remarks>Daemon applications can authenticate with AAD through two mechanisms: ClientSecret
        /// (which is a kind of application password: the property above)
        /// or a certificate previously shared with AzureAD during the application registration 
        /// (and identified by this CertificateName property)
        /// <remarks> 
        public string CertificateName { get; set; }

        /// <summary>
        /// URL of the authority
        /// </summary>
        public string Authority => string.Format(CultureInfo.InvariantCulture, Instance, Tenant);
    }
}
