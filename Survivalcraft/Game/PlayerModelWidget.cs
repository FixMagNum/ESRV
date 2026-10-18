using System;
using Engine;
using Engine.Graphics;

namespace Game;

public class PlayerModelWidget : CanvasWidget
{
	public enum Shot
	{
		Body,
		Bust
	}

	public ModelWidget m_modelWidget;

	public CharacterSkinsCache m_publicCharacterSkinsCache;

	public CharacterSkinsCache m_characterSkinsCache;

	public Vector2? m_lastDrag;

	public float m_rotation;

	public CharacterSkinsCache CharacterSkinsCache
	{
		get
		{
			return m_characterSkinsCache;
		}
		set
		{
			if (value != null)
			{
				m_publicCharacterSkinsCache.Clear();
				m_characterSkinsCache = value;
			}
			else
			{
				m_characterSkinsCache = m_publicCharacterSkinsCache;
			}
		}
	}

	public Shot CameraShot { get; set; }

	public int AnimateHeadSeed { get; set; }

	public int AnimateHandsSeed { get; set; }

	public bool OuterClothing { get; set; }

	public PlayerClass PlayerClass { get; set; }

	public string CharacterSkinName { get; set; }

	public Texture2D CharacterSkinTexture { get; set; }

	public Texture2D OuterClothingTexture { get; set; }

	public PlayerData.PlayerTag BodyTag { get; set; } = PlayerData.PlayerTag.Tall;

	public PlayerModelWidget()
	{
		m_modelWidget = new ModelWidget
		{
			UseAlphaThreshold = true,
			IsPerspective = true
		};
		Children.Add(m_modelWidget);
		IsHitTestVisible = false;
		m_publicCharacterSkinsCache = new CharacterSkinsCache();
		m_characterSkinsCache = m_publicCharacterSkinsCache;
	}

	public override void Update()
	{
		if (Input.Press.HasValue)
		{
			if (m_lastDrag.HasValue)
			{
				m_rotation += 0.01f * (Input.Press.Value.X - m_lastDrag.Value.X);
				m_lastDrag = Input.Press.Value;
				Input.Clear();
			}
			else if (HitTestGlobal(Input.Press.Value) == this)
			{
				m_lastDrag = Input.Press.Value;
			}
		}
		else
		{
			m_lastDrag = null;
			m_rotation = MathUtils.NormalizeAngle(m_rotation);
			if (MathUtils.Abs(m_rotation) > 0.01f)
			{
				m_rotation *= MathUtils.PowSign(0.1f, Time.FrameDuration);
			}
			else
			{
				m_rotation = 0f;
			}
		}
		Vector3 num = Vector3.One;
		switch (BodyTag)
		{
		case PlayerData.PlayerTag.Short:
			num = new Vector3(0.85f, 0.85f, 0.85f);
			break;
		case PlayerData.PlayerTag.Fat:
			num = new Vector3(1.5f, 1f, 1.5f);
			break;
		case PlayerData.PlayerTag.Tall:
			num = new Vector3(1f, 1f, 1f);
			break;
		case PlayerData.PlayerTag.Dwarf:
			num = new Vector3(1.25f, 0.7f, 1.25f);
			break;
		}
		if (m_modelWidget.Model != null)
		{
			ModelBone num4 = m_modelWidget.Model.FindBone("Hand1");
			ModelBone num5 = m_modelWidget.Model.FindBone("Hand2");
			if (num4 != null)
			{
				m_modelWidget.SetBoneTransform(num4.Index, m_modelWidget.GetBoneTransform(num4.Index));
			}
			if (num5 != null)
			{
				m_modelWidget.SetBoneTransform(num5.Index, m_modelWidget.GetBoneTransform(num5.Index));
			}
		}
		Matrix num6 = ((m_rotation != 0f) ? Matrix.CreateRotationY(m_rotation) : Matrix.Identity);
		float i = 0.9f * (1f - num.Y);
		m_modelWidget.ModelMatrix = Matrix.CreateTranslation(0f, i, 0f) * Matrix.CreateScale(num) * num6;
	}

