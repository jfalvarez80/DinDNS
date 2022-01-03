using CleverNimbus.DinDNS.Client;
using Microsoft.Extensions.Configuration;
using SFTP.Wrapper;
using SFTP.Wrapper.Configs;
using SFTP.Wrapper.Requests;
using System.Reflection;

var configuration = new ConfigurationBuilder()
				.SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
				.AddJsonFile("appsettings.json", false)
				.AddUserSecrets(Assembly.GetExecutingAssembly())
				.Build();

var config = new SftpConfig
{
	Host = configuration["FtpServer"],
	UserName = configuration["FtpUser"],
	Password = configuration["FtpPassword"],
	Port = 22
};

var sftpClient = new SftpManager(config);
var uFileR = new DownloadFileRequest(configuration["FtpFilePath"]);

var result = sftpClient.DownloadFileAsync(uFileR).Result;
if (!result.Status)
	return;

var sReader = new StreamReader(result.Data.Stream);
var data = sReader.ReadToEnd();
data = data.Replace("cosa", "");

WindowsClipboard.SetText(data);

