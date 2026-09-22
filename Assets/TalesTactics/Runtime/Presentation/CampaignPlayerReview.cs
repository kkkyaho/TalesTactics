#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TalesTactics
{
    // Opt-in development-player check. No code from this file enters release players.
    // Only the isolated CampaignFile callback is used; never click settings/equipment save buttons.
    public sealed class CampaignPlayerReview : MonoBehaviour
    {
        [Serializable] public sealed class Report
        {
            public string phase, unityVersion, runId, error;
            public bool passed, tactical;
            public List<string> captureWarnings = new List<string>();
            public float seconds;
            public int playerTurns, moves, attacks;
            public List<string> checks = new List<string>();
            public List<string> errors = new List<string>();
        }

        BattleDirector battle;
        CampaignFile store;
        string directory, savePath;
        Report report;
        float started;
        readonly int[] party = { 0, 2, 3, 6, 7, 9 };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Initialize()
        {
            if (Application.isEditor) return;
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "--campaign-review");
            if (index < 0) return;
            // A GUID, not a caller-supplied filesystem path, confines all writes to Reviews.
            if (index + 2 >= args.Length || !Guid.TryParseExact(args[index + 1], "N", out _) ||
                !new[] { "chapter1", "chapter2", "resume" }.Contains(args[index + 2]))
            { Debug.LogError("Invalid campaign review arguments."); Application.Quit(2); return; }
            var review = new GameObject("Campaign player review").AddComponent<CampaignPlayerReview>();
            review.report = new Report { phase = args[index + 2], runId = args[index + 1], unityVersion = Application.unityVersion };
            review.report.tactical=args.Contains("--tactical-review");
            review.directory = Path.Combine(Application.persistentDataPath, "Reviews", review.report.runId);
            review.savePath = Path.Combine(review.directory, "campaign.json");
        }

        IEnumerator Start()
        {
            started = Time.realtimeSinceStartup;
            Application.logMessageReceived += OnLog;
            Application.runInBackground = true;
            // Pump nested enumerators ourselves so a failed assertion always writes a report.
            var work = new Stack<IEnumerator>();
            work.Push(Run());
            Exception failure = null;
            while (work.Count > 0)
            {
                object next = null;
                try
                {
                    if (Time.realtimeSinceStartup - started > 300) throw new TimeoutException("Review exceeded 300 seconds.");
                    if (!work.Peek().MoveNext()) { work.Pop(); continue; }
                    next = work.Peek().Current;
                }
                catch (Exception e) { failure = e; break; }
                if (next is IEnumerator nested) work.Push(nested);
                else yield return next;
            }
            report.error = failure?.ToString();
            report.passed = failure == null && report.errors.Count == 0;
            report.seconds = Time.realtimeSinceStartup - started;
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, report.phase + ".json"), JsonUtility.ToJson(report, true));
            Application.logMessageReceived -= OnLog;
            Application.Quit(report.passed ? 0 : 1);
        }

        void OnLog(string message, string trace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                report.errors.Add(message + "\n" + trace);
        }

        void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
            report.checks.Add(message);
        }

        Button FindButton(Func<Button, bool> predicate) =>
            UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Single(predicate);

        void Click(string name)
        {
            var button = FindButton(b => b.name == name);
            if (!button.interactable) throw new InvalidOperationException("Button unavailable: " + name);
            ExecuteEvents.Execute(button.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
            if(name=="전투 시작")while(battle.StoryActive)battle.AdvanceStory();
        }

        IEnumerator Run()
        {
            yield return null; // Allow the scene's normal Start methods to initialize.
            battle = UnityEngine.Object.FindAnyObjectByType<BattleDirector>();
            Check(battle != null && battle.enabled, "Scene initialized");
            Check(report.phase == "chapter1" ? !File.Exists(savePath) : File.Exists(savePath), "Expected isolated save exists/missing");
            store = new CampaignFile(savePath);
            battle.Campaign = store.Load();
            Check(store.CanSave && string.IsNullOrEmpty(store.Notice), "Save loads without fallback or corruption");
            battle.PersistCampaign = store.Save;
            battle.TrainingMode = false;
            battle.UseCT=battle.UseUtilityAI=report.tactical;
            battle.Deployment.Clear();
            battle.Deployment.AddRange(party);
            battle.Hud.ShowDeployment();
            yield return null;
            Time.timeScale = 4;
            if (report.phase == "chapter1") yield return BuyAndEquipArmor();

            if (report.phase == "resume")
            {
                VerifyProgress(2);
                var original = File.ReadAllBytes(savePath);
                var completed = FindButton(b => b.name.Contains(CampaignStages.Title(1)));
                Check(completed.interactable && completed.name.Contains("완료"), "Reloaded chapter 2 completion appears in deployment UI");
                yield return Capture("resume-deployment");
                // A normal, deliberately passive one-person party loses through enemy attacks.
                battle.Deployment.Clear(); battle.Deployment.Add(1);
                battle.Hud.ShowDeployment(); yield return null;
                Click("전투 시작");
                yield return Fight(false);
                Check(battle.Session.Result == BattleResult.Defeat, "Passive party reaches defeat without forced damage");
                Check(File.ReadAllBytes(savePath).SequenceEqual(original), "Defeat leaves saved progress byte-for-byte unchanged");
                yield return Capture("defeat");
                Click("출전 화면 / Restart"); yield return null;
                Check(battle.Session == null, "Defeat restart returns to deployment");
                battle.Deployment.Clear(); battle.Deployment.AddRange(party);
                battle.Hud.ShowDeployment(); yield return null;
                Click("전투 시작"); yield return null;
                Check(battle.Session.Units.Where(u => u.Team == Team.Player).All(u => u.Alive && u.CurrentHP == u.Stats.HP), "New battle restores all party members to full health");
                Check(File.ReadAllBytes(savePath).SequenceEqual(original), "Starting another battle does not grant duplicate rewards");
            }
            else
            {
                int stage = report.phase == "chapter1" ? 0 : 1;
                if (stage == 0)
                {
                    Check(!CampaignStages.Unlocked(battle.Campaign, 1), "New campaign has chapter 2 locked");
                    Check(!FindButton(b => b.name.Contains(CampaignStages.Title(1))).interactable, "Locked chapter button is disabled");
                }
                else
                {
                    VerifyProgress(1);
                    Click(FindButton(b => b.name.Contains(CampaignStages.Title(1))).name);
                    yield return null;
                    Check(battle.SelectedStage == 1, "Reloaded chapter 2 can be selected through UI");
                }
                Click("전투 시작"); yield return null;
                Check(battle.Session.Units.First(u => u.Team == Team.Enemy).Level == 1 + stage * 2, "Correct campaign enemy level");
                yield return Fight(true);
                Check(battle.Session.Result == BattleResult.Victory, "Normal attacks complete chapter " + (stage + 1));
                Check(!battle.RewardPending, "Victory reward saved on first attempt");
                VerifyProgress(stage + 1);
                var saved = File.ReadAllBytes(savePath);
                battle.CompleteBattle(); battle.SaveBattleReward();
                Check(File.ReadAllBytes(savePath).SequenceEqual(saved), "Repeated completion does not duplicate saved rewards");
                Click("전투 후 이야기"); yield return null;
                Check(battle.StoryActive,"Victory opens chapter ending dialogue");
                while(battle.StoryActive){battle.AdvanceStory();yield return null;}
                Check(File.ReadAllBytes(savePath).SequenceEqual(saved),"Reading ending does not change saved rewards");
                yield return Capture("victory");
                Click("출전 화면 / Restart"); yield return null;
                Check(battle.Session == null, "Victory restart returns to deployment");
                Check(FindButton(b => b.name.Contains(CampaignStages.Title(1))).interactable, "Chapter 2 is available after victory");
            }
            Time.timeScale = 1;
        }

        IEnumerator BuyAndEquipArmor()
        {
            var armor=battle.Catalog.Equipment.Single(e=>e.Id=="leather-armor");
            Click("장비 상점");yield return null;
            Click(armor.DisplayName+" · "+armor.BuyPrice+"G");yield return null;
            Click("구매 · 저장");yield return null;
            Check(battle.Campaign.Gold==CampaignInventory.StartingGold-armor.BuyPrice,"Shop deducts exact purchase price");
            Check(CampaignInventory.Owned(battle.Campaign,armor.Id)==1,"Shop grants one owned armor");
            Click("출전 준비로");yield return null;Click("장비 관리");yield return null;
            Click(battle.Catalog.Characters[0].DisplayName);yield return null;
            Click("방어구: 없음");yield return null;Click("적용 · 저장");yield return null;
            Check(new CampaignFile(savePath).Load().Get(battle.Catalog.Characters[0].Id).Equipment[(int)EquipmentSlot.Armor]==armor.Id,"Equipment UI persists purchased armor");
            Click("돌아가기 (미적용 취소)");yield return null;Click("출전 준비로");yield return null;
        }
        void VerifyProgress(int chapters)
        {
            var loaded = new CampaignFile(savePath).Load();
            Check(loaded.StoryProgress.Count == chapters && Enumerable.Range(0, chapters).All(i => loaded.StoryProgress.Contains(CampaignStages.Id(i))), "Disk contains exactly the expected completion flags");
            Check(party.All(i => { var p = loaded.Get(battle.Catalog.Characters[i].Id); return p.Level == 2 && p.EXP == chapters * 120 - 100; }), "Disk contains exact party levels and EXP");
            Check(!loaded.Characters.Any(c => c.Promoted), "No premature promotion");
            var armor=battle.Catalog.Equipment.Single(e=>e.Id=="leather-armor");
            Check(loaded.Version==2,"Reload uses schema version 2");
            for(int stage=0;stage<chapters;stage++)Check(CampaignInventory.Owned(loaded,CampaignStages.EquipmentReward(stage))==1,"Disk contains chapter equipment reward "+stage);
            Check(loaded.Gold==CampaignInventory.StartingGold-armor.BuyPrice+Enumerable.Range(0,chapters).Sum(CampaignStages.GoldReward),"Disk contains exact purchase and victory gold");
            Check(CampaignInventory.Owned(loaded,armor.Id)==1&&loaded.Get(battle.Catalog.Characters[0].Id).Equipment[(int)EquipmentSlot.Armor]==armor.Id,"Purchased and equipped armor survives process restart");
        }

        IEnumerator Fight(bool attack)
        {
            int turns = 0;
            while (!(battle.State is BattleEndState))
            {
                if (!battle.IsPlayerCommand) { yield return null; continue; }
                if (++turns > 200) throw new InvalidOperationException("Battle exceeded 200 player turns.");
                report.playerTurns++;
                var unit = battle.Session.Active;
                var plan = new EnemyPlanner().Plan(battle.Session, unit);
                if (attack && plan.Destination != unit.Position)
                {
                    Click("Move / 이동"); battle.State.Tile(plan.Destination);
                    while (battle.State is ActionExecutionState) yield return null;
                    Check(unit.Position == plan.Destination, "Legal movement completed"); report.moves++;
                }
                if (attack && plan.Target != null)
                {
                    if(report.tactical)battle.SelectSkill(plan.Skill);else Click("Attack / 공격");
                    battle.State.Tile(plan.Aim??plan.Target.Position);
                    Check(battle.Target.HasValue, "Attack preview selected a valid target");
                    Click("실행");
                    while (battle.State is ActionExecutionState) yield return null;
                    Check(unit.Acted, "Normal attack consumed the action"); report.attacks++;
                }
                if (battle.State is BattleEndState) break;
                Click("Wait / 방향 선택");
                Click(unit.Facing.ToString());
                yield return null;
            }
        }

        IEnumerator Capture(string name)
        {
            Directory.CreateDirectory(directory);
            yield return new WaitForEndOfFrame();
            var image = ScreenCapture.CaptureScreenshotAsTexture();
            try
            {
                if (image.GetPixels32().Any(p => p.r != 0 || p.g != 0 || p.b != 0))
                    File.WriteAllBytes(Path.Combine(directory, report.phase + "-" + name + ".png"), image.EncodeToPNG());
                else report.captureWarnings.Add(name + ": black capture in hidden player; not visual validation evidence.");
            }
            finally { Destroy(image); }
        }
    }
}
#endif
