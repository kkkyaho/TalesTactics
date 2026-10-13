using System;
using System.Linq;
namespace TalesTactics
{
    public sealed class PersonalArc
    {
        public readonly string Owner,Name,WeaponId,WeaponName,TrialBoss;
        public readonly EquipmentEffect Effect;
        public readonly string[] Titles,Conflicts,Answers;
        public PersonalArc(string owner,string name,string weapon,string weaponName,string boss,EquipmentEffect effect,string[] titles,string[] conflicts,string[] answers)
        {Owner=owner;Name=name;WeaponId=weapon;WeaponName=weaponName;TrialBoss=boss;Effect=effect;Titles=titles;Conflicts=conflicts;Answers=answers;}
    }
    public static partial class PersonalScenarios
    {
        public const int FirstStage=26,Count=30;
        public static readonly PersonalArc[] Arcs={
            new PersonalArc("cless","크레스","eternal-sword","이터널 소드 / Eternal Sword","origin",EquipmentEffect.EternalEdge,
                new[]{"돌아오지 않는 날","검을 거둘 이유","시간을 잇는 계약"},
                new[]{"이 검으로도 지키지 못한 사람이 있어. 과거로 돌아갈 수 있다면 무엇부터 해야 할까.","복수를 뒤쫓는 길과 다친 사람을 데려가는 길이 갈라졌어. 먼저 구할 사람을 정하자.","오리진은 시간을 바꿀 힘보다, 바꾸지 못한 어제를 안고 나아갈 의지를 묻는다."},
                new[]{"민트의 말처럼 지금 눈앞의 손을 놓지 않는 것부터 시작하겠어.","적을 쫓기 전에 사람을 구했어. 검을 거두는 것도 내가 내린 선택이야.","오리진과의 계약 시련을 마쳤다. 이터널 소드는 과거를 지워 주는 보상이 아니라 앞으로 갈 책임과 함께 주어졌다."}),
            new PersonalArc("mint","민트","bc-rod","BC 로드 / BC Rod","guardian",EquipmentEffect.MercifulRod,
                new[]{"치유하는 손의 떨림","한 사람을 위한 시간","푸른 수정의 기도"},
                new[]{"도와 달라는 목소리를 모두 들을 수는 없어요. 그래도 듣지 못한 척하고 싶지는 않아요.","치료소가 두 곳으로 나뉘었어요. 시온 씨와 역할을 나누고 먼저 갈 곳을 정해요.","기록 속 치료소에 치유의 빛이 갇혔다. 적을 모두 쓰러뜨리는 대신 다친 이에게 닿아야 한다."},
                new[]{"시온 씨도 아픈 사람을 두고 가지 못하는군요. 서로의 손이 닿지 않는 곳을 맡아 봐요.","한쪽을 먼저 치료해도 다른 쪽을 포기하지 않았어요. 모두에게 돌아갈 시간을 만들었어요.","푸른 수정은 두 치료소를 잇는 기도를 받아들였다. BC 로드를 얻었다. 이 힘으로 아직 닿지 못한 사람에게 갈 수 있어요."}),
            new PersonalArc("farah","파라","god-weapon","갓 웨폰 / God Weapon","maxwell",EquipmentEffect.ResoluteFists,
                new[]{"괜찮다는 말 뒤에서","앞서가는 발걸음","네 원소의 약속"},
                new[]{"괜찮다고 말하면 다들 안심하잖아. 그런데 가끔은 나도 누군가 먼저 물어봐 줬으면 해.","피난로와 보급로를 동시에 지킬 수 없다고? 먼저 여는 길을 정하고 돌아오면 돼.","맥스웰은 네 원소의 흐름 사이에서 동료와 보조를 맞추는 시련을 제시한다."},
                new[]{"키사라가 내 걸음이 너무 빠르다고 했어. 다음에는 기다리는 사람 얼굴도 볼게.","혼자 먼저 도착하는 것보다 모두 도착하는 게 좋네. 다음 길도 함께 열자.","맥스웰의 선택 시련을 마쳤다. 경계의 공방에서 갓 웨폰이 완성됐다. 다음에는 이 손으로 뒤따라오는 동료를 붙잡을 거야."}),
            new PersonalArc("tear","티아","unicorn-horn","유니콘 혼 / Unicorn Horn","guardian",EquipmentEffect.HarmonicHorn,
                new[]{"명령이 없는 노래","누구에게 들려줄 것인가","멈추지 않는 선율"},
                new[]{"명령 없이 노래를 부르면 무엇을 전해야 할까. 오빠에게 배운 노래도 내 선택으로 부를 수 있을까.","부상자를 안정시키는 노래와 통로를 여는 노래. 먼저 필요한 쪽을 동료들과 정하자.","기록 장치는 완벽하게 반복되는 노래만 받아들인다. 살아 있는 목소리로 공명점을 연결해야 한다."},
                new[]{"민트가 말했어. 누구에게 배웠는지보다 지금 누구를 위해 부르는지가 중요하다고.","두 곳에 모두 신호가 닿았어. 내 목소리로 정한 순서였어.","완벽한 반복을 벗어난 선율이 공명점을 이었다. 유니콘 혼을 얻었다. 지금 부르는 노래의 주인은 티아다."}),
            new PersonalArc("jade","제이드","blood-pain","블러드 페인 / Blood Pain","executor",EquipmentEffect.FonicPiercer,
                new[]{"남겨 둔 실험 기록","지워야 할 수식","책임의 끝이 아닌 시작"},
                new[]{"지식을 없애면 책임도 사라질까요. 예전의 저는 그 질문조차 피했을 겁니다.","폭주 장치와 복제 기록이 서로 다른 방에 있습니다. 증거를 보존하며 사람을 구할 순서를 정하죠.","기록자는 제이드의 옛 수식을 재생한다. 장치를 멈추되 그 안의 생명을 실험 재료로 대하지 않아야 한다."},
                new[]{"나탈리아의 질문에 답하려면 결과만 보고할 수는 없겠군요. 제 선택도 기록하겠습니다.","위험한 수식은 봉인했고 기록된 사람들의 이름은 남겼습니다. 파괴와 망각은 다른 일입니다.","블러드 페인을 얻었다. 날에 새겨진 이름들을 지우지 않고, 그 이름에 책임지는 길을 선택했다."}),
            new PersonalArc("natalia","나탈리아","holy-quelqatl","홀리 퀠카틀 / Holy Quelqatl","rassius",EquipmentEffect.RoyalMercy,
                new[]{"국경 밖의 이름","가장 늦게 온 사람","왕관 없이도 겨누는 활"},
                new[]{"제 나라 사람이 아니라면 제 책임이 아닌가요? 그런 대답을 배운 적은 없어요.","서류가 있는 행렬과 이름조차 잃은 행렬이 기다리고 있어요. 누구부터 길을 안내할까요.","라시우스는 신분을 밝힐 수 없는 사람까지 같은 힘으로 보호할 수 있는지 묻는다."},
                new[]{"알펜은 제가 왕녀인지 묻기 전에 붕대를 건네줬어요. 오늘은 그 순서를 기억할게요.","먼저 온 사람만 구조받을 권리가 있는 것은 아니에요. 두 행렬을 모두 안내했어요.","라시우스는 보호의 약속을 인정했다. 홀리 퀠카틀을 얻었다. 이 활이 닿는 곳에는 국경 밖의 사람도 있을 것이다."}),
            new PersonalArc("velvet","벨벳","unnamed-blade","무명의 칼날 / Unnamed Blade","shigure",EquipmentEffect.UnboundBlade,
                new[]{"먹지 않은 한 끼","복수의 옆길","이름을 주지 않은 칼"},
                new[]{"내가 왜 너희랑 식사를 해야 하지? …남길 거면 그릇은 이리 줘.","원수의 흔적과 부상자의 흔적이 갈라진다. 누구도 벨벳 대신 먼저 갈 길을 정하지 않는다.","시구레는 분노만으로 휘두르는 칼인지 묻는다. 벨벳은 그의 기준이 아니라 자기 답으로 결투한다."},
                new[]{"파라는 아무것도 묻지 않고 음식을 남겼다. 벨벳도 그 한 끼를 남기지 않았다.","잠깐 돌아온 것뿐이야. 내 복수를 버린 건 아니야. 그래도 저 사람은 살아 있잖아.","시구레가 칼을 거뒀다. 무명의 칼날을 얻었다. 벨벳은 검에도 자기 길에도 남이 정한 구원의 이름을 붙이지 않았다."}),
            new PersonalArc("alphen","알펜","nebilim","네빌림 / Nebilim","executor",EquipmentEffect.FlamingOath,
                new[]{"사슬을 끊은 다음","빌려주지 않는 선택","불꽃의 주인"},
                new[]{"사슬을 끊었다고 그 뒤의 삶까지 내가 정해 줄 수는 없어. 내가 원하는 자유도 그런 걸까.","서로 다른 두 계획으로 탈출하려는 사람들이 있어. 위험을 설명하고 먼저 도울 쪽을 정하자.","집행관은 자유를 주는 대가로 새로운 명령권을 요구한다. 알펜은 그 명령 장치 자체를 겨눈다."},
                new[]{"크레스도 돌아간 뒤의 정답을 알고 있지는 않대. 그래도 같이 길을 열 수는 있어.","어느 계획을 먼저 도와도 다른 선택을 금지하지 않았어. 모두 자기 발로 문을 넘었어.","명령의 핵을 끊고 네빌림을 얻었다. 본작의 HP 비용 감소는 원작의 처치 수 성장과 구분되는 새 효과다."}),
            new PersonalArc("shionne","시온","damonisch-core","데모니시 코어 / Dämonisch Core","guardian",EquipmentEffect.ThornRelease,
                new[]{"닿지 않아도 건네는 것","가시 너머의 신호","함께 돌아간다는 말"},
                new[]{"고맙다는 말 대신 가까이 오지 말라고 했어. 다치게 하고 싶지 않았는데, 말은 늘 그렇게 나오네.","멀리 있는 부상자에게 약을 보낼 길과 엄호 사선을 확보할 길. 먼저 열 쪽을 선택한다.","동료가 없는 환영이 안전하다고 속삭인다. 시온은 거리를 지키면서도 구조 신호를 연결해야 한다."},
                new[]{"민트는 억지로 손을 잡지 않았어. 붕대를 놓고 기다려 줬지. 그런 도움도 있다는 걸 잊고 있었네.","내가 보낸 신호를 알아봤어. 손이 닿지 않아도 혼자 두지 않을 수 있어.","환영을 벗어나 데모니시 코어를 얻었다. 시온은 먼저 돌아오겠다는 약속을 동료에게 건넸다."}),
            new PersonalArc("kisara","키사라","os-rex","오스 렉스 / Os Rex","rassius",EquipmentEffect.UnbrokenShield,
                new[]{"방패를 내려놓는 시간","뒤를 맡기는 용기","지키는 사람의 귀환"},
                new[]{"모두를 지키려면 내가 마지막까지 서 있어야 한다고 생각했다. 쉬는 동안 누가 대신 설 수 있는지도 보지 않았지.","앞의 행렬과 뒤의 보급대가 동시에 위험하다. 키사라는 동료에게 한쪽을 맡기고 먼저 지킬 쪽을 고른다.","라시우스의 시련은 혼자 버티는 싸움이 아니다. 동료에게 등을 맡기고 자기 귀환까지 지켜야 한다."},
                new[]{"벨벳이 말없이 경계를 대신 섰다. 오늘은 내가 먼저 잠들어도 되는 밤이었다.","내가 없는 쪽도 동료들이 지켰다. 방패를 맡기는 것이 책임을 버리는 일은 아니었다.","서로의 퇴로를 지킨 뒤 오스 렉스를 얻었다. 키사라도 보호받으며 돌아가야 할 사람 중 한 명이다."})
        };
        public static bool Contains(int stage)=>stage>=FirstStage&&stage<FirstStage+Count;
        public static PersonalArc Arc(int stage)=>Contains(stage)?Arcs[(stage-FirstStage)/3]:throw new ArgumentOutOfRangeException(nameof(stage));
        public static int Tier(int stage)=>(stage-FirstStage)%3;
        public static string Flag(int stage)=>"personal."+Arc(stage).Owner+"."+(Tier(stage)+1);
        public static int RequiredEpisode(int stage)=>new[]{6,10,14}[Tier(stage)]+((stage-FirstStage)/3)%3;
        public static bool Unlocked(CampaignSave save,int stage)=>Contains(stage)&&save.StoryProgress.Contains(CrossoverCampaign.Flag(RequiredEpisode(stage)))&&(Tier(stage)==0||save.StoryProgress.Contains(Flag(stage-1)));
        public static string Title(int stage)=>Arc(stage).Name+" "+(Tier(stage)+1)+" · "+Arc(stage).Titles[Tier(stage)];
        public static StoryLine[] Story(int stage,bool after)
        {
            var arc=Arc(stage);int tier=Tier(stage);
            int index=(stage-FirstStage)/3;
            return after?new[]{new StoryLine(arc.Name,arc.Answers[tier]),new StoryLine(Companions[index],Responses[index]),new StoryLine("기록",tier==2?arc.WeaponName+" 획득. 전용 장착과 고유 효과는 장비 관리에서 확인할 수 있다.":"이 이야기는 동료에게 남았다. 다음 이야기는 메인 진행에 따라 열린다.")}:
                new[]{new StoryLine("동료의 이야기 · "+arc.Name,arc.Titles[tier]),new StoryLine(arc.Name,arc.Conflicts[tier]),new StoryLine(Companions[index],Questions[index][tier]),new StoryLine("기록",tier==0?"잠시 전투를 멈추고 동료의 이야기를 듣는다.":tier==1?"먼저 열 길을 고른다. 순서가 달라져도 두 길을 모두 도울 수 있다.":"선택 무기 시련. 실패해도 다시 도전할 수 있으며 메인 귀환은 막히지 않는다.")};
        }
        public static string OwnerName(string id)=>Arcs.FirstOrDefault(a=>a.Owner==id)?.Name??id;
        public static CampaignStages.Chapter Chapter(int stage)
        {
            var arc=Arc(stage);int level=RequiredEpisode(stage)*2-1,tier=Tier(stage);
            return new CampaignStages.Chapter(Title(stage),level,level,10,tier==0?0:150,tier==2?arc.WeaponId:null,tier==2?arc.WeaponName:"이야기 기록",tier==2);
        }
    }
}
