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
        public void PresentImpact(UnitRuntime caster,SkillData skill,Vector2Int aim,Dictionary<UnitRuntime,int> before,UnitRuntime[] recipients=null,Dictionary<UnitRuntime,int> after=null,bool costs=true)
        {
            if(root==null)return;
            var affected=new HashSet<UnitRuntime>(recipients??battle.Session.Resolver.Targets(caster,skill,aim));
            int cost=costs?battle.Session.Resolver.HPCost(caster,skill):0;
            var style=CombatEffect.Resolve(caster.Data,skill);var signature=SkillSignatures.Resolve(skill);var arte=ArteProfiles.Resolve(skill);
            bool shared=affected.Count(unit=>units.ContainsKey(unit)&&before.ContainsKey(unit))>1;
            var origin=(motions.TryGetValue(caster,out var sourceMotion)?sourceMotion.transform.position:units[caster].position)+BattleCamera.transform.up*.65f;
            // Knockback/revival may have changed cells during resolution.
            foreach(var unit in units.Keys)units[unit].position=battle.Session.Grid[unit.Position].WorldPosition(battle.Catalog.Rules.TileHeight);
            if(shared)
            {
                // One large area silhouette; every actual recipient retains a small contact mark and number.
                bool strikes=skill.Effects.Any(e=>e.Kind==EffectKind.Damage&&!e.AffectCaster);
                var feedback=strikes?CombatFeedback.Strike:skill.Effects.Any(e=>e.Kind==EffectKind.Revive)?CombatFeedback.Revive:
                    skill.Effects.Any(e=>e.Kind==EffectKind.Heal)?CombatFeedback.Heal:CombatFeedback.Support;
                var center=battle.Session.Grid[aim].WorldPosition(battle.Catalog.Rules.TileHeight)+(signature==SkillSignature.None&&!arte.Enabled?BattleCamera.transform.up*.65f:Vector3.up*.08f);
                // A null recipient marks a visual-only centerpiece. It owns the common mesh and,
                // for older signatures such as Time Stop/Cage, their one retained pattern layer.
                var centerpiece=new GameObject("Area VFX centerpiece");centerpiece.transform.SetParent(root,false);
                centerpiece.AddComponent<CombatEffect>().Initialize(SpriteMaterial,BattleCamera,style,feedback,null,
                    origin,center,ElementColor(skill.Element),skill.IsUltimate,skill.Presentation,skill.Element,battle.Preferences.ReducedEffects,false,signature,arte);
            }
            foreach(var pair in before)
            {
                var unit=pair.Key;
                if(!units.ContainsKey(unit))continue;
                int hp=after!=null&&after.TryGetValue(unit,out var value)?value:unit.CurrentHP;
                int delta=hp-pair.Value+(unit==caster?cost:0);
                if(delta==0&&!affected.Contains(unit))continue;
                var feedback=delta>0?(pair.Value==0?CombatFeedback.Revive:CombatFeedback.Heal):
                    delta<0||skill.Effects.Any(e=>e.Kind==EffectKind.Damage&&!e.AffectCaster)?CombatFeedback.Strike:CombatFeedback.Support;
                var color=feedback==CombatFeedback.Revive?new Color(1,0.95f,0.5f):
                    feedback==CombatFeedback.Heal?new Color(0.35f,1,0.6f):ElementColor(skill.Element);
                // Life drain is a secondary recovery, not another strike on the caster.
                // Primary healing/revival recipients keep the skill's authored profile.
                bool secondaryRecovery=delta>0&&!affected.Contains(unit);
                var recipientSignature=secondaryRecovery?SkillSignature.None:signature;
                var recipientArte=secondaryRecovery?default:arte;
                var effect=new GameObject("Combat VFX");effect.transform.SetParent(root,false);
                effect.AddComponent<CombatEffect>().Initialize(SpriteMaterial,BattleCamera,style,feedback,unit,
                    origin,units[unit].position+((recipientSignature==SkillSignature.None&&!recipientArte.Enabled)||shared?BattleCamera.transform.up*.65f:Vector3.up*.08f),color,skill.IsUltimate,skill.Presentation,skill.Element,battle.Preferences.ReducedEffects,shared&&affected.Contains(unit),recipientSignature,recipientArte);
                if(delta!=0)
                {
                    // Self costs/drain must not interrupt the caster's attack or ultimate.
                    if(unit!=caster)SetAnimation(unit,hp>0?(delta<0?AnimationKind.Damage:AnimationKind.Cast):AnimationKind.Dead);
                    if(unit!=caster&&delta<0&&motions.TryGetValue(unit,out var hitMotion))
                        hitMotion.Impact((units[unit].position-units[caster].position).normalized,skill.IsUltimate||style==CombatVisualStyle.Shield);
                    StartCoroutine(Number(unit,delta));
                }
            }
            if(cost>0)StartCoroutine(Number(caster,-cost,true));
            battle.Audio.PlaySkillCue(skill,style,true,battle.Preferences.ReducedEffects);
        }
        public void PresentExperience(IEnumerable<ExperienceGain> gains)
        {
            foreach(var gain in gains)if(root!=null&&gain!=null&&units.ContainsKey(gain.Unit))battle.Hud.CombatFeedback.Enqueue(gain);
        }
        IEnumerator Number(UnitRuntime unit,int delta,bool cost=false)
        {
            if(units.ContainsKey(unit))battle.Hud.CombatFeedback.Number(unit,delta,cost);
            yield break;
        }
        public Vector3 HeadWorld(UnitRuntime unit)
        {
            if(sprites.TryGetValue(unit,out var sprite)&&sprite!=null)
                return sprite.bounds.center+BattleCamera.transform.up*(sprite.sprite.bounds.extents.y*sprite.transform.lossyScale.y+.18f);
            return battle.Session.Grid[unit.Position].WorldPosition(battle.Catalog.Rules.TileHeight)+BattleCamera.transform.up*1.4f;
        }
    }
}
