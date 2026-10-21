using UnityEngine;

namespace TalesTactics
{
    // Shared UI-only treatment. Source alpha, atlas UVs, stencil masks and raycast behaviour are preserved.
    [DisallowMultipleComponent]
    public sealed class SdUiIconArt:MonoBehaviour
    {
        static Material shared;
        static int users;
        UnityEngine.UI.Graphic graphic;
        Material previous;
        bool acquired;
        public static void Apply(UnityEngine.UI.Graphic target)
        {
            if(target!=null&&target.GetComponent<SdUiIconArt>()==null)target.gameObject.AddComponent<SdUiIconArt>();
        }
        void OnEnable()
        {
            graphic=GetComponent<UnityEngine.UI.Graphic>();if(graphic==null||acquired)return;
            if(shared==null)
            {
                var shader=Resources.Load<Shader>("TalesTactics/SdUiInk");if(shader==null||!shader.isSupported)return;
                shared=new Material(shader){name="SD UI illustrated icons",hideFlags=HideFlags.DontSave};
            }
            previous=graphic.material;graphic.material=shared;users++;acquired=true;
        }
        void OnDisable()
        {
            if(!acquired)return;
            if(graphic!=null&&graphic.material==shared)graphic.material=previous;
            acquired=false;users=Mathf.Max(0,users-1);
            if(users==0&&shared!=null){Destroy(shared);shared=null;}
        }
    }
}
