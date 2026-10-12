# Engine simulation using real content, legal AI actions and isolated campaign files.
$ErrorActionPreference='Stop'
$rows=@()
foreach($catchUp in @($false,$true)){
foreach($stage in 0..5){
$code=@'
var catalog=UnityEditor.AssetDatabase.LoadAssetAtPath<TalesTactics.BattleCatalog>("Assets/TalesTactics/Content/BattleCatalog.asset");
int stage=STAGE;bool catchUp=CATCHUP;string path="Temp/action-experience-progression-"+catchUp+".json";
var save=stage==0?new TalesTactics.CampaignSave():UnityEngine.JsonUtility.FromJson<TalesTactics.CampaignSave>(System.IO.File.ReadAllText(path));
var party=TalesTactics.TacticalDevelopment.Recommended(catalog);
var trained=new System.Collections.Generic.List<string>();
if(catchUp)foreach(int index in party)if(TalesTactics.TacticalDevelopment.CatchUp(save,catalog.Characters[index],stage,_=>true))trained.Add(catalog.Characters[index].Id);
var b=new TalesTactics.BattleSession(catalog,party,1,TalesTactics.CampaignStages.EnemyLevel(stage),stage,utilityAI:true,teamTurns:true,phaseSurvival:true,tacticalCombat:true,bossEncounters:true,reactions:true);
var players=b.Units.Where(u=>u.Team==TalesTactics.Team.Player).ToArray();
foreach(var u in players){var p=save.Get(u.Data.Id);u.Level=p.Level;u.EXP=p.EXP;TalesTactics.TacticalDevelopment.Apply(u,save,stage);u.CurrentHP=u.Stats.HP;u.CurrentMP=u.Stats.MP;}
var before=players.ToDictionary(u=>u.Data.Id,u=>u.Level);var actions=players.ToDictionary(u=>u.Data.Id,u=>0);var support=players.ToDictionary(u=>u.Data.Id,u=>0);
int turns=0;var ai=new TalesTactics.EnemyPlanner();
while(b.Result==TalesTactics.BattleResult.Ongoing&&turns++<400){
 b.Advance();var u=b.Active;
 if(!u.Has(TalesTactics.StatusKind.Stun)&&!u.Has(TalesTactics.StatusKind.Sleep)){
 var plan=ai.Plan(b,u);TalesTactics.EnemyTactics.Commit(u,plan);
 if(plan.Destination!=u.Position)b.Move(plan.Destination);
 if(b.Result==TalesTactics.BattleResult.Ongoing&&plan.Skill!=null&&(plan.Aim.HasValue||plan.Target!=null)){
 var aim=plan.Aim??plan.Target.Position;if(aim!=u.Position)u.Facing=TalesTactics.SkillResolver.Toward(u.Position,aim);
 if(!b.Resolver.Execute(u,plan.Skill,aim,out var error))throw new System.Exception(error);
 if(u.Team==TalesTactics.Team.Player){actions[u.Data.Id]++;if(plan.Skill.Target!=TalesTactics.TargetType.Enemy)support[u.Data.Id]++;}
 }else if(plan.Guard)u.AddStatus(TalesTactics.StatusKind.Guard,2);
 }
 b.EndTurn();
}
bool won=b.Result==TalesTactics.BattleResult.Victory;
if(won){var reward=TalesTactics.CampaignEconomy.Prepare(save,stage,9999);if(!TalesTactics.CampaignStages.TryReward(save,reward,players.Select(u=>u.Data).ToArray(),_=>true,players.ToDictionary(u=>u.Data.Id,u=>u.BattleEXP)))throw new System.Exception("Reward failed");}
System.IO.File.WriteAllText(path,UnityEngine.JsonUtility.ToJson(save));
return new {stage=stage+1,catchUp,trained,result=b.Result.ToString(),turns,units=players.Select(u=>new {id=u.Data.Id,beforeLevel=before[u.Data.Id],battleLevel=u.Level,actionEXP=u.BattleEXP,actions=actions[u.Data.Id],supportActions=support[u.Data.Id],savedLevel=save.Get(u.Data.Id).Level,savedEXP=save.Get(u.Data.Id).EXP,alive=u.Alive}).ToArray()};
'@
$response=unity command eval --caller plugin --skill unity-cli --format json -- $code.Replace('STAGE',"$stage").Replace('CATCHUP',$catchUp.ToString().ToLowerInvariant())|ConvertFrom-Json
if(!$response.success -or !$response.data.result.success){throw ($response|ConvertTo-Json -Depth 8)}
$row=$response.data.result.result;$rows+=$row
$rows|ConvertTo-Json -Depth 8|Set-Content Docs/ActionExperience/progression.json -Encoding utf8
Write-Output "stage=$($row.stage) catchUp=$catchUp result=$($row.result) turns=$($row.turns)"
if($row.result -ne 'Victory'){break}
}
}
