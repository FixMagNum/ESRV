using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Engine;
using SimpleJson;

namespace Game;

public static class WebManager
{
	public class ProgressHttpContent : HttpContent
	{
		public Stream m_sourceStream;

		public CancellableProgress m_progress;

		public ProgressHttpContent(Stream sourceStream, CancellableProgress progress)
		{
			m_sourceStream = sourceStream;
			m_progress = progress ?? new CancellableProgress();
		}

		protected override bool TryComputeLength(out long length)
		{
			length = m_sourceStream.Length;
			return true;
		}

		protected override async Task SerializeToStreamAsync(Stream targetStream, TransportContext context)
		{
			byte[] buffer = new byte[1024];
			long written = 0L;
			while (true)
			{
				m_progress.Total = m_sourceStream.Length;
				m_progress.Completed = written;
				if (m_progress.CancellationToken.IsCancellationRequested)
				{
					break;
				}
				int read = m_sourceStream.Read(buffer, 0, buffer.Length);
				if (read > 0)
				{
					await targetStream.WriteAsync(buffer, 0, read, m_progress.CancellationToken);
					written += read;
				}
				if (read <= 0)
				{
					return;
				}
			}
			throw new OperationCanceledException("Operation cancelled.");
		}
	}

	[DllImport("wininet.dll")]
	public static extern bool InternetGetConnectedState(out int Description, int ReservedValue);

	public static bool IsInternetConnectionAvailable()
	{
		try
		{
			int i;
			return InternetGetConnectedState(out i, 0);
		}
		catch (Exception e)
		{
			Log.Warning(ExceptionManager.MakeFullErrorMessage("Could not check internet connection availability.", e));
		}
		return true;
	}

