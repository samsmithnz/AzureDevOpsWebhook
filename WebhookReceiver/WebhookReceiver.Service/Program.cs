using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WebhookReceiver.Service
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    //Load the appsettings.json configuration file
                    config.AddUserSecrets<Program>();
                    IConfigurationRoot configuration = config.Build();

                    string azureKeyVaultURL = configuration["AppSettings:KeyVaultURL"];
                    string keyVaultClientId = configuration["AppSettings:ClientId"];
                    string keyVaultClientSecret = configuration["AppSettings:ClientSecret"];
                    config.AddAzureKeyVault(azureKeyVaultURL, keyVaultClientId, keyVaultClientSecret);

                    //Load a connection to our Azure key vault instance
                    AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();
                    KeyVaultClient keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
                    config.AddAzureKeyVault(azureKeyVaultURL, keyVaultClientId, keyVaultClientSecret);
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.CaptureStartupErrors(true);
                });
        }
    }
}
