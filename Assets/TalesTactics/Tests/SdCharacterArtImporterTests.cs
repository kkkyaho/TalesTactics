using System;
using System.Globalization;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using Importer=TalesTactics.Editor.SdCharacterArtImporter;
using Object=UnityEngine.Object;

namespace TalesTactics.Tests
{
    public sealed class SdCharacterArtImporterTests
    {
        [Test] public void AtomicReplacementPreservesMappedOldBytesAndSpriteReferencesWithoutTruncation()
        {
            if(Environment.OSVersion.Platform!=PlatformID.Win32NT)Assert.Ignore("Exercises Windows mapped-file and delete-sharing behavior.");
            string folder="Assets/SdMappedImportRegression-"+Guid.NewGuid().ToString("N"),path=folder+"/atlas.png";
            Assert.That(Directory.Exists(folder)||File.Exists(folder+".meta"),Is.False);
            bool refreshLocked=false;
            try
            {
                Assert.That(AssetDatabase.CreateFolder("Assets",Path.GetFileName(folder)),Is.Not.Empty);
                var initial=Measure(path,64,40,32,
                    Frame("fixture_0",4,6,18,22,.25f,.1f),Frame("fixture_1",34,8,20,20,.7f,.2f));
                WritePng(initial);Invoke("Import",initial,path,false);
                var originalSprites=LoadGeometry(path,initial);
                string sourceGuid=AssetDatabase.AssetPathToGUID(path);
                var ids=originalSprites.ToDictionary(s=>s.name,s=>LocalId(s,sourceGuid));var spriteIds=SpriteIds(path);
                byte[] original=File.ReadAllBytes(path),originalMeta=File.ReadAllBytes(path+".meta");
                var replacement=Measure(folder+"/candidate.png",96,80,73.5f,
                    Frame("fixture_0",8,10,30,42,.8f,.15f),Frame("fixture_1",52,14,32,38,.2f,.35f));
                AssetDatabase.DisallowAutoRefresh();refreshLocked=true;
                WritePng(replacement);byte[] next=File.ReadAllBytes(replacement.source);
                replacement.source=path;replacement.candidate=path;replacement.sourceGuid=sourceGuid;
                foreach(var f in replacement.frames){f.originalSpriteId=spriteIds[f.name];f.originalInternalId=ids[f.name].ToString(CultureInfo.InvariantCulture);}
                Assert.That(next.SequenceEqual(original),Is.False);
                // This is an OS read-only mapping, not a copied managed byte buffer. Delete
                // sharing allows the directory entry to change while the old view survives.
                using(var file=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete))
                using(var mapped=MemoryMappedFile.CreateFromFile(file,null,0,MemoryMappedFileAccess.Read,HandleInheritability.None,true))
                using(var view=mapped.CreateViewAccessor(0,original.Length,MemoryMappedFileAccess.Read))
                {
                    AssertMappedBytes(view,original);
                    Invoke("ReplaceFileAtomically",path,next);
                    CollectionAssert.AreEqual(next,File.ReadAllBytes(path));AssertMappedBytes(view,original);
                    CollectionAssert.AreEqual(originalMeta,File.ReadAllBytes(path+".meta"),"Replacing PNG bytes must not rewrite its GUID metadata.");
                    Invoke("Import",replacement,path,true);
                    foreach(var sprite in LoadGeometry(path,replacement))Assert.That(LocalId(sprite,sourceGuid),Is.EqualTo(ids[sprite.name]));
                    CollectionAssert.AreEquivalent(spriteIds,SpriteIds(path));AssertMappedBytes(view,original);
                    // Exercise the same atomic source/meta restoration used after a failed
                    // revision; the mapping remains open throughout both replacements.
                    Invoke("ReplaceFileAtomically",path,original);Invoke("ReplaceFileAtomically",path+".meta",originalMeta);
                    AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
                    CollectionAssert.AreEqual(original,File.ReadAllBytes(path));CollectionAssert.AreEqual(originalMeta,File.ReadAllBytes(path+".meta"));
                    foreach(var sprite in LoadGeometry(path,initial))Assert.That(LocalId(sprite,sourceGuid),Is.EqualTo(ids[sprite.name]));
                    CollectionAssert.AreEquivalent(spriteIds,SpriteIds(path));AssertMappedBytes(view,original);
                }
                // No delete sharing must fail without switching to truncation/copy/retries.
                using(var denied=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read))
                {
                    var error=Assert.Throws<IOException>(()=>Invoke("ReplaceFileAtomically",path,next));
                    Assert.That(error.Message,Does.Contain("no in-place fallback or retry"));
                    CollectionAssert.AreEqual(original,File.ReadAllBytes(path));
                }
                CollectionAssert.AreEqual(originalMeta,File.ReadAllBytes(path+".meta"));
                Assert.That(AssetDatabase.AssetPathToGUID(path),Is.EqualTo(sourceGuid));
                Assert.That(Directory.GetFiles(folder,".*.sd-*").Length,Is.Zero,"Successful/ordinary sharing-failure attempts must clean their owned sibling files.");
                GC.KeepAlive(originalSprites);
            }
            finally
            {
                if(refreshLocked)AssetDatabase.AllowAutoRefresh();
                if(AssetDatabase.IsValidFolder(folder))Assert.That(AssetDatabase.DeleteAsset(folder),Is.True,"Remove only this test's owned temporary assets.");
            }
        }

        static void AssertMappedBytes(MemoryMappedViewAccessor view,byte[] expected)
        {
            var actual=new byte[expected.Length];Assert.That(view.ReadArray(0,actual,0,actual.Length),Is.EqualTo(actual.Length));
            CollectionAssert.AreEqual(expected,actual,"Existing mapped view must continue reading the old file, including after replacement.");
        }

        [Test] public void LoadedSpritesReimportNewPixelsAndGeometryWithoutChangingReferences()
        {
            string folder="Assets/SdImportRegression-"+Guid.NewGuid().ToString("N");
            Assert.That(Directory.Exists(folder)||File.Exists(folder+".meta"),Is.False);
            string path=folder+"/atlas.png";
            try
            {
                Assert.That(AssetDatabase.CreateFolder("Assets",Path.GetFileName(folder)),Is.Not.Empty);
                var initial=Measure(path,64,40,32,
                    Frame("fixture_0",4,6,18,22,.25f,.1f),Frame("fixture_1",34,8,20,20,.7f,.2f));
                WritePng(initial);Invoke("Import",initial,path,false);
                var loaded=LoadGeometry(path,initial);
                string sourceGuid=AssetDatabase.AssetPathToGUID(path);
                var ids=loaded.ToDictionary(s=>s.name,s=>LocalId(s,sourceGuid));
                var spriteIds=SpriteIds(path);
                var revisions=new[]{
                    Measure(path,96,80,73.5f,
                        Frame("fixture_0",8,10,30,42,.8f,.15f),Frame("fixture_1",52,14,32,38,.2f,.35f)),
                    Measure(path,48,96,21.25f,
                        Frame("fixture_0",3,8,18,32,.35f,.25f),Frame("fixture_1",25,46,20,40,.65f,.1f))
                };
                foreach(var revision in revisions)
                {
                    revision.sourceGuid=sourceGuid;
                    foreach(var frame in revision.frames)
                    {frame.originalSpriteId=spriteIds[frame.name];frame.originalInternalId=ids[frame.name].ToString(CultureInfo.InvariantCulture);}
                    // Keep prior Sprite/texture/mesh objects loaded while replacing only this
                    // unique fixture's PNG. Import must return with the new values observable.
                    WritePng(revision);Invoke("Import",revision,path,true);
                    var current=LoadGeometry(path,revision);
                    Assert.That(AssetDatabase.AssetPathToGUID(path),Is.EqualTo(sourceGuid));
                    foreach(var sprite in current)Assert.That(LocalId(sprite,sourceGuid),Is.EqualTo(ids[sprite.name]));
                    CollectionAssert.AreEquivalent(spriteIds,SpriteIds(path));
                    Assert.That((Sprite[])Invoke("ValidateImportedSprites",revision,path,true),Has.Length.EqualTo(2));
                    GC.KeepAlive(loaded);loaded=current;
                }
                // Exercise actual/expected diagnostics against loaded final objects without
                // changing any asset or weakening the geometry validator.
                var mismatch=JsonUtility.FromJson<Importer.Measurement>(JsonUtility.ToJson(revisions[1]));
                mismatch.frames[0].width+=1;mismatch.pixelsPerUnit+=7;
                var error=Assert.Throws<InvalidOperationException>(()=>Invoke("ValidateImportedSprites",mismatch,path,true));
                Assert.That(error.Message,Does.Contain("actual rect=(3, 8, 18, 32), PPU=21.25"));
                Assert.That(error.Message,Does.Contain("expected rect=(3, 8, 19, 32), PPU=28.25"));
                Assert.That(error.Message,Does.Contain(path+"/fixture_0"));
                mismatch=JsonUtility.FromJson<Importer.Measurement>(JsonUtility.ToJson(revisions[1]));
                mismatch.frames[0].pivotX+=.1f;
                error=Assert.Throws<InvalidOperationException>(()=>Invoke("ValidateImportedSprites",mismatch,path,true));
                Assert.That(error.Message,Does.Contain("Measured pivot differs").And.Contain("actual pixels=").And.Contain("expected pixels="));
                mismatch=JsonUtility.FromJson<Importer.Measurement>(JsonUtility.ToJson(revisions[1]));
                mismatch.frames[0].name="fixture_missing";mismatch.frameCount=3;
                error=Assert.Throws<InvalidOperationException>(()=>Invoke("ValidateImportedSprites",mismatch,path,true));
                Assert.That(error.Message,Does.Contain("actual count=2, expected count=3").And.Contain("missing=[fixture_missing]").And.Contain("unexpected=[fixture_0]"));
                GC.KeepAlive(loaded);
            }
            finally
            {
                // This fixed, newly generated child of Assets is the only deletion target.
                if(AssetDatabase.IsValidFolder(folder))Assert.That(AssetDatabase.DeleteAsset(folder),Is.True,"Remove only the owned temporary asset directory.");
            }
        }

        static Importer.Measurement Measure(string path,int width,int height,float ppu,params Importer.Frame[] frames)
            =>new Importer.Measurement{schemaVersion=1,source=path,candidate=path,width=width,height=height,
                pixelsPerUnit=ppu,frameCount=frames.Length,frames=frames};

        static Importer.Frame Frame(string name,float x,float y,float width,float height,float pivotX,float pivotY)
        {
            float left=-width/2+2,right=width/2-2,bottom=-height/2+2,top=height/2-2;
            return new Importer.Frame{name=name,x=x,y=y,width=width,height=height,pivotX=pivotX,pivotY=pivotY,
                outlines=new[]{new Importer.Polygon{points=new[]{
                    new Importer.Point{x=left,y=bottom},new Importer.Point{x=right,y=bottom},
                    new Importer.Point{x=right,y=top},new Importer.Point{x=left,y=top}}}}};
        }

        static void WritePng(Importer.Measurement m)
        {
            var texture=new Texture2D(m.width,m.height,TextureFormat.RGBA32,false);
            try
            {
                var pixels=new Color32[m.width*m.height];
                for(int i=0;i<m.frames.Length;i++)
                {
                    var f=m.frames[i];var color=new Color32((byte)(80+i*90),150,210,255);
                    for(int y=(int)f.y+1;y<f.y+f.height-1;y++)for(int x=(int)f.x+1;x<f.x+f.width-1;x++)pixels[y*m.width+x]=color;
                }
                texture.SetPixels32(pixels);texture.Apply(false,false);File.WriteAllBytes(m.source,texture.EncodeToPNG());
            }
            finally{Object.DestroyImmediate(texture);}
        }

        static Sprite[] LoadGeometry(string path,Importer.Measurement expected)
        {
            var sprites=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s=>s.name,StringComparer.Ordinal).ToArray();
            Assert.That(sprites.Select(s=>s.name),Is.EqualTo(expected.frames.Select(f=>f.name)));
            foreach(var sprite in sprites)
            {
                var f=expected.frames.Single(v=>v.name==sprite.name);
                Assert.That(sprite.texture.width,Is.EqualTo(expected.width));Assert.That(sprite.texture.height,Is.EqualTo(expected.height));
                Assert.That(sprite.rect,Is.EqualTo(new Rect(f.x,f.y,f.width,f.height)));
                Assert.That(sprite.pixelsPerUnit,Is.EqualTo(expected.pixelsPerUnit).Within(.01f));
                Assert.That(sprite.pivot.x,Is.EqualTo(f.pivotX*f.width).Within(.01f));Assert.That(sprite.pivot.y,Is.EqualTo(f.pivotY*f.height).Within(.01f));
                Assert.That(sprite.vertices.Length,Is.GreaterThanOrEqualTo(3));Assert.That(sprite.triangles.Length,Is.GreaterThanOrEqualTo(3));
            }
            return sprites;
        }

        static long LocalId(Sprite sprite,string expectedGuid)
        {
            Assert.That(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite,out string guid,out long id),Is.True);
            Assert.That(guid,Is.EqualTo(expectedGuid));return id;
        }

        static System.Collections.Generic.Dictionary<string,string> SpriteIds(string path)
        {
            var factories=new SpriteDataProviderFactories();factories.Init();
            var provider=factories.GetSpriteEditorDataProviderFromObject(AssetImporter.GetAtPath(path));
            Assert.That(provider,Is.Not.Null);provider.InitSpriteEditorDataProvider();
            return provider.GetSpriteRects().ToDictionary(r=>r.name,r=>r.spriteID.ToString());
        }

        static object Invoke(string name,params object[] arguments)
        {
            var method=typeof(Importer).GetMethod(name,BindingFlags.Static|BindingFlags.NonPublic);
            Assert.That(method,Is.Not.Null,"Exercise the production import/validation path without widening its API.");
            try{return method.Invoke(null,arguments);}
            catch(TargetInvocationException error)when(error.InnerException!=null)
            {ExceptionDispatchInfo.Capture(error.InnerException).Throw();throw;}
        }
    }
}
