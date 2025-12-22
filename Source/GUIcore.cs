using ToolbarControl_NS;
using UnityEngine;

namespace SimpleLogistics
{
    /*
    public struct GuiTexture
    {
        public static Texture2D tWindowBack = new Texture2D(8, 8);
        public static Texture2D tButtonBack = new Texture2D(8, 8);
        public static Texture2D tButtonHover = new Texture2D(8, 8);
        internal static void LoadTextures()
        {
            tWindowBack.LoadImage(System.IO.File.ReadAllBytes("GameData/SimpleLogistics/Textures/window-back.png"));
            tButtonBack.LoadImage(System.IO.File.ReadAllBytes("GameData/SimpleLogistics/Textures/button-back.png"));
            tButtonHover.LoadImage(System.IO.File.ReadAllBytes("GameData/SimpleLogistics/Textures/button-hover-back.png"));
        }
    }
    */
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class GUIcore : MonoBehaviour
    {

        void Start()
        {
            ToolbarControl.RegisterMod(SimpleLogistics.MODID, SimpleLogistics.MODNAME);
        }
        static private GUIcore _instance = null;
        static public GUIcore Instance
        {
            get { return _instance; }
        }
        public void Awake()
        {
            if (_instance != null)
            {
                Destroy(this);
                return;
            }
            _instance = this;
        }

        public void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        static private bool skinInitialized = false;
        public void OnGUI()
        {
            if (skinInitialized)
                return;
            //GuiTexture.LoadTextures();
            skinInitialized = true;
            Destroy(this); // Quit after initialized
        }
    }
}

