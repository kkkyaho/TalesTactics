using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TalesTactics;
public static class RosterPaletteScan
{
 public static string Main(string output)
 {
  var battle=UnityEngine.Object.FindAnyObjectByType<BattleDirector>();
  if(!Application.isPlaying||battle?.Session==null||battle.Session.CampaignStage!=11)throw new InvalidOperationException("Only isolated actual episode-six battle is supported.");
  string allowed="C:/dev/TalesTactics/Logs/RosterMotion20261125/";
  output=Path.GetFullPath(output).Replace('\\','/');
  if(!output.StartsWith(allowed,StringComparison.OrdinalIgnoreCase)||File.Exists(output))throw new InvalidOperationException("Use fresh report inside task logs.");
  var renderers=UnityEngine.Object.FindObjectsByType<Renderer>().Where(r=>r.gameObject.activeInHierarchy).ToArray();
  var materials=renderers.SelectMany(r=>r.sharedMaterials.Select((m,i)=>new {renderer=r.name,slot=i,material=m==null?null:m.name,shader=m?.shader==null?null:m.shader.name,defaultMaterial=m!=null&&m.name=="Default-Material",nullShader=m==null||m.shader==null,errorShader=m?.shader!=null&&m.shader.name.Contains("InternalErrorShader")})).ToArray();
  var particles=UnityEngine.Object.FindObjectsByType<ParticleSystem>().Select(p=>new {name=p.name,mode=p.main.startColor.mode.ToString(),color=ColorUtility.ToHtmlStringRGBA(p.main.startColor.color),colorMin=ColorUtility.ToHtmlStringRGBA(p.main.startColor.colorMin),colorMax=ColorUtility.ToHtmlStringRGBA(p.main.startColor.colorMax),active=p.gameObject.activeInHierarchy}).ToArray();
  var images=battle.Hud.GetComponentsInChildren<Image>(true).Select(i=>new {name=i.name,sprite=i.sprite?.name,color=ColorUtility.ToHtmlStringRGBA(i.color),active=i.gameObject.activeInHierarchy,whiteTint=i.color.r>.99f&&i.color.g>.99f&&i.color.b>.99f,grayTint=Mathf.Abs(i.color.r-i.color.g)<.01f&&Mathf.Abs(i.color.g-i.color.b)<.01f,interpretation=i.sprite!=null?"Tint multiplies authored sprite; white preserves sprite colors.":"No sprite; color alone does not establish intent."}).ToArray();
  var texts=battle.Hud.GetComponentsInChildren<TMP_Text>(true).Select(t=>new {name=t.name,font=t.font?.name,fontSize=t.fontSize,autoSize=t.enableAutoSizing,defaultFont=t.font==TMP_Settings.defaultFontAsset,defaultFontSize=Mathf.Abs(t.fontSize-TMP_Settings.defaultFontSize)<.01f,color=ColorUtility.ToHtmlStringRGBA(t.color),active=t.gameObject.activeInHierarchy}).ToArray();
  var selected=battle.Session.Active;
  var motion=battle.Board.GetComponentsInChildren<CharacterMotion>().Single(m=>m.Owner==selected);var sr=motion.GetComponent<SpriteRenderer>();
  var result=new {scope="Read-only palette/material telemetry of isolated actual episode-six BoardView; not whole-game quality approval or authored-intent judgment.",utc=DateTime.UtcNow.ToString("o"),stage=battle.Session.CampaignStage,selected=battle.Session.Active.Data.Id,rendererCount=renderers.Length,defaultMaterialCount=materials.Count(m=>m.defaultMaterial),nullShaderCount=materials.Count(m=>m.nullShader),errorShaderCount=materials.Count(m=>m.errorShader),materials,particles,hudImages=images,textDefaults=new {font=TMP_Settings.defaultFontAsset?.name,size=TMP_Settings.defaultFontSize},texts,selectedCharacter=new {id=selected.Data.Id,sprite=sr.sprite?.name,hp=selected.CurrentHP,root=motion.transform.parent.parent.name,children=motion.transform.parent.parent.GetComponentsInChildren<Transform>(true).Select(t=>t.name).ToArray()}};
  string json=Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented);Directory.CreateDirectory(Path.GetDirectoryName(output));File.WriteAllText(output,json);return output;
 }
}