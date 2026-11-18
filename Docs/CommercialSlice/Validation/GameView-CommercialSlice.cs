using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Temporary GameView sizes only; never saves project settings or replaces existing sizes.
// Restore removes only the exact two sizes owned by this review and restores the recorded index 3.
public static class CommercialSliceGameView
{
    const string Label = "CommercialSlice20261117 ";
    static readonly Assembly EditorAssembly = typeof(Editor).Assembly;
    static readonly BindingFlags Members = BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static object Group()
    {
        var type=EditorAssembly.GetType("UnityEditor.GameViewSizes");
        var instance=type.GetProperty("instance",BindingFlags.Public|BindingFlags.Static|BindingFlags.FlattenHierarchy).GetValue(null);
        return type.GetProperty("currentGroup").GetValue(instance);
    }
    static EditorWindow View()
    {
        var windows=Resources.FindObjectsOfTypeAll(EditorAssembly.GetType("UnityEditor.GameView"));
        if(windows.Length!=1)throw new InvalidOperationException("Expected exactly one existing GameView.");
        return (EditorWindow)windows[0];
    }
    static object Size(object group,int index)=>group.GetType().GetMethod("GetGameViewSize").Invoke(group,new object[]{index});
    static int Count(object group)=>(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null);
    static void Select(EditorWindow view,int index){view.GetType().GetProperty("selectedSizeIndex",Members).SetValue(view,index);view.Repaint();}
    public static string Set(int width,int height)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Change the view between test runs.");
        if(!((width==1280&&height==720)||(width==1024&&height==768)||(width==1920&&height==1080)))throw new ArgumentException("Unexpected review size.");
        var group=Group();var view=View();int chosen=-1;
        for(int i=0;i<Count(group);i++)
        {
            var size=Size(group,i);var type=size.GetType();
            if((int)type.GetProperty("width").GetValue(size)==width&&(int)type.GetProperty("height").GetValue(size)==height&&type.GetProperty("sizeType").GetValue(size).ToString()=="FixedResolution"){chosen=i;break;}
        }
        if(chosen<0)
        {
            var kind=Enum.Parse(EditorAssembly.GetType("UnityEditor.GameViewSizeType"),"FixedResolution");
            var size=Activator.CreateInstance(EditorAssembly.GetType("UnityEditor.GameViewSize"),new object[]{kind,width,height,Label+width+"x"+height});
            group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{size});chosen=Count(group)-1;
        }
        Select(view,chosen);
        return Newtonsoft.Json.JsonConvert.SerializeObject(new{selectedIndex=chosen,requestedWidth=width,requestedHeight=height,total=Count(group)});
    }
    public static string Restore()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop testing before restoring the view.");
        var group=Group();var view=View();var baseline=Size(group,3);var type=baseline.GetType();
        if((int)type.GetProperty("width").GetValue(baseline)!=1920||(int)type.GetProperty("height").GetValue(baseline)!=1080)throw new InvalidOperationException("Recorded original index has changed.");
        Select(view,3);
        for(int i=Count(group)-1;i>=0;i--)
        {
            var size=Size(group,i);string label=(string)size.GetType().GetProperty("baseText").GetValue(size);
            if(label==Label+"1280x720"||label==Label+"1024x768")group.GetType().GetMethod("RemoveCustomSize").Invoke(group,new object[]{i});
        }
        return Newtonsoft.Json.JsonConvert.SerializeObject(new{selectedIndex=3,total=Count(group),expectedTotal=7});
    }
}