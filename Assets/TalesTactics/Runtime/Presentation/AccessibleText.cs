using TMPro;
using UnityEngine;
namespace TalesTactics
{
    // Text may shrink back to its authored size in compact controls, instead of clipping.
    public sealed class AccessibleText:MonoBehaviour
    {
        TMP_Text text;BattleDirector battle;float maximum,minimum,last=-1;bool authoredAuto;
        void Start(){text=GetComponent<TMP_Text>();battle=GetComponentInParent<BattleHud>()?.GetComponentInParent<BattleDirector>();if(battle==null)battle=FindAnyObjectByType<BattleDirector>();authoredAuto=text.enableAutoSizing;maximum=authoredAuto?text.fontSizeMax:text.fontSize;minimum=authoredAuto?text.fontSizeMin:text.fontSize;}
        void LateUpdate()
        {
            if(text==null)return;float scale=battle?.Preferences?.TextScale??1;if(Mathf.Approximately(scale,last))return;last=scale;
            text.enableAutoSizing=authoredAuto||scale>1;text.fontSizeMin=minimum;text.fontSizeMax=maximum*scale;if(!text.enableAutoSizing)text.fontSize=maximum;
        }
    }
}
