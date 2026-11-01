using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
namespace TalesTactics.Tests
{
    public class UnitHealthLayoutTests
    {
        [Test] public void ABlockedHeadGaugeNeverMigratesToAnotherUnitOrScreenEdge()
        {
            var safe=new Rect(0,0,1280,720);var origin=new Vector2(600,400);
            var covered=new List<Rect>{new Rect(500,340,200,130)};
            Assert.That(UnitHealthLayout.TryPlaceNearHead(origin,new Vector2(74,16),safe,covered,1,out _),Is.False,
                "A nearby large menu has no safe local slot; the gauge must defer to the fixed status panel.");
            Assert.That(UnitHealthLayout.TryPlaceNearHead(new Vector2(-100,400),new Vector2(74,16),safe,new List<Rect>(),1,out _),Is.False,
                "Offscreen units must not acquire a health bar at the viewport edge.");
        }
        [TestCase(.8f)][TestCase(1f)][TestCase(1.5f)]
        public void SixCrowdedHeadGaugesRemainLocalWithoutOverlapping(float scale)
        {
            var safe=new Rect(0,0,1280,720);var occupied=new List<Rect>();int visible=0;
            for(int i=0;i<6;i++)
            {
                var origin=new Vector2(610+i%3*12,400+i/3*12);
                if(!UnitHealthLayout.TryPlaceNearHead(origin,new Vector2(74,16)*scale,safe,occupied,scale,out var bounds))continue;
                var shift=new Vector2(bounds.center.x,bounds.yMin)-origin;
                Assert.That(Mathf.Abs(shift.x),Is.LessThanOrEqualTo(UnitHealthLayout.MaximumHorizontalShift*scale+.01f));
                Assert.That(Mathf.Abs(shift.y),Is.LessThanOrEqualTo(UnitHealthLayout.MaximumVerticalShift*scale+.01f));
                foreach(var other in occupied)Assert.That(bounds.Overlaps(other),Is.False);
                Assert.That(safe.Contains(bounds.min)&&safe.Contains(bounds.max),Is.True);
                occupied.Add(bounds);visible++;
            }
            Assert.That(visible,Is.GreaterThan(0),"The highest-priority gauge keeps a nearby readable position.");
            Assert.That(visible,Is.LessThan(6),"Artificially coincident units do not produce six detached bars.");
        }
        [Test] public void SmallReservationUsesTheClosestFreeHeadSlot()
        {
            var origin=new Vector2(400,300);var safe=new Rect(0,0,1280,720);
            var reserved=new List<Rect>{new Rect(363,300,74,5)};
            Assert.That(UnitHealthLayout.TryPlaceNearHead(origin,new Vector2(74,16),safe,reserved,1,out var bounds),Is.True);
            Assert.That(bounds.Overlaps(reserved[0]),Is.False);
            Assert.That(Vector2.Distance(origin,new Vector2(bounds.center.x,bounds.yMin)),Is.LessThanOrEqualTo(24));
        }
    }
}
