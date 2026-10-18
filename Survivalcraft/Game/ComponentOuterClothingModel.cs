using System;
using System.Linq;
using Engine;
using Engine.Graphics;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentOuterClothingModel : ComponentModel
{
	public ComponentHumanModel m_componentHumanModel;

	public ComponentCreature m_componentCreature;

	public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
	{
		base.Load(valuesDictionary, idToEntityMap);
		m_subsystemSky = Project.FindSubsystem<SubsystemSky>(throwOnError: true);
		m_componentHumanModel = Entity.FindComponent<ComponentHumanModel>(throwOnError: true);
		m_componentCreature = Entity.FindComponent<ComponentCreature>(throwOnError: true);
	}

	public override void Animate()
	{
		Opacity = m_componentHumanModel.Opacity;
		SubsystemPlayers subsystemPlayers = Project.FindSubsystem<SubsystemPlayers>(throwOnError: true);
		PlayerData mainPlayerData = subsystemPlayers.MainPlayerData;
		bool flag = mainPlayerData != null && mainPlayerData.IsMainPlayer && Entity == subsystemPlayers.MainPlayer?.Entity;
		bool flag2 = Project.FindSubsystem<SubsystemGameWidgets>(throwOnError: true).GameWidgets.Any((GameWidget widget) => widget.ActiveCamera != null && widget.ActiveCamera.GameWidget.IsEntityFirstPersonTarget(Entity) && SettingsManager.FirstPersonCameraEnabled);
		bool flag3 = flag & flag2;
		foreach (ModelBone bone in Model.Bones)
		{
			if ((bone.Name == "Head") & flag3)
			{
				Matrix valueOrDefault = m_componentHumanModel.GetBoneTransform(bone.Index).GetValueOrDefault();
				valueOrDefault.Translation += new Vector3(0f, 1f, 0f);
				SetBoneTransform(bone.Index, valueOrDefault * Matrix.CreateScale(0.001f));
				continue;
			}
			ModelBone modelBone = m_componentHumanModel.Model.FindBone(bone.Name);
			if (modelBone != null)
			{
				Matrix matrix = m_componentHumanModel.GetBoneTransform(modelBone.Index).Value;
				if (Entity.ValuesDictionary.DatabaseObject.Name.IndexOf("Werewolf", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					matrix = Matrix.CreateScale(1.25f) * matrix;
				}
				SetBoneTransform(bone.Index, matrix);
			}
		}
		if (Opacity.HasValue && Opacity.Value < 1f)
		{
			bool flag4 = m_componentCreature.ComponentBody.ImmersionFactor >= 1f;
			bool flag5 = m_subsystemSky.ViewUnderWaterDepth > 0f;
			RenderingMode = ((flag4 == flag5) ? ModelRenderingMode.TransparentAfterWater : ModelRenderingMode.TransparentBeforeWater);
		}
		else
		{
			RenderingMode = ModelRenderingMode.AlphaThreshold;
		}
		base.Animate();
	}

	public override void SetModel(Model model)
	{
		base.SetModel(model);
		if (MeshDrawOrders.Length != 6)
		{
			throw new InvalidOperationException("Invalid number of meshes in OuterClothing model.");
		}
		MeshDrawOrders[0] = model.Meshes.IndexOf(model.FindMesh("Leg1"));
		MeshDrawOrders[1] = model.Meshes.IndexOf(model.FindMesh("Leg2"));
		MeshDrawOrders[2] = model.Meshes.IndexOf(model.FindMesh("Body"));
		MeshDrawOrders[3] = model.Meshes.IndexOf(model.FindMesh("Head"));
		MeshDrawOrders[4] = model.Meshes.IndexOf(model.FindMesh("Hand1"));
		MeshDrawOrders[5] = model.Meshes.IndexOf(model.FindMesh("Hand2"));
	}

	public void SetSleeveTransforms(Matrix Hand1, Matrix Hand2)
	{
		int num = MeshDrawOrders[4];
		int num2 = MeshDrawOrders[5];
		if (num >= 0 && num2 >= 0)
		{
			SetBoneTransform(num, Hand1);
			SetBoneTransform(num2, Hand2);
		}
	}
}
