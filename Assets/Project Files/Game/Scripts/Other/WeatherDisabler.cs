using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    public class WeatherDisabler : MonoBehaviour
    {
        [MissionPicker]
        [SerializeField] string disableWeatherUntillMissionWithIdCompleted;

        private Mission missionRef;
        private bool subscribedToTheMission = false;

        public void Start()
        {
            missionRef = MissionsController.GetMissionById(disableWeatherUntillMissionWithIdCompleted);

            if (missionRef != null)
            {
                if (missionRef.MissionStage != Mission.Stage.Collected)
                {
                    if (!subscribedToTheMission)
                        missionRef.OnStageChanged += OnStageChanged;

                    EnvironmentController.ApplyPreset(EnvironmentController.Database.GetPreset(EnvironmentPresetType.World1), PartOfDay.Day);
                    EnvironmentController.ApplyWeather(EnvironmentController.Database.GetPreset(EnvironmentPresetType.World1).Weather[3].WeatherPreset); // 3 is clear

                    subscribedToTheMission = true;
                }
            }
        }

        private void OnStageChanged(Mission.Stage previousStage, Mission.Stage currentStage)
        {
            if (currentStage == Mission.Stage.Collected)
            {
                EnvironmentController.DayNightEnabled = true;
                EnvironmentController.WeatherEnabled = true;

                missionRef.OnStageChanged -= OnStageChanged;
            }
        }
    }
}
