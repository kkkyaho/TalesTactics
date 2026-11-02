using System;
using UnityEngine;
namespace TalesTactics
{
    // Presentation-only samples. No gameplay RNG, authored clip or equipment asset is changed.
    public readonly struct CharacterIdleSample
    {
        public readonly Vector3 Motion;
        public readonly float EyeTension,Focus;
        public readonly Vector2 Gaze;
        public CharacterIdleSample(Vector3 motion,float tension,Vector2 gaze,float focus)
        {Motion=motion;EyeTension=Mathf.Clamp01(tension);Gaze=gaze;Focus=Mathf.Clamp01(focus);}
    }
    public sealed class CharacterIdlePresentation
    {
        static readonly int MotionId=Shader.PropertyToID("_IdleMotion"),FaceId=Shader.PropertyToID("_IdleFace"),
            EyeAId=Shader.PropertyToID("_IdleEyeA"),EyeBId=Shader.PropertyToID("_IdleEyeB"),
            UvXId=Shader.PropertyToID("_IdleUvX"),UvYId=Shader.PropertyToID("_IdleUvY"),ShapeId=Shader.PropertyToID("_IdleShape"),BoundsId=Shader.PropertyToID("_IdleBounds");
        MaterialPropertyBlock block;
        Sprite cachedSprite;Vector4 eyeA,eyeB;Vector2 uvX,uvY;Vector4 shape,bounds;
        bool applied;
        public static CharacterIdleSample Sample(string id,float time)
        {
            uint seed=2166136261;foreach(char c in id??string.Empty)seed=unchecked((seed^c)*16777619);
            float phase=(seed%4096)/4096f*13.7f,t=time+phase;
            bool lively=id=="farah",calm=id=="mint"||id=="tear",focused=id=="velvet"||id=="alphen"||id=="shionne";
            float strength=lively?1.25f:calm?.65f:id=="kisara"?.55f:1;
            float period=lively?3.7f:calm?5.7f:4.6f;
            float b=Mathf.Repeat(t,period),tension=EyeTension(b);
            if(lively)tension=Mathf.Max(tension,EyeTension(b-.29f));
            var motion=new Vector3((Mathf.Sin(t*1.6f)*.009f+Mathf.Sin(t*.57f)*.003f)*strength,
                Mathf.Sin(t*2.1f)*.007f*strength,Mathf.Sin(t*1.6f+1.1f)*.006f*strength);
            // Hold a small glance, then return slowly; serious heroes narrow their eyes slightly.
            float glance=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.15f,.75f,Mathf.Sin(t*.63f)));
            var gaze=new Vector2(Mathf.Sin(t*.29f)*glance*(lively?.18f:.10f),Mathf.Sin(t*.41f)*glance*.035f);
            float focus=(focused?.55f:id=="jade"?.30f:lively?.18f:.10f)*Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.2f,.8f,Mathf.Sin(t*.46f)));
            return new CharacterIdleSample(motion,tension,gaze,focus);
        }
        static float EyeTension(float t)
        {
            if(t<0||t>.20f)return 0;
            return t<.065f?Mathf.SmoothStep(0,1,t/.065f):t<.10f?1:1-Mathf.SmoothStep(0,1,(t-.10f)/.10f);
        }
        public void Apply(SpriteRenderer renderer,CharacterData character,Facing view,CharacterIdleSample sample,bool active=true)
        {
            if(renderer==null)return;
            // UnitTone currently uses authored directions, not UnitySprites' _Flip vertex
            // path. Keep unusual flipped renderers unchanged until that path is supported.
            if(!active||!CharacterVisualSizing.IsHero(character)||renderer.sprite==null||renderer.flipX||renderer.flipY){Clear(renderer);return;}
            // CharacterMotion owns this helper as a managed field; Unity objects must be
            // created here on the main thread, never in a MonoBehaviour field constructor.
            if(block==null)block=new MaterialPropertyBlock();
            if(cachedSprite!=renderer.sprite)Prepare(renderer.sprite);
            renderer.GetPropertyBlock(block);
            block.SetVector(MotionId,new Vector4(sample.Motion.x,sample.Motion.y,sample.Motion.z,1));
            block.SetVector(FaceId,view==Facing.Back?Vector4.zero:new Vector4(sample.EyeTension,sample.Gaze.x,sample.Gaze.y,sample.Focus));
            block.SetVector(EyeAId,view==Facing.Back?Vector4.zero:eyeA);block.SetVector(EyeBId,view==Facing.Back?Vector4.zero:eyeB);
            block.SetVector(UvXId,new Vector4(uvX.x,uvX.y,0,0));block.SetVector(UvYId,new Vector4(uvY.x,uvY.y,0,0));
            block.SetVector(ShapeId,shape);
            block.SetVector(BoundsId,bounds);
            renderer.SetPropertyBlock(block);applied=true;
        }
        public void Clear(SpriteRenderer renderer)
        {
            if(!applied||renderer==null)return;
            renderer.GetPropertyBlock(block);block.SetVector(MotionId,Vector4.zero);block.SetVector(FaceId,Vector4.zero);
            renderer.SetPropertyBlock(block);applied=false;
        }
        void Prepare(Sprite sprite)
        {
            cachedSprite=sprite;eyeA=eyeB=Vector4.zero;uvX=uvY=Vector2.zero;
            var rect=sprite.rect;var pivot=sprite.pivot;float ppu=sprite.pixelsPerUnit;
            bounds=new Vector4(-pivot.x/ppu,-pivot.y/ppu,(rect.width-pivot.x)/ppu,(rect.height-pivot.y)/ppu);
            float top=UnitHeadAnchors.TryGetLocal(sprite,out var head)?head.y:sprite.bounds.max.y;
            shape=new Vector4(Mathf.Max(.1f,top),head.x,0,0);
            foreach(var face in Faces)
                if(face.Name==sprite.name&&Mathf.Abs(rect.width-face.Width)<.5f&&Mathf.Abs(rect.height-face.Height)<.5f)
                {
                    eyeA=LocalEye(face.A,rect,pivot,ppu);eyeB=LocalEye(face.B,rect,pivot,ppu);break;
                }
            // Derive the affine basis from actual mesh UVs, including rotated atlas packing.
            var vertices=sprite.vertices;var uv=sprite.uv;
            for(int i=1;i<vertices.Length;i++)for(int j=i+1;j<vertices.Length;j++)
            {
                var a=vertices[i]-vertices[0];var b=vertices[j]-vertices[0];float det=a.x*b.y-a.y*b.x;
                if(Mathf.Abs(det)<.000001f)continue;
                var u=uv[i]-uv[0];var v=uv[j]-uv[0];uvX=(u*b.y-v*a.y)/det;uvY=(v*a.x-u*b.x)/det;return;
            }
        }
        static Vector4 LocalEye(Vector4 normalized,Rect rect,Vector2 pivot,float ppu)
        {
            if(normalized.z<=0)return Vector4.zero;
            return new Vector4((normalized.x*rect.width-pivot.x)/ppu,(normalized.y*rect.height-pivot.y)/ppu,
                normalized.z*rect.width/ppu,normalized.w*rect.height/ppu);
        }
        readonly struct Face
        {
            public readonly string Name;public readonly int Width,Height;public readonly Vector4 A,B;
            public Face(string name,int width,int height,Vector4 a,Vector4 b)
            {Name=name;Width=width;Height=height;A=a;B=b;}
        }
        // Measured on the original and signature idle sheets separately. Back views and
        // unknown replacement art intentionally have no face mask. Units are normalized rect UV.
        static readonly Face[] Faces={
            new Face("cless-hq_0_0",306,299,new Vector4(0.5000000f,0.6789298f,0.0294118f,0.0267559f),new Vector4(0.6013072f,0.6789298f,0.0294118f,0.0267559f)),
            new Face("cless-hq_0_3",288,340,new Vector4(0.4444444f,0.7205882f,0.0277778f,0.0235294f),Vector4.zero),
            new Face("cless-hq_0_2",270,299,new Vector4(0.6444444f,0.6956522f,0.0296296f,0.0267559f),Vector4.zero),
            new Face("mint-hq_0_0",246,274,new Vector4(0.4471545f,0.6532847f,0.0325203f,0.0291971f),new Vector4(0.5772358f,0.6532847f,0.0325203f,0.0291971f)),
            new Face("mint-hq_0_3",252,268,new Vector4(0.4087302f,0.6716418f,0.0317460f,0.0335821f),Vector4.zero),
            new Face("mint-hq_0_2",250,271,new Vector4(0.5920000f,0.6568266f,0.0320000f,0.0332103f),Vector4.zero),
            new Face("farah-hq_0_0",243,313,new Vector4(0.3868313f,0.6198083f,0.0411523f,0.0319489f),new Vector4(0.5473251f,0.6198083f,0.0411523f,0.0319489f)),
            new Face("farah-hq_0_3",210,311,new Vector4(0.3571429f,0.6205788f,0.0380952f,0.0353698f),Vector4.zero),
            new Face("farah-hq_0_2",243,303,new Vector4(0.5802469f,0.6270627f,0.0329218f,0.0363036f),Vector4.zero),
            new Face("jade-hq_0_0",285,399,new Vector4(0.4631579f,0.6115288f,0.0280702f,0.0100251f),new Vector4(0.5614035f,0.6115288f,0.0280702f,0.0100251f)),
            new Face("jade-hq_0_3",264,371,new Vector4(0.4242424f,0.6145553f,0.0265152f,0.0134771f),Vector4.zero),
            new Face("jade-hq_0_2",284,399,new Vector4(0.6021127f,0.6466165f,0.0281690f,0.0125313f),Vector4.zero),
            new Face("tear-hq_0_0",310,319,new Vector4(0.4548387f,0.6927900f,0.0193548f,0.0188088f),new Vector4(0.5451613f,0.7115987f,0.0258065f,0.0282132f)),
            new Face("tear-hq_0_3",265,319,new Vector4(0.4566038f,0.7115987f,0.0301887f,0.0282132f),Vector4.zero),
            new Face("tear-hq_0_2",280,310,new Vector4(0.5571429f,0.7129032f,0.0285714f,0.0290323f),Vector4.zero),
            new Face("natalia-hq_0_0",249,297,new Vector4(0.4417671f,0.7373737f,0.0401606f,0.0336700f),new Vector4(0.5823293f,0.7373737f,0.0401606f,0.0336700f)),
            new Face("natalia-hq_0_3",222,298,new Vector4(0.4054054f,0.7348993f,0.0405405f,0.0369128f),Vector4.zero),
            new Face("natalia-hq_0_2",242,298,new Vector4(0.5454545f,0.7416107f,0.0371901f,0.0369128f),Vector4.zero),
            new Face("velvet-hq_0_0",264,278,new Vector4(0.4318182f,0.6906475f,0.0303030f,0.0215827f),new Vector4(0.5454545f,0.6942446f,0.0303030f,0.0215827f)),
            new Face("velvet-hq_0_3",262,273,new Vector4(0.4007634f,0.7142857f,0.0305344f,0.0219780f),Vector4.zero),
            new Face("velvet-hq_0_2",271,279,new Vector4(0.5793358f,0.7025090f,0.0295203f,0.0250896f),Vector4.zero),
            new Face("alphen-hq_0_0",285,319,new Vector4(0.6070175f,0.6394984f,0.0280702f,0.0188088f),Vector4.zero),
            new Face("alphen-hq_0_3",276,291,new Vector4(0.4094203f,0.5979381f,0.0253623f,0.0206186f),Vector4.zero),
            new Face("alphen-hq_0_2",287,325,new Vector4(0.5853659f,0.6523077f,0.0243902f,0.0184615f),Vector4.zero),
            new Face("shionne-hq_0_0",292,315,new Vector4(0.3458904f,0.6698413f,0.0239726f,0.0285714f),new Vector4(0.4383562f,0.6761905f,0.0273973f,0.0317460f)),
            new Face("shionne-hq_0_2",270,331,new Vector4(0.3703704f,0.7009063f,0.0259259f,0.0271903f),Vector4.zero),
            new Face("shionne-hq_0_3",288,335,new Vector4(0.5763889f,0.7014925f,0.0277778f,0.0268657f),new Vector4(0.6354167f,0.6835821f,0.0104167f,0.0179104f)),
            new Face("kisara-hq_0_0",283,275,new Vector4(0.5229682f,0.7890909f,0.0282686f,0.0290909f),new Vector4(0.6183746f,0.7890909f,0.0282686f,0.0290909f)),
            new Face("kisara-hq_0_3",261,273,new Vector4(0.3218391f,0.7985348f,0.0268199f,0.0329670f),Vector4.zero),
            new Face("kisara-hq_0_2",252,273,new Vector4(0.6190476f,0.7985348f,0.0277778f,0.0329670f),Vector4.zero),
            new Face("alphen-hq_0_0",274,269,new Vector4(0.6204380f,0.6654275f,0.0291971f,0.0223048f),Vector4.zero),
            new Face("alphen-hq_0_3",280,308,new Vector4(0.4392857f,0.7175325f,0.0285714f,0.0194805f),Vector4.zero),
            new Face("alphen-hq_0_2",273,270,new Vector4(0.5970696f,0.6814815f,0.0293040f,0.0222222f),Vector4.zero),
            new Face("cless-hq_0_0",285,268,new Vector4(0.5192982f,0.6828358f,0.0280702f,0.0261194f),new Vector4(0.6140351f,0.6828358f,0.0280702f,0.0261194f)),
            new Face("cless-hq_0_3",281,270,new Vector4(0.4626335f,0.7037037f,0.0284698f,0.0259259f),Vector4.zero),
            new Face("cless-hq_0_2",241,293,new Vector4(0.6348548f,0.7167235f,0.0331950f,0.0238908f),Vector4.zero),
            new Face("farah-hq_0_0",242,322,new Vector4(0.4256198f,0.6677019f,0.0371901f,0.0217391f),new Vector4(0.5619835f,0.6677019f,0.0371901f,0.0217391f)),
            new Face("farah-hq_0_3",229,324,new Vector4(0.3799127f,0.6882716f,0.0349345f,0.0246914f),Vector4.zero),
            new Face("farah-hq_0_2",230,325,new Vector4(0.6130435f,0.6892308f,0.0347826f,0.0246154f),Vector4.zero),
            new Face("jade-hq_0_0",299,368,new Vector4(0.4481605f,0.6304348f,0.0267559f,0.0135870f),new Vector4(0.5484950f,0.6304348f,0.0267559f,0.0135870f)),
            new Face("jade-hq_0_3",278,358,new Vector4(0.4568345f,0.6033520f,0.0251799f,0.0139665f),Vector4.zero),
            new Face("jade-hq_0_2",282,360,new Vector4(0.6170213f,0.6194444f,0.0283688f,0.0138889f),new Vector4(0.6879433f,0.6055556f,0.0141844f,0.0111111f)),
            new Face("kisara-hq_0_0",288,271,new Vector4(0.5069444f,0.7749077f,0.0277778f,0.0295203f),new Vector4(0.6006944f,0.7749077f,0.0277778f,0.0295203f)),
            new Face("kisara-hq_0_3",256,271,new Vector4(0.4023438f,0.7822878f,0.0273438f,0.0332103f),Vector4.zero),
            new Face("kisara-hq_0_2",250,272,new Vector4(0.5960000f,0.7830882f,0.0280000f,0.0330882f),Vector4.zero),
            new Face("mint-hq_0_0",249,296,new Vector4(0.4016064f,0.6520270f,0.0321285f,0.0337838f),new Vector4(0.5220884f,0.6486486f,0.0321285f,0.0337838f)),
            new Face("mint-hq_0_3",234,296,new Vector4(0.3760684f,0.6689189f,0.0341880f,0.0371622f),Vector4.zero),
            new Face("mint-hq_0_2",248,309,new Vector4(0.5967742f,0.6407767f,0.0322581f,0.0355987f),Vector4.zero),
            new Face("natalia-hq_0_0",243,289,new Vector4(0.4403292f,0.7474048f,0.0370370f,0.0346021f),new Vector4(0.5720165f,0.7474048f,0.0370370f,0.0346021f)),
            new Face("natalia-hq_0_3",221,287,new Vector4(0.4162896f,0.7456446f,0.0407240f,0.0348432f),Vector4.zero),
            new Face("natalia-hq_0_2",241,286,new Vector4(0.5228216f,0.7517483f,0.0373444f,0.0349650f),Vector4.zero),
            new Face("shionne-hq_0_0",298,341,new Vector4(0.3489933f,0.6392962f,0.0234899f,0.0263930f),new Vector4(0.4328859f,0.6480938f,0.0268456f,0.0293255f)),
            new Face("shionne-hq_0_2",291,364,new Vector4(0.3848797f,0.6620879f,0.0274914f,0.0274725f),new Vector4(0.3230241f,0.6510989f,0.0103093f,0.0137363f)),
            new Face("shionne-hq_0_3",297,377,new Vector4(0.5925926f,0.6790451f,0.0269360f,0.0238727f),new Vector4(0.6531987f,0.6631300f,0.0101010f,0.0159151f)),
            new Face("tear-hq_0_0",289,291,new Vector4(0.4463668f,0.6357388f,0.0276817f,0.0309278f),new Vector4(0.5397924f,0.6357388f,0.0276817f,0.0309278f)),
            new Face("tear-hq_0_3",265,279,new Vector4(0.4754717f,0.6344086f,0.0301887f,0.0322581f),Vector4.zero),
            new Face("tear-hq_0_2",283,272,new Vector4(0.5477032f,0.6360294f,0.0282686f,0.0330882f),Vector4.zero),
            new Face("velvet-hq_0_0",276,283,new Vector4(0.3949275f,0.7279152f,0.0289855f,0.0212014f),new Vector4(0.4963768f,0.7279152f,0.0289855f,0.0212014f)),
            new Face("velvet-hq_0_3",256,291,new Vector4(0.3945312f,0.7594502f,0.0273438f,0.0240550f),Vector4.zero),
            new Face("velvet-hq_0_2",268,289,new Vector4(0.6082090f,0.7577855f,0.0261194f,0.0242215f),Vector4.zero),
        };
    }
}

