using UnityEngine;
namespace TalesTactics
{
    public sealed partial class PixelBattlefield
    {
        // Only the exposed outside of the grid is dressed. The standing top still ends at exactly
        // half a tile; all fractured shelves are below it and create neither colliders nor routes.
        void DressPerimeter(GridTile tile,GridMap map,Vector3 outward,float step)
        {
            var top=tile.WorldPosition(step);
            var across=Vector3.Cross(outward,Vector3.up);
            var edge=top+outward*.503f;
            int rockSurface=Verdant||stage==2?6:wall;
            int salt=71+(int)outward.x*3+(int)outward.z*7;
            const int pieces=3;
            for(int i=0;i<pieces;i++)
            {
                var a=edge+across*(-.5f+i/(float)pieces);
                var b=edge+across*(-.5f+(i+1)/(float)pieces);
                var upperA=PerimeterLayer(a,outward,1,salt);
                var upperB=PerimeterLayer(b,outward,1,salt);
                var lowerA=PerimeterLayer(a,outward,2,salt);
                var lowerB=PerimeterLayer(b,outward,2,salt);
                var footA=PerimeterLayer(a,outward,3,salt);
                var footB=PerimeterLayer(b,outward,3,salt);
                // The narrow upper face covers the existing tile side, preserving its exact rim.
                geometry[stage>=3?wall:rockSurface].Quad(upperA,a,b,upperB,.94f,new Vector2(.34f,.36f));
                geometry[rockSurface].Quad(lowerA,upperA,upperB,lowerB,.88f,new Vector2(.34f,.7f));
                geometry[rockSurface].Quad(footA,lowerA,lowerB,footB,.80f,new Vector2(.34f,.8f));
            }

            ClosePerimeterSteps(tile,map,outward,step,rockSurface,salt);

            // Two outward faces diverge below a corner. Join those mitered profiles so a rotated
            // camera never sees a triangular hole in the fractured foundation.
            if(outward.x!=0)
            {
                foreach(int direction in new[]{-1,1})
                {
                    if(map[tile.Coordinate+new Vector2Int(0,direction)]!=null)continue;
                    ClosePerimeterCorner(top,outward,Vector3.forward*direction,rockSurface);
                }
            }

            // Capstone fragments describe masonry without the old continuous four-tier rail.
            // Their highest point stays at/below the top, away from every unit's standing center.
            float noise=DetailNoise(tile.Coordinate.x,tile.Coordinate.y,salt);
            int fragments=stage>=3?3:2;
            for(int i=0;i<fragments;i++)
            {
                float fragment=DetailNoise(tile.Coordinate.x*3+i,tile.Coordinate.y,salt+2);
                if(stage<3&&fragment<.30f)continue;
                float length=(stage>=3?.265f:.20f)+fragment*.04f;
                float depth=.10f+fragment*.055f;
                float height=.045f+fragment*.035f;
                float along=fragments==3?(i-1)*.326f:(i-.5f)*.60f;
                var c=top+outward*(.50f-depth*.18f)+across*along-Vector3.up*(height*.5f+.008f);
                geometry[stage==2?5:wall].Box(c,
                    new Vector3(outward.x==0?length:depth,height,outward.z==0?length:depth),.90f+fragment*.07f);
            }

            // Small fallen fragments are rough and well below the play plane. A radial mesh replaces
            // another rectangular ledge; bounded spacing keeps every vertex outside the grid.
            if(noise>.70f)
            {
                float radius=.22f+noise*.12f;
                var foot=edge+outward*(radius*1.1f+.20f)-Vector3.up*(.62f+noise*.28f);
                geometry[rockSurface].Rock(foot,radius,.18f+noise*.12f,
                    tile.Coordinate.x*31+tile.Coordinate.y*17+salt);
            }
        }

