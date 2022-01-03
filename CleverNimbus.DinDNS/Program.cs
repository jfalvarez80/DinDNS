using CleverNimbus.DinDNS.Common.Crypto;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using SFTP.Wrapper;
using SFTP.Wrapper.Configs;
using SFTP.Wrapper.Requests;
using SFTP.Wrapper.Responses;
using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;

namespace CleverNimbus.DinDNS
{
	internal class Program
	{
		public static void Main(string[] args)
		{
			var configuration = LoadAppConfiguration();

			var dataFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AppData.json");
			var appData = LoadAppData(dataFilePath);

			string externalIpString = GetExternalIP(configuration);

			if (!string.IsNullOrWhiteSpace(appData.IPAddress) && appData.IPAddress.Equals(externalIpString))
			{
				return;
			}

			var fileContents = Encoding.UTF8.GetBytes(AES.Encrypt($"{GetRandomString()}{externalIpString}{GetRandomString()}", configuration["SecretKey"]));
			using var ms = new MemoryStream(fileContents);
			var result = UploadFtpFile(configuration, ms);
			SaveAppData(appData, dataFilePath, externalIpString, result);
		}

		private static string GetRandomString()
		{
			var random = new Random().Next(40);
			var result = "";
			for (int i = 0; i < random; i++)
			{
				result += "cosa";
			}
			return result;
		}

		private static string GetExternalIP(IConfigurationRoot configuration)
		{
			using var httpClient = new HttpClient();
			return httpClient.GetStringAsync(configuration["GetExternalIPUrl"]).Result.Replace("\n", "");
		}

		private static IConfigurationRoot LoadAppConfiguration()
		{
			return new ConfigurationBuilder()
				.SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
				.AddJsonFile("appsettings.json", false)
				.AddUserSecrets(Assembly.GetExecutingAssembly())
				.Build();
		}

		private static AppData LoadAppData(string dataFilePath)
		{
			if (File.Exists(dataFilePath))
			{
				return JsonConvert.DeserializeObject<AppData>(File.ReadAllText(dataFilePath));
			}
			else
			{
				return new AppData();
			}
		}

		private static void SaveAppData(AppData appData, string dataFilePath, string externalIpString, ResultStatus<UploadFileResponse> result)
		{
			appData.LastResultDescription = result.Message;
			appData.LastResultCode = result.Status;
			appData.IPAddress = externalIpString;
			appData.LastResultDate = DateTime.Now;

			File.WriteAllText(dataFilePath, JsonConvert.SerializeObject(appData));
		}

		private static ResultStatus<UploadFileResponse> UploadFtpFile(IConfigurationRoot configuration, MemoryStream ms)
		{
			var config = new SftpConfig
			{
				Host = configuration["FtpServer"],
				UserName = configuration["FtpUser"],
				Password = configuration["FtpPassword"],
				Port = 22
			};

			var sftpClient = new SftpManager(config);
			var uFileR = new UploadFileRequest(ms, configuration["FtpFilePath"]);

			var result = sftpClient.UploadFileAsync(uFileR).Result;
			return result;
		}
	}
}