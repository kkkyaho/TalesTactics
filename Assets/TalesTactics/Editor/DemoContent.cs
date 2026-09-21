using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TalesTactics.Editor
{
    public static class DemoContent
    {
        const string Root="Assets/TalesTactics/Content";
        static T Asset<T>(string path) where T:ScriptableObject
        {
            var existing=AssetDatabase.LoadAssetAtPath<T>(path);if(existing!=null)return existing;
            var value=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(value,path);return value;
        }
        static SkillEffect E(EffectKind kind,float power=1,bool magic=false,StatusKind status=StatusKind.None,int duration=2,float chance=1,bool caster=false)
            => new SkillEffect{Kind=kind,Power=power,Magic=magic,Status=status,Duration=duration,Chance=chance,AffectCaster=caster};
        public static BattleCatalog Create()
        {
            System.IO.Directory.CreateDirectory(Root+"/Skills");System.IO.Directory.CreateDirectory(Root+"/Characters");AssetDatabase.Refresh();
            var catalog=Asset<BattleCatalog>(Root+"/BattleCatalog.asset");
            catalog.Rules=Asset<BattleRules>(Root+"/BattleRules.asset");
            string[] ids={"cless","mint","velvet","farah","tear","jade","natalia","alphen","shionne","kisara"};
            string[] names={"크레스 알베인","민트 아드네이드","벨벳 크라우","파라 엘스테드","티아 그란츠","제이드 커티스","나탈리아","알펜","시온 아이메리스","키사라"};
            string[] jobs={"시공검사","힐러","클로 딜러","격투가","성가 술사","포닉 술사","아처","화염검사","총사 / 힐러","가디언"};
            string[] sources={"Tales of Phantasia","Tales of Phantasia","Tales of Berseria","Tales of Eternia","Tales of the Abyss","Tales of the Abyss","Tales of the Abyss","Tales of Arise","Tales of Arise","Tales of Arise"};
            WeaponType[] weapons={WeaponType.Sword,WeaponType.Staff,WeaponType.Claw,WeaponType.Fist,WeaponType.Staff,WeaponType.Spear,WeaponType.Bow,WeaponType.Sword,WeaponType.Gun,WeaponType.Shield};
            string[][] lists={
                new[]{"마신검","호아파참","추사우","봉황천구","마신쌍파참","차원참","시공창파참"},
                new[]{"퍼스트 에이드","힐","너스","피코 해머","리커버","레이즈 데드","리저렉션"},
                new[]{"컨슘 클로","헤븐즈 클로","헬즈 클로","나이트메어 클로"},
                new[]{"장저파","삼산화","비연연각","연아탄","와룡공파","쌍장저파","사자전후"},
                new[]{"퍼스트 에이드","나이트메어","힐링 서클","홀리 송","홀리 랜스","리저렉션","저지먼트"},
                new[]{"스탈라그마이트","스플래시","그라운드 대셔","썬더 블레이드","프리즘 소드","앱솔루트","메테오 스톰","인디그네이션"},
                new[]{"피어싱 라인","스톰 엣지","힐","스타 스트로크","갤런트 배러지","큐어","리바이브"},
                new[]{"마신검","비룡승파","추사우·알파","패왕참","봉황천구","인페르널 토렌트","인시너레이션 웨이브"},
                new[]{"마그나 레이","퍼스트 에이드","제미니 아쿠아","트레스 벤토스","힐링 서클","그라비타스 필드","레저렉션"},
                new[]{"가디언 필드","호아파참","피어싱 로어","라이온즈 하울","플레이밍 메테오","이그니어스 디스차지","빙장화"}
            };
            string[] ultimates={"명공참상검","타임 스톱","Impulse Desire","사후폭쇄진 / 獅吼爆砕陣","이노센트 샤인","미스틱 케이지 / Mystic Cage","아스트랄 레인","스칼렛 아웃버스트","컨슈밍 와일드파이어","플레어 데몰리셔"};
            var chars=new List<CharacterData>();
            for(int i=0;i<ids.Length;i++)
            {
                var c=Asset<CharacterData>(Root+"/Characters/"+ids[i]+".asset");c.Id=ids[i];c.DisplayName=names[i];c.Job=jobs[i];c.SourceTitle=sources[i];c.Weapon=weapons[i];c.AudioTheme=ids[i]+".theme";
                bool mage=i==1||i==4||i==5;
                c.BaseStats=new Stats{HP=mage?135:190,MP=mage?120:85,STR=mage?22:42,MAG=mage?48:27,DEF=i==9?32:19,MDF=mage?26:16,SPD=20-i+(i==3?8:0),MOV=i==3||i==2?5:i==9?3:4,JMP=i==3?3:2};
                c.GrowthStats=new Stats{HP=7,MP=3,STR=mage?1:2,MAG=mage?3:1,DEF=1,MDF=1,SPD=1};
                c.PromotionBonus=new Stats{HP=40,MP=20,STR=8,MAG=8,DEF=5,MDF=5};c.PromotionJob="상급 "+jobs[i];
                c.PlaceholderColor=Color.HSVToRGB(i/10f,0.55f,0.95f);
                c.BasicAttack=Skill(ids[i]+".attack","공격",0,i==6||i==8?4:i==5?2:1,0,TargetType.Enemy,E(EffectKind.Damage));c.BasicAttack.Animation=AnimationKind.Attack;
                var skills=new List<SkillData>();
                for(int j=0;j<lists[i].Length;j++)
                {
                    var s=Skill(ids[i]+"."+j,lists[i][j],6+j*3,mage?4:2,j>=4?1:0,TargetType.Enemy,E(EffectKind.Damage,1.25f+j*0.2f,mage));
                    s.UnlockLevel=1+j*3;
                    Configure(i,j,s);skills.Add(s);EditorUtility.SetDirty(s);
                }
                c.Skills=skills.ToArray();
                c.UltimateSkill=Skill(ids[i]+".ultimate",ultimates[i],30,4,2,TargetType.Enemy,E(EffectKind.Damage,3.8f,mage));
                c.UltimateSkill.IsUltimate=true;c.UltimateSkill.GaugeCost=100;c.UltimateSkill.UnlockLevel=20;c.UltimateSkill.Animation=AnimationKind.Ultimate;
                if(i==1){c.UltimateSkill.Range=9;c.UltimateSkill.Area=9;c.UltimateSkill.Effects=new[]{E(EffectKind.Status,status:StatusKind.Stun,duration:2)};}
                if(i==2)c.UltimateSkill.Gate=SkillGate.ClawFinisher;
                if(i==3){c.UltimateSkill.Gate=SkillGate.FarahTiming;c.UltimateSkill.MPCost=50;c.UltimateSkill.GaugeCost=0;c.UltimateSkill.UnlockLevel=19;c.UltimateSkill.Range=2;}
                if(i==5)c.UltimateSkill.Effects=new[]{E(EffectKind.Damage,3.5f,true),E(EffectKind.Debuff,status:StatusKind.Cage,duration:3)};
                if(i==9)c.UltimateSkill.Gate=SkillGate.GuardIgnition;
                c.PassiveSkill=i==2?"Consume Claw combo":i==7?"Flaming Edge":i==9?"Directional Guard Ignition":"";
                EditorUtility.SetDirty(c.UltimateSkill);EditorUtility.SetDirty(c);chars.Add(c);
            }
            catalog.Characters=chars.ToArray();
            var enemy=Asset<CharacterData>(Root+"/Characters/sentinel.asset");enemy.Id="sentinel";enemy.DisplayName="유적 파수병";enemy.Job="Sentinel";enemy.BaseStats=new Stats{HP=145,MP=30,STR=30,MAG=20,DEF=15,MDF=12,SPD=13,MOV=3,JMP=1};enemy.GrowthStats=new Stats{HP=5,STR=1,MAG=1,DEF=1,MDF=1,SPD=1};enemy.PlaceholderColor=new Color(0.85f,0.25f,0.23f);enemy.BasicAttack=Skill("enemy.attack","창 찌르기",0,2,0,TargetType.Enemy,E(EffectKind.Damage));catalog.Enemy=enemy;EditorUtility.SetDirty(enemy);
            var equip=Asset<EquipmentData>(Root+"/BronzeSword.asset");equip.Id="bronze-sword";equip.DisplayName="청동검";equip.Slot=EquipmentSlot.Weapon;equip.Weapon=WeaponType.Sword;equip.Bonus=new Stats{STR=4};catalog.Equipment=new[]{equip};EditorUtility.SetDirty(equip);
            catalog.Audio=Asset<AudioLibrary>(Root+"/AudioLibrary.asset");var audio=new List<AudioEntry>{new AudioEntry{Id="battle",Usage="전투 BGM"},new AudioEntry{Id="victory",Usage="승리 BGM"},new AudioEntry{Id="boss",Usage="보스 BGM"},new AudioEntry{Id="story",Usage="스토리 BGM"}};
            foreach(var c in chars)audio.Add(new AudioEntry{Id=c.AudioTheme,Usage=c.DisplayName+" 테마",SourceMetadata=c.SourceTitle+" — 제공 음원 연결 예정"});catalog.Audio.Entries=audio.ToArray();EditorUtility.SetDirty(catalog.Audio);EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();return catalog;
        }
        static SkillData Skill(string id,string title,int mp,int range,int area,TargetType target,params SkillEffect[] effects)
        {var s=Asset<SkillData>(Root+"/Skills/"+id+".asset");s.Id=id;s.DisplayName=title;s.MPCost=mp;s.Range=range;s.MinRange=target==TargetType.Self?0:1;s.Area=area;s.Target=target;s.Effects=effects;EditorUtility.SetDirty(s);return s;}
        static void Heal(SkillData s,float power,int area=0){s.Target=TargetType.Ally;s.MinRange=0;s.Range=4;s.Area=area;s.Effects=new[]{E(EffectKind.Heal,power,true)};}
        static void Revive(SkillData s,float fraction,int area=0){s.Target=TargetType.FallenAlly;s.MinRange=0;s.Range=4;s.Area=area;s.Effects=new[]{E(EffectKind.Revive,fraction)};}
        static void Configure(int c,int j,SkillData s)
        {
            if(c==1)
            {
                if(j<3)Heal(s,j==0?1.6f:j==1?2.7f:2.1f,j==2?3:0);
                if(j==3)s.Effects=new[]{E(EffectKind.Damage,1.3f,true),E(EffectKind.Status,status:StatusKind.Stun,duration:1,chance:0.35f)};
                if(j==4){s.Target=TargetType.Ally;s.MinRange=0;s.Effects=new[]{E(EffectKind.Cleanse)};}
                if(j==5)Revive(s,0.5f);if(j==6)Revive(s,1,2);
            }
            if(c==2)
            {
                s.UnlockLevel=1;s.Range=1;s.Area=0;
                if(j==0){s.Target=TargetType.Self;s.MinRange=s.Range=0;s.HPPercentCost=0.1f;s.Effects=new[]{E(EffectKind.ConsumeClaw)};}
                else {s.Gate=j==3?SkillGate.Nightmare:SkillGate.Claw;var hit=E(EffectKind.Damage,j==3?3.2f:2);hit.Drain=j==3?0.65f:0.5f;hit.IgnoreDefense=j==3;s.Effects=new[]{hit};}
                if(j==2){s.MinRange=s.Range=0;s.Area=2;s.Effects=new[]{E(EffectKind.Damage,1.7f),new SkillEffect{Kind=EffectKind.Gauge,Flat=25,AffectCaster=true}};}
            }
            if(c==3&&j==6){s.IsLionHowl=true;s.Range=2;s.Area=1;}
            if(c==4)
            {
                if(j==0)Heal(s,1.3f);if(j==2)Heal(s,1.6f,2);
                if(j==1)s.Effects=new[]{E(EffectKind.Damage,1.4f,true),E(EffectKind.Status,status:StatusKind.Sleep,duration:1,chance:0.5f)};
                if(j==3){s.Target=TargetType.Ally;s.MinRange=0;s.Area=3;s.Effects=new[]{E(EffectKind.Buff,status:StatusKind.Song,duration:3)};}
                if(j==5)Revive(s,0.7f,1);if(j==4||j==6)s.Element=Element.Light;
            }
            if(c==5){s.Range=5;s.Area=j>1?2:1;s.Element=(Element)(1+j%8);}
            if(c==6){s.Range=5;if(j==2)Heal(s,1.6f);if(j==5)Heal(s,3);if(j==6){s.Target=TargetType.Ally;s.MinRange=0;s.Effects=new[]{E(EffectKind.AutoRevive,duration:5)};}}
            if(c==7){if(j<4)s.StartsFlamingChain=true;else{s.Gate=SkillGate.FlamingEdge;s.MPCost=0;s.HPPercentCost=0.15f+(j-4)*0.05f;s.Element=Element.Fire;s.Effects=new[]{E(EffectKind.Damage,2.2f+(j-4)*0.6f)};}}
            if(c==8){s.Range=5;if(j==1)Heal(s,1.5f);if(j==4)Heal(s,1.5f,2);if(j==6)Revive(s,0.65f);if(j==5){s.Area=2;s.Effects=new[]{E(EffectKind.Damage,1.8f,true),new SkillEffect{Kind=EffectKind.Pull,Distance=2}};}}
            if(c==9&&j==0){s.Area=1;s.Effects=new[]{E(EffectKind.Damage,1.2f),E(EffectKind.Heal,1.2f,true,caster:true)};}
        }
    }
}
