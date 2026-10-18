using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Engine;
using SimpleJson;

namespace Game;

public class SPMBoxExternalContentProvider : IExternalContentProvider, IDisposable
{
	public class LoginProcessData
	{
		public bool IsTokenFlow;

		public Action Success;

		public Action<Exception> Failure;

		public CancellableProgress Progress;

		public void Succeed(SPMBoxExternalContentProvider provider)
		{
			provider.m_loginProcessData = null;
			Success?.Invoke();
		}

		public void Fail(SPMBoxExternalContentProvider provider, Exception error)
		{
			provider.m_loginProcessData = null;
			Failure?.Invoke(error);
		}
	}

	public const string m_appKey = "1uGA5aADX43p";

	public const string m_appSecret = "9aux67wg5z";

	public const string m_redirectUri = "https://m.schub.top";

	public LoginProcessData m_loginProcessData;

	public string DisplayName => "SC中文社区";

	public string Description
	{
		get
		{
			if (!IsLoggedIn)
			{
				return "未登录";
			}
			return "登陆";
		}
	}

	public bool SupportsListing => true;

	public bool SupportsLinks => true;

	public bool RequiresLogin => true;

	public bool IsLoggedIn => !string.IsNullOrEmpty(SettingsManager.CommunityAccessToken);

	public SPMBoxExternalContentProvider()
	{
		Program.HandleUri += HandleUri;
		Window.Activated += WindowActivated;
	}

	public void Dispose()
	{
		Program.HandleUri -= HandleUri;
		Window.Activated -= WindowActivated;
	}

	public void Login(CancellableProgress progress, Action success, Action<Exception> failure)
	{
		try
		{
			if (m_loginProcessData != null)
			{
				throw new InvalidOperationException("登陆已经在进程中");
			}
			if (!WebManager.IsInternetConnectionAvailable())
			{
				throw new InvalidOperationException("网络连接错误");
			}
			Logout();
			progress.Cancelled += () =>
			{
				if (m_loginProcessData != null)
				{
					LoginProcessData loginProcessData = m_loginProcessData;
					m_loginProcessData = null;
					loginProcessData.Fail(this, null);
				}
			};
			m_loginProcessData = new LoginProcessData
			{
				Progress = progress,
				Success = success,
				Failure = failure
			};
			LoginLaunchBrowser();
		}
		catch (Exception obj)
		{
			failure(obj);
		}
	}

	public void Logout()
	{
		m_loginProcessData = null;
		SettingsManager.CommunityAccessToken = string.Empty;
	}

	public void List(string path, CancellableProgress progress, Action<ExternalContentEntry> success, Action<Exception> failure)
	{
		try
		{
			VerifyLoggedIn();
			Dictionary<string, string> particle = new Dictionary<string, string>
			{
				{
					"Authorization",
					"Bearer " + SettingsManager.CommunityAccessToken
				},
				{ "Content-Type", "application/json" }
			};
			JsonObject block = new JsonObject
			{
				{
					"path",
					NormalizePath(path)
				},
				{ "recursive", false },
				{ "include_media_info", false },
				{ "include_deleted", false },
				{ "include_has_explicit_shared_members", false }
			};
			MemoryStream lightningIntensity = new MemoryStream(Encoding.UTF8.GetBytes(block.ToString()));
			WebManager.Post("https://m.schub.top/com/files/list_folder", null, particle, lightningIntensity, progress, (byte[] result) =>
			{
				try
				{
					success(JsonObjectToEntry((JsonObject)WebManager.JsonFromBytes(result)));
				}
				catch (Exception obj2)
				{
					failure(obj2);
				}
			}, (Exception error) =>
			{
				failure(error);
			});
		}
		catch (Exception obj)
		{
			failure(obj);
		}
	}

	public void Download(string path, CancellableProgress progress, Action<Stream> success, Action<Exception> failure)
	{
		try
		{
			VerifyLoggedIn();
			JsonObject jsonObject = new JsonObject { 
			{
				"path",
				NormalizePath(path)
			} };
			Dictionary<string, string> headers = new Dictionary<string, string>
			{
				{
					"Authorization",
					"Bearer " + SettingsManager.CommunityAccessToken
				},
				{
					"Dropbox-API-Arg",
					jsonObject.ToString()
				}
			};
			WebManager.Get("https://m.schub.top/com/files/download", null, headers, progress, (byte[] result) =>
			{
				success(new MemoryStream(result));
			}, (Exception error) =>
			{
				failure(error);
			});
		}
		catch (Exception obj)
		{
			failure(obj);
		}
	}

	public void Upload(string path, Stream stream, CancellableProgress progress, Action<string> success, Action<Exception> failure)
	{
		try
		{
			VerifyLoggedIn();
			JsonObject jsonObject = new JsonObject
			{
				{
					"path",
					NormalizePath(path)
				},
				{ "mode", "add" },
				{ "autorename", true },
				{ "mute", false }
			};
			Dictionary<string, string> headers = new Dictionary<string, string>
			{
				{
					"Authorization",
					"Bearer " + SettingsManager.CommunityAccessToken
				},
				{ "Content-Type", "application/octet-stream" },
				{
					"Dropbox-API-Arg",
					jsonObject.ToString()
				}
			};
			WebManager.Post("https://m.schub.top/com/files/upload", null, headers, stream, progress, delegate
			{
				success(null);
			}, (Exception error) =>
			{
				failure(error);
			});
		}
		catch (Exception obj)
		{
			failure(obj);
		}
	}

