using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
namespace TalesTactics
{
    [Serializable] public sealed class RewindFrame { public string Label,Json; }
    public sealed partial class BattleSession
    {
        public const int RewindLimit=3,HistoryLimit=12;
        public int RewindsUsed;
        public readonly List<RewindFrame> History=new List<RewindFrame>();
        public int RewindsLeft=>Math.Max(0,RewindLimit-RewindsUsed);
        public void RecordAction(string label)
        {
            if(CampaignStage<0||Result!=BattleResult.Ongoing||Active?.Team!=Team.Player||!Active.Alive||RewindsLeft==0)return;
            var deployment=Units.Where(u=>u.Team==Team.Player).Select(u=>Array.IndexOf(catalog.Characters,u.Data)).ToArray();
            var snapshot=BattleCheckpoint.Capture(this,deployment,false);snapshot.HasOpening=false;snapshot.Opening=null;
            History.Add(new RewindFrame{Label=label,Json=JsonUtility.ToJson(snapshot)});
            if(History.Count>HistoryLimit)History.RemoveAt(0);
        }
        public BattleSession RewindLast(out UnityEngine.Random.State random)
        {
            if(History.Count==0||RewindsLeft==0||Result==BattleResult.Victory)throw new InvalidOperationException("No rewind available");
            var snapshot=ReadFrame(History[History.Count-1]);var restored=snapshot.Restore(catalog);
            restored.Opening=Opening;restored.RewindsUsed=RewindsUsed+1;
            restored.History.AddRange(History.Take(History.Count-1));random=snapshot.RandomState;return restored;
        }
        internal static BattleCheckpoint ReadFrame(RewindFrame frame)
        {
            if(frame==null||string.IsNullOrEmpty(frame.Label)||frame.Label.Length>80||string.IsNullOrEmpty(frame.Json)||frame.Json.Length>1000000)throw new InvalidDataException("Invalid rewind frame");
            var checkpoint=JsonUtility.FromJson<BattleCheckpoint>(frame.Json);
            if(checkpoint==null||checkpoint.Version!=8||checkpoint.HasOpening||checkpoint.History?.Length>0)throw new InvalidDataException("Nested rewind history");
            return checkpoint;
        }
    }
}
