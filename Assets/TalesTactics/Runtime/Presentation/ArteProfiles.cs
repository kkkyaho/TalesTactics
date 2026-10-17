using System;
using System.Collections.Generic;
using UnityEngine;
using M=TalesTactics.ArteMotif;
using P=TalesTactics.ArteMotion;
namespace TalesTactics
{
    // Exact asset IDs only. Motifs interpret names, roles and authored SourceNote metadata;
    // they do not assert frame-for-frame original-game fidelity or alter Content assets.
    public static class ArteProfiles
    {
        static readonly Color Steel=new Color(.82f,.93f,1f), Holy=new Color(1f,.9f,.58f),
            Gold=new Color(1f,.72f,.27f), Healing=new Color(.39f,1f,.68f),
            Crimson=new Color(1f,.22f,.27f), Violet=new Color(.66f,.35f,1f),
            Flame=new Color(1f,.36f,.09f), EarthTone=new Color(.79f,.56f,.3f),
            Aqua=new Color(.23f,.68f,1f), Frost=new Color(.57f,.89f,1f),
            Electric=new Color(.73f,.78f,1f), Gale=new Color(.48f,1f,.78f);

        // id, motif, motion, visual count, arrangement, windup, travel, recovery,
        // spread, angle (radians), tint, accent, ultimate. No count is a damage request.
        static readonly ArteProfile[] entries=
        {
            // cless
            new ArteProfile("cless.attack",M.Slash,P.Attack,1,0,.36f,0f,.65f,.55f,-.55f,Steel,M.None), // 공격
            new ArteProfile("cless.ultimate",M.CrossSlash,P.Blink,7,2,.8f,.3f,1.15f,1.05f,.78f,Holy,M.Radiance,true), // 명공참상검
            new ArteProfile("cless.1",M.RisingSlash,P.Jump,2,0,.45f,0f,.85f,.55f,1.15f,Steel,M.Slash), // 호아파참
            new ArteProfile("cless.2",M.Thrust,P.Dash,5,1,.45f,.22f,.85f,.32f,0f,Steel,M.None), // 추사우
            new ArteProfile("cless.3",M.Phoenix,P.Jump,1,0,.45f,.18f,.85f,.85f,.5f,Flame,M.Fire), // 봉황천구
            new ArteProfile("cless.4",M.Wave,P.Attack,2,1,.4f,.35f,.85f,.7f,0f,Steel,M.Slash), // 마신쌍파참
            new ArteProfile("cless.5",M.CrossSlash,P.Blink,2,2,.45f,.22f,.85f,.7f,.78f,Violet,M.Gravity), // 차원참
            new ArteProfile("cless.6",M.Wave,P.Blink,3,2,.45f,.22f,.85f,1f,0f,Holy,M.Clock), // 시공창파참
            // mint
            new ArteProfile("mint.attack",M.Thrust,P.Attack,1,0,.36f,0f,.65f,.35f,-.35f,Holy,M.None), // 공격
            new ArteProfile("mint.ultimate",M.Clock,P.Cast,8,0,.8f,.3f,1.15f,1.1f,0f,Holy,M.Barrier,true), // 타임 스톱
            new ArteProfile("mint.0",M.Heal,P.Cast,1,0,.65f,.2f,1.05f,.45f,0f,Healing,M.None), // 퍼스트 에이드
            new ArteProfile("mint.1",M.Heal,P.Cast,3,1,.65f,.2f,1.05f,.55f,.2f,Healing,M.Radiance), // 힐
            new ArteProfile("mint.2",M.Song,P.Cast,5,2,.65f,.2f,1.05f,1f,0f,Healing,M.Heal), // 너스
            new ArteProfile("mint.3",M.Hammer,P.Cast,1,0,.65f,.35f,.85f,.5f,-.35f,Gold,M.None), // 피코 해머
            new ArteProfile("mint.4",M.Cleanse,P.Cast,4,0,.65f,.2f,1.05f,.6f,.78f,Healing,M.None), // 리커버
            new ArteProfile("mint.5",M.Revive,P.Cast,2,0,.65f,.2f,1.05f,.65f,0f,Holy,M.Radiance), // 레이즈 데드
            new ArteProfile("mint.6",M.Revive,P.Cast,5,1,.65f,.2f,1.05f,1f,0f,Holy,M.Heal), // 리저렉션
            // velvet
            new ArteProfile("velvet.attack",M.Slash,P.Attack,1,0,.36f,0f,.65f,.5f,-.6f,Crimson,M.Claw), // 공격
            new ArteProfile("velvet.ultimate",M.Claw,P.Dash,6,2,.8f,.3f,1.15f,1f,-.8f,Crimson,M.Gravity,true), // Impulse Desire
            new ArteProfile("velvet.0",M.Claw,P.Guard,3,0,.45f,0f,.85f,.6f,-.7f,Crimson,M.Gravity), // 컨슘 클로
            new ArteProfile("velvet.1",M.Claw,P.Jump,3,1,.45f,0f,.85f,.55f,1.15f,Crimson,M.RisingSlash), // 헤븐즈 클로
            new ArteProfile("velvet.2",M.Claw,P.Attack,4,2,.45f,0f,.85f,.7f,-.7f,Flame,M.Fire), // 헬즈 클로
            new ArteProfile("velvet.3",M.Claw,P.Dash,5,3,.45f,.22f,.85f,.8f,-.7f,Violet,M.Sleep), // 나이트메어 클로
            // farah
            new ArteProfile("farah.attack",M.Martial,P.Attack,2,0,.36f,0f,.65f,.45f,.2f,Gold,M.None), // 공격
            new ArteProfile("farah.ultimate",M.Lion,P.Jump,6,2,.8f,.3f,1.15f,1.1f,0f,Gold,M.Martial,true), // 사후폭쇄진 / 獅吼爆砕陣
            new ArteProfile("farah.0",M.Martial,P.Attack,1,1,.45f,0f,.85f,.6f,0f,Gold,M.Wave), // 장저파
            new ArteProfile("farah.1",M.Martial,P.Attack,3,2,.45f,0f,.85f,.55f,.65f,Gold,M.None), // 삼산화
            new ArteProfile("farah.2",M.RisingSlash,P.Jump,4,1,.45f,0f,.85f,.65f,1.2f,Gold,M.Martial), // 비연연각
            new ArteProfile("farah.3",M.Martial,P.Spin,5,3,.45f,.12f,.85f,.5f,0f,Gold,M.Thrust), // 연아탄
            new ArteProfile("farah.4",M.Dragon,P.Jump,2,0,.45f,.18f,.85f,.8f,1.1f,Gold,M.Martial), // 와룡공파
            new ArteProfile("farah.5",M.Wave,P.Attack,2,2,.45f,.18f,.85f,.9f,0f,Gold,M.Martial), // 쌍장저파
            new ArteProfile("farah.6",M.Lion,P.Attack,1,0,.45f,0f,.85f,.85f,0f,Gold,M.Wave), // 사자전후
            // tear
            new ArteProfile("tear.attack",M.Thrust,P.Attack,1,1,.36f,0f,.65f,.35f,.15f,Holy,M.None), // 공격
            new ArteProfile("tear.ultimate",M.Radiance,P.Cast,7,2,.8f,.3f,1.15f,1.1f,0f,Holy,M.Judgment,true), // 이노센트 샤인
            new ArteProfile("tear.0",M.Heal,P.Cast,1,1,.65f,.2f,1.05f,.45f,0f,Healing,M.Song), // 퍼스트 에이드
            new ArteProfile("tear.1",M.Sleep,P.Cast,3,0,.65f,.2f,1.05f,.7f,.25f,Violet,M.Gravity), // 나이트메어
            new ArteProfile("tear.2",M.Heal,P.Cast,4,3,.65f,.2f,1.05f,.95f,0f,Healing,M.Barrier), // 힐링 서클
            new ArteProfile("tear.3",M.Song,P.Cast,4,1,.65f,.2f,1.05f,.95f,0f,Holy,M.Barrier), // 홀리 송
            new ArteProfile("tear.4",M.Prism,P.Cast,4,0,.65f,.2f,1.05f,.65f,.12f,Holy,M.Radiance), // 홀리 랜스
            new ArteProfile("tear.5",M.Revive,P.Cast,4,2,.65f,.2f,1.05f,.95f,0f,Holy,M.Song), // 리저렉션
            new ArteProfile("tear.6",M.Judgment,P.Cast,7,1,.65f,.4f,1.05f,1.1f,-.15f,Holy,M.Radiance), // 저지먼트
            // jade
            new ArteProfile("jade.attack",M.Thrust,P.Attack,1,0,.36f,0f,.65f,.35f,0f,Steel,M.None), // 공격
            new ArteProfile("jade.ultimate",M.Cage,P.Cast,6,0,.8f,.3f,1.15f,1.05f,0f,Violet,M.Gravity,true), // 미스틱 케이지 / Mystic Cage
            new ArteProfile("jade.0",M.Earth,P.Cast,4,0,.65f,.2f,1.05f,.8f,0f,EarthTone,M.None), // 스탈라그마이트
            new ArteProfile("jade.1",M.Water,P.Cast,4,0,.65f,.2f,1.05f,.9f,-.3f,Aqua,M.None), // 스플래시
            new ArteProfile("jade.2",M.Earth,P.Cast,5,1,.65f,.2f,1.05f,1.05f,0f,EarthTone,M.Wave), // 그라운드 대셔
            new ArteProfile("jade.4",M.Prism,P.Cast,6,1,.65f,.2f,1.05f,.95f,.5f,Holy,M.Radiance), // 프리즘 소드
            new ArteProfile("jade.5",M.Ice,P.Cast,6,1,.65f,.2f,1.05f,.9f,0f,Frost,M.Barrier), // 앱솔루트
            new ArteProfile("jade.7",M.Lightning,P.Cast,1,2,.8f,.4f,1.1f,1.1f,0f,Electric,M.Judgment), // 인디그네이션
            // natalia
            new ArteProfile("natalia.attack",M.Arrow,P.Shoot,1,0,.36f,.22f,.65f,.25f,0f,Gold,M.None), // 공격
            new ArteProfile("natalia.ultimate",M.ArrowRain,P.Shoot,8,2,.8f,.4f,1.15f,1.15f,-.45f,Holy,M.Radiance,true), // 아스트랄 레인
            new ArteProfile("natalia.0",M.Arrow,P.Shoot,1,1,.45f,.22f,.85f,.3f,0f,Steel,M.Thrust), // 피어싱 라인
            new ArteProfile("natalia.1",M.Arrow,P.Shoot,3,2,.45f,.22f,.85f,.6f,.25f,Gale,M.Wind), // 스톰 엣지
            new ArteProfile("natalia.2",M.Heal,P.Cast,3,2,.65f,.2f,1.05f,.55f,0f,Healing,M.None), // 힐
            new ArteProfile("natalia.3",M.ArrowRain,P.Shoot,5,0,.45f,.4f,1.05f,.75f,-.55f,Holy,M.Prism), // 스타 스트로크
            new ArteProfile("natalia.4",M.ArrowRain,P.Shoot,7,1,.45f,.4f,1.05f,.9f,-.25f,Gold,M.Arrow), // 갤런트 배러지
            new ArteProfile("natalia.5",M.Heal,P.Cast,5,3,.65f,.2f,1.05f,.7f,.2f,Healing,M.Radiance), // 큐어
            new ArteProfile("natalia.6",M.Barrier,P.Guard,4,3,.6f,0f,1.05f,.75f,0f,Holy,M.Revive), // 리바이브
            // alphen
            new ArteProfile("alphen.attack",M.Slash,P.Attack,1,1,.36f,0f,.65f,.6f,-.6f,Steel,M.None), // 공격
            new ArteProfile("alphen.ultimate",M.FlameFinale,P.Jump,6,0,.8f,.3f,1.15f,1.15f,.6f,Flame,M.Phoenix,true), // 스칼렛 아웃버스트
            new ArteProfile("alphen.0",M.Wave,P.Attack,1,0,.4f,.35f,.85f,.65f,0f,Steel,M.Slash), // 마신검
            new ArteProfile("alphen.1",M.Dragon,P.Jump,2,1,.45f,.18f,.85f,.8f,1.15f,Steel,M.RisingSlash), // 비룡승파
            new ArteProfile("alphen.2",M.Thrust,P.Dash,6,2,.45f,.22f,.85f,.4f,0f,Steel,M.Slash), // 추사우·알파
            new ArteProfile("alphen.3",M.Slash,P.Attack,1,2,.45f,0f,.85f,.95f,1.1f,Gold,M.Earth), // 패왕참
            new ArteProfile("alphen.4",M.Phoenix,P.Jump,1,1,.45f,.18f,.85f,.95f,.45f,Flame,M.Fire), // 봉황천구
            new ArteProfile("alphen.5",M.Fire,P.Dash,4,1,.45f,.22f,.85f,.95f,0f,Flame,M.Thrust), // 인페르널 토렌트
            new ArteProfile("alphen.6",M.Wave,P.Attack,2,3,.45f,.18f,.85f,1.1f,0f,Flame,M.Fire), // 인시너레이션 웨이브
            // shionne
            new ArteProfile("shionne.attack",M.Rifle,P.Shoot,1,0,.36f,.22f,.65f,.25f,0f,Steel,M.None), // 공격
            new ArteProfile("shionne.ultimate",M.Grenade,P.Shoot,5,2,.8f,.3f,1.15f,1.1f,-.4f,Flame,M.FlameFinale,true), // 컨슈밍 와일드파이어
            new ArteProfile("shionne.0",M.Rifle,P.Shoot,1,1,.45f,.22f,.85f,.4f,0f,Holy,M.Radiance), // 마그나 레이
            new ArteProfile("shionne.1",M.Heal,P.Cast,1,2,.65f,.2f,1.05f,.45f,0f,Healing,M.None), // 퍼스트 에이드
            new ArteProfile("shionne.2",M.Rifle,P.Shoot,2,0,.45f,.22f,.85f,.65f,-.45f,Aqua,M.Water), // 제미니 아쿠아
            new ArteProfile("shionne.3",M.Rifle,P.Shoot,3,1,.45f,.22f,.85f,.85f,-.35f,Gale,M.Wind), // 트레스 벤토스
            new ArteProfile("shionne.4",M.Heal,P.Cast,4,1,.65f,.2f,1.05f,.95f,0f,Healing,M.Barrier), // 힐링 서클
            new ArteProfile("shionne.5",M.Gravity,P.Shoot,5,1,.5f,.22f,1.05f,1f,0f,Violet,M.Rifle), // 그라비타스 필드
            new ArteProfile("shionne.6",M.Revive,P.Cast,5,2,.65f,.2f,1.05f,1f,0f,Holy,M.Radiance), // 레저렉션
            // kisara
            new ArteProfile("kisara.attack",M.Shield,P.Attack,1,0,.36f,0f,.65f,.6f,0f,Gold,M.None), // 공격
            new ArteProfile("kisara.ultimate",M.Shield,P.Jump,5,3,.8f,.3f,1.15f,1.15f,.5f,Flame,M.FlameFinale,true), // 플레어 데몰리셔
            new ArteProfile("kisara.0",M.Shield,P.Attack,1,2,.45f,0f,.85f,.85f,0f,Gold,M.Heal), // 가디언 필드
            new ArteProfile("kisara.1",M.RisingSlash,P.Jump,2,2,.45f,0f,.85f,.7f,1.2f,Gold,M.Shield), // 호아파참
            new ArteProfile("kisara.2",M.Shield,P.Dash,1,1,.45f,.22f,.85f,.6f,0f,Gold,M.Thrust), // 피어싱 로어
            new ArteProfile("kisara.3",M.Lion,P.Attack,1,1,.45f,0f,.85f,.9f,0f,Gold,M.Shield), // 라이온즈 하울
            new ArteProfile("kisara.4",M.Hammer,P.Attack,1,1,.5f,.1f,.9f,.9f,-.6f,Flame,M.Fire), // 플레이밍 메테오
            new ArteProfile("kisara.5",M.Fire,P.Attack,4,2,.45f,0f,.85f,1f,0f,Flame,M.Shield), // 이그니어스 디스차지
            new ArteProfile("kisara.6",M.Ice,P.Spin,6,2,.45f,0f,.85f,.9f,.3f,Frost,M.Shield), // 빙장화
            // enemy
            new ArteProfile("enemy.attack",M.Thrust,P.Attack,1,0,.36f,0f,.65f,.35f,0f,Steel,M.None), // 창 찌르기
            // campaign
            new ArteProfile("wolf-attack",M.Claw,P.Dash,2,0,.36f,.22f,.65f,.35f,-.5f,Steel,M.Thrust), // 송곳니
            new ArteProfile("slime-attack",M.Water,P.Attack,2,1,.36f,0f,.65f,.5f,0f,Aqua,M.Martial), // 점액 타격
            new ArteProfile("golem-attack",M.Martial,P.Attack,1,3,.36f,0f,.65f,.7f,.8f,EarthTone,M.Earth), // 바위 주먹
            new ArteProfile("sentinel-attack",M.Thrust,P.Attack,1,1,.36f,0f,.65f,.4f,0f,Steel,M.None), // 창 찌르기
            new ArteProfile("archer-attack",M.Arrow,P.Shoot,1,0,.36f,.22f,.65f,.25f,0f,Steel,M.None), // 유적의 화살
            new ArteProfile("mage-attack",M.Gravity,P.Cast,1,0,.36f,.2f,.65f,.4f,0f,Violet,M.None), // 그림자 탄
            new ArteProfile("polwigle-attack",M.Slash,P.Spin,1,1,.36f,0f,.65f,.55f,-.2f,Aqua,M.Water), // 꼬리치기
            new ArteProfile("eggbear-attack",M.Claw,P.Attack,3,0,.36f,0f,.65f,.65f,-.7f,EarthTone,M.Martial), // 곰의 일격
            new ArteProfile("penguinist-attack",M.Thrust,P.Dash,1,0,.36f,.22f,.65f,.3f,0f,Frost,M.Ice), // 서리 부리
            new ArteProfile("dhaos-attack",M.Prism,P.Cast,1,2,.36f,.2f,.65f,.4f,0f,Holy,M.Thrust), // 다오스 레이저
            new ArteProfile("dhaos-blast",M.Radiance,P.Cast,3,0,.65f,.2f,1.05f,.9f,0f,Holy,M.Wave), // 다오스 블래스트
            // Project-authored enemy choreography; source names retain project tactical behavior.
            new ArteProfile("boss.demitel.attack",M.Slash,P.Attack,1,0,.36f,0f,.65f,.55f,-.6f,Violet,M.None), // 공격
            new ArteProfile("boss.demitel.0",M.Prism,P.Cast,2,2,.65f,.2f,1.05f,.6f,.3f,Violet,M.Gravity), // Hell Gem
            new ArteProfile("boss.demitel.1",M.Cage,P.Cast,2,1,.65f,.2f,1.05f,.9f,0f,Violet,M.Gravity), // Summon Demon
            new ArteProfile("boss.demitel.2",M.Gravity,P.Cast,3,2,.65f,.2f,1.05f,.65f,0f,Violet,M.Claw), // 마력 착취
            new ArteProfile("boss.jahmir.attack",M.Slash,P.Attack,1,1,.36f,0f,.65f,.5f,-.5f,Violet,M.None), // 공격
            new ArteProfile("boss.jahmir.0",M.Gravity,P.Cast,4,0,.65f,.2f,1.05f,.85f,0f,Violet,M.Radiance), // Tractor Beam
            new ArteProfile("boss.jahmir.1",M.Earth,P.Cast,5,2,.65f,.2f,1.05f,.85f,0f,EarthTone,M.None), // Grave
            new ArteProfile("boss.jahmir.2",M.Cage,P.Cast,1,2,.65f,.2f,1.05f,.8f,0f,Violet,M.Gravity), // Summon Demon
            new ArteProfile("boss.shizel.attack",M.Slash,P.Attack,1,2,.36f,0f,.65f,.6f,-.65f,Violet,M.None), // 공격
            new ArteProfile("boss.shizel.0",M.Fire,P.Cast,5,3,.65f,.2f,1.05f,1f,.2f,Violet,M.Gravity), // Fear Flare
            new ArteProfile("boss.shizel.1",M.Cage,P.Cast,2,3,.65f,.2f,1.05f,1f,0f,Violet,M.Sleep), // Summon Demon
            new ArteProfile("boss.shizel.2",M.Judgment,P.Cast,8,3,.8f,.4f,1.15f,1.2f,0f,Violet,M.Gravity,true), // Eternal Finality
            new ArteProfile("boss.hyades.attack",M.Martial,P.Attack,2,1,.36f,0f,.65f,.55f,.3f,Gold,M.None), // 공격
            new ArteProfile("boss.hyades.0",M.Lightning,P.Cast,3,0,.65f,.2f,1.05f,.8f,0f,Electric,M.Wave), // Spark Wave
            new ArteProfile("boss.hyades.1",M.Earth,P.Attack,4,3,.45f,0f,.85f,1f,.78f,EarthTone,M.CrossSlash), // Earth Shaker
            new ArteProfile("boss.hyades.2",M.Lightning,P.Cast,1,1,.65f,.2f,1.05f,.9f,-.4f,Electric,M.Prism), // Thunder Blade
            new ArteProfile("boss.rassius.attack",M.Slash,P.Attack,1,0,.36f,0f,.65f,.6f,-.5f,Steel,M.None), // 공격
            new ArteProfile("boss.rassius.0",M.Dragon,P.Dash,3,2,.45f,.22f,.85f,.85f,0f,Steel,M.Slash), // Dragon Swarm
            new ArteProfile("boss.rassius.1",M.Dragon,P.Dash,1,3,.45f,.22f,.85f,.65f,0f,Steel,M.Thrust), // Dragon Flash
            new ArteProfile("boss.rassius.2",M.Barrier,P.Guard,3,1,.45f,0f,.85f,.8f,0f,Holy,M.Shield), // 기사의 약속
            new ArteProfile("boss.van.attack",M.Slash,P.Attack,1,1,.36f,0f,.65f,.65f,-.7f,Steel,M.None), // 공격
            new ArteProfile("boss.van.0",M.Dragon,P.Attack,2,0,.45f,.18f,.85f,.95f,0f,Holy,M.Radiance), // Radiant Dragon
            new ArteProfile("boss.van.1",M.CrossSlash,P.Dash,5,1,.8f,.3f,1.15f,.9f,.78f,Steel,M.Slash,true), // Imperial Slaughter
            new ArteProfile("boss.van.2",M.Cage,P.Cast,2,2,.65f,.2f,1.05f,1f,0f,Violet,M.Prism), // 복제의 성역
            new ArteProfile("boss.asch.attack",M.Slash,P.Attack,1,2,.36f,0f,.65f,.55f,-.6f,Crimson,M.None), // 공격
            new ArteProfile("boss.asch.0",M.CrossSlash,P.Dash,4,2,.8f,.3f,1.15f,1f,.78f,Crimson,M.Fire,true), // Rending Saber
            new ArteProfile("boss.asch.1",M.CrossSlash,P.Attack,2,0,.45f,0f,.85f,.65f,.78f,Crimson,M.None), // 검술 연격
            new ArteProfile("boss.asch.2",M.Barrier,P.Guard,2,2,.45f,0f,.85f,.7f,-.55f,Crimson,M.Slash), // 간파의 자세
            new ArteProfile("boss.legretta.attack",M.Rifle,P.Shoot,2,1,.36f,.22f,.65f,.3f,0f,Steel,M.None), // 공격
            new ArteProfile("boss.legretta.0",M.Rifle,P.Shoot,2,2,.45f,.22f,.85f,.5f,0f,Holy,M.Radiance), // Rage Laser
            new ArteProfile("boss.legretta.1",M.Prism,P.Cast,4,0,.65f,.2f,1.05f,.85f,.15f,Holy,M.Radiance), // Holy Lance
            new ArteProfile("boss.legretta.2",M.Prism,P.Shoot,7,3,.8f,.3f,1.15f,1.15f,-.5f,Holy,M.Rifle,true), // Prism Ballet
            new ArteProfile("boss.artorius.attack",M.Slash,P.Attack,1,0,.36f,0f,.65f,.65f,-.6f,Holy,M.None), // 공격
            new ArteProfile("boss.artorius.0",M.RisingSlash,P.Jump,4,3,.45f,0f,.85f,1f,1.15f,Holy,M.Radiance), // Ascension
            new ArteProfile("boss.artorius.1",M.CrossSlash,P.Dash,6,3,.8f,.3f,1.15f,1.05f,.78f,Gold,M.Claw,true), // Savage Wolf Fury
            new ArteProfile("boss.artorius.2",M.Cage,P.Cast,5,3,.65f,.2f,1.05f,1f,0f,Violet,M.Barrier), // 감정 봉인진
            new ArteProfile("boss.shigure.attack",M.Slash,P.Attack,1,2,.36f,0f,.65f,.7f,-.2f,Steel,M.None), // 공격
            new ArteProfile("boss.shigure.0",M.CrossSlash,P.Blink,4,3,.8f,.3f,1.15f,1.1f,.78f,Gale,M.Wind,true), // Rangetsu Eagle
            new ArteProfile("boss.shigure.1",M.Barrier,P.Guard,1,3,.45f,0f,.85f,.75f,-.2f,Steel,M.Slash), // 발도 반격
            new ArteProfile("boss.shigure.2",M.Slash,P.Dash,1,3,.45f,.22f,.85f,1f,-.15f,Steel,M.Wave), // 일도 양단
            new ArteProfile("boss.oscar.attack",M.Slash,P.Attack,1,1,.36f,0f,.65f,.6f,-.6f,Holy,M.None), // 공격
            new ArteProfile("boss.oscar.0",M.Slash,P.Dash,3,3,.45f,.22f,.85f,.85f,.2f,Holy,M.Water), // Heavenly Torrent
            new ArteProfile("boss.oscar.1",M.Barrier,P.Guard,4,2,.45f,0f,.85f,1f,0f,Holy,M.Radiance), // 성료 기사의 지휘
            new ArteProfile("boss.oscar.2",M.Thrust,P.Dash,2,3,.45f,.22f,.85f,.7f,0f,Holy,M.Wave), // 돌격 명령
            new ArteProfile("boss.vholran.attack",M.Slash,P.Attack,1,1,.36f,0f,.65f,.65f,-.65f,Aqua,M.None), // 공격
            new ArteProfile("boss.vholran.0",M.Water,P.Cast,4,2,.65f,.2f,1.05f,1.15f,0f,Aqua,M.Wave), // Tidal Wave
            new ArteProfile("boss.vholran.1",M.CrossSlash,P.Blink,6,2,.8f,.3f,1.15f,1.05f,.78f,Aqua,M.Water,true), // Lex Talionis
            new ArteProfile("boss.vholran.2",M.Ice,P.Cast,5,3,.65f,.2f,1.05f,.95f,0f,Frost,M.Thrust), // 빙결 추격
            new ArteProfile("boss.balseph.attack",M.Slash,P.Attack,1,3,.36f,0f,.65f,.85f,1.2f,Gold,M.None), // 공격
            new ArteProfile("boss.balseph.0",M.Fire,P.Attack,6,3,.8f,.3f,1.15f,1.15f,0f,Flame,M.Earth,true), // Pyroclasm
            new ArteProfile("boss.balseph.1",M.Slash,P.Attack,1,3,.45f,0f,.85f,.95f,1.2f,Flame,M.Fire), // 폭군의 내려찍기
            new ArteProfile("boss.balseph.2",M.Earth,P.Attack,4,1,.45f,0f,.85f,1f,0f,Flame,M.Fire), // 불타는 균열
            new ArteProfile("boss.ganabelt.attack",M.Thrust,P.Attack,1,2,.36f,0f,.65f,.5f,0f,Holy,M.None), // 공격
            new ArteProfile("boss.ganabelt.0",M.Grenade,P.Cast,5,3,.65f,.2f,1.05f,1.1f,0f,Holy,M.Prism), // Mine Barrage
            new ArteProfile("boss.ganabelt.1",M.Prism,P.Cast,1,2,.65f,.2f,1.05f,.6f,0f,Holy,M.Thrust), // Rage Laser
            new ArteProfile("boss.ganabelt.2",M.Lightning,P.Cast,1,3,.8f,.3f,1.15f,1.2f,0f,Holy,M.Judgment,true), // Indignation
            new ArteProfile("boss.ordo.attack",M.Prism,P.Cast,1,0,.36f,.2f,.65f,.45f,0f,Violet,M.None), // 공격
            new ArteProfile("boss.ordo.0",M.Clock,P.Cast,4,1,.65f,.2f,1.05f,.95f,.4f,Violet,M.Prism), // 기록 재생
            new ArteProfile("boss.ordo.1",M.Cleanse,P.Cast,4,2,.65f,.2f,1.05f,.9f,0f,Holy,M.Barrier), // 인과의 봉합
            new ArteProfile("boss.ordo.2",M.Clock,P.Cast,8,3,.8f,.3f,1.15f,1.2f,0f,Violet,M.Cage,true), // 영원한 정지
            new ArteProfile("boss.executor.attack",M.Thrust,P.Attack,1,2,.36f,0f,.65f,.55f,0f,Violet,M.None), // 공격
            new ArteProfile("boss.executor.0",M.Thrust,P.Dash,1,3,.45f,.22f,.85f,1f,0f,Violet,M.Cage), // 경계 절단
            new ArteProfile("boss.executor.1",M.Prism,P.Dash,1,3,.45f,.22f,.85f,.75f,0f,Crimson,M.Thrust), // 처형 표식
            new ArteProfile("boss.executor.2",M.Shield,P.Guard,4,2,.45f,0f,.85f,.95f,0f,Violet,M.Cage), // 방위 고정
            new ArteProfile("boss.origin.attack",M.Thrust,P.Attack,2,1,.36f,0f,.65f,.6f,.3f,Holy,M.None), // 공격
            new ArteProfile("boss.origin.0",M.Lightning,P.Cast,4,2,.65f,.2f,1.05f,1.05f,.78f,Electric,M.CrossSlash), // Collapse
            new ArteProfile("boss.origin.1",M.Barrier,P.Guard,6,3,.45f,0f,.85f,1f,0f,Holy,M.Shield), // Barrier
            new ArteProfile("boss.origin.2",M.Prism,P.Cast,4,3,.65f,.2f,1.05f,1.1f,.78f,Holy,M.Wave), // 사방의 계약
            new ArteProfile("boss.maxwell.attack",M.Prism,P.Cast,1,1,.36f,.2f,.65f,.5f,0f,Holy,M.None), // 공격
            new ArteProfile("boss.maxwell.0",M.Meteor,P.Cast,6,2,.65f,.5f,1.1f,1.15f,-.55f,Flame,M.Earth), // Meteor Swarm
            new ArteProfile("boss.maxwell.1",M.Gravity,P.Cast,5,2,.65f,.2f,1.05f,.95f,0f,Violet,M.Radiance), // Tractor Beam
            new ArteProfile("boss.maxwell.2",M.Wind,P.Cast,5,3,.65f,.2f,1.05f,1.1f,.2f,Gale,M.Wave), // Cyclone
            new ArteProfile("boss.dhaos.0",M.Prism,P.Cast,1,2,.65f,.2f,1.05f,.65f,0f,Holy,M.Thrust), // Dhaos Laser
            new ArteProfile("boss.dhaos.1",M.Radiance,P.Cast,3,1,.65f,.2f,1.05f,1.05f,0f,Holy,M.Wave), // Dhaos Blast
            new ArteProfile("boss.dhaos.2",M.Martial,P.Dash,4,2,.45f,.22f,.85f,.8f,.6f,Gold,M.CrossSlash), // Tetra Assault
            new ArteProfile("boss.golem.0",M.Earth,P.Attack,4,3,.45f,0f,.85f,1.1f,.78f,EarthTone,M.CrossSlash), // 지각 강타
            new ArteProfile("boss.golem.1",M.Heal,P.Guard,3,3,.45f,0f,.85f,.95f,0f,Healing,M.Earth), // 핵 재충전
            new ArteProfile("boss.golem.2",M.Earth,P.Attack,6,2,.45f,0f,.85f,1.2f,-.4f,EarthTone,M.Meteor), // 유적 붕괴
            // tactic
            new ArteProfile("tactic.heal",M.Heal,P.Cast,2,0,.65f,.2f,1.05f,.6f,0f,Healing,M.None), // 응급 치료
            new ArteProfile("tactic.burst",M.Fire,P.Cast,4,0,.65f,.2f,1.05f,.95f,0f,Flame,M.Barrier), // 예고 화염진
            new ArteProfile("tactic.charge",M.Thrust,P.Dash,1,2,.45f,.22f,.85f,.65f,0f,Steel,M.Wave), // 예고 돌진
            new ArteProfile("tactic.nova",M.Fire,P.Cast,6,2,.65f,.2f,1.05f,1.15f,0f,Flame,M.Gravity), // 다오스 붕괴진
        };

        // The wrapper prevents callers from replacing a shared row through the public API.
        public static IReadOnlyList<ArteProfile> All {get;}=Array.AsReadOnly(entries);
        static readonly Dictionary<string,ArteProfile> byId=CreateLookup();
        static Dictionary<string,ArteProfile> CreateLookup()
        {
            var result=new Dictionary<string,ArteProfile>(entries.Length,StringComparer.Ordinal);
            foreach(var profile in entries)result.Add(profile.Id,profile);
            return result;
        }
        public static ArteProfile Resolve(SkillData skill)
        {
            // cless.0, jade.3 and jade.6 intentionally keep the established trio signatures.
            if(skill==null||skill.Id==null)return default;
            return byId.TryGetValue(skill.Id,out var profile)?profile:default;
        }
    }
}
