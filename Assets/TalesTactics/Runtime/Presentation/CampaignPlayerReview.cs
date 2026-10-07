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
    // Every save and settings operation uses the isolated review storage root.
    public sealed class CampaignPlayerReview : MonoBehaviour
    {
        [Serializable] public sealed class Report
        {
            public string phase, unityVersion, runId, error;
            public bool passed, tactical;
            public List<string> captureWarnings = new List<string>();
            public float seconds;
            public int playerTurns, moves, attacks;
            public int enemySpeed;
            public bool briefEnemies;
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
                !Enumerable.Range(0,CampaignStages.Count).Select(CampaignStages.Id).Concat(new[]{"resume"}).Contains(args[index + 2]))
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

        IEnumerator ReviewGamepad()
        {
            // The review runner intentionally hides the window. Permit its synthetic device
            // while unfocused, then restore the normal player's background-input policy.
            var background=UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior;
            UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
            var pad=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Gamepad>();
            try
            {
                yield return new WaitForSecondsRealtime(.25f);
                var pointer=battle.GetComponent<GamepadPointer>();
                var button=FindButton(b=>b.name=="장비 상점");Canvas.ForceUpdateCanvases();
                var rect=(RectTransform)button.transform;
                pointer.MoveTo(RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center)));
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(pad,new UnityEngine.InputSystem.LowLevel.GamepadState().WithButton(UnityEngine.InputSystem.LowLevel.GamepadButton.South));
                yield return null;yield return null;
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(pad,new UnityEngine.InputSystem.LowLevel.GamepadState());yield return null;
                Check(FindButton(b=>b.name=="출전 준비로")!=null,"Gamepad event opens shop in player; active="+pointer.Active+" deviceEnabled="+pad.enabled+" focused="+Application.isFocused);
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(pad,new UnityEngine.InputSystem.LowLevel.GamepadState().WithButton(UnityEngine.InputSystem.LowLevel.GamepadButton.East));
                yield return null;yield return null;
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(pad,new UnityEngine.InputSystem.LowLevel.GamepadState());yield return null;
                Check(FindButton(b=>b.name=="전투 시작")!=null,"Gamepad cancel returns to deployment in player");
            }
            finally{UnityEngine.InputSystem.InputSystem.RemoveDevice(pad);UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=background;}
            yield return null;
            Check(!battle.GetComponent<GamepadPointer>().Active,"Gamepad removal restores mouse UI in player");
        }

        IEnumerator ReviewMusic()
        {
            var entries=battle.Audio.Library.Entries.Where(e=>e.Id=="battle"||e.Id=="boss"||e.Id=="story"||e.Id=="victory"||e.Id.EndsWith(".theme")).ToArray();
            Check(entries.Length==14&&entries.All(e=>e.Clip!=null),"All 14 music tracks included in player");
            var samples=new float[1024];var source=battle.Audio.GetComponent<AudioSource>();
            foreach(var entry in entries)
            {
                battle.Audio.Play(entry.Id);source.GetOutputData(samples,0);
                float rms=0,deadline=Time.realtimeSinceStartup+2;
                do
                {
                    yield return new WaitForSecondsRealtime(0.05f);
                    source.GetOutputData(samples,0);float power=0;foreach(float sample in samples)power+=sample*sample;
                    rms=Mathf.Sqrt(power/samples.Length);
                }while(rms<=0.00001f&&Time.realtimeSinceStartup<deadline);
                Check(source.isPlaying&&source.timeSamples>0&&rms>0.00001f,"Music signal "+entry.Id+" RMS="+rms.ToString("F5",System.Globalization.CultureInfo.InvariantCulture)
                    +" playing="+source.isPlaying+" samples="+source.timeSamples+" volume="+source.volume+" listener="+AudioListener.volume+" paused="+AudioListener.pause+" loaded="+entry.Clip.loadState);
            }
            battle.Audio.StopAll();
        }

        IEnumerator Run()
        {
            yield return null; // Allow the scene's normal Start methods to initialize.
            battle = UnityEngine.Object.FindAnyObjectByType<BattleDirector>();
            Check(battle != null && battle.enabled, "Scene initialized");
            var font=Resources.Load<TMPro.TMP_FontAsset>("TalesTactics/Korean");
            Check(font!=null&&battle.Hud.Font==font&&font.atlasPopulationMode==TMPro.AtlasPopulationMode.Static,"Bundled Korean font selected without OS font lookup");
            Check(font.HasCharacters("전투 출전 승리 패배 장비 사후폭쇄진 魔神剣",out uint[] missing,true,true),"Bundled Korean and CJK glyph coverage");
            Check(File.Exists(Path.Combine(Application.streamingAssetsPath,"Licenses/NotoSansCJK-OFL.txt"))&&File.Exists(Path.Combine(Application.streamingAssetsPath,"Licenses/NotoSansCJK-NOTICE.txt")),"Font license and copyright included in player");
            yield return ReviewGamepad();
            yield return ReviewMusic();
            Check(report.phase == "chapter1" ? !File.Exists(savePath) : File.Exists(savePath), "Expected isolated save exists/missing");
            battle.ConfigureStorage(directory);
            store = battle.Profiles.Store;
            Check(store.CanSave && string.IsNullOrEmpty(store.Notice), "Save loads without fallback or corruption");

            battle.RewardRoll=()=>2500; // Deterministic 15% drop branch; distribution is checked separately.
            battle.TrainingMode = false;
            battle.UseCT=battle.UseUtilityAI=report.tactical;
            var flow=JsonUtility.FromJson<PlayerPreferences>(JsonUtility.ToJson(battle.Preferences));
            flow.CT=flow.Utility=report.tactical;
            flow.EnemySpeedMode=report.phase=="chapter2"?1:report.phase=="chapter3"?2:0;
            flow.SkipEnemyAnimations=report.phase=="chapter4"||report.phase=="chapter5";
            Check(battle.SavePreferences(flow,false),"Enemy presentation settings persisted in isolated profile");
            report.enemySpeed=1<<flow.EnemySpeedMode;report.briefEnemies=flow.SkipEnemyAnimations;
            battle.Deployment.Clear();
            battle.Deployment.AddRange(party);
            battle.Hud.ShowDeployment();
            yield return null;
            Time.timeScale = 4;
            if (report.phase == "chapter1") { yield return ReviewTutorial(); yield return BuyAndEquipArmor(); }

            if (report.phase == "resume")
            {
                VerifyProgress(CampaignStages.Count);
                yield return SelectChapter(CampaignStages.Count-1);
                var original = File.ReadAllBytes(savePath);
                var completed = FindButton(b => b.name.Contains(CampaignStages.Title(CampaignStages.Count-1)));
                Check(completed.interactable && completed.name.Contains("완료"), "Reloaded final chapter completion appears in deployment UI");
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
                int stage = int.Parse(report.phase.Substring("chapter".Length))-1;
                if (stage == 0)
                {
                    Check(!CampaignStages.Unlocked(battle.Campaign, 1), "New campaign has chapter 2 locked");
                    Check(!FindButton(b => b.name.Contains(CampaignStages.Title(1))).interactable, "Locked chapter button is disabled");
                }
                else
                {
                    VerifyProgress(stage);
                    yield return SelectChapter(stage);
                    yield return null;
                    Check(battle.SelectedStage == stage, "Reloaded chapter " + (stage + 1) + " can be selected through UI");
                }
                Click("전투 시작"); yield return null;
                Check(battle.Session.Units.First(u => u.Team == Team.Enemy).Level == CampaignStages.EnemyLevel(stage), "Correct campaign enemy level");
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
                Check(CampaignStages.Unlocked(battle.Campaign,Math.Min(stage+1,CampaignStages.Count-1)), "Next chapter is available after victory");
            }
            Time.timeScale = 1;
        }

        IEnumerator ReviewTutorial()
        {
            var original=JsonUtility.ToJson(battle.Campaign);var deployment=battle.Deployment.ToArray();
            Click("처음 플레이 · 도움말");yield return null;
            Check(battle.Hud.HelpOpen,"Player opens first-play help");
            Click("입문 연습 시작");yield return null;
            Check(battle.Tutorial==TutorialStep.Movement,"Player enters isolated tutorial");
            Click("Move / 이동");battle.State.Tile(BattleDirector.TutorialDestination);
            while(battle.State is ActionExecutionState)yield return null;
            Check(battle.Tutorial==TutorialStep.Attack,"Tutorial movement advances only after completion");
            Click("Attack / 공격");battle.CycleTarget(1);battle.Confirm();
            while(battle.State is ActionExecutionState)yield return null;
            Check(battle.Tutorial==TutorialStep.Healing,"Tutorial attack advances to Mint");
            var heal=battle.Session.Active.Data.Skills.Single(s=>s.Id=="mint.0");int mp=battle.Session.Active.CurrentMP;
            Click("Skill / 스킬");Click(heal.DisplayName+" · MP"+heal.MPCost);battle.CycleTarget(1);battle.Confirm();
            while(battle.State is ActionExecutionState)yield return null;
            Check(battle.Tutorial==TutorialStep.Waiting&&battle.Session.Active.CurrentMP==mp-heal.MPCost,"Tutorial healing restores HP through normal resolver and spends MP");
            Click("Wait / 방향 선택");Click("Front");yield return null;
            Check(battle.Tutorial==TutorialStep.Complete,"Tutorial finishes after facing selection");
            Click("출전 준비로");yield return null;
            Check(!battle.TutorialActive&&battle.Session==null&&JsonUtility.ToJson(battle.Campaign)==original&&battle.Deployment.SequenceEqual(deployment)&&!File.Exists(savePath),"Tutorial leaves campaign, deployment and save untouched");
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
            Click("방어구: 없음");yield return null;Click("장착 후보: "+armor.DisplayName);yield return null;Click("적용 · 저장");yield return null;
            Check(new CampaignFile(savePath).Load().Get(battle.Catalog.Characters[0].Id).Equipment[(int)EquipmentSlot.Armor]==armor.Id,"Equipment UI persists purchased armor");
            Click("돌아가기 (미적용 취소)");yield return null;Click("출전 준비로");yield return null;
        }
        IEnumerator SelectChapter(int stage)
        {
            while(battle.Hud.ChapterPage<stage/3){Click("다음 장 목록");yield return null;}
            while(battle.Hud.ChapterPage>stage/3){Click("이전 장 목록");yield return null;}
            Click(FindButton(b=>b.name.Contains(CampaignStages.Title(stage))).name);yield return null;
        }
        void VerifyProgress(int chapters)
        {
            var loaded=new CampaignFile(savePath).Load();
            Check(loaded.StoryProgress.Count==chapters&&Enumerable.Range(0,chapters).All(i=>loaded.StoryProgress.Contains(CampaignStages.Id(i))),"Disk contains exactly expected chapter flags");
            int[] levels={2,3,4,5,7,9},exp={20,0,0,200,0,0},gold={120,180,240,320,420,560};
            Check(party.All(i=>{var p=loaded.Get(battle.Catalog.Characters[i].Id);return p.Level==levels[chapters-1]&&p.EXP==exp[chapters-1];}),"Disk contains exact party levels and EXP");
            Check(!loaded.Characters.Any(c=>c.Promoted),"No premature promotion");
            Check(loaded.Version==2,"Reload uses schema version 2");
            Check(loaded.Gold==150+gold.Take(chapters).Sum(),"Disk contains exact purchase and victory gold");
            Check(CampaignInventory.Owned(loaded,"vital-charm")==1,"First chapter charm persists");
            Check(CampaignInventory.Owned(loaded,"iron-sword")== (chapters>=2?1:0),"Second chapter sword persists");
            Check(CampaignInventory.Owned(loaded,"reinforced-armor")==Math.Max(0,chapters-1)+(chapters>=3?1:0),"All deterministic armor drops persist");
            Check(CampaignInventory.Owned(loaded,"guardian-medal")== (chapters>=4?1:0)+(chapters>=6?1:0),"Medal rewards persist");
            Check(CampaignInventory.Owned(loaded,"tempered-armor")== (chapters>=5?1:0),"Tempered armor reward persists");
            Check(CampaignInventory.Owned(loaded,"leather-armor")==2&&loaded.Get(battle.Catalog.Characters[0].Id).Equipment[(int)EquipmentSlot.Armor]=="leather-armor","Purchased equipment survives process restart");
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
                    battle.Confirm();
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
