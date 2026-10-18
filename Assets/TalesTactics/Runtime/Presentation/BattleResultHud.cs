using UnityEngine;
namespace TalesTactics
{
    public sealed partial class BattleHud
    {
        RectTransform resultPanel;
        bool abandoningReward;
        void HideResult()
        {
            if(resultPanel!=null)resultPanel.gameObject.SetActive(false);
            if(left!=null)left.gameObject.SetActive(true);if(commands!=null)commands.gameObject.SetActive(true);
            abandoningReward=false;
        }
        void DrawResult()
        {
            if(resultPanel==null)resultPanel=Panel("BattleResults",Vector2.one*.5f,Vector2.one*.5f,Vector2.zero,Vector2.zero);
            resultPanel.sizeDelta=new Vector2(Mathf.Min(1040,canvas.rect.width-72),Mathf.Min(battle.GrowthResults.Length>2?760:600,canvas.rect.height-180));
            resultPanel.anchoredPosition=Vector2.zero;
            resultPanel.GetComponent<UnityEngine.UI.Image>().color=new Color(.025f,.045f,.08f,1);
            resultPanel.gameObject.SetActive(true);Clear(resultPanel);left.gameObject.SetActive(false);commands.gameObject.SetActive(false);
            bool win=battle.Session.Result==BattleResult.Victory;
            FormationText(resultPanel,win?"승리":"패배",new Vector2(.05f,.88f),new Vector2(.95f,.99f),34,TMPro.TextAlignmentOptions.Center);
            FormationText(resultPanel,battle.ResultStage,new Vector2(.05f,.81f),new Vector2(.95f,.89f),21,TMPro.TextAlignmentOptions.Center);
            string status=battle.RewardPending?"저장 실패 · 보상 미적용":win&&!battle.ResultTraining?"보상 저장 완료":"저장 보상 없음";
            var notice=FormationText(resultPanel,status,new Vector2(.05f,.75f),new Vector2(.95f,.82f),18,TMPro.TextAlignmentOptions.Center);notice.color=battle.RewardPending?new Color(1,.65f,.4f):formationTeal;
            if(battle.GrowthResults.Length>0)
            {
                int rewards=1+battle.ItemResults.Length;
                for(int i=0;i<rewards;i++)
                {
                    float x=.04f+i*.92f/rewards,w=.92f/rewards;
                    var tile=ResultTile("Reward-"+i,new Vector2(x,.59f),new Vector2(x+w-.01f,.74f),i>0);
                    if(i==0){FormationIcon(tile,"gold",new Vector2(.03f,.1f),new Vector2(.28f,.9f),Color.white);FormationText(tile,"골드 +"+battle.GoldResult.ToString("N0"),new Vector2(.3f,.12f),new Vector2(.98f,.88f),21);}
                    else
                    {
                        var reward=battle.ItemResults[i-1];tile.name="보상 상세: "+reward.Item.Id;
                        var button=tile.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=tile.GetComponent<UnityEngine.UI.Image>();button.targetGraphic.raycastTarget=true;
                        button.onClick.AddListener(()=>ShowRewardItemDetails(reward,tile));StyleButton(tile);
                        EquipmentArt(tile,reward.Item,new Vector2(.02f,.08f),new Vector2(.28f,.92f));
                        FormationText(tile,reward.Item.DisplayName+" ×"+reward.Count,new Vector2(.30f,.30f),new Vector2(.98f,.94f),19);
                        FormationText(tile,"선택하여 상세 확인",new Vector2(.30f,.03f),new Vector2(.98f,.31f),14).color=formationTeal;
                    }
                }
                FormationText(resultPanel,"캐릭터 성장",new Vector2(.04f,.52f),new Vector2(.96f,.59f),20);
                int rows=Mathf.Max(1,(battle.GrowthResults.Length+1)/2);float height=.36f/rows;
                for(int i=0;i<battle.GrowthResults.Length;i++)
                {
                    var growth=battle.GrowthResults[i];float x=.04f+i%2*.47f,top=.52f-i/2*height;
                    var row=ResultTile("Growth-"+growth.Character.Id,new Vector2(x,top-height+.008f),new Vector2(x+.45f,top-.008f));
                    IntermissionPortrait(row,growth.Character,new Vector2(.01f,.04f),new Vector2(.24f,.96f));
                    FormationText(row,growth.Character.DisplayName.Split(' ')[0],new Vector2(.26f,.58f),new Vector2(.96f,.98f),20);
                    FormationText(row,"Lv."+growth.BeforeLevel+" → "+growth.AfterLevel+"    EXP +"+growth.Experience,new Vector2(.26f,.20f),new Vector2(.97f,.59f),17);
                    var track=FormationRect(row,"ResultExperienceTrack",new Vector2(.28f,.12f),new Vector2(.95f,.18f));track.gameObject.AddComponent<UnityEngine.UI.Image>().color=new Color(.13f,.2f,.25f);
                    var fill=FormationRect(track,"ResultExperienceFill",Vector2.zero,new Vector2(growth.AfterLevel>=50?1:growth.CurrentEXP/100f,1));fill.gameObject.AddComponent<UnityEngine.UI.Image>().color=formationTeal;
                }
            }
            else FormationText(resultPanel,battle.ResultRewards,new Vector2(.06f,.32f),new Vector2(.94f,.70f),22,TMPro.TextAlignmentOptions.Center);
            if(battle.RewardPending)
            {
                ResultButton("보상 저장 재시도",0,2,battle.SaveBattleReward);
                ResultButton("보상 포기 안내",1,2,()=>
                {
                    if(abandoningReward){battle.Restart();return;}
                    abandoningReward=true;
                    var button=resultPanel.Find("보상 포기 안내");button.GetComponentInChildren<TMPro.TMP_Text>().text="보상 포기 확정 · 준비로";
                });
            }
            else if(win&&!battle.ResultTraining)
            {
                FormationText(resultPanel,"전투 후 이야기가 이어집니다.",new Vector2(.04f,.10f),new Vector2(.96f,.16f),16,TMPro.TextAlignmentOptions.Center);
                var next=FormationButton(resultPanel,"결과 계속","계속",new Vector2(.30f,.015f),new Vector2(.70f,.10f),battle.ReadEnding,true,23);StyleButton(next,HudRole.Primary);
            }
            else
            {
                ResultButton("출전 화면 / Restart",0,3,battle.Restart,"출전 준비로");
                if(battle.CanReadEnding)ResultButton("전투 후 이야기",1,3,battle.ReadEnding);
                else if(win)ResultButton("다시 도전",1,3,battle.RetryBattle);
                else if(battle.CanRetryOpening)ResultButton("다시 도전",1,3,()=>ShowSystemMenu(4));
                if(!win&&battle.CanRewind)ResultButton("행동 되감기",2,3,()=>ShowSystemMenu(5),"되돌리기");
                else if(battle.CanPrepareNext)ResultButton("다음 장 출전 준비",2,3,battle.PrepareNextBattle);
                else if(win&&!battle.ResultTraining)ResultButton("다시 도전",2,3,battle.RetryBattle);
            }
        }
        RectTransform ResultTile(string name,Vector2 min,Vector2 max,bool interactive=false)
        {
            var tile=FormationRect(resultPanel,name,min,max);var image=tile.gameObject.AddComponent<UnityEngine.UI.Image>();image.color=new Color(.025f,.055f,.08f,.96f);image.raycastTarget=false;if(!interactive)OrnateButtonGraphic.Create(tile,true);return tile;
        }
        void ResultButton(string name,int index,int count,System.Action action,string display=null)
        {
            Button(resultPanel,name,0,action,true,44);var r=(RectTransform)resultPanel.GetChild(resultPanel.childCount-1);
            r.anchorMin=new Vector2((float)index/count,0);r.anchorMax=new Vector2((float)(index+1)/count,0);r.pivot=new Vector2(.5f,0);r.anchoredPosition=new Vector2(0,16);r.sizeDelta=new Vector2(-24,44);
            if(display!=null)r.GetComponentInChildren<TMPro.TMP_Text>().text=display;
        }
        void RememberedSkillButton(SkillData skill)
        {
            if(skill!=battle.LastSkill)return;
            var button=commands.GetChild(commands.childCount-1).GetComponent<UnityEngine.UI.Button>();button.GetComponent<UnityEngine.UI.Image>().color=new Color(.22f,.34f,.44f);
            button.GetComponentInChildren<TMPro.TMP_Text>().text="▶ "+button.GetComponentInChildren<TMPro.TMP_Text>().text;
            Canvas.ForceUpdateCanvases();battle.GetComponent<GamepadPointer>()?.FocusButton(button);
        }
    }
}
