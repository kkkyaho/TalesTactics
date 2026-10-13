using System;
using System.Collections.Generic;
using UnityEngine;

namespace TalesTactics
{
    public enum SkillAreaShape { Diamond, Line, Cone, Cross, Diagonal, Lanes, Scatter }

    public static class SkillGeometry
    {
        public static int EffectiveRange(GridMap grid, Vector2Int from, Vector2Int to, SkillData skill)
        {
            int limit=Math.Max(0,skill.HeightRangeLimit);
            int bonus=grid[from]==null||grid[to]==null?0:Mathf.Clamp(grid[from].Height-grid[to].Height,-limit,limit);
            return Math.Max(skill.MinRange,skill.Range+bonus);
        }
        public static bool CanAim(GridMap grid, Vector2Int from, SkillData skill, Vector2Int aim,SkillAreaShape? shapeOverride=null)
        {
            if (skill == null || grid[from] == null || grid[aim] == null) return false;
            int distance = GridMap.Distance(from, aim);
            if (distance < skill.MinRange || distance > EffectiveRange(grid,from,aim,skill)) return false;
            var delta = aim - from;
            var shape=shapeOverride??skill.Shape;
            if ((shape == SkillAreaShape.Line || shape == SkillAreaShape.Cone) && (distance == 0 || delta.x != 0 && delta.y != 0)) return false;
            return VisibleHeight(grid, from, aim, skill);
        }

        static bool VisibleHeight(GridMap grid, Vector2Int from, Vector2Int to, SkillData skill)
        {
            if (skill.MaxHeightDifference >= 0 && Math.Abs(grid[from].Height - grid[to].Height) > skill.MaxHeightDifference) return false;
            return !skill.RequiresLineOfSight || HasLineOfSight(grid, from, to);
        }

        public static IEnumerable<Vector2Int> Area(GridMap grid, Vector2Int from, SkillData skill, Vector2Int aim,SkillAreaShape? shapeOverride=null,int? areaOverride=null)
        {
            if (!CanAim(grid, from, skill, aim,shapeOverride)) yield break;
            var shape=shapeOverride??skill.Shape;int radius=areaOverride??skill.Area;
            var delta = aim - from;
            int dx = Math.Sign(delta.x), dz = Math.Sign(delta.y);
            foreach (var p in grid.Tiles.Keys)
            {
                bool inside;
                if (shape == SkillAreaShape.Diamond) inside = GridMap.Distance(p, aim) <= radius;
                else if (shape == SkillAreaShape.Cross) inside = (p.x==aim.x||p.y==aim.y)&&GridMap.Distance(p,aim)<=radius;
                else if (shape == SkillAreaShape.Diagonal) inside = Math.Abs(p.x-aim.x)==Math.Abs(p.y-aim.y)&&Math.Abs(p.x-aim.x)<=radius;
                else if (shape == SkillAreaShape.Lanes) inside = Math.Abs(p.x-aim.x)%2==0&&GridMap.Distance(p,aim)<=radius;
                else if (shape == SkillAreaShape.Scatter) inside = GridMap.Distance(p,aim)<=radius&&(p==aim||(p.x+2*p.y)%3==0);
                else
                {
                    var relative = p - from;
                    int forward = relative.x * dx + relative.y * dz;
                    int side = Math.Abs(relative.x * dz - relative.y * dx);
                    inside = forward > 0 && forward + side >= skill.MinRange && forward + side <= EffectiveRange(grid,from,p,skill) &&
                        (shape == SkillAreaShape.Line ? side == 0 : side < forward);
                }
                if (inside && VisibleHeight(grid, from, p, skill)) yield return p;
            }
        }

        // Supercover grid ray: both side cells at a corner must be clear.
        // Units do not block shots; impassable tiles and terrain above the eye ray do.
        public static bool HasLineOfSight(GridMap grid, Vector2Int from, Vector2Int to)
        {
            if (grid[from] == null || grid[to] == null || !grid[to].Walkable) return false;
            if (from == to) return true;
            int nx = Math.Abs(to.x - from.x), nz = Math.Abs(to.y - from.y);
            int sx = Math.Sign(to.x - from.x), sz = Math.Sign(to.y - from.y);
            int x = from.x, z = from.y, ix = 0, iz = 0;
            bool Blocked(int bx, int bz)
            {
                var p = new Vector2Int(bx, bz);
                if (p == from || p == to) return false;
                var tile = grid[p]; if (tile == null || !tile.Walkable) return true;
                double t = ((bx - from.x) * (to.x - from.x) + (bz - from.y) * (to.y - from.y)) / (double)(nx * nx + nz * nz);
                double eye = grid[from].Height + 1 + (grid[to].Height - grid[from].Height) * t;
                return tile.Height >= eye;
            }
            while (ix < nx || iz < nz)
            {
                long a = (1L + 2L * ix) * nz, b = (1L + 2L * iz) * nx;
                if (a == b)
                {
                    if (Blocked(x + sx, z) || Blocked(x, z + sz)) return false;
                    x += sx; z += sz; ix++; iz++;
                }
                else if (a < b) { x += sx; ix++; }
                else { z += sz; iz++; }
                if (Blocked(x, z)) return false;
            }
            return true;
        }
    }
}
