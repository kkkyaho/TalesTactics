using UnityEngine;
namespace TalesTactics
{
    public sealed class WindowDisplayController:MonoBehaviour
    {
        BattleDirector battle;int width,height;bool maximized;float stableAt,ignoreUntil;bool restore;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("user32.dll")] static extern bool IsZoomed(System.IntPtr window);
        [System.Runtime.InteropServices.DllImport("user32.dll")] static extern bool IsIconic(System.IntPtr window);
        [System.Runtime.InteropServices.DllImport("user32.dll")] static extern bool ShowWindow(System.IntPtr window,int command);
        System.IntPtr windowHandle;
        System.IntPtr Window {get {if(windowHandle==System.IntPtr.Zero)using(var process=System.Diagnostics.Process.GetCurrentProcess())windowHandle=process.MainWindowHandle;return windowHandle;}}
#endif
        public void Initialize(BattleDirector owner){battle=owner;DisplayApplied();}
        public void DisplayApplied(){ignoreUntil=Time.unscaledTime+1;restore=true;width=height=0;}
        void Update()
        {
            if(Application.isEditor||battle?.Preferences==null||Time.unscaledTime<ignoreUntil)return;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            var window=Window;if(window==System.IntPtr.Zero||IsIconic(window))return;
            if(restore){restore=false;if(!Screen.fullScreen&&battle.Preferences.WindowMaximized){ShowWindow(window,3);ignoreUntil=Time.unscaledTime+.5f;return;}}
            bool max=IsZoomed(window);
#else
            bool max=false;if(restore)restore=false;
#endif
            if(Screen.fullScreen){width=height=0;return;}
            if(width!=Screen.width||height!=Screen.height||maximized!=max)
            {width=Screen.width;height=Screen.height;maximized=max;stableAt=Time.unscaledTime;return;}
            if(Time.unscaledTime-stableAt>.6f)Store();
        }
        void Store()
        {
            if(width<640||height<480||width>7680||height>4320)return;
            var p=battle.Preferences;
            if(p.WindowMaximized==maximized&&(maximized||p.Width==width&&p.Height==height))return;
            var draft=JsonUtility.FromJson<PlayerPreferences>(JsonUtility.ToJson(p));draft.WindowMaximized=maximized;
            if(!maximized){draft.Width=width;draft.Height=height;}
            if(!battle.SavePreferences(draft,false))stableAt=Time.unscaledTime+5;
        }
        void OnApplicationQuit(){if(!Application.isEditor&&battle!=null&&!Screen.fullScreen)Store();}
    }
}
