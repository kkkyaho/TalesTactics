using System.Collections.Generic;
using UnityEngine;
namespace TalesTactics
{
    public sealed partial class BoardView
    {
        readonly Dictionary<string,List<SpriteRenderer>> fieldMarkers=new Dictionary<string,List<SpriteRenderer>>();
        readonly Dictionary<UnitRuntime,SpriteRenderer> bossMarkers=new Dictionary<UnitRuntime,SpriteRenderer>();
        SpriteRenderer FieldArt(string kind)
        {
            var g=new GameObject("Field icon - "+kind);g.transform.SetParent(root,false);
            var art=g.AddComponent<SpriteRenderer>();art.sprite=IllustratedIcons.Sprite(kind);art.sharedMaterial=SpriteMaterial;art.sortingOrder=7;
            var facing=g.AddComponent<FieldIconBillboard>();facing.Battle=battle;return art;
        }
        void FieldMarkers(string kind,IEnumerable<Vector2Int> cells)
        {
            if(!fieldMarkers.TryGetValue(kind,out var icons))fieldMarkers[kind]=icons=new List<SpriteRenderer>();
            int index=0;
            foreach(var cell in cells)
            {
                var tile=battle.Session.Grid[cell];if(tile==null)continue;
                if(index==icons.Count)icons.Add(FieldArt(kind));var art=icons[index++];art.gameObject.SetActive(true);
                art.transform.position=tile.WorldPosition(battle.Catalog.Rules.TileHeight)+new Vector3(.25f,.30f,.18f);art.transform.localScale=Vector3.one*.52f;
            }
            for(;index<icons.Count;index++)icons[index].gameObject.SetActive(false);
        }
        void UpdateBossMarkers()
        {
            foreach(var unit in battle.Session.Units)
            {
                bool visible=unit.Alive&&unit.Team==Team.Enemy&&(unit.IntentPhase==1||unit.IntentPhase==2);
                if(!bossMarkers.TryGetValue(unit,out var art)){if(!visible)continue;bossMarkers[unit]=art=FieldArt("core");}
                art.gameObject.SetActive(visible);if(!visible)continue;
                art.sprite=IllustratedIcons.Sprite(unit.IntentPhase==1?"danger":"core");art.transform.position=HeadWorld(unit)+Vector3.up*.35f;art.transform.localScale=Vector3.one*.4f;
            }
        }
    }
    public sealed class FieldIconBillboard:MonoBehaviour
    {
        public BattleDirector Battle;SpriteRenderer art;
        void Awake(){art=GetComponent<SpriteRenderer>();}
        void LateUpdate()
        {
            if(Battle==null||Battle.Board==null)return;
            art.enabled=!Battle.StoryActive&&!Battle.Hud.InputModalOpen;
            transform.rotation=Battle.Board.BattleCamera.transform.rotation;
        }
    }
}
