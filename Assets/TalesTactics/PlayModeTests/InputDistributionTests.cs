using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        IEnumerator PadPress(Gamepad pad,GamepadButton button)
        {
            InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(button));yield return null;yield return null;
            InputSystem.QueueStateEvent(pad,new GamepadState());yield return null;
        }
        void PointAt(GamepadPointer pointer,string name)
        {
            Canvas.ForceUpdateCanvases();
            var button=director.Hud.GetComponentsInChildren<Button>().Single(b=>b.name==name);
            var rect=(RectTransform)button.transform;
            pointer.MoveTo(RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center)));
        }
        IEnumerator KeyboardPress(Keyboard keyboard,params Key[] keys)
        {
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));yield return null;yield return null;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
        }
        [UnityTest] public IEnumerator KeyboardMenusCycleModalButtonsAndSubmitOnceAfterPad()
        {
            var keyboard=InputSystem.AddDevice<Keyboard>();var pad=InputSystem.AddDevice<Gamepad>();
            try
            {
                var pointer=director.GetComponent<GamepadPointer>();
                yield return PadPress(pad,GamepadButton.DpadDown);Assert.That(pointer.Active,Is.True);
                director.Hud.ShowSystemMenu();yield return null;
                EventSystem.current.SetSelectedGameObject(null);
                var buttons=director.Hud.GetComponentsInChildren<Button>().Where(b=>b.isActiveAndEnabled&&b.IsInteractable()).ToArray();
                yield return KeyboardPress(keyboard,Key.Tab);
                Assert.That(pointer.Active,Is.False);Assert.That(EventSystem.current.sendNavigationEvents,Is.True);
                Assert.That(EventSystem.current.currentSelectedGameObject,Is.SameAs(buttons[0].gameObject));
                Assert.That(buttons[0].GetComponent<Outline>().effectDistance.x,Is.EqualTo(3));
                yield return KeyboardPress(keyboard,Key.LeftShift,Key.Tab);
                Assert.That(EventSystem.current.currentSelectedGameObject,Is.SameAs(buttons[buttons.Length-1].gameObject));
                int submissions=0;buttons[buttons.Length-1].onClick.AddListener(()=>submissions++);
                yield return KeyboardPress(keyboard,Key.Enter);
                Assert.That(submissions,Is.EqualTo(1));Assert.That(director.Hud.SystemMenuOpen,Is.False);
            }
            finally {InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(pad);}
        }
        [UnityTest] public IEnumerator GamepadMenusDoNotDoubleSubmitAndReconnect()
        {
            var pad=InputSystem.AddDevice<Gamepad>();
            try
            {
                var pointer=director.GetComponent<GamepadPointer>();
                var button=director.Hud.GetComponentsInChildren<Button>().First(b=>b.IsInteractable());
                int clicked=0;button.onClick.AddListener(()=>clicked++);
                PointAt(pointer,button.name);
                EventSystem.current.SetSelectedGameObject(button.gameObject);
                yield return PadPress(pad,GamepadButton.South);
                Assert.That(clicked,Is.EqualTo(1),"Native UI submit must not also execute");
                Assert.That(pointer.Active,Is.True);
                yield return PadPress(pad,GamepadButton.DpadDown);
                var before=pointer.Position;
                InputSystem.QueueStateEvent(pad,new GamepadState{leftStick=Vector2.right});yield return null;yield return null;
                Assert.That(pointer.Position.x,Is.GreaterThan(before.x));
                InputSystem.RemoveDevice(pad);yield return null;
                Assert.That(pointer.Active,Is.False);Assert.That(EventSystem.current.sendNavigationEvents,Is.True);
                pad=InputSystem.AddDevice<Gamepad>();yield return PadPress(pad,GamepadButton.DpadDown);
                Assert.That(pointer.Active,Is.True);
            }
            finally{if(pad.added)InputSystem.RemoveDevice(pad);}
        }
        [UnityTest] public IEnumerator GamepadSelectsTilesCancelsAndHandlesTiming()
        {
            var pad=InputSystem.AddDevice<Gamepad>();
            try
            {
                var pointer=director.GetComponent<GamepadPointer>();
                PointAt(pointer,"전투 시작");yield return PadPress(pad,GamepadButton.South);
                Assert.That(director.Session,Is.Not.Null);
                var unit=director.Session.Active;
                Assert.That(unit.Team,Is.EqualTo(Team.Player));
                director.SetState(new MoveSelectionState(director));
                var destination=director.Session.Grid.Reachable(unit,out _).Keys.First(p=>p!=unit.Position);
                pointer.MoveTo(director.Board.BattleCamera.WorldToScreenPoint(director.Session.Grid[destination].WorldPosition(director.Catalog.Rules.TileHeight)));
                yield return PadPress(pad,GamepadButton.South);yield return new WaitForSeconds(1);
                Assert.That(unit.Position,Is.EqualTo(destination));
                director.SetState(new ActionSelectionState(director));yield return PadPress(pad,GamepadButton.East);
                Assert.That(director.State,Is.TypeOf<CommandState>());
                var rotation=director.Board.BattleCamera.transform.rotation;
                yield return PadPress(pad,GamepadButton.RightShoulder);
                Assert.That(Quaternion.Angle(rotation,director.Board.BattleCamera.transform.rotation),Is.GreaterThan(80));
                director.TimingActive=true;director.TimingProgress=.5f;
                yield return PadPress(pad,GamepadButton.South);Assert.That(director.TimingSuccess,Is.True);
                director.TimingActive=false;
            }
            finally{if(pad.added)InputSystem.RemoveDevice(pad);}
        }
        [UnityTest] public IEnumerator BundledFontCoversCatalogWithoutOperatingSystemFonts()
        {
            var font=Resources.Load<TMP_FontAsset>("TalesTactics/Korean");
            Assert.That(director.Hud.Font,Is.SameAs(font));Assert.That(font.atlasPopulationMode,Is.EqualTo(AtlasPopulationMode.Static));
            var text="한글 전투 출전 성장 장비 저장 승리 패배 魔神剣 사후폭쇄진";
            foreach(var c in director.Catalog.Characters){text+=c.DisplayName;foreach(var s in c.Skills)text+=s.DisplayName;}
            Assert.That(font.HasCharacters(text,out uint[] missing,true,true),Is.True,string.Join(",",missing??new uint[0]));
            Assert.That(font.fallbackFontAssetTable.Single().atlasPopulationMode,Is.EqualTo(AtlasPopulationMode.Dynamic));
            Assert.That(font.fallbackFontAssetTable.Single().sourceFontFile,Is.Not.Null);
            yield return null;
        }
    }
}
