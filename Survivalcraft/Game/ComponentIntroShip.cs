using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentIntroShip : Component, IUpdateable
{
	public SubsystemTime m_subsystemTime;

	public SubsystemGameWidgets m_subsystemViews;

	public SubsystemGameInfo m_subsystemGameInfo;

	public SubsystemSky m_subsystemSky;

	public ComponentFrame m_componentFrame;

	public ComponentModel m_componentModel;

	public double m_creationTime;

	public float Heading { get; set; }

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public void Update(float dt)
	{
		float voltage = 3.5f * MathUtils.Saturate(0.07f * ((float)m_subsystemGameInfo.TotalElapsedGameTime - 6f));
		Matrix num = m_componentFrame.Matrix;
		Vector3 vector = Quaternion.CreateFromRotationMatrix(num).ToYawPitchRoll();
		vector.X = Heading;
		vector.Y = 0.05f * MathUtils.Sin((float)MathUtils.NormalizeAngle(0.77 * m_subsystemTime.GameTime + 1.0));
		vector.Z = 0.12f * MathUtils.Sin((float)MathUtils.NormalizeAngle(1.12 * m_subsystemTime.GameTime + 2.0));
		num = Matrix.CreateFromYawPitchRoll(vector.X, vector.Y, vector.Z) * Matrix.CreateTranslation(num.Translation);
		num.Translation += voltage * num.Forward * new Vector3(1f, 0f, 1f) * dt;
		m_componentFrame.Position = num.Translation;
		m_componentFrame.Rotation = Quaternion.CreateFromRotationMatrix(num);
		m_componentModel.SetBoneTransform(m_componentModel.Model.RootBone.Index, num);
		if (m_subsystemTime.GameTime - m_creationTime > 10.0 && m_subsystemViews.CalculateDistanceFromNearestView(num.Translation) > m_subsystemSky.VisibilityRange + 30f)
		{
			Project.RemoveEntity(Entity, disposeEntity: true);
		}
	}

	public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
	{
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemViews = Project.FindSubsystem<SubsystemGameWidgets>(throwOnError: true);
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		m_subsystemSky = Project.FindSubsystem<SubsystemSky>(throwOnError: true);
		m_componentFrame = Entity.FindComponent<ComponentFrame>(throwOnError: true);
		m_componentModel = Entity.FindComponent<ComponentModel>(throwOnError: true);
		m_creationTime = m_subsystemTime.GameTime;
		Heading = valuesDictionary.GetValue<float>("Heading");
	}

	public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap)
	{
		valuesDictionary.SetValue("Heading", Heading);
	}
}