	public static void Get(string address, Dictionary<string, string> parameters, Dictionary<string, string> headers, CancellableProgress progress, Action<byte[]> success, Action<Exception> failure)
	{
		MemoryStream targetStream = null;
		Task.Run(async () =>
		{
			try
			{
				progress = progress ?? new CancellableProgress();
				if (!IsInternetConnectionAvailable())
				{
					throw new InvalidOperationException("Internet connection is unavailable.");
				}
				using HttpClient client = new HttpClient(new HttpClientHandler
				{
					ClientCertificateOptions = ClientCertificateOption.Manual,
					ServerCertificateCustomValidationCallback = (HttpRequestMessage httpRequestMessage, X509Certificate2 cert, X509Chain cetChain, SslPolicyErrors policyErrors) => true
				});
				Uri requestUri = ((parameters != null && parameters.Count > 0) ? new Uri(string.Format("{0}?{1}", new object[2]
				{
					address,
					UrlParametersToString(parameters)
				})) : new Uri(address));
				client.DefaultRequestHeaders.Referrer = new Uri(address);
				if (headers != null)
				{
					foreach (KeyValuePair<string, string> header in headers)
					{
						client.DefaultRequestHeaders.Add(header.Key, header.Value);
					}
				}
				ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
				HttpResponseMessage responseMessage = await client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, progress.CancellationToken);
				await VerifyResponse(responseMessage);
				long? contentLength = responseMessage.Content.Headers.ContentLength;
				progress.Total = contentLength.GetValueOrDefault();
				using Stream responseStream = await responseMessage.Content.ReadAsStreamAsync();
				targetStream = new MemoryStream();
				try
				{
					long written = 0L;
					byte[] buffer = new byte[1024];
					int num;
					do
					{
						num = await responseStream.ReadAsync(buffer, 0, buffer.Length, progress.CancellationToken);
						if (num > 0)
						{
							targetStream.Write(buffer, 0, num);
							written += num;
							progress.Completed = written;
						}
					}
					while (num > 0);
					Dispatcher.Dispatch(() =>
					{
						success(targetStream.ToArray());
					});
				}
				finally
				{
					if (targetStream != null)
					{
						((IDisposable)targetStream).Dispose();
					}
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex.Message);
			}
		});
	}

	public static void Put(string address, Dictionary<string, string> parameters, Dictionary<string, string> headers, Stream data, CancellableProgress progress, Action<byte[]> success, Action<Exception> failure)
	{
		PutOrPost(isPost: false, address, parameters, headers, data, progress, success, failure);
	}

	public static void Post(string address, Dictionary<string, string> parameters, Dictionary<string, string> headers, Stream data, CancellableProgress progress, Action<byte[]> success, Action<Exception> failure)
	{
		PutOrPost(isPost: true, address, parameters, headers, data, progress, success, failure);
	}

	public static string UrlParametersToString(Dictionary<string, string> values)
	{
		StringBuilder stringBuilder = new StringBuilder();
		string value = string.Empty;
		foreach (KeyValuePair<string, string> value2 in values)
		{
			stringBuilder.Append(value);
			value = "&";
			stringBuilder.Append(Uri.EscapeDataString(value2.Key));
			stringBuilder.Append('=');
			if (!string.IsNullOrEmpty(value2.Value))
			{
				stringBuilder.Append(Uri.EscapeDataString(value2.Value));
			}
		}
		return stringBuilder.ToString();
	}

	public static byte[] UrlParametersToBytes(Dictionary<string, string> values)
	{
		return Encoding.UTF8.GetBytes(UrlParametersToString(values));
	}

	public static MemoryStream UrlParametersToStream(Dictionary<string, string> values)
	{
		return new MemoryStream(Encoding.UTF8.GetBytes(UrlParametersToString(values)));
	}

	public static Dictionary<string, string> UrlParametersFromString(string s)
	{
		Dictionary<string, string> valuesDictionary2 = new Dictionary<string, string>();
		string[] valuesDictionary3 = s.Split(new char[1] { '&' }, StringSplitOptions.RemoveEmptyEntries);
		for (int i = 0; i < valuesDictionary3.Length; i++)
		{
			string[] valuesDictionary5 = Uri.UnescapeDataString(valuesDictionary3[i]).Split('=');
			if (valuesDictionary5.Length == 2)
			{
				valuesDictionary2[valuesDictionary5[0]] = valuesDictionary5[1];
			}
		}
		return valuesDictionary2;
	}

	public static Dictionary<string, string> UrlParametersFromBytes(byte[] bytes)
	{
		return UrlParametersFromString(Encoding.UTF8.GetString(bytes, 0, bytes.Length));
	}

	public static object JsonFromString(string s)
	{
		return global::SimpleJson.SimpleJson.DeserializeObject(s);
	}

	public static object JsonFromBytes(byte[] bytes)
	{
		return JsonFromString(Encoding.UTF8.GetString(bytes, 0, bytes.Length));
	}

	public static void PutOrPost(bool isPost, string address, Dictionary<string, string> parameters, Dictionary<string, string> headers, Stream data, CancellableProgress progress, Action<byte[]> success, Action<Exception> failure)
	{
		byte[] responseData = null;
		Exception e = null;
		Task.Run(async () =>
		{
			try
			{
				if (!IsInternetConnectionAvailable())
				{
					throw new InvalidOperationException("Internet connection is unavailable.");
				}
				using HttpClient client = new HttpClient(new HttpClientHandler
				{
					ClientCertificateOptions = ClientCertificateOption.Manual,
					ServerCertificateCustomValidationCallback = (HttpRequestMessage httpRequestMessage, X509Certificate2 cert, X509Chain cetChain, SslPolicyErrors policyErrors) => true
				});
				Dictionary<string, string> dictionary = new Dictionary<string, string>();
				if (headers != null)
				{
					foreach (KeyValuePair<string, string> header in headers)
					{
						if (!client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value))
						{
							dictionary.Add(header.Key, header.Value);
						}
					}
				}
				Uri requestUri = ((parameters != null && parameters.Count > 0) ? new Uri(string.Format("{0}?{1}", new object[2]
				{
					address,
					UrlParametersToString(parameters)
				})) : new Uri(address));
				ProgressHttpContent progressHttpContent = new ProgressHttpContent(data, progress);
				foreach (KeyValuePair<string, string> item in dictionary)
				{
					progressHttpContent.Headers.Add(item.Key, item.Value);
				}
				HttpResponseMessage httpResponseMessage = (isPost ? (await client.PostAsync(requestUri, progressHttpContent, progress.CancellationToken)) : (await client.PutAsync(requestUri, progressHttpContent, progress.CancellationToken)));
				HttpResponseMessage responseMessage = httpResponseMessage;
				await VerifyResponse(responseMessage);
				responseData = await responseMessage.Content.ReadAsByteArrayAsync();
				Dispatcher.Dispatch(() =>
				{
					success(responseData);
				});
			}
			catch (Exception ex)
			{
				e = ex;
				Log.Error(ExceptionManager.MakeFullErrorMessage(e));
				Dispatcher.Dispatch(() =>
				{
					failure(e);
				});
			}
		});
	}

	public static async Task VerifyResponse(HttpResponseMessage message)
	{
		if (!message.IsSuccessStatusCode)
		{
			string responseText = string.Empty;
			try
			{
				responseText = await message.Content.ReadAsStringAsync();
			}
			catch
			{
			}
			throw new InvalidOperationException(string.Format("{0} ({1})\n{2}", new object[3]
			{
				message.StatusCode.ToString(),
				(int)message.StatusCode,
				responseText
			}));
		}
	}
}
