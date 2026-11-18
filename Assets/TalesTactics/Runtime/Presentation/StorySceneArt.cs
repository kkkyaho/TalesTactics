using UnityEngine;
using UnityEngine.UI;

namespace TalesTactics
{
    // Reuse authored chapter panoramas without stretching on a different aspect ratio.
    [RequireComponent(typeof(RawImage))]
    public sealed class StorySceneArt : MonoBehaviour
    {
        static readonly string[] Panoramas = { "Ruins", "Sanctum", "Canyon", "Observatory", "Sluice", "Relay" };
        RawImage art;

        public static string Resource(int stage) => CrossoverCampaign.IsMain(stage)||PersonalScenarios.Contains(stage)
            ? "TalesTactics/TerrainArt/" + Panoramas[CrossoverCampaign.TerrainTheme(stage)]
            : "TalesTactics/FormationBackdrop";

        public static string Location(int stage)
        {
            if(CrossoverTacticalMissions.Contains(stage))return CrossoverTacticalMissions.Get(stage).Location;
            if(PersonalScenarios.Contains(stage))return "야영지 · "+PersonalScenarios.Arc(stage).Name+"의 이야기";
            if (!CrossoverCampaign.IsMain(stage)) return CampaignContent.Location(stage);
            switch (CrossoverCampaign.Episode(stage).Number)
            {
                case 1: return "무너진 유적 · 구조 신호가 남은 길";
                case 2: return "감옥 외곽 · 두 개의 봉쇄 장치";
                case 3: return "갈라진 마을 · 세 다리의 피난로";
                default: return "경계를 잇는 의지";
            }
        }

        public void Initialize(int stage)
        {
            art = GetComponent<RawImage>();
            art.texture = Resources.Load<Texture2D>(Resource(stage)) ?? Resources.Load<Texture2D>("TalesTactics/FormationBackdrop");
            art.color = new Color(.78f, .82f, .88f, 1);
            art.raycastTarget = false;
            Fit();
        }

        void OnRectTransformDimensionsChange() { Fit(); }
        void Fit()
        {
            if (art == null || art.texture == null) return;
            var rect = ((RectTransform)transform).rect;
            if (rect.width <= 0 || rect.height <= 0) return;
            float screenAspect = rect.width / rect.height;
            float textureAspect = (float)art.texture.width / art.texture.height;
            float width = Mathf.Min(1, screenAspect / textureAspect);
            float height = Mathf.Min(1, textureAspect / screenAspect);
            art.uvRect = new Rect((1 - width) * .5f, (1 - height) * .5f, width, height);
        }
    }
}