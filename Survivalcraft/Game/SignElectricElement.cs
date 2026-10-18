using Engine;

namespace Game;

public class SignElectricElement(SubsystemElectricity subsystemElectricity, CellFace cellFace) : ElectricElement(subsystemElectricity, cellFace)
{
	public bool m_isMessageAllowed = true;

	public double? m_lastMessageTime;

	public override bool Simulate()
	{
		bool num = CalculateHighInputsCount() > 0;
		if (num && m_isMessageAllowed && (!m_lastMessageTime.HasValue || SubsystemElectricity.SubsystemTime.GameTime - m_lastMessageTime.Value > 0.5))
		{
			m_isMessageAllowed = false;
			m_lastMessageTime = SubsystemElectricity.SubsystemTime.GameTime;
			SignData valuesDictionary2 = SubsystemElectricity.Project.FindSubsystem<SubsystemSignBlockBehavior>(throwOnError: true).GetSignData(new Point3(CellFaces[0].X, CellFaces[0].Y, CellFaces[0].Z));
			if (valuesDictionary2 != null)
			{
				string value = string.Join("\n", valuesDictionary2.Lines);
				value = value.Trim('\n');
				value = value.Replace("\\\n", "");
				Color valuesDictionary3 = ((valuesDictionary2.Colors[0] == Color.Black) ? Color.White : valuesDictionary2.Colors[0]);
				valuesDictionary3 *= 255f / (float)MathUtils.Max(valuesDictionary3.R, valuesDictionary3.G, valuesDictionary3.B);
				foreach (ComponentPlayer componentPlayer in SubsystemElectricity.Project.FindSubsystem<SubsystemPlayers>(throwOnError: true).ComponentPlayers)
				{
					componentPlayer.ComponentGui.DisplaySmallMessage(value, valuesDictionary3, blinking: true, playNotificationSound: true);
				}
			}
		}
		if (!num)
		{
			m_isMessageAllowed = true;
		}
		return false;
	}
}
