using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest] public IEnumerator AllSkillDetailPanelsFitAndKeepBattleValues()
        {
            var rows=new List<string>{"id,preferredHeight,availableHeight,missingGlyphs"};
            var failures=new List<string>();
            foreach(var owner in director.Catalog.Characters)
            {
                director.Restart();director.Deployment.Clear();director.Deployment.Add(System.Array.IndexOf(director.Catalog.Characters,owner));
                director.BeginBattle();director.StopAllCoroutines();
                for(int i=0;i<100&&director.Session.Active.Data!=owner;i++)director.Session.Advance();
                Assert.That(director.Session.Active.Data,Is.SameAs(owner));
                var unit=director.Session.Active;int hp=unit.CurrentHP,mp=unit.CurrentMP;
                var skills=new[]{owner.BasicAttack}.Concat(owner.Skills).Concat(new[]{owner.UltimateSkill});
                if(owner==director.Catalog.Characters[0])skills=skills.Concat(new[]{director.Catalog.Enemy.BasicAttack});
                foreach(var skill in skills.Where(s=>s!=null))
                {
                    director.SetState(new SkillDetailsState(director,skill));yield return null;
                    Canvas.ForceUpdateCanvases();
                    var description=director.Hud.GetComponentsInChildren<TMP_Text>().Single(t=>t.text==director.Session.Resolver.Describe(unit,skill));
                    description.ForceMeshUpdate();
                    bool glyphs=description.font.HasCharacters(description.text,out uint[] missing,true,true);
                    rows.Add(skill.Id+","+description.preferredHeight.ToString(System.Globalization.CultureInfo.InvariantCulture)+","+description.rectTransform.rect.height.ToString(System.Globalization.CultureInfo.InvariantCulture)+","+(missing?.Length??0));
                    if(description.preferredHeight>description.rectTransform.rect.height+1)failures.Add(skill.Id+" text exceeds panel by "+(description.preferredHeight-description.rectTransform.rect.height));
                    if(!glyphs)failures.Add(skill.Id+" missing glyphs");
                    Assert.That(unit.CurrentHP,Is.EqualTo(hp));Assert.That(unit.CurrentMP,Is.EqualTo(mp));
                }
            }
            File.WriteAllLines(Path.Combine(Application.dataPath,"../Docs/skill-panel-review.csv"),rows);
            Assert.That(rows.Count,Is.EqualTo(90));Assert.That(failures,Is.Empty,string.Join("\n",failures));
        }
    }
}
