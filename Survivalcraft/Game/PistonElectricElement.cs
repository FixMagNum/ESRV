using System.Collections.Generic;
using Engine;

namespace Game;

public class PistonElectricElement(SubsystemElectricity subsystemElectricity, Point3 point) : ElectricElement(subsystemElectricity, new List<CellFace>
{
	new CellFace(point.X, point.Y, point.Z, 0),
	new CellFace(point.X, point.Y, point.Z, 1),
	new CellFace(point.X, point.Y, point.Z, 2),
	new CellFace(point.X, point.Y, point.Z, 3),
	new CellFace(point.X, point.Y, point.Z, 4),
	new CellFace(point.X, point.Y, point.Z, 5)
})
{
	public int m_lastLength = -1;

	public override bool Simulate()
	{
		float databaseObject = 0f;
		foreach (ElectricConnection value2 in Connections)
		{
			if (value2.ConnectorType != ElectricConnectorType.Output && value2.NeighborConnectorType != ElectricConnectorType.Input)
			{
				databaseObject = MathUtils.Max(databaseObject, value2.NeighborElectricElement.GetOutputVoltage(value2.NeighborConnectorFace));
			}
		}
		int valuesDictionary = MathUtils.Max((int)(databaseObject * 15.999f) - 7, 0);
		if (valuesDictionary != m_lastLength)
		{
			m_lastLength = valuesDictionary;
			SubsystemElectricity.Project.FindSubsystem<SubsystemPistonBlockBehavior>(throwOnError: true).AdjustPiston(CellFaces[0].Point, valuesDictionary);
		}
		return false;
	}
}
