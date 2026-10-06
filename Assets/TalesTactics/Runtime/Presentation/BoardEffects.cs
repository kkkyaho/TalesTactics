using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
namespace TalesTactics
{
    public sealed partial class BoardView
    {
        LineRenderer timingRing;
        LineRenderer timingWindow;
        CharacterMotion timingMotion;
        public Dictionary<UnitRuntime,int> CaptureHealth()
        {
            var snapshot=new Dictionary<UnitRuntime,int>();
            foreach(var unit in units.Keys)snapshot[unit]=unit.CurrentHP;
            return snapshot;
        }
        static Color ElementColor(Element element)
        {
            switch(element)
            {
                case Element.Fire:return new Color(1,0.35f,0.1f);
                case Element.Water:case Element.Ice:return new Color(0.25f,0.8f,1);
                case Element.Wind:return new Color(0.45f,1,0.7f);
                case Element.Earth:return new Color(1,0.7f,0.3f);
                case Element.Dark:return new Color(0.85f,0.35f,1);
                case Element.Lightning:return new Color(1,0.95f,0.3f);
                default:return new Color(1,0.95f,0.8f);
            }
        }
        LineRenderer EffectLine(string name,Color color,float width)
        {
            var g=new GameObject(name);g.transform.SetParent(root,false);
            var renderer=g.AddComponent<LineRenderer>();renderer.sharedMaterial=SpriteMaterial;
            renderer.startWidth=renderer.endWidth=width;renderer.startColor=renderer.endColor=color;
            renderer.numCapVertices=3;renderer.sortingOrder=20;return renderer;
        }
        void ShowTimingRing(UnitRuntime unit,float progress)
        {
            if(root==null||!units.ContainsKey(unit))return;
            if(timingRing==null)timingRing=EffectLine("Timing arc",Color.white,0.06f);
            if(timingWindow==null)timingWindow=EffectLine("Timing success window",new Color(0.3f,1,0.55f,0.5f),0.09f);
            var rules=battle.Catalog.Rules;
            var color=progress>=rules.TimingWindowStart&&progress<=rules.TimingWindowEnd?Color.green:Color.yellow;
            timingRing.startColor=timingRing.endColor=color;
            timingRing.positionCount=33;
            timingWindow.positionCount=33;
            for(int i=0;i<33;i++)
            {
                float a=(float)i/32*progress*Mathf.PI*2;
                timingRing.SetPosition(i,units[unit].position+new Vector3(Mathf.Cos(a)*0.55f,0.1f,Mathf.Sin(a)*0.55f));
                float w=Mathf.Lerp(rules.TimingWindowStart,rules.TimingWindowEnd,i/32f)*Mathf.PI*2;
                timingWindow.SetPosition(i,units[unit].position+new Vector3(Mathf.Cos(w)*0.67f,0.1f,Mathf.Sin(w)*0.67f));
            }
        }
        public void ClearTiming()
        {
            if(timingRing!=null)Destroy(timingRing.gameObject);timingRing=null;
            if(timingWindow!=null)Destroy(timingWindow.gameObject);timingWindow=null;
            if(timingMotion!=null)timingMotion.EndSpin();timingMotion=null;
        }
        public void PresentImpact(UnitRuntime caster,SkillData skill,Vector2Int aim,Dictionary<UnitRuntime,int> before,UnitRuntime[] recipients=null)
        {
            if(root==null)return;
            var affected=new HashSet<UnitRuntime>(recipients??battle.Session.Resolver.Targets(caster,skill,aim));
            int cost=battle.Session.Resolver.HPCost(caster,skill);
            var style=CombatEffect.Resolve(caster.Data,skill);
            bool healing=false,damage=false;
            // Knockback/revival may have changed cells during resolution.
            foreach(var unit in units.Keys)units[unit].position=battle.Session.Grid[unit.Position].WorldPosition(battle.Catalog.Rules.TileHeight);
            foreach(var pair in before)
            {
                var unit=pair.Key;
                int delta=unit.CurrentHP-pair.Value+(unit==caster?cost:0);
                if(delta==0&&!affected.Contains(unit))continue;
                healing|=delta>0;damage|=delta<0;
                var feedback=delta>0?(pair.Value==0?CombatFeedback.Revive:CombatFeedback.Heal):
                    delta<0||skill.Effects.Any(e=>e.Kind==EffectKind.Damage&&!e.AffectCaster)?CombatFeedback.Strike:CombatFeedback.Support;
                var color=feedback==CombatFeedback.Revive?new Color(1,0.95f,0.5f):
                    feedback==CombatFeedback.Heal?new Color(0.35f,1,0.6f):ElementColor(skill.Element);
                var effect=new GameObject("Combat VFX");effect.transform.SetParent(root,false);
                effect.AddComponent<CombatEffect>().Initialize(SpriteMaterial,BattleCamera,style,feedback,unit,
                    units[caster].position+BattleCamera.transform.up*0.65f,units[unit].position+BattleCamera.transform.up*0.65f,color,skill.IsUltimate,skill.Presentation);
                if(delta!=0)
                {
                    // Self costs/drain must not interrupt the caster's attack or ultimate.
                    if(unit!=caster)SetAnimation(unit,unit.Alive?(delta<0?AnimationKind.Damage:AnimationKind.Cast):AnimationKind.Dead);
                    StartCoroutine(Number(unit,delta));
                }
            }
            if(cost>0)StartCoroutine(Number(caster,-cost,true));
            battle.Audio.PlayEffect(damage?"hit":healing?"heal":"cast");
        }
        IEnumerator Number(UnitRuntime unit,int delta,bool cost=false)
        {
            var g=new GameObject("HP feedback");g.transform.SetParent(root,false);
            var label=g.AddComponent<TextMeshPro>();label.text=cost?"HP "+delta:delta>0?"+"+delta:delta.ToString();
            label.font=battle.Hud.Font;
            label.fontSize=4.5f;label.alignment=TextAlignmentOptions.Center;
            label.color=cost?new Color(1,0.65f,0.3f):delta>0?new Color(0.4f,1,0.6f):new Color(1,0.8f,0.6f);
            label.sortingOrder=30;label.rectTransform.sizeDelta=new Vector2(2.4f,0.65f);
            var backing=GameObject.CreatePrimitive(PrimitiveType.Quad);backing.name="Feedback backing";Destroy(backing.GetComponent<Collider>());backing.transform.SetParent(g.transform,false);
            backing.transform.localPosition=new Vector3(0,0,.02f);backing.transform.localScale=new Vector3(Mathf.Clamp(label.text.Length*.3f,.9f,2.4f),.48f,1);
            var background=backing.GetComponent<Renderer>();background.sharedMaterial=SpriteMaterial;background.sortingOrder=29;
            var origin=units[unit].position;
            for(float t=0;t<0.65f;t+=Time.deltaTime)
            {g.transform.position=origin+BattleCamera.transform.up*((cost?1.05f:1.7f)+t*.45f)+BattleCamera.transform.right*(cost?-.4f:0);g.transform.rotation=BattleCamera.transform.rotation;float alpha=1-Mathf.InverseLerp(.38f,.65f,t);label.alpha=alpha;SetColor(background,new Color(.025f,.035f,.06f,.85f*alpha));yield return null;}
            Destroy(g);
        }
    }
}