	public override void MeasureOverride(Vector2 parentAvailableSize)
	{
		m_modelWidget.Model = (OuterClothing ? CharacterSkinsManager.GetOuterClothingModel(PlayerClass) : CharacterSkinsManager.GetPlayerModel(PlayerClass));
		if (CameraShot == Shot.Body)
		{
			m_modelWidget.ViewPosition = ((PlayerClass == PlayerClass.Male) ? new Vector3(0f, 1.46f, -3.2f) : new Vector3(0f, 1.39f, -3.04f));
			m_modelWidget.ViewTarget = ((PlayerClass == PlayerClass.Male) ? new Vector3(0f, 0.9f, 0f) : new Vector3(0f, 0.86f, 0f));
			m_modelWidget.ViewFov = 0.57f;
		}
		else
		{
			if (CameraShot != Shot.Bust)
			{
				throw new InvalidOperationException("Unknown shot.");
			}
			m_modelWidget.ViewPosition = ((PlayerClass == PlayerClass.Male) ? new Vector3(0f, 1.5f, -1.05f) : new Vector3(0f, 1.43f, -1f));
			m_modelWidget.ViewTarget = ((PlayerClass == PlayerClass.Male) ? new Vector3(0f, 1.5f, 0f) : new Vector3(0f, 1.43f, 0f));
			m_modelWidget.ViewFov = 0.57f;
		}
		ModelWidget modelWidget = m_modelWidget;
		Texture2D textureOverride;
		if (OuterClothing)
		{
			textureOverride = OuterClothingTexture;
		}
		else
		{
			textureOverride = ((CharacterSkinName != null) ? CharacterSkinsCache.GetTexture(CharacterSkinName) : CharacterSkinTexture);
		}
		modelWidget.TextureOverride = textureOverride;
		if (AnimateHeadSeed != 0)
		{
			int random = ((AnimateHeadSeed < 0) ? GetHashCode() : AnimateHeadSeed);
			float i = (float)MathUtils.Remainder(Time.FrameStartTime + 1000.0 * (double)random, 10000.0);
			Vector2 j = new Vector2
			{
				X = MathUtils.Lerp(-0.75f, 0.75f, SimplexNoise.OctavedNoise(i + 100f, 0.2f, 1, 2f, 0.5f)),
				Y = MathUtils.Lerp(-0.5f, 0.5f, SimplexNoise.OctavedNoise(i + 200f, 0.17f, 1, 2f, 0.5f))
			};
			Matrix num = Matrix.CreateRotationX(j.Y) * Matrix.CreateRotationZ(j.X);
			m_modelWidget.SetBoneTransform(m_modelWidget.Model.FindBone("Head").Index, num);
		}
		if (!OuterClothing && AnimateHandsSeed != 1)
		{
			int cellValueFast = ((AnimateHandsSeed < 0) ? GetHashCode() : AnimateHandsSeed);
			float num2 = (float)MathUtils.Remainder(Time.FrameStartTime + 1000.0 * (double)cellValueFast, 10000.0);
			Vector2 temperatureFast = new Vector2
			{
				X = MathUtils.Lerp(0.2f, 0f, SimplexNoise.OctavedNoise(num2 + 100f, 0.7f, 1, 2f, 0.5f)),
				Y = MathUtils.Lerp(-0.3f, 0.3f, SimplexNoise.OctavedNoise(num2 + 200f, 0.7f, 1, 2f, 0.5f))
			};
			Vector2 humidityFast = new Vector2
			{
				X = MathUtils.Lerp(-0.2f, 0f, SimplexNoise.OctavedNoise(num2 + 300f, 0.7f, 1, 2f, 0.5f)),
				Y = MathUtils.Lerp(-0.3f, 0.3f, SimplexNoise.OctavedNoise(num2 + 400f, 0.7f, 1, 2f, 0.5f))
			};
			Matrix num3 = Matrix.CreateRotationX(temperatureFast.Y) * Matrix.CreateRotationY(temperatureFast.X);
			Matrix data = Matrix.CreateRotationX(humidityFast.Y) * Matrix.CreateRotationY(humidityFast.X);
			m_modelWidget.SetBoneTransform(m_modelWidget.Model.FindBone("Hand1").Index, num3);
			m_modelWidget.SetBoneTransform(m_modelWidget.Model.FindBone("Hand2").Index, data);
		}
		base.MeasureOverride(parentAvailableSize);
	}

	public override void UpdateCeases()
	{
		if (RootWidget == null)
		{
			if (m_publicCharacterSkinsCache.ContainsTexture(m_modelWidget.TextureOverride))
			{
				m_modelWidget.TextureOverride = null;
			}
			m_publicCharacterSkinsCache.Clear();
		}
		base.UpdateCeases();
	}
}
