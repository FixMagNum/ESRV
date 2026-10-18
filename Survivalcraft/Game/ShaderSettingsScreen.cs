using System.Xml.Linq;
using Engine;

namespace Game;

public class ShaderSettingsScreen : Screen
{
	private string fName;

	private ButtonWidget m_plantWindButton;

	private ButtonWidget m_heatBlurButton;

	private ButtonWidget m_treeSwayButton;

	private ButtonWidget m_waterWavesButton;

	private SliderWidget m_shaderRangeSlider;

	private Widget m_treeHeightPanel;

	private SliderWidget m_treeHeightSlider;

	public ShaderSettingsScreen()
	{
		fName = GetType().Name;
		XElement text2 = ContentManager.Get<XElement>("Screens/ShaderSettingsScreen");
		LoadContents(this, text2);
		m_plantWindButton = Children.Find<ButtonWidget>("PlantWindButton");
		m_heatBlurButton = Children.Find<ButtonWidget>("HeatBlurButton");
		m_treeSwayButton = Children.Find<ButtonWidget>("TreeSwayButton");
		m_waterWavesButton = Children.Find<ButtonWidget>("WaterWavesButton");
		m_shaderRangeSlider = Children.Find<SliderWidget>("ShaderRangeSlider");
		m_shaderRangeSlider.MinValue = 0f;
		m_shaderRangeSlider.Granularity = 1f;
		m_treeHeightPanel = Children.Find<Widget>("TreeBandHeightPanel");
		m_treeHeightSlider = Children.Find<SliderWidget>("TreeBandHeightSlider");
		m_treeHeightSlider.MinValue = 6f;
		m_treeHeightSlider.MaxValue = 50f;
		m_treeHeightSlider.Granularity = 1f;
	}

	public override void Update()
	{
		if (Input.Back || Input.Cancel || Children.Find<ButtonWidget>("TopBar.Back").IsClicked)
		{
			ScreensManager.SwitchScreen(ScreensManager.PreviousScreen);
			return;
		}
		int visibilityIndex = GetVisibilityIndex();
		m_shaderRangeSlider.MaxValue = visibilityIndex;
		if (m_plantWindButton.IsClicked)
		{
			SettingsManager.PlantWindAnimationsEnabled = !SettingsManager.PlantWindAnimationsEnabled;
		}
		if (m_heatBlurButton.IsClicked)
		{
			SettingsManager.HeatAndUnderwaterBlurEnabled = !SettingsManager.HeatAndUnderwaterBlurEnabled;
		}
		if (m_treeSwayButton.IsClicked)
		{
			SettingsManager.TreeSwayEnabled = !SettingsManager.TreeSwayEnabled;
		}
		if (m_waterWavesButton.IsClicked)
		{
			SettingsManager.WaterWavesAnimationEnabled = !SettingsManager.WaterWavesAnimationEnabled;
		}
		m_plantWindButton.Text = (SettingsManager.PlantWindAnimationsEnabled ? LanguageControl.Yes : LanguageControl.No);
		m_heatBlurButton.Text = (SettingsManager.HeatAndUnderwaterBlurEnabled ? LanguageControl.Yes : LanguageControl.No);
		m_treeSwayButton.Text = (SettingsManager.TreeSwayEnabled ? LanguageControl.Yes : LanguageControl.No);
		m_waterWavesButton.Text = (SettingsManager.WaterWavesAnimationEnabled ? LanguageControl.Yes : LanguageControl.No);
		m_treeHeightPanel.IsVisible = SettingsManager.TreeSwayEnabled;
		if (m_shaderRangeSlider.IsSliding)
		{
			int index = MathUtils.Clamp((int)m_shaderRangeSlider.Value, 0, visibilityIndex);
			SettingsManager.ShadersRenderRange = SettingsPerformanceScreen.m_visibilityRanges[index];
		}
		int value = MathUtils.Min(SettingsManager.ShadersRenderRange, SettingsManager.VisibilityRange);
		int indexForValue = GetIndexForValue(value);
		m_shaderRangeSlider.Value = MathUtils.Clamp(indexForValue, 0, visibilityIndex);
		m_shaderRangeSlider.Text = SettingsPerformanceScreen.m_visibilityRanges[(int)m_shaderRangeSlider.Value].ToString();
		if (m_treeHeightSlider.IsSliding)
		{
			SettingsManager.TreeSwayBandHeight = (int)m_treeHeightSlider.Value;
		}
		m_treeHeightSlider.Value = SettingsManager.TreeSwayBandHeight;
		int treeSwayBandHeight = SettingsManager.TreeSwayBandHeight;
		if (treeSwayBandHeight == 40)
		{
			m_treeHeightSlider.Text = LanguageControl.Get("ShaderSettingsScreen", "TreeBandHeightNormal");
		}
		else
		{
			m_treeHeightSlider.Text = treeSwayBandHeight.ToString();
		}
	}

	private int GetVisibilityIndex()
	{
		int num = SettingsPerformanceScreen.m_visibilityRanges.IndexOf(SettingsManager.VisibilityRange);
		if (num >= 0)
		{
			return num;
		}
		int result = 0;
		int visibilityRange = SettingsManager.VisibilityRange;
		for (int i = 0; i < SettingsPerformanceScreen.m_visibilityRanges.Count && SettingsPerformanceScreen.m_visibilityRanges[i] <= visibilityRange; i++)
		{
			result = i;
		}
		return result;
	}

	private int GetIndexForValue(int value)
	{
		int item = SettingsPerformanceScreen.m_visibilityRanges.IndexOf(value);
		if (item >= 0)
		{
			return item;
		}
		int result = 0;
		for (int i = 0; i < SettingsPerformanceScreen.m_visibilityRanges.Count && SettingsPerformanceScreen.m_visibilityRanges[i] <= value; i++)
		{
			result = i;
		}
		return result;
	}
}
