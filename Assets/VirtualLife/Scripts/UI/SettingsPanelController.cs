using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VirtualLife.Core;

namespace VirtualLife.UI
{
    /// <summary>
    /// Basic settings: master volume, graphics quality, resolution/fullscreen (PC) and a
    /// mobile graphics preset (Android). Values persist via PlayerPrefs for Stage 1.
    /// </summary>
    public sealed class SettingsPanelController : MonoBehaviour
    {
        private const string VolumeKey = "VL.MasterVolume";
        private const string QualityKey = "VL.Quality";
        private const string FullscreenKey = "VL.Fullscreen";
        private const string ResolutionKey = "VL.Resolution";
        private const string MobilePresetKey = "VL.MobilePreset";

        [SerializeField] private Slider masterVolume;
        [SerializeField] private Dropdown qualityDropdown;
        [SerializeField] private Dropdown resolutionDropdown;
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private Dropdown mobilePresetDropdown;
        [SerializeField] private Button backButton;
        [Tooltip("Objects shown only on desktop (PC).")]
        [SerializeField] private GameObject[] desktopOnly;
        [Tooltip("Objects shown only on mobile (Android).")]
        [SerializeField] private GameObject[] mobileOnly;

        public event Action BackRequested;

        private readonly List<Resolution> resolutions = new();
        private static readonly string[] MobilePresets = { "Battery Saver", "Balanced", "High" };

        private void Awake()
        {
            bool mobile = CoreServices.Platform?.IsMobile ?? Application.isMobilePlatform;
            SetActive(desktopOnly, !mobile);
            SetActive(mobileOnly, mobile);

            SetupVolume();
            SetupQuality();
            if (mobile) SetupMobilePreset(); else SetupDesktopDisplay();
            if (backButton != null) backButton.onClick.AddListener(() => BackRequested?.Invoke());
        }

        private static void SetActive(GameObject[] objects, bool value)
        {
            if (objects == null) return;
            foreach (GameObject go in objects) if (go != null) go.SetActive(value);
        }

        private void SetupVolume()
        {
            float volume = PlayerPrefs.GetFloat(VolumeKey, 1f);
            AudioListener.volume = volume;
            if (masterVolume == null) return;
            masterVolume.minValue = 0f; masterVolume.maxValue = 1f;
            masterVolume.SetValueWithoutNotify(volume);
            masterVolume.onValueChanged.AddListener(v =>
            {
                AudioListener.volume = v;
                PlayerPrefs.SetFloat(VolumeKey, v);
            });
        }

        private void SetupQuality()
        {
            int saved = Mathf.Clamp(PlayerPrefs.GetInt(QualityKey, QualitySettings.GetQualityLevel()), 0, QualitySettings.names.Length - 1);
            QualitySettings.SetQualityLevel(saved, true);
            if (qualityDropdown == null) return;
            qualityDropdown.ClearOptions();
            qualityDropdown.AddOptions(new List<string>(QualitySettings.names));
            qualityDropdown.SetValueWithoutNotify(saved);
            qualityDropdown.onValueChanged.AddListener(i =>
            {
                QualitySettings.SetQualityLevel(i, true);
                PlayerPrefs.SetInt(QualityKey, i);
            });
        }

        private void SetupMobilePreset()
        {
            if (mobilePresetDropdown == null) return;
            mobilePresetDropdown.ClearOptions();
            mobilePresetDropdown.AddOptions(new List<string>(MobilePresets));
            mobilePresetDropdown.SetValueWithoutNotify(PlayerPrefs.GetInt(MobilePresetKey, 1));
            mobilePresetDropdown.onValueChanged.AddListener(i =>
            {
                // Maps the preset onto the project's quality levels (lowest / middle / highest).
                int last = QualitySettings.names.Length - 1;
                int level = i == 0 ? 0 : i == 1 ? last / 2 : last;
                QualitySettings.SetQualityLevel(level, true);
                PlayerPrefs.SetInt(MobilePresetKey, i);
                PlayerPrefs.SetInt(QualityKey, level);
                if (qualityDropdown != null) qualityDropdown.SetValueWithoutNotify(level);
            });
        }

        private void SetupDesktopDisplay()
        {
            if (fullscreenToggle != null)
            {
                fullscreenToggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1);
                fullscreenToggle.onValueChanged.AddListener(_ => ApplyDisplay());
            }

            if (resolutionDropdown == null) return;
            var labels = new List<string>();
            int current = 0;
            foreach (Resolution r in Screen.resolutions)
            {
                string label = $"{r.width} x {r.height}";
                if (labels.Contains(label)) continue; // drop duplicate refresh-rate entries
                labels.Add(label);
                resolutions.Add(r);
                if (r.width == Screen.width && r.height == Screen.height) current = resolutions.Count - 1;
            }
            resolutionDropdown.ClearOptions();
            resolutionDropdown.AddOptions(labels);
            resolutionDropdown.SetValueWithoutNotify(Mathf.Clamp(PlayerPrefs.GetInt(ResolutionKey, current), 0, Mathf.Max(0, labels.Count - 1)));
            resolutionDropdown.onValueChanged.AddListener(_ => ApplyDisplay());
        }

        private void ApplyDisplay()
        {
            bool full = fullscreenToggle == null || fullscreenToggle.isOn;
            PlayerPrefs.SetInt(FullscreenKey, full ? 1 : 0);
            if (resolutionDropdown != null && resolutions.Count > 0)
            {
                int i = Mathf.Clamp(resolutionDropdown.value, 0, resolutions.Count - 1);
                PlayerPrefs.SetInt(ResolutionKey, i);
                Screen.SetResolution(resolutions[i].width, resolutions[i].height,
                    full ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
            }
            else Screen.fullScreen = full;
        }

        private void OnDisable() => PlayerPrefs.Save();
    }
}
