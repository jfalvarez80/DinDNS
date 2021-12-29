using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Renci.SshNet;
using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;

namespace CleverNimbus.DinDNS
{
	internal class Program
	{
		public static void Main(string[] args)
		{
			var configuration = new ConfigurationBuilder()
				.SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
				.AddJsonFile("appsettings.json", false)
				.AddUserSecrets(Assembly.GetExecutingAssembly())
				.Build();

			AppData appData;

			var dataFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AppData.json");
			if (File.Exists(dataFilePath))
			{
				appData = JsonConvert.DeserializeObject<AppData>(System.IO.File.ReadAllText(dataFilePath));
			}
			else
			{
				appData = new AppData();
			}

			string externalIpString = new WebClient().DownloadString(configuration["GetExternalIPUrl"]).Replace("\\r\\n", "").Replace("\\n", "").Trim();
			var externalIp = IPAddress.Parse(externalIpString);

			if (!string.IsNullOrWhiteSpace(appData.IPAddress) && appData.IPAddress.Equals(externalIp.ToString()))
			{
				return;
			}

			appData.IPAddress = externalIp.ToString();

			var assembly = Assembly.GetExecutingAssembly();
			var resourceName = "CleverNimbus.DinDNS.RemoteData.rdp";
			using Stream stream = assembly.GetManifestResourceStream(resourceName);
			using StreamReader reader = new StreamReader(stream);
			string fileContent = reader.ReadToEnd().Replace("{{IPADDRESS}}", externalIp.ToString() + ":" + configuration["RDPPort"]);
			var fileContents = Encoding.UTF8.GetBytes(fileContent);

			using var client = new SftpClient(configuration["FtpServer"], 22, configuration["FtpUser"], configuration["FtpPassword"]);
			client.Connect();
			if (client.IsConnected)
			{
				using (var ms = new MemoryStream(fileContents))
				{
					client.BufferSize = (uint)ms.Length;
					client.UploadFile(ms, configuration["FtpFilePath"]);
				}
			}

			//appData.LastResultDescription = response.StatusDescription;
			//appData.LastResultCode = (int)response.StatusCode;
			appData.LastResultDate = DateTime.Now;

			File.WriteAllText(dataFilePath, JsonConvert.SerializeObject(appData));
		}
	}
}