using System.Text;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class SubsystemEnvironmentSync : Subsystem, IUpdateable
{
	public SubsystemPlayers m_subsystemPlayers;

	public SubsystemTime m_subsystemTime;

	public bool m_karmaDelivered;

	public bool m_checkCompleted;

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public override void Load(ValuesDictionary valuesDictionary)
	{
		m_subsystemPlayers = Project.FindSubsystem<SubsystemPlayers>(throwOnError: true);
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_karmaDelivered = valuesDictionary.GetValue("SyncState", defaultValue: false);
	}

	public override void Save(ValuesDictionary valuesDictionary)
	{
		valuesDictionary.SetValue("SyncState", m_karmaDelivered);
	}

	public void Update(float dt)
	{
		if (!m_checkCompleted && m_subsystemTime.PeriodicGameTimeEvent(2.0, 0.0))
		{
			CheckIntegrityStatus();
			m_checkCompleted = true;
		}
	}

	private void CheckIntegrityStatus()
	{
		foreach (PlayerData playersDatum in m_subsystemPlayers.PlayersData)
		{
			if (!playersDatum.IsMainPlayer || !playersDatum.IsCheater)
			{
				continue;
			}
			ComponentPlayer componentPlayer = playersDatum.ComponentPlayer;
			if (componentPlayer != null && componentPlayer.ComponentGui != null)
			{
				if (!m_karmaDelivered)
				{
					string largeText = LanguageControl.Get("ComponentHealth", "Analysis_Log_1");
					string text = LanguageControl.Get("ComponentHealth", "Analysis_Log_2");
					string smallText = WrapText(text, 45);
					componentPlayer.ComponentGui.DisplayLargeMessage(largeText, smallText, 20f, 1f);
					m_karmaDelivered = true;
				}
				else
				{
					string largeText2 = LanguageControl.Get("ComponentHealth", "Critical_Error_1");
					string smallText2 = LanguageControl.Get("ComponentHealth", "Critical_Error_2");
					componentPlayer.ComponentGui.DisplayLargeMessage(largeText2, smallText2, 5f, 1f);
				}
			}
		}
	}

	private string WrapText(string text, int maxLineLength)
	{
		if (string.IsNullOrEmpty(text))
		{
			return text;
		}
		string[] array = text.Split(' ');
		StringBuilder stringBuilder = new StringBuilder();
		int num = 0;
		string[] array2 = array;
		foreach (string text2 in array2)
		{
			if (num + text2.Length + 1 > maxLineLength)
			{
				stringBuilder.Append("\n");
				num = 0;
			}
			else if (num > 0)
			{
				stringBuilder.Append(" ");
				num++;
			}
			stringBuilder.Append(text2);
			num += text2.Length;
		}
		return stringBuilder.ToString();
	}
}
