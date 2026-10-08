$ErrorActionPreference='Stop'
$rows=@()
foreach($stage in 0..5){
$code=@'
var catalog=UnityEditor.AssetDatabase.FindAssets("t:BattleCatalog").Select(g=>UnityEditor.AssetDatabase.LoadAssetAtPath<TalesTactics.BattleCatalog>(UnityEditor.AssetDatabase.GUIDToAssetPath(g))).First();
int stage=STAGE;var party=TalesTactics.TacticalDevelopment.Recommended(catalog);var save=new TalesTactics.CampaignSave();
for(int i=0;i<stage;i++)save.StoryProgress.Add(TalesTactics.CampaignStages.Id(i));
var session=new TalesTactics.BattleSession(catalog,party,TalesTactics.CampaignStages.Get(stage).EntryLevel,TalesTactics.CampaignStages.EnemyLevel(stage),stage,teamTurns:true,phaseSurvival:true,tacticalCombat:true);
foreach(var u in session.Units.Where(u=>u.Team==TalesTactics.Team.Player))TalesTactics.TacticalDevelopment.Apply(u,save,stage);
int turns=0,skills=0,pincers=0;var ai=new TalesTactics.EnemyPlanner();
while(session.Result==TalesTactics.BattleResult.Ongoing&&turns++<400){
 session.Advance();var u=session.Active;
 if(!u.Has(TalesTactics.StatusKind.Stun)&&!u.Has(TalesTactics.StatusKind.Sleep)){
 var plan=ai.Plan(session,u);TalesTactics.EnemyTactics.Commit(u,plan);
 if(plan.Destination!=u.Position)session.Move(plan.Destination);
 if(session.Result==TalesTactics.BattleResult.Ongoing&&plan.Skill!=null&&(plan.Aim.HasValue||plan.Target!=null)){
 var aim=plan.Aim??plan.Target.Position;if(aim!=u.Position)u.Facing=TalesTactics.SkillResolver.Toward(u.Position,aim);
 pincers+=session.Resolver.Targets(u,plan.Skill,aim).Count(t=>session.Resolver.HasPincer(u,t));
 if(!session.Resolver.Execute(u,plan.Skill,aim,out var error))throw new System.Exception(error);skills++;
 }else if(plan.Guard)u.AddStatus(TalesTactics.StatusKind.Guard,2);
 }
 session.EndTurn();
}
return new {stage=stage+1,level=TalesTactics.CampaignStages.Get(stage).EntryLevel,result=session.Result.ToString(),turns,skills,pincers,survivors=session.Units.Count(u=>u.Team==TalesTactics.Team.Player&&u.Alive)};
'@
$r=unity command eval --caller plugin --skill ui-ugui --format json -- $code.Replace('STAGE',"$stage")|ConvertFrom-Json
if(!$r.success -or !$r.data.result.success){throw ($r|ConvertTo-Json -Depth 10)}
$rows+=$r.data.result.result
$rows|ConvertTo-Json|Set-Content Docs/TacticalPolish/campaign-scenarios.json -Encoding utf8
$r.data.result.result|Format-Table
}
if($rows|Where-Object result -eq Ongoing){throw 'A battle exceeded 400 turns'}
