using System;
using System.Collections.Generic;
using UnityEngine;
namespace TalesTactics
{
    // Authored from opaque head pixels offline. Never reads or makes a texture readable at runtime.
    public static class UnitHeadAnchors
    {
        [Serializable] sealed class Catalog {public int schemaVersion;public Entry[] sprites;}
        [Serializable] sealed class Entry {public string sprite;public int width,height;public float u,v;}
        static Dictionary<string,Vector2> normalized;
        static readonly Dictionary<Sprite,Vector3> local=new Dictionary<Sprite,Vector3>();
        static readonly HashSet<Sprite> missing=new HashSet<Sprite>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset(){normalized=null;local.Clear();missing.Clear();}
        static string Key(string name,int width,int height)=>name+"|"+width+"x"+height;
        public static bool TryGetLocal(Sprite sprite,out Vector3 point)
        {
            point=default;if(sprite==null)return false;
            if(local.TryGetValue(sprite,out point))return true;
            if(missing.Contains(sprite))return false;
            if(normalized==null)
            {
                normalized=new Dictionary<string,Vector2>(StringComparer.Ordinal);
                var asset=Resources.Load<TextAsset>("TalesTactics/UnitHeadAnchors");
                var catalog=asset!=null?JsonUtility.FromJson<Catalog>(asset.text):null;
                if(catalog?.schemaVersion==1&&catalog.sprites!=null)
                    foreach(var entry in catalog.sprites)normalized[Key(entry.sprite,entry.width,entry.height)]=new Vector2(entry.u,entry.v);
            }
            // Sprite names and source rect dimensions survive atlas packing. Two golem sheets share names.
            var rect=sprite.rect;
            if(!normalized.TryGetValue(Key(sprite.name,Mathf.RoundToInt(rect.width),Mathf.RoundToInt(rect.height)),out var uv))
            {missing.Add(sprite);return false;}
            point=new Vector3((uv.x*rect.width-sprite.pivot.x)/sprite.pixelsPerUnit,
                (uv.y*rect.height-sprite.pivot.y)/sprite.pixelsPerUnit,0);
            local[sprite]=point;return true;
        }
    }
    public sealed partial class BoardView
    {
        public Vector3 UnitHeadScreenOrigin(UnitRuntime unit)
        {
            Vector3 world;
            if(sprites.TryGetValue(unit,out var renderer)&&renderer!=null&&renderer.sprite!=null)
            {
                if(!UnitHeadAnchors.TryGetLocal(renderer.sprite,out var point))
                {
                    // During an authored action use its stable directional body anchor rather than a weapon's changing bounds.
                    var direction=CharacterMotion.ViewFacing(unit.Facing,BattleCamera.transform.rotation);
                    var basis=unit.Data.Sprites?.Get(direction);
                    if(!UnitHeadAnchors.TryGetLocal(basis,out point))
                    {
                        var bounds=(basis!=null?basis:renderer.sprite).bounds;
                        point=new Vector3(bounds.center.x,bounds.max.y,0);
                    }
                }
                // Includes imported pivot, body scale, camera-facing rotation, motion bob, recoil and lift.
                world=renderer.transform.TransformPoint(point);
            }
            else world=battle.Session.Grid[unit.Position].WorldPosition(battle.Catalog.Rules.TileHeight)+BattleCamera.transform.up*1.3f;
            var screen=BattleCamera.WorldToScreenPoint(world);
            screen.y+=5*Mathf.Clamp(Screen.height/1080f,.8f,1.5f);
            return screen;
        }
    }
}
