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
            if(resultPanel==null)resultPanel=Panel("BattleResults",Vector2.zero,Vector2.one,new Vector2(12,138),new Vector2(-12,-88));
            resultPanel.gameObject.SetActive(true);Clear(resultPanel);left.gameObject.SetActive(false);commands.gameObject.SetActive(false);
            bool win=battle.Session.Result==BattleResult.Victory;
            Label(resultPanel,(win?"승리":"패배")+"  ·  "+battle.ResultStage,12,58,30);
            string status=battle.RewardPending?"저장 실패 · 보상 미적용":win&&!battle.ResultTraining?"보상 저장 완료":"저장 보상 없음";
            var notice=Label(resultPanel,status,80,36,20);notice.color=battle.RewardPending?new Color(1,.65f,.4f):new Color(.6f,1,.75f);
            Label(resultPanel,battle.ResultRewards,126,82,21);
            if(battle.ResultGrowth.Length>0)
            {
                Label(resultPanel,"출전 캐릭터 성장",218,36,20);
                for(int i=0;i<battle.ResultGrowth.Length;i++)Label(resultPanel,battle.ResultGrowth[i],260+i*36,34,18);
            }
            else Label(resultPanel,battle.RewardPending?"저장이 완료되면 실제 성장 결과를 표시합니다.":win?"출전 준비에서 편성과 장비를 확인할 수 있습니다.":"편성·장비·회복 역할을 조정한 뒤 다시 도전하세요.\n이번 전투의 패배로 저장된 성장이나 장비를 잃지 않습니다.",230,100,20);
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
            else
            {
                ResultButton("출전 화면 / Restart",0,3,battle.Restart,"출전 준비로");
                if(battle.CanReadEnding)ResultButton("전투 후 이야기",1,3,battle.ReadEnding);
                else if(win)ResultButton("다시 도전",1,3,battle.RetryBattle);
                else if(battle.CanRetryOpening)ResultButton("다시 도전",1,3,()=>ShowSystemMenu(4));
                if(!win&&battle.CanRewind)ResultButton("행동 되감기",2,3,()=>ShowSystemMenu(5));
                else if(battle.CanPrepareNext)ResultButton("다음 장 출전 준비",2,3,battle.PrepareNextBattle);
                else if(win&&!battle.ResultTraining)ResultButton("다시 도전",2,3,battle.RetryBattle);
            }
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
