using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        RectTransform SystemMenuPanel()=>director.Hud.GetComponentsInChildren<RectTransform>().Single(r=>r.name=="SystemWindow");
        void AssertTouchSettingsFit()
        {
            Canvas.ForceUpdateCanvases();var panel=SystemMenuPanel();var corners=new Vector3[4];panel.GetWorldCorners(corners);
            Assert.That(corners.All(p=>p.x>=0&&p.x<=Screen.width&&p.y>=0&&p.y<=Screen.height),Is.True,"Mobile settings window is on screen.");
            var buttons=panel.GetComponentsInChildren<UnityEngine.UI.Button>();
            foreach(var button in buttons)
            {
                Assert.That(((RectTransform)button.transform).rect.height,Is.GreaterThanOrEqualTo(56),button.name);
                Assert.That(button.IsInteractable(),Is.True,button.name);
            }
            for(int i=0;i<buttons.Length;i++)for(int j=i+1;j<buttons.Length;j++)
            {
                var a=new Vector3[4];var b=new Vector3[4];((RectTransform)buttons[i].transform).GetWorldCorners(a);((RectTransform)buttons[j].transform).GetWorldCorners(b);
                var first=Rect.MinMaxRect(a[0].x,a[0].y,a[2].x,a[2].y);var second=Rect.MinMaxRect(b[0].x,b[0].y,b[2].x,b[2].y);
                Assert.That(first.Overlaps(second),Is.False,buttons[i].name+" overlaps "+buttons[j].name);
            }
            foreach(var text in panel.GetComponentsInChildren<TMPro.TMP_Text>())
            {text.ForceMeshUpdate();Assert.That(text.isTextOverflowing,Is.False,text.text);}
            Assert.That(director.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().Where(b=>b.IsInteractable()).All(b=>b.transform.IsChildOf(panel)),Is.True,"Modal must block underlying controls.");
        }

        [UnityTest,Category("AndroidMenus")]
        public IEnumerator MobileSettingsUseWorkingOptionsAndSaveOnlyToMobilePreferences()
        {
            var desktop=Path.Combine(testStorage,"settings.json");
            var original=JsonUtility.FromJson<PlayerPreferences>(JsonUtility.ToJson(director.Preferences));
            Assert.That(director.SavePreferences(original,false),Is.True);var desktopBefore=File.ReadAllText(desktop);
            director.ConfigureStorage(testStorage,false,true);UseMobileHud();yield return null;
            float music=director.Preferences.Music;int width=director.Preferences.Width,height=director.Preferences.Height;bool fullscreen=director.Preferences.Fullscreen;
            director.Hud.ShowSystemMenu(1);yield return null;yield return null;
            Assert.That(SystemMenuPanel().GetComponentsInChildren<UnityEngine.UI.Button>().Any(b=>b.name.StartsWith("해상도:")||b.name.StartsWith("화면:")),Is.False);
            AssertTouchSettingsFit();Click("음악 +");yield return null;
            Assert.That(director.Preferences.Music,Is.EqualTo(music),"Draft is not yet applied.");
            director.Hud.CloseSystemMenu();director.Hud.ShowSystemMenu(1);yield return null;
            Assert.That(director.Preferences.Music,Is.EqualTo(music),"Closing discards draft.");
            Click("음악 +");Click("설정 적용 · 저장");yield return null;yield return null;AssertTouchSettingsFit();
            Assert.That(director.Audio.MusicVolume,Is.EqualTo(music+.1f).Within(.001f));
            director.Hud.CloseSystemMenu();director.ConfigureStorage(testStorage,false,true);
            Assert.That(director.Preferences.Music,Is.EqualTo(music+.1f).Within(.001f));
            Assert.That(director.Preferences.Width,Is.EqualTo(width));Assert.That(director.Preferences.Height,Is.EqualTo(height));Assert.That(director.Preferences.Fullscreen,Is.EqualTo(fullscreen));
            Assert.That(File.ReadAllText(desktop),Is.EqualTo(desktopBefore),"Mobile settings must not alter desktop preferences.");
        }

        [UnityTest,Category("AndroidMenus")]
        public IEnumerator MobileLargeTextControlsPersistAndOpenTouchHelp()
        {
            director.ConfigureStorage(testStorage,false,true);UseMobileHud();yield return null;
            foreach(float scale in new[]{1f,1.3f})
            {
                director.Hud.CloseSystemMenu();director.Preferences.TextScale=scale;
                director.Hud.ShowSystemMenu(1);yield return null;yield return null;AssertTouchSettingsFit();
                director.Hud.ShowSystemMenu(3);yield return null;yield return null;AssertTouchSettingsFit();
            }
            Click("글자 설정 저장");yield return null;director.ConfigureStorage(testStorage,false,true);
            Assert.That(director.Preferences.TextScale,Is.EqualTo(1.3f));
            Click("전체 조작 보기");yield return null;
            Assert.That(director.Hud.SystemMenuOpen,Is.False);Assert.That(director.Hud.HelpOpen,Is.True);
            var text=string.Join("\n",director.Hud.GetComponentsInChildren<TMPro.TMP_Text>().Select(t=>t.text));
            Assert.That(text,Does.Contain("터치"));Assert.That(text,Does.Not.Contain("M 이동"));
            director.Hud.CloseHelp();Assert.That(director.Hud.HelpOpen,Is.False);
        }
    }
}