using Renci.SshNet;
using SFTP.Wrapper.Configs;
using SFTP.Wrapper.Requests;
using SFTP.Wrapper.Responses;

namespace SFTP.Wrapper
{
	public class SftpManager
	{
		private readonly SftpConfig _config;

		public SftpManager(SftpConfig config)
		{
			_config = config ?? throw new ArgumentNullException();
		}

		public virtual async Task<ResultStatus<DownloadFileResponse>> DownloadFileAsync(DownloadFileRequest request)
		{
			var response = await HandleAsync(async (client, req) =>
			{
				var stream = new MemoryStream();
				await Task.Factory.FromAsync(client.BeginDownloadFile(request.File, stream), client.EndDownloadFile).ConfigureAwait(false);

				return new DownloadFileResponse
				{
					Status = true,
					FileName = Path.GetFileName(request.File),
					Stream = stream
				};
			}, request, nameof(DownloadFileAsync)).ConfigureAwait(false);

			return response;
		}

		public virtual async Task<ResultStatus<UploadFileResponse>> UploadFileAsync(UploadFileRequest request)
		{
			var response = await HandleAsync(async (client, req) =>
			{
				await Task.Factory.FromAsync(client.BeginUploadFile(request.StreamToUpload, request.WhereToUpload), client.EndUploadFile).ConfigureAwait(false);

				return new UploadFileResponse(request.WhereToUpload);
			}, request, nameof(UploadFileAsync)).ConfigureAwait(false);

			return response;
		}

		public virtual async Task<ResultStatus<TResponse>> HandleAsync<TRequest, TResponse>(Func<SftpClient, TRequest, Task<TResponse>> operation, TRequest request, string nameOfOperation)
		where TRequest : class, IValidatable where TResponse : class
		{
			var isValid = request?.IsValid() ?? false;

			if (!isValid)
			{
				return ResultStatus<TResponse>.Error("Invalid request");
			}

			if (operation == null)
			{
				return ResultStatus<TResponse>.Error("Please specify the response to handle");
			}

			if (string.IsNullOrWhiteSpace(nameOfOperation))
			{
				return ResultStatus<TResponse>.Error("Please specify a name for the operation");
			}

			try
			{
				using (var client = new SftpClient(_config.Host, _config.Port == 0 ? 22 : _config.Port, _config.UserName, _config.Password))
				{
					try
					{
						client.Connect();

						var response = await operation(client, request).ConfigureAwait(false);
						return ResultStatus<TResponse>.Success(response);
					}
					catch (Exception exception)
					{
						var message = exception.Message ?? $"Error occured in {nameOfOperation}";
						return ResultStatus<TResponse>.Error(message, exception);
					}
					finally
					{
						client.Disconnect();
					}
				}
			}
			catch (Exception exception)
			{
				var message = exception.Message ?? "Cannot connect to the SFTP host";
				return ResultStatus<TResponse>.Error(message, exception);
			}
		}
	}
}