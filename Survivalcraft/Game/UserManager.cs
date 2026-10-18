using System;
using System.Collections.Generic;
using System.Linq;
using Engine;

namespace Game;

public static class UserManager
{
	public static List<UserInfo> m_users;

	public static UserInfo ActiveUser
	{
		get
		{
			return GetUser(SettingsManager.UserId) ?? GetUsers().FirstOrDefault();
		}
		set
		{
			SettingsManager.UserId = ((value != null) ? value.UniqueId : string.Empty);
		}
	}

	static UserManager()
	{
		m_users = new List<UserInfo>();
		string num3;
		try
		{
			string num2 = "app:/UserId.dat";
			if (!Storage.FileExists(num2))
			{
				num3 = Guid.NewGuid().ToString();
				Storage.WriteAllText(num2, num3);
			}
			else
			{
				num3 = Storage.ReadAllText(num2);
			}
		}
		catch (Exception)
		{
			num3 = Guid.NewGuid().ToString();
		}
		m_users.Add(new UserInfo(num3.ToString(), "Windows User"));
	}

	public static IEnumerable<UserInfo> GetUsers()
	{
		return new ReadOnlyList<UserInfo>(m_users);
	}

	public static UserInfo GetUser(string uniqueId)
	{
		return GetUsers().FirstOrDefault((UserInfo u) => u.UniqueId == uniqueId);
	}
}