        void ClosePerimeterSteps(GridTile tile,GridMap map,Vector3 outward,float step,int surface,int salt)
        {
            var across=Vector3.Cross(outward,Vector3.up);
            var outside=new Vector2Int((int)outward.x,(int)outward.z);
            foreach(int sign in new[]{-1,1})
            {
                var towardNeighbor=across*sign;
                var next=tile.Coordinate+new Vector2Int((int)towardNeighbor.x,(int)towardNeighbor.z);
                var neighbor=map[next];
                if(neighbor==null||map[next+outside]!=null)continue;
                var rim=tile.WorldPosition(step)+outward*.503f+towardNeighbor*.5f;
                if(neighbor.Terrain==TerrainType.Water)
                {
                    // Water never calls DressPerimeter. The dry side owns this single end wall,
                    // including a higher waterfall, without placing rock across the falling sheet.
                    CloseWaterfallEnd(rim,neighbor.WorldPosition(step),outward,towardNeighbor,surface,salt);
                    continue;
                }
                if(neighbor.Height>=tile.Height)continue;
                var lowRim=rim;lowRim.y=neighbor.WorldPosition(step).y;
                var previousHigh=rim;var previousLow=lowRim;
                for(int layer=1;layer<=3;layer++)
                {
                    var high=PerimeterLayer(rim,outward,layer,salt);
                    var low=PerimeterLayer(lowRim,outward,layer,salt);
                    PerimeterSeamQuad(previousHigh,high,low,previousLow,towardNeighbor,surface);
                    previousHigh=high;previousLow=low;
                }
            }
        }

        void CloseWaterfallEnd(Vector3 dryRim,Vector3 waterTop,Vector3 outward,Vector3 towardWater,int surface,int salt)
        {
            // Match Waterfall's actual edge offset, half-width and vertical endpoints. Only its
            // .02-unit end inset is bridged; the front and playable water mouth remain uncovered.
            var waterRim=waterTop+outward*.535f-towardWater*.48f-Vector3.up*.04f;
            var previousDry=dryRim;var previousWater=waterRim;
            var previousEnd=WaterfallSidePoint(dryRim,waterRim,outward);
            for(int layer=1;layer<=3;layer++)
            {
                var dry=PerimeterLayer(dryRim,outward,layer,salt);
                var end=WaterfallSidePoint(dry,waterRim,outward);
                var water=waterRim;
                water.y=layer==3?bottom-.04f:Mathf.Clamp(dry.y,bottom-.04f,waterRim.y);
                // The first rock layer can lie behind the waterfall's front plane. Close that
                // tiny wedge separately before bridging to water; an unsplit quad would twist.
                PerimeterSeamQuad(previousDry,dry,end,previousEnd,towardWater,surface);
                PerimeterSeamQuad(previousEnd,end,water,previousWater,towardWater+outward*.1f,surface);
                previousDry=dry;previousEnd=end;previousWater=water;
            }
        }

        static Vector3 WaterfallSidePoint(Vector3 point,Vector3 waterRim,Vector3 outward)
        {
            return point+outward*Mathf.Max(0,Vector3.Dot(waterRim-point,outward));
        }

        void PerimeterSeamQuad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 facing,int surface)
        {
            // A common bottom point naturally collapses the last quad to one triangle. Determine
            // the winding from both triangles, so all four outside directions face the lower tile.
            var normal=Vector3.Cross(b-a,c-a)+Vector3.Cross(c-a,d-a);
            if(normal.sqrMagnitude<.000000001f)return;
            if(Vector3.Dot(normal,facing)<0)geometry[surface].Quad(d,c,b,a,.86f,new Vector2(.6f,.8f));
            else geometry[surface].Quad(a,b,c,d,.86f,new Vector2(.6f,.8f));
        }

        void ClosePerimeterCorner(Vector3 top,Vector3 xOut,Vector3 zOut,int surface)
        {
            var a=top+xOut*.503f+zOut*.5f;
            var b=top+xOut*.5f+zOut*.503f;
            int saltA=71+(int)xOut.x*3,saltB=71+(int)zOut.z*7;
            bool reverse=Vector3.Dot(Vector3.Cross(Vector3.up,b-a),xOut+zOut)<0;
            var lastA=a;var lastB=b;
            for(int layer=1;layer<=3;layer++)
            {
                var nextA=PerimeterLayer(a,xOut,layer,saltA);
                var nextB=PerimeterLayer(b,zOut,layer,saltB);
                if(reverse)geometry[surface].Quad(nextB,lastB,lastA,nextA,.88f,new Vector2(.4f,.6f));
                else geometry[surface].Quad(nextA,lastA,lastB,nextB,.88f,new Vector2(.4f,.6f));
                lastA=nextA;lastB=nextB;
            }
        }

