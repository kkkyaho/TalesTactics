using System.Collections.Generic;
using System.Linq;
namespace TalesTactics
{
    public static class CatalogValidation
    {
        public static bool TryValidate(BattleCatalog catalog,out string error)
        {
            if(catalog==null){error="BattleCatalog reference is missing.";return false;}
            if(catalog.Rules==null){error="BattleCatalog.Rules reference is missing.";return false;}
            if(catalog.Rules.MaxDeployment<1||catalog.Rules.MaxDeployment>6){error="MaxDeployment must be between 1 and 6.";return false;}
            if(catalog.Characters==null||catalog.Characters.Length==0){error="BattleCatalog.Characters is empty.";return false;}
            var ids=new HashSet<string>();
            for(int i=0;i<catalog.Characters.Length;i++)
            {
                var c=catalog.Characters[i];
                if(!Character(c,"Characters["+i+"]",out error))return false;
                if(!ids.Add(c.Id)){error="Duplicate character ID: "+c.Id;return false;}
            }
            if(!Character(catalog.Enemy,"Enemy",out error))return false;
            if(catalog.Enemies!=null&&catalog.Enemies.Length>0)
            {
                var enemyIds=new HashSet<string>();
                foreach(var enemy in catalog.Enemies)
                {
                    if(!Character(enemy,"Enemies",out error))return false;
                    if(!enemyIds.Add(enemy.Id)){error="Duplicate enemy ID: "+enemy.Id;return false;}
                }
                for(int stage=0;stage<(catalog.CrossoverReady?CampaignStages.Count:CampaignStages.LegacyCount);stage++)for(int slot=0;slot<4;slot++)
                    if(!enemyIds.Contains(CampaignEnemies.Id(stage,slot))){error="Missing campaign enemy: "+CampaignEnemies.Id(stage,slot);return false;}
            }
            if(catalog.CrossoverReady&&!TryValidateCrossover(catalog,out error))return false;
            error=null;return true;
        }
        public static bool TryValidateCrossover(BattleCatalog catalog,out string error)
        {
            if(catalog.Civilians==null||catalog.Civilians.Length!=4||catalog.Civilians.Any(c=>!Directions(c))){error="Missing civilian rescue presentation";return false;}
            foreach(var entry in CrossoverBossSources.Entries)
            {
                CharacterData boss;
                try{boss=CrossoverCampaign.Boss(catalog,entry[0]);}catch(System.InvalidOperationException){error="Missing crossover boss: "+entry[0];return false;}
                if(!Character(boss,"Crossover",out error))return false;
                var skills=boss.Skills.Where(s=>s.Id!=null&&s.Id.StartsWith("boss."+boss.Id+".")&&s!=boss.BasicAttack).ToArray();
                if(skills.Length!=3||Enumerable.Range(0,3).Any(i=>skills.Count(s=>s.Id=="boss."+boss.Id+"."+i)!=1)){error="Boss requires three unique authored skills: "+boss.Id;return false;}
                if(!Directions(boss.Sprites)||boss.Poses==null||new[]{boss.Poses.Walk,boss.Poses.AttackMotion,boss.Poses.Skill,boss.Poses.Ultimate}.Any(c=>c==null||c.Frames==null||c.Frames.Length<4||c.Frames.Any(f=>!Directions(f)))){error="Missing directional boss motion: "+boss.Id;return false;}
            }
            foreach(var boss in CrossoverBossSources.Entries.Select(e=>CrossoverCampaign.Boss(catalog,e[0])).Concat(catalog.CrossoverVariants??new CharacterData[0]))
                if(boss.Poses?.Dead?.Frames==null||boss.Poses.Dead.Frames.Length<2||boss.Poses.Dead.Frames.Any(f=>!Directions(f))){error="Missing directional collapse motion: "+boss.Id;return false;}
            if(catalog.CrossoverVariants==null||!catalog.CrossoverVariants.Any(c=>c!=null&&c.Id=="hyades"&&c.CrossoverEarlyForm&&Directions(c.Sprites))){error="Missing early Hyades appearance";return false;}
            var device=catalog.Enemies.FirstOrDefault(c=>c.Id=="boundary-device");
            if(device==null||!Directions(device.Sprites)||device.Poses?.Dead?.Frames==null||device.Poses.Dead.Frames.Length==0){error="Missing destructible device presentation";return false;}
            foreach(var arc in PersonalScenarios.Arcs)
            {
                var weapon=(catalog.Equipment??new EquipmentData[0]).Where(e=>e!=null&&e.Id==arc.WeaponId).ToArray();
                if(weapon.Length!=1||weapon[0].ExclusiveCharacterId!=arc.Owner||weapon[0].Effect!=arc.Effect||weapon[0].Slot!=EquipmentSlot.Weapon){error="Invalid exclusive weapon: "+arc.WeaponId;return false;}
                if(!catalog.Characters.Any(c=>c.Id==arc.Owner)){error="Missing personal scenario owner: "+arc.Owner;return false;}
            }
            error=null;return true;
        }
        static bool Directions(DirectionalSprites sprites)=>sprites!=null&&sprites.Front!=null&&sprites.Back!=null&&sprites.Left!=null&&sprites.Right!=null;
        static bool Character(CharacterData c,string location,out string error)
        {
            if(c==null){error=location+" reference is missing.";return false;}
            if(string.IsNullOrWhiteSpace(c.Id)){error=location+" has no ID.";return false;}
            if(c.BasicAttack==null){error=location+" ("+c.Id+") has no BasicAttack.";return false;}
            if(c.Skills==null){error=location+" ("+c.Id+") has a null Skills array.";return false;}
            foreach(var skill in c.Skills)if(skill==null){error=location+" ("+c.Id+") has a missing skill reference.";return false;}
            if(c.BaseStats.HP<1||c.BaseStats.SPD<1||(c.BaseStats.MOV<1&&c.Id!="boundary-device")){error=location+" requires positive HP/SPD/MOV.";return false;}
            error=null;return true;
        }
    }
}
