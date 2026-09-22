using System;
using UnityEngine;

namespace TalesTactics
{
    public sealed partial class BattleDirector
    {
        public bool StoryActive { get; private set; }
        public int StoryIndex { get; private set; }
        StoryLine[] storyLines;
        Action storyFinished;
        public bool CanReadEnding => Session != null && !battleTraining && !RewardPending && Session.Result == BattleResult.Victory;

        public void RequestBattle()
        {
            if (StoryActive || Session != null || Deployment.Count == 0 || !TrainingMode && !CampaignStages.Unlocked(Campaign, SelectedStage)) return;
            if (TrainingMode) { BeginBattle(); return; }
            OpenStory(SelectedStage, false, BeginBattle);
        }
        public void ReplayStory(bool after)
        {
            if (StoryActive || Session != null || !CampaignStages.Unlocked(Campaign, SelectedStage) ||
                after && !Campaign.StoryProgress.Contains(CampaignStages.Id(SelectedStage))) return;
            OpenStory(SelectedStage, after, Hud.ShowDeployment);
        }
        public void ReadEnding()
        {
            if (StoryActive || !CanReadEnding) return;
            OpenStory(battleStage, true, Hud.Refresh);
        }
        void OpenStory(int stage, bool after, Action finished)
        {
            storyLines = CampaignContent.Story(stage, after); storyFinished = finished;
            StoryIndex = 0; StoryActive = true; Audio.Play("story");
            Hud.ShowStoryLine(storyLines[StoryIndex], StoryIndex + 1, storyLines.Length);
        }
        public void AdvanceStory()
        {
            if (!StoryActive) return;
            if (++StoryIndex >= storyLines.Length) { FinishStory(); return; }
            Hud.ShowStoryLine(storyLines[StoryIndex], StoryIndex + 1, storyLines.Length);
        }
        public void FinishStory()
        {
            if (!StoryActive) return;
            var finished = storyFinished; CloseStory(); finished?.Invoke();
        }
        void CloseStory()
        {
            StoryActive = false; storyFinished = null; storyLines = null; StoryIndex = 0;
            if (Hud != null) Hud.HideStory();
        }
    }

    public sealed partial class BattleHud
    {
        RectTransform storyPanel;
        public void ShowStoryLine(StoryLine line, int number, int count)
        {
            if (storyPanel == null)
                storyPanel = Panel("CampaignStory", Vector2.zero, Vector2.one, new Vector2(20, 20), new Vector2(-20, -20));
            storyPanel.GetComponent<UnityEngine.UI.Image>().color = new Color(0.045f, 0.07f, 0.1f, 1f);
            storyPanel.gameObject.SetActive(true); storyPanel.SetAsLastSibling(); Clear(storyPanel);
            Label(storyPanel, "TALES / TACTICS · 이야기", 25, 45, 25);
            Label(storyPanel, line.Speaker, 100, 65, 24);
            var body = Label(storyPanel, line.Text, 180, 260, 22);
            body.enableAutoSizing = true; body.fontSizeMin = 16; body.fontSizeMax = 22;
            Label(storyPanel, number + " / " + count, 450, 35, 17);
            Button(storyPanel, number == count ? "이야기 마치기" : "다음 대사", 505, battle.AdvanceStory, true, 48);
            Button(storyPanel, "이야기 건너뛰기", 565, battle.FinishStory);
        }
        public void HideStory() { if (storyPanel != null) storyPanel.gameObject.SetActive(false); }
    }
}
