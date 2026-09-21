using System.Collections.Generic;
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
            error=null;return true;
        }
        static bool Character(CharacterData c,string location,out string error)
        {
            if(c==null){error=location+" reference is missing.";return false;}
            if(string.IsNullOrWhiteSpace(c.Id)){error=location+" has no ID.";return false;}
            if(c.BasicAttack==null){error=location+" ("+c.Id+") has no BasicAttack.";return false;}
            if(c.Skills==null){error=location+" ("+c.Id+") has a null Skills array.";return false;}
            foreach(var skill in c.Skills)if(skill==null){error=location+" ("+c.Id+") has a missing skill reference.";return false;}
            if(c.BaseStats.HP<1||c.BaseStats.SPD<1||c.BaseStats.MOV<1){error=location+" requires positive HP/SPD/MOV.";return false;}
            error=null;return true;
        }
    }
}
