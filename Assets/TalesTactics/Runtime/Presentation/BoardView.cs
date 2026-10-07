using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace TalesTactics
{
    public sealed class TileView:MonoBehaviour { public Vector2Int Coordinate; }
    public sealed partial class BoardView:MonoBehaviour
    {
        public Camera BattleCamera;
        public Material TileMaterial, HighlightMaterial, SpriteMaterial;
        readonly Dictionary<Vector2Int,Renderer> tiles=new Dictionary<Vector2Int,Renderer>();
        readonly Dictionary<Vector2Int,Color> colors=new Dictionary<Vector2Int,Color>();
        readonly Dictionary<Vector2Int,LineRenderer> rangeBorders=new Dictionary<Vector2Int,LineRenderer>();
        readonly Dictionary<UnitRuntime,Transform> units=new Dictionary<UnitRuntime,Transform>();
        readonly Dictionary<UnitRuntime,SpriteRenderer> sprites=new Dictionary<UnitRuntime,SpriteRenderer>();
        readonly Dictionary<UnitRuntime,Animator> animators=new Dictionary<UnitRuntime,Animator>();
        readonly Dictionary<UnitRuntime,CharacterMotion> motions=new Dictionary<UnitRuntime,CharacterMotion>();
        readonly Dictionary<UnitRuntime,Transform> bars=new Dictionary<UnitRuntime,Transform>();
        BattleDirector battle;Transform root;LineRenderer line;Sprite placeholder;
        Rect lastViewport;
        Bounds battlefieldBounds;
        Quaternion initialRotation;
        Vector3 initialPosition;
        float zoom=1;
        public void RotateCamera(float degrees)
        {
            if(root==null||battle.TimingActive)return;
            BattleCamera.transform.RotateAround(battlefieldBounds.center,Vector3.up,degrees);
            FitBattlefield(true);
            foreach(var sprite in sprites.Values)sprite.transform.rotation=BattleCamera.transform.rotation;
        }
        public void ZoomCamera(float factor)
        {if(root==null||battle.Session==null)return;zoom=Mathf.Clamp(zoom*factor,0.55f,1.3f);FitBattlefield(true);}
        public void ResetCamera()
        {
            focusAction=false;zoom=1;BattleCamera.transform.SetPositionAndRotation(initialPosition,initialRotation);
            if(root==null||battle.Session==null)return;
            FitBattlefield(true);
            foreach(var sprite in sprites.Values)sprite.transform.rotation=BattleCamera.transform.rotation;
        }
        void LateUpdate()
        {
            if(root==null||battle.Session==null)return;
            FitBattlefield();
            UpdateSelectionMarkers();
            foreach(var pair in bars)
            {
                pair.Value.position=units[pair.Key].position+BattleCamera.transform.up*1.43f;
                pair.Value.rotation=BattleCamera.transform.rotation;
            }
        }
        void FitBattlefield(bool force=false)
        {
            var viewport=battle.Hud.BattlefieldViewport;
            if(!force&&viewport==lastViewport)return;
            lastViewport=viewport;BattleCamera.rect=new Rect(0,0,1,1);
            var min=new Vector2(float.PositiveInfinity,float.PositiveInfinity);
            var max=new Vector2(float.NegativeInfinity,float.NegativeInfinity);
            foreach(var tile in battle.Session.Grid.Tiles.Values)
            {
                var p=tile.WorldPosition(battle.Catalog.Rules.TileHeight);
                for(int i=0;i<8;i++)
                {
                    var local=BattleCamera.transform.InverseTransformPoint(p+new Vector3((i&1)==0?-.5f:.5f,(i&2)==0?-.3f:1.5f,(i&4)==0?-.5f:.5f));
                    min=Vector2.Min(min,new Vector2(local.x,local.y));max=Vector2.Max(max,new Vector2(local.x,local.y));
                }
            }
            if(focusAction&&battle.Session.Active!=null)FocusBounds(ref min,ref max);
            var center=(min+max)*0.5f;var size=(max-min)*0.5f;
            BattleCamera.transform.position+=BattleCamera.transform.right*center.x+BattleCamera.transform.up*center.y;
            BattleCamera.orthographicSize=Mathf.Max(size.y/viewport.height,size.x/(BattleCamera.aspect*viewport.width))*1.04f*zoom;
            // Render the scenery edge-to-edge while fitting playable tiles inside the HUD's safe area.
            var offset=viewport.center-new Vector2(.5f,.5f);
            BattleCamera.transform.position-=BattleCamera.transform.right*(offset.x*2*BattleCamera.orthographicSize*BattleCamera.aspect)
                +BattleCamera.transform.up*(offset.y*2*BattleCamera.orthographicSize);
        }
        public void Initialize(BattleDirector b)
        {
            battle=b;initialRotation=BattleCamera.transform.rotation;initialPosition=BattleCamera.transform.position;
            BattleCamera.allowMSAA=false;BattleCamera.allowHDR=false;BattleCamera.allowDynamicResolution=false;
            // Clear the screen during setup and transitions before the battlefield is available.
            var background=new GameObject("Screen Background Camera",typeof(Camera));
            background.transform.SetParent(transform,false);
            var camera=background.GetComponent<Camera>();camera.cullingMask=0;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=BattleCamera.backgroundColor;
            camera.depth=BattleCamera.depth-1;camera.rect=new Rect(0,0,1,1);
        }
        public void ResetBoard(){StopAllCoroutines();ClearTiming();if(root!=null)Destroy(root.gameObject);root=null;activeMarker=null;targetMarker=null;destinationMarker=null;line=null;tiles.Clear();colors.Clear();rangeBorders.Clear();units.Clear();sprites.Clear();animators.Clear();motions.Clear();bars.Clear();}
        public void Build(BattleSession session)
        {
            ResetBoard();root=new GameObject("Runtime Battlefield").transform;root.SetParent(transform,false);
            var scenery=root.gameObject.AddComponent<PixelBattlefield>();scenery.Initialize(session.CampaignStage,TileMaterial,SpriteMaterial,BattleCamera);
            foreach(var t in session.Grid.Tiles.Values)
            {
                var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name="Tile "+t.Coordinate;g.transform.SetParent(root,false);
                float height=0.25f+t.Height*session.Rules.TileHeight;g.transform.position=new Vector3(t.Coordinate.x,height/2-0.25f,t.Coordinate.y);g.transform.localScale=new Vector3(0.96f,height,0.96f);
                g.AddComponent<TileView>().Coordinate=t.Coordinate;var renderer=g.GetComponent<Renderer>();renderer.sharedMaterial=scenery.Surface(t);
                Color color=scenery.Tint(t);
                if((session.Objective==ObjectiveKind.Reach||session.Objective==ObjectiveKind.Escort)&&t.Coordinate==session.Destination)color=new Color(1,0.75f,0.12f);
                tiles[t.Coordinate]=renderer;colors[t.Coordinate]=color;SetColor(renderer,color);
                scenery.Decorate(t,session.Grid,session.Rules.TileHeight);
            }
            if(placeholder==null)placeholder=CreatePlaceholder();
            foreach(var u in session.Units)
            {
                var g=new GameObject(u.Data.DisplayName);g.transform.SetParent(root,false);units[u]=g.transform;
                var visual=new GameObject("Directional Sprite");visual.transform.SetParent(g.transform,false);visual.transform.localPosition=Vector3.up*0.04f;
                var sr=visual.AddComponent<SpriteRenderer>();sr.sharedMaterial=SpriteMaterial;sprites[u]=sr;
                if(u.Data.Animator!=null){var a=visual.AddComponent<Animator>();a.runtimeAnimatorController=u.Data.Animator;animators[u]=a;}
                else{var motion=visual.AddComponent<CharacterMotion>();motion.Initialize(sr,u,BattleCamera);motions[u]=motion;}
                var bar=GameObject.CreatePrimitive(PrimitiveType.Cube);Destroy(bar.GetComponent<Collider>());bar.name="HP";bar.transform.SetParent(g.transform,false);bar.transform.localPosition=Vector3.up*1.3f;bar.GetComponent<Renderer>().sharedMaterial=TileMaterial;SetColor(bar.GetComponent<Renderer>(),u==session.ObjectiveUnit?new Color(1,0.75f,0.12f):u.Team==Team.Player?Color.cyan:new Color(1,0.3f,0.25f));bars[u]=bar.transform;
                var marker=GameObject.CreatePrimitive(PrimitiveType.Cube);Destroy(marker.GetComponent<Collider>());marker.name="Facing";marker.transform.SetParent(g.transform,false);marker.transform.localScale=new Vector3(0.14f,0.04f,0.25f);marker.GetComponent<Renderer>().sharedMaterial=TileMaterial;SetColor(marker.GetComponent<Renderer>(),Color.white);
            }
            var path=new GameObject("Movement Path");path.transform.SetParent(root,false);line=path.AddComponent<LineRenderer>();line.sharedMaterial=HighlightMaterial;line.startWidth=line.endWidth=0.07f;line.startColor=line.endColor=Color.cyan;SetColor(line,Color.cyan);
            battlefieldBounds=new Bounds();bool first=true;
            foreach(var tile in session.Grid.Tiles.Values)
            {
                var p=tile.WorldPosition(session.Rules.TileHeight);
                if(first){battlefieldBounds=new Bounds(p,Vector3.zero);first=false;}
                battlefieldBounds.Encapsulate(p+new Vector3(-0.5f,-0.3f,-0.5f));
                battlefieldBounds.Encapsulate(p+new Vector3(0.5f,1.5f,0.5f));
            }
            ResetCamera();Sync();
        }
        public bool Pick(Vector2 screen,out Vector2Int p)
        {
            p=default;if(BattleCamera==null||!BattleCamera.pixelRect.Contains(screen))return false;
            if(Physics.Raycast(BattleCamera.ScreenPointToRay(screen),out var hit,200)){var t=hit.collider.GetComponent<TileView>();if(t!=null){p=t.Coordinate;return true;}}return false;
        }
        public void Sync()
        {
            if(battle.Session==null)return;
            foreach(var u in battle.Session.Units)
            {
                var tr=units[u];tr.position=battle.Session.Grid[u.Position].WorldPosition(battle.Catalog.Rules.TileHeight);
                var sr=sprites[u];var art=u.Data.Sprites.Get(u.Facing);sr.sprite=art!=null?art:placeholder;sr.color=u.Alive?(art!=null?Color.white:u.Data.PlaceholderColor):new Color(0.3f,0.3f,0.3f,0.35f);
                sr.transform.rotation=BattleCamera.transform.rotation;sr.transform.localScale=u.Alive?Vector3.one:new Vector3(1,0.3f,1);
                bars[u].localScale=new Vector3(0.7f*Mathf.Max(0.01f,(float)u.CurrentHP/u.Stats.HP),0.055f,0.06f);bars[u].gameObject.SetActive(u.Alive);
                var f=SkillResolver.FacingVector(u.Facing);var marker=tr.Find("Facing");marker.localPosition=new Vector3(f.x*0.38f,0.04f,f.y*0.38f);marker.localRotation=Quaternion.Euler(0,f.x!=0?90:0,0);marker.gameObject.SetActive(u.Alive);
                SetAnimation(u,u.Alive?u.Has(StatusKind.Guard)?AnimationKind.Guard:AnimationKind.Idle:AnimationKind.Dead);
            }
        }
        public void SetAnimation(UnitRuntime u,AnimationKind kind)
        {
            if(motions.TryGetValue(u,out var motion))motion.Set(kind);
            if(!animators.TryGetValue(u,out var animator))return;
            foreach(var parameter in animator.parameters)
            {
                if(parameter.name=="Facing"&&parameter.type==AnimatorControllerParameterType.Int)animator.SetInteger("Facing",(int)u.Facing);
                if(parameter.name=="Action"&&parameter.type==AnimatorControllerParameterType.Int)animator.SetInteger("Action",(int)kind);
            }
        }
        public void ShowTimingSpin(UnitRuntime u,float progress)
        {
            bool starting=timingRing==null;ShowTimingRing(u,progress);if(starting)SetAnimation(u,AnimationKind.Skill);
            if(motions.TryGetValue(u,out var motion)){timingMotion=motion;motion.Spin(progress);}
        }
        public IEnumerator AnimateMove(UnitRuntime u,List<Vector2Int> path,float speed=1)
        {
            SetAnimation(u,AnimationKind.Walk);
            for(int i=1;i<path.Count;i++)
            {
                if(motions.TryGetValue(u,out var motion))motion.WalkFacing(SkillResolver.Toward(path[i-1],path[i]));
                Vector3 from=battle.Session.Grid[path[i-1]].WorldPosition(battle.Catalog.Rules.TileHeight),to=battle.Session.Grid[path[i]].WorldPosition(battle.Catalog.Rules.TileHeight);
                for(float t=0;t<1;t+=Time.deltaTime*Mathf.Max(1,speed)/battle.Catalog.Rules.StepSeconds){units[u].position=Vector3.Lerp(from,to,t);yield return null;}
                units[u].position=to;
            }
        }
        public void ClearHighlights(){foreach(var p in tiles)SetColor(p.Value,colors[p.Key]);foreach(var border in rangeBorders.Values)border.enabled=false;if(line!=null)line.positionCount=0;if(destinationMarker!=null)destinationMarker.enabled=false;}
        public void ShowRange(IEnumerable<Vector2Int> points,Color color){ClearHighlights();foreach(var p in points)if(tiles.TryGetValue(p,out var r)){SetColor(r,Color.Lerp(colors[p],color,0.65f));ShowBorder(p,Color.Lerp(color,Color.white,0.3f));}}
        public void ShowSkillRange(UnitRuntime u,SkillData skill){var points=new List<Vector2Int>();foreach(var p in tiles.Keys)if(battle.Session.Resolver.InRange(u,skill,p))points.Add(p);ShowRange(points,new Color(0.8f,0.25f,0.2f));}
        public void ShowArea(Vector2Int center,int radius){ShowSkillRange(battle.Session.Active,battle.SelectedSkill);foreach(var p in battle.Session.Resolver.AreaTiles(battle.Session.Active,battle.SelectedSkill,center)){SetColor(tiles[p],new Color(1,0.7f,0.25f));ShowBorder(p,new Color(1,0.85f,0.25f),0.065f);}}
        void ShowBorder(Vector2Int p,Color color,float width=0.035f)
        {
            if(!rangeBorders.TryGetValue(p,out var border))
            {
                var g=new GameObject("Range border "+p);g.transform.SetParent(tiles[p].transform,false);
                border=g.AddComponent<LineRenderer>();rangeBorders[p]=border;
                border.sharedMaterial=HighlightMaterial;border.useWorldSpace=true;border.loop=true;
                border.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                border.receiveShadows=false;border.positionCount=4;
                var b=tiles[p].bounds;float y=b.max.y+0.025f;
                border.SetPositions(new[]{new Vector3(b.min.x+0.045f,y,b.min.z+0.045f),new Vector3(b.max.x-0.045f,y,b.min.z+0.045f),new Vector3(b.max.x-0.045f,y,b.max.z-0.045f),new Vector3(b.min.x+0.045f,y,b.max.z-0.045f)});
            }
            border.startWidth=border.endWidth=width;border.startColor=border.endColor=color;
            SetColor(border,color);border.enabled=true;
        }
        public void ShowPath(List<Vector2Int> path){if(battle.TutorialActive&&path.Count>0&&path[path.Count-1]!=BattleDirector.TutorialDestination)path=new List<Vector2Int>();Marker(ref destinationMarker,"Movement destination",path.Count>1?path[path.Count-1]:(Vector2Int?)null,Color.cyan,.42f);line.positionCount=path.Count;for(int i=0;i<path.Count;i++)line.SetPosition(i,battle.Session.Grid[path[i]].WorldPosition(battle.Catalog.Rules.TileHeight)+Vector3.up*0.1f);}
        static void SetColor(Renderer r,Color c){var properties=new MaterialPropertyBlock();properties.SetColor("_BaseColor",c);properties.SetColor("_Color",c);r.SetPropertyBlock(properties);}
        static Sprite CreatePlaceholder()
        {
            var tex=new Texture2D(20,32,TextureFormat.RGBA32,false);tex.filterMode=FilterMode.Point;var pixels=new Color[640];
            for(int y=0;y<32;y++)for(int x=0;x<20;x++){bool head=(x-10)*(x-10)+(y-25)*(y-25)<25;bool body=y>=7&&y<=20&&x>=5&&x<=14;bool legs=y<8&&y>1&&((x>=5&&x<9)||(x>10&&x<=14));pixels[y*20+x]=head||body||legs?Color.white:Color.clear;}
            tex.SetPixels(pixels);tex.Apply();return Sprite.Create(tex,new Rect(0,0,20,32),new Vector2(0.5f,0),26);
        }
    }
}