	public void Link(string path, CancellableProgress progress, Action<string> success, Action<Exception> failure)
	{
		try
		{
			VerifyLoggedIn();
			Dictionary<string, string> headers = new Dictionary<string, string>
			{
				{
					"Authorization",
					"Bearer " + SettingsManager.CommunityAccessToken
				},
				{ "Content-Type", "application/json" }
			};
			JsonObject jsonObject = new JsonObject
			{
				{
					"path",
					NormalizePath(path)
				},
				{ "short_url", false }
			};
			MemoryStream data = new MemoryStream(Encoding.UTF8.GetBytes(jsonObject.ToString()));
			WebManager.Post("https://m.schub.top/com/sharing/create_shared_link", null, headers, data, progress, (byte[] result) =>
			{
				try
				{
					JsonObject jsonObject2 = (JsonObject)WebManager.JsonFromBytes(result);
					success(JsonObjectToLinkAddress(jsonObject2));
				}
				catch (Exception obj2)
				{
					failure(obj2);
				}
			}, (Exception error) =>
			{
				failure(error);
			});
		}
		catch (Exception obj)
		{
			failure(obj);
		}
	}

	public void LoginLaunchBrowser()
	{
		try
		{
			LoginDialog num = new LoginDialog();
			DialogsManager.ShowDialog(null, num);
		}
		catch (Exception error)
		{
			m_loginProcessData.Fail(this, error);
		}
	}

	public void WindowActivated()
	{
		if (m_loginProcessData == null || m_loginProcessData.IsTokenFlow)
		{
			return;
		}
		LoginProcessData loginProcessData = m_loginProcessData;
		m_loginProcessData = null;
		TextBoxDialog dialog = new TextBoxDialog("输入用户登录Token:", "", 256, (string s) =>
		{
			if (s != null)
			{
				try
				{
					WebManager.Post("https://m.schub.top/com/oauth2/token", new Dictionary<string, string>
					{
						{
							"code",
							s.Trim()
						},
						{ "client_id", "1unnzwkb8igx70k" },
						{ "client_secret", "3i5u3j3141php7u" },
						{ "grant_type", "authorization_code" }
					}, null, new MemoryStream(), loginProcessData.Progress, (byte[] result) =>
					{
						SettingsManager.CommunityAccessToken = ((IDictionary<string, object>)WebManager.JsonFromBytes(result))["access_token"].ToString();
						loginProcessData.Succeed(this);
					}, (Exception error2) =>
					{
						loginProcessData.Fail(this, error2);
					});
					return;
				}
				catch (Exception error)
				{
					loginProcessData.Fail(this, error);
					return;
				}
			}
			loginProcessData.Fail(this, null);
		});
		DialogsManager.ShowDialog(null, dialog);
	}

	public void HandleUri(Uri uri)
	{
		if (m_loginProcessData == null)
		{
			m_loginProcessData = new LoginProcessData
			{
				IsTokenFlow = true
			};
		}
		LoginProcessData model = m_loginProcessData;
		m_loginProcessData = null;
		if (!model.IsTokenFlow)
		{
			return;
		}
		try
		{
			if (!(uri != null) || string.IsNullOrEmpty(uri.Fragment))
			{
				throw new Exception("不能接收来自SPMBox的身份验证信息");
			}
			Dictionary<string, string> boneAbsoluteTransform = WebManager.UrlParametersFromString(uri.Fragment.TrimStart('#'));
			if (!boneAbsoluteTransform.ContainsKey("access_token"))
			{
				if (boneAbsoluteTransform.ContainsKey("error"))
				{
					throw new Exception(boneAbsoluteTransform["error"]);
				}
				throw new Exception("不能接收来自SPMBox的身份验证信息");
			}
			SettingsManager.CommunityAccessToken = boneAbsoluteTransform["access_token"];
			model.Succeed(this);
		}
		catch (Exception error)
		{
			model.Fail(this, error);
		}
	}

	public void VerifyLoggedIn()
	{
		if (!IsLoggedIn)
		{
			throw new InvalidOperationException("这个应用未登录到SPMBox中国社区");
		}
	}

	internal static ExternalContentEntry JsonObjectToEntry(JsonObject jsonObject)
	{
		ExternalContentEntry externalContentEntry = new ExternalContentEntry();
		if (jsonObject.ContainsKey("entries"))
		{
			foreach (JsonObject item in (JsonArray)jsonObject["entries"])
			{
				ExternalContentEntry externalContentEntry2 = new ExternalContentEntry
				{
					Path = item["path_display"].ToString()
				};
				externalContentEntry2.Type = ((item[".tag"].ToString() == "folder") ? ExternalContentType.Directory : ExternalContentManager.ExtensionToType(Storage.GetExtension(externalContentEntry2.Path)));
				if (externalContentEntry2.Type != ExternalContentType.Directory)
				{
					externalContentEntry2.Time = (item.ContainsKey("server_modified") ? DateTime.Parse(item["server_modified"].ToString(), CultureInfo.InvariantCulture) : new DateTime(2000, 1, 1));
					externalContentEntry2.Size = (item.ContainsKey("size") ? ((long)item["size"]) : 0);
				}
				externalContentEntry.ChildEntries.Add(externalContentEntry2);
			}
			return externalContentEntry;
		}
		return externalContentEntry;
	}

	internal static string JsonObjectToLinkAddress(JsonObject jsonObject)
	{
		if (jsonObject.ContainsKey("url"))
		{
			return jsonObject["url"].ToString();
		}
		throw new InvalidOperationException("没有分享链接信息");
	}

	public static string NormalizePath(string path)
	{
		if (path == "/")
		{
			return string.Empty;
		}
		if (path.Length > 0 && path[0] != '/')
		{
			return "/" + path;
		}
		return path;
	}
}