        Vector3 PerimeterLayer(Vector3 rim,Vector3 outward,int layer,int salt)
        {
            float noise=PerimeterNoise(rim,salt);
            float broad=PerimeterNoise(rim,salt+4);
            float rise=Mathf.Max(.8f,rim.y-bottom);
            if(layer==1)return rim+outward*(.025f+noise*.12f)-Vector3.up*(.24f+broad*.13f);
            if(layer==2)return rim+outward*(.15f+noise*.29f)-Vector3.up*(rise*.64f+broad*.10f);
            return new Vector3(rim.x,bottom-.12f-broad*.25f,rim.z)+outward*(.12f+noise*.40f);
        }

        static float PerimeterNoise(Vector3 p,int salt)
        {
            // Coordinate-derived values join adjacent edge segments and never touch gameplay RNG.
            return DetailNoise(Mathf.RoundToInt(p.x*6),Mathf.RoundToInt(p.z*6),salt);
        }

        Vector3 ExteriorLandform(int side,float t,float detail)
        {
            // Low-frequency shape makes one or two substantial landforms per side. Independent
            // phases and shallow corners avoid merely adding fine noise to a rectangular platform.
            float majorCenter=.23f+DetailNoise(side,stage,151)*.46f;
            float majorWidth=.15f+DetailNoise(side,stage,157)*.07f;
            float minorCenter=Mathf.Repeat(majorCenter+.48f,1);
            float gullyCenter=Mathf.Repeat(majorCenter+.28f,1);
            float major=1-Mathf.SmoothStep(0,1,Mathf.Abs(t-majorCenter)/majorWidth);
            float minor=1-Mathf.SmoothStep(0,1,Mathf.Abs(t-minorCenter)/.17f);
            float gully=1-Mathf.SmoothStep(0,1,Mathf.Abs(t-gullyCenter)/.13f);
            float corner=Mathf.SmoothStep(0,1,Mathf.Min(t,1-t)/.12f);
            float reach=Mathf.Lerp(.56f,.74f+major*(1.70f+DetailNoise(side,stage,163)*.60f)+minor*.96f,corner);
            reach=Mathf.Max(.48f,reach-gully*.22f+detail*.15f);
            float drop=.58f+detail*.16f+gully*.30f+(1-corner)*.15f;
            return new Vector3(reach,drop,gully);
        }

        void DressExteriorCliff(Vector3 a,Vector3 b)
        {
            int materialIndex=Verdant||stage==2?6:wall;
            var upperA=ExteriorCliffLayer(a,1);var upperB=ExteriorCliffLayer(b,1);
            var shelfA=ExteriorCliffLayer(a,2);var shelfB=ExteriorCliffLayer(b,2);
            var footA=ExteriorCliffLayer(a,3);var footB=ExteriorCliffLayer(b,3);
            geometry[materialIndex].Quad(upperA,a,b,upperB,.88f,new Vector2(1,.7f));
            geometry[materialIndex].Quad(shelfA,upperA,upperB,shelfB,.83f,new Vector2(1,1));
            geometry[materialIndex].Quad(footA,shelfA,shelfB,footB,.77f,new Vector2(1,1.5f));
        }

        Vector3 ExteriorCliffLayer(Vector3 rim,int layer)
        {
            var away=new Vector3(rim.x-playableCenter.x,0,rim.z-playableCenter.z).normalized;
            float noise=PerimeterNoise(rim,131+stage*3);
            float broad=PerimeterNoise(rim,137+stage*3);
            if(layer==1)return rim+away*(.10f+noise*.32f)-Vector3.up*(.42f+broad*.40f);
            if(layer==2)return rim+away*(.26f+noise*.42f)-Vector3.up*(1.0f+broad*.55f);
            return rim+away*(.04f+noise*.36f)-Vector3.up*(2.5f+broad*.8f);
        }
    }
}
