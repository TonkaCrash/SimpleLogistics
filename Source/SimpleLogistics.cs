using ClickThroughFix;
using KSP.IO;
using KSP.Localization;
using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using ToolbarControl_NS;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SimpleLogistics
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class RegisterToolbar : MonoBehaviour
    {
        void Start()
        {
            ToolbarControl.RegisterMod(SimpleLogistics.MODID, SimpleLogistics.MODNAME);
        }
    }

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class SimpleLogistics : MonoBehaviour
    {
        public const string MODID = "SimpleLogisticsUI";
        public const string MODNAME = "Simple Logistics";

        private static SimpleLogistics instance;
        public static SimpleLogistics Instance { get { return instance; } }

        // Network Pool
        private bool NetworkActive = false;
        private int NetworkCount = 0;
        private SortedList<string, double> resourcePool;
        private SortedList<string, double> emptySpacePool;

        // Vessel Inventory
        private SortedList<string, double> vesselAmount;
        private SortedList<string, double> vesselMaxAmount;
        private SortedList<string, double> VesselRequestList;

        private List<PartResource> partResources;

        // Request for resources 
        private bool requested;

        private PluginConfiguration config;

        // GUI vars
        private Rect windowRect;
        private int windowId;
        private bool gamePaused;
        private bool GUIhidden;
        private bool GUIactive;

        private ToolbarControl toolbarControl;
        private GUIStyle TitleStyle;
        private GUIStyle HeaderStyle;
        private GUIStyle ResHeaderStyle;
        private GUIStyle ResNameStyle;
        private GUIStyle NumHeaderStyle;
        private GUIStyle NumStyle;
        private GUIStyle ButtonStyle;
        private GUIStyle SliderStyle;
        private GUIStyle ThumbStyle;
        private float GUIScale = GameSettings.UI_SCALE;

        // Same as Debug Toolbar lock mask
        private const ulong lockMask = 900719925474097919;

        #region Primary Functions
        private void Awake()
        {
            if (instance != null)
            {
                Destroy(this);
                return;
            }
            instance = this;
        }

        private void Start()
        {
            // Network state
            resourcePool = new SortedList<string, double>();
            emptySpacePool = new SortedList<string, double>();

            // Vessel State
            vesselAmount = new SortedList<string, double>();
            vesselMaxAmount = new SortedList<string, double>();
            VesselRequestList = new SortedList<string, double>();

            partResources = new List<PartResource>();

            config = PluginConfiguration.CreateForType<SimpleLogistics>();
            config.load();

            windowRect = config.GetValue<Rect>(this.name, new Rect(0, 0, 0, 0));
            windowId = GUIUtility.GetControlID(FocusType.Passive);

            GUIhidden = false;
            gamePaused = false;
            GUIactive = false;
            requested = false;

            CreateButtonIcon();

            // SceneManager.sceneLoaded += OnSceneLoaded;
            GameEvents.onVesselChange.Add(OnVesselChange);
            GameEvents.onHideUI.Add(OnHideUI);
            GameEvents.onShowUI.Add(OnShowUI);
            GameEvents.onGamePause.Add(OnGamePause);
            GameEvents.onGameUnpause.Add(OnGameUnpause);
            GameEvents.onUIScaleChange.Add(OnUIScaleChange);
        }

        private void OnDestroy()
        {
            config.SetValue(this.name, windowRect);
            config.save();

            //SceneManager.sceneLoaded -= OnSceneLoaded;
            GameEvents.onVesselChange.Remove(OnVesselChange);
            GameEvents.onHideUI.Remove(OnHideUI);
            GameEvents.onShowUI.Remove(OnShowUI);
            GameEvents.onGamePause.Remove(OnGamePause);
            GameEvents.onGameUnpause.Remove(OnGameUnpause);
            GameEvents.onUIScaleChange.Remove(OnUIScaleChange);

            UnlockControls();
            DestroyLauncher();

            if (instance == this) instance = null;
        }
        // Doesn't seem to ever get called
        public void OnSceneLoaded(Scene gameScene, LoadSceneMode mode)
        {
            if (Enum.TryParse<GameScenes>(gameScene.buildIndex.ToString(), out GameScenes scene))
                if (scene == GameScenes.FLIGHT)
                {
                    OnVesselChange(FlightGlobals.ActiveVessel);
                    InventoryNetwork();
                }
                else
                    Debug.Log("Simple Logistics Inactive in this scene");
        }
        private void OnVesselChange(Vessel vessel)
        {
            VesselRequestList.Clear();
            InventoryVessel(vessel);
            ResetGUI();
        }
        #endregion

        #region UI Functions
        private void CreateButtonIcon()
        {
            toolbarControl = gameObject.AddComponent<ToolbarControl>();
            toolbarControl.AddToAllToolbars(OnAppTrue, OnAppFalse,
                ApplicationLauncher.AppScenes.FLIGHT,
                 MODID,
                "SimpleLogisticsButton",
                "SimpleLogistics/Textures/SLicon_38",
                "SimpleLogistics/Textures/SLicon_24",
                MODNAME
            );
        }
        public void DestroyLauncher()
        {
            if (toolbarControl != null)
            {
                toolbarControl.OnDestroy();
                Destroy(toolbarControl);
            }
        }
        public void OnUIScaleChange()
        {
            GUIScale = GameSettings.UI_SCALE;
        }

        public void OnGUI()
        {
            if (gamePaused || GUIhidden || !GUIactive) return;

            if (!(FlightGlobals.ActiveVessel.situation == Vessel.Situations.LANDED ||
                  FlightGlobals.ActiveVessel.situation == Vessel.Situations.SPLASHED ||
                  FlightGlobals.ActiveVessel.situation == Vessel.Situations.PRELAUNCH))
            {
                toolbarControl.SetFalse();
            }
            GUI.skin = HighLogic.Skin;
            TitleStyle = new GUIStyle(GUI.skin.window)
            {
                fontSize = (int)(16 * GUIScale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperCenter
            };
            HeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = (int)(14 * GUIScale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            ButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = (int)(12 * GUIScale)
            };
            SliderStyle = new GUIStyle(GUI.skin.horizontalSlider)
            {
                alignment = TextAnchor.MiddleCenter
            };
            ThumbStyle = new GUIStyle(GUI.skin.horizontalSliderThumb);
            ResHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = (int)(12 * GUIScale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            NumHeaderStyle = new GUIStyle(ResHeaderStyle)
            {
                alignment = TextAnchor.MiddleRight
            };
            ResNameStyle = new GUIStyle(ResHeaderStyle)
            {
                normal = { textColor = Color.white },
                fontStyle = FontStyle.Normal,
            };
            NumStyle = new GUIStyle(ResNameStyle)
            {
                alignment = TextAnchor.MiddleRight
            };

            windowRect = ClickThruBlocker.GUILayoutWindow(
                windowId,
                windowRect,
                DrawGUI,
                Localizer.Format("#SimpLog_WindowTitle"), TitleStyle,
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true)
            );
            if (windowRect.Contains(Event.current.mousePosition))
                LockControls();
            else
                UnlockControls();
        }
        public void ResetGUI()
        {
            windowRect.height = 0;
            windowRect.width = 0;
        }
        private void DrawGUI(int windowId)
        {
            LogisticsModule lm = FlightGlobals.ActiveVessel.FindPartModuleImplementing<LogisticsModule>();
            if (lm == null) return; // no logistics module present, so skip GUI

            GUILayout.BeginVertical();
            GUILayout.Space(10 * GUIScale);
            GUILayout.Label(Localizer.Format("#SimpLog_VesselName", FlightGlobals.ActiveVessel.GetDisplayName()), HeaderStyle);
            GUILayout.Space(10 * GUIScale);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (NetworkActive)
                if (NetworkCount == 1)
                    GUILayout.Label(Localizer.Format("#SimpLog_OneConnected"), HeaderStyle); // "Only One Vessel in Resource Network"
                else
                    GUILayout.Label(Localizer.Format("#SimpLog_MoreConnected", NetworkCount), HeaderStyle); // <<1>> Vessels in Resource Network
            else
                GUILayout.Label(Localizer.Format("#SimpLog_NoneConnected"), HeaderStyle); // "No Vessels Sharing Resources"
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(10 * GUIScale);

            bool ableToRequest = false;

            GUILayout.BeginHorizontal();
            bool CurrentState = lm.IsActive;
            if (GUILayout.Button(Localizer.Format("#SimpLog_Toggle"), ButtonStyle)) // "Toggle Connection"
                lm.Toggle();
            if (CurrentState != lm.IsActive)
            { // Toggle changed
                ResetGUI();
                NetworkActive = false;
                return;
            }
            GUI.contentColor = lm.IsActive ? Color.green : Color.red;
            GUILayout.Label(" ", GUILayout.Width(20 * GUIScale));
            GUILayout.Label(lm.IsActive ? Localizer.Format("#SimpLog_Connected") : Localizer.Format("#SimpLog_Unplugged"), HeaderStyle); // "Connected" or "Unplugged"	
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(10 * GUIScale);
            GUI.contentColor = Color.white;

            // if not plugged in and there is an active network
            ableToRequest = !lm.IsActive && NetworkActive;

            // 3 display modes
            //        1) Not Connected to active network - display request sliders if resources can be loaded
            //        2) Connected to active network - display pool inventory
            //        3) No network - diplay current vessel inventory
            if (NetworkActive)
            {
                if (ableToRequest)  // not plugged into active network
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(Localizer.Format("#SimpLog_VesselResources"), ResHeaderStyle, GUILayout.Width(150 * GUIScale)); // Vessel Resources
                    GUILayout.Label(" ", GUILayout.Width(100));
                    GUILayout.Label(Localizer.Format("#SimpLog_Request"), NumHeaderStyle, GUILayout.Width(60 * GUIScale));
                    GUILayout.Label(Localizer.Format("#SimpLog_Pool"), NumHeaderStyle, GUILayout.Width(60 * GUIScale));
                    GUILayout.Label(Localizer.Format("#SimpLog_Vessel"), NumHeaderStyle, GUILayout.Width(60 * GUIScale));
                    GUILayout.Label(Localizer.Format("#SimpLog_Full"), NumHeaderStyle, GUILayout.Width(60 * GUIScale));
                    GUILayout.Label(" ", GUILayout.Width(5));
                    GUILayout.EndHorizontal();

                    foreach (var resource in vesselAmount)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(resource.Key, ResNameStyle, GUILayout.Width(150 * GUIScale));
                        if (resourcePool.ContainsKey(resource.Key))
                        {
                            double amount = Math.Min(vesselMaxAmount[resource.Key] - vesselAmount[resource.Key], resourcePool[resource.Key]);
                            VesselRequestList[resource.Key] = GUILayout.HorizontalSlider((float)VesselRequestList[resource.Key],
                                0, (float)amount, SliderStyle, ThumbStyle, GUILayout.Width(100));
                            VesselRequestList[resource.Key] = Math.Min(VesselRequestList[resource.Key], resourcePool[resource.Key]);
                            GUILayout.Label(VesselRequestList[resource.Key].ToString("0.0"), NumStyle, GUILayout.Width(60 * GUIScale));
                            GUILayout.Label(resourcePool[resource.Key].ToString("0.0"), NumStyle, GUILayout.Width(60 * GUIScale));
                        }
                        else
                        {
                            GUILayout.Label(" ", GUILayout.Width(100));
                            GUILayout.Label("0.0", NumStyle, GUILayout.Width(60 * GUIScale));
                            GUILayout.Label(Localizer.Format("#SimpLog_na"), NumStyle, GUILayout.Width(60 * GUIScale));
                        }
                        GUILayout.Label(vesselAmount[resource.Key].ToString("0.0"), NumStyle, GUILayout.Width(60 * GUIScale));
                        // %Capacity
                        if (vesselMaxAmount[resource.Key] > 0)
                            GUILayout.Label((vesselAmount[resource.Key] / vesselMaxAmount[resource.Key]).ToString("0%"), NumStyle, GUILayout.Width(60 * GUIScale));
                        else
                            GUILayout.Label("0.0", NumStyle, GUILayout.Width(60 * GUIScale));

                        GUILayout.Label(" ", GUILayout.Width(5));
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.Space(10 * GUIScale);
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(Localizer.Format("#SimpLog_RequestResources"), ButtonStyle)) // "Request Resources"
                        requested = true;
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                }
                else // connected to network
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(Localizer.Format(Localizer.Format("#SimpLog_ResourcePool")), ResHeaderStyle, GUILayout.Width(150 * GUIScale));
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(Localizer.Format("#SimpLog_Available"), NumHeaderStyle, GUILayout.Width(60 * GUIScale));
                    GUILayout.Label(Localizer.Format("#SimpLog_Max"), NumHeaderStyle, GUILayout.Width(60 * GUIScale));
                    GUILayout.Label(" ", GUILayout.Width(5));
                    GUILayout.EndHorizontal();

                    foreach (var resource in resourcePool)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(resource.Key, ResNameStyle, GUILayout.Width(150 * GUIScale));
                        GUILayout.FlexibleSpace();
                        GUILayout.Label(resource.Value.ToString("0.0"), NumStyle, GUILayout.Width(60 * GUIScale));
                        GUILayout.Label((resource.Value + emptySpacePool[resource.Key]).ToString("0.0"), NumStyle, GUILayout.Width(60 * GUIScale));
                        GUILayout.Label(" ", GUILayout.Width(5));
                        GUILayout.EndHorizontal();
                    }
                }
            }
            else // No active network
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#SimpLog_VesselResources"), ResHeaderStyle, GUILayout.Width(150 * GUIScale));
                GUILayout.FlexibleSpace();
                GUILayout.Label(Localizer.Format("#SimpLog_Amount"), NumHeaderStyle, GUILayout.Width(60 * GUIScale));
                GUILayout.FlexibleSpace();
                GUILayout.Label(Localizer.Format("#SimpLog_Max"), NumHeaderStyle, GUILayout.Width(60 * GUIScale));
                GUILayout.Label(" ", GUILayout.Width(5));
                GUILayout.EndHorizontal();

                foreach (var resource in vesselAmount)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(resource.Key, ResNameStyle, GUILayout.Width(150 * GUIScale));
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(vesselAmount[resource.Key].ToString("0.0"), NumStyle, GUILayout.Width(60 * GUIScale));
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(vesselMaxAmount[resource.Key].ToString("0.0"), NumStyle, GUILayout.Width(60 * GUIScale));
                    GUILayout.Label(" ", GUILayout.Width(5));
                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.Space(10 * GUIScale);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.contentColor = Color.red;
            if (GUILayout.Button(Localizer.Format("#SimpLog_Close"), ButtonStyle))
                toolbarControl.SetFalse();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            GUI.DragWindow();
        }
        public void OnGamePause()
        {
            gamePaused = true;
            UnlockControls();
        }

        public void OnGameUnpause()
        {
            gamePaused = false;
        }

        private void OnHideUI()
        {
            GUIhidden = true;
            UnlockControls();
        }

        private void OnShowUI()
        {
            GUIhidden = false;
        }
        public bool IsGUIhidden()
        { return GUIhidden; }

        public void OnAppTrue()
        {
            if (FlightGlobals.ActiveVessel.situation == Vessel.Situations.LANDED ||
                FlightGlobals.ActiveVessel.situation == Vessel.Situations.SPLASHED ||
                FlightGlobals.ActiveVessel.situation == Vessel.Situations.PRELAUNCH)
            {
                GUIactive = true;
            }
            else
            {
                GUIactive = false;
                ScreenMessages.PostScreenMessage(Localizer.Format("#SimpLog_ErrorMsg"));
            }
            return;
        }

        public void OnAppFalse()
        {
            GUIactive = false;
            UnlockControls();
        }

        internal virtual void OnToggle()
        {
            if (FlightGlobals.ActiveVessel.situation == Vessel.Situations.LANDED ||
                FlightGlobals.ActiveVessel.situation == Vessel.Situations.SPLASHED ||
                FlightGlobals.ActiveVessel.situation == Vessel.Situations.PRELAUNCH)
            {
                GUIactive = !GUIactive;
                if (!GUIactive)
                    UnlockControls();
                else
                    ResetGUI();
            }
            else
            {
                ScreenMessages.PostScreenMessage(Localizer.Format("#SimpLog_ErrorMsg"));
                return;
            }

        }

        private ControlTypes LockControls()
        {
            return InputLockManager.SetControlLock((ControlTypes)lockMask, this.name);
        }

        private void UnlockControls()
        {
            InputLockManager.RemoveControlLock(this.name);
        }
        #endregion

        #region Resource Sharing
        private void FixedUpdate()
        {
            InventoryVessel(FlightGlobals.ActiveVessel);
            InventoryNetwork();

            if (NetworkActive)  // Skip processing if nothing is active
            {
                // Distribute resources
                foreach (var resource in resourcePool)
                {
                    var resList = partResources.FindAll(r => r.info.name == resource.Key);
                    double amount = resource.Value;
                    double empty = emptySpacePool[resource.Key];
                    // remove requests from pool
                    if (requested && VesselRequestList.ContainsKey(resource.Key))
                    {
                        amount -= VesselRequestList[resource.Key];
                        empty += VesselRequestList[resource.Key];
                    }
                    ShareResource(resList, amount, empty);
                }
                if (requested)
                {
                    TransferResources();
                    requested = false;
                    VesselRequestList.Clear();
                    InventoryVessel(FlightGlobals.ActiveVessel);
                }
            }
        }

        /// <summary>
        /// Distributes resource evenly across every capacitor with priority to low capacity first
        /// </summary>
        /// <param name="resources">List of resources</param>
        /// <param name="amount">Overall amount</param>
        private void ShareResource(List<PartResource> resources, double amount, double empty)
        {
            // Fix for resource converter adding resources to pool. Calculate an average spare space (max 5 units)
            // that is left in all parts and let tanks fill. 
            double spare = empty / resources.Count;
            //if (spare < 1.0f) spare = 0.0f;
            if (spare > 5.0f) spare = 5.0f;

            // Portion each may potentially receive
            double portion = amount / resources.Count;

            // Those who may not grab whole portion
            var minors = resources.FindAll(r => r.maxAmount < Math.Max(0.0f, portion - spare));

            // Those who can grab more than the average portion
            var majors = resources.FindAll(r => r.maxAmount >= portion);

            if (minors.Count > 0)
            {   // small tanks that are near full
                foreach (var minor in minors)
                {
                    minor.amount = Math.Max(0.0f, minor.maxAmount - spare);
                    amount -= minor.amount;
                    empty -= spare;
                }
                // Big tanks that should take full portion
                ShareResource(majors, amount, empty);
            }
            else
            {   // Portion size is good for everybody which means everybody is either same size or mostly empty.
                foreach (var major in majors)
                    major.amount = portion;
            }
        }

        // private static bool onetime = true;
        private void TransferResources()
        {
            List<PartResource> AvailableResources = new List<PartResource>();
            SortedList<string, double> AvailablePool = new SortedList<string, double>();

            foreach (Part part in FlightGlobals.ActiveVessel.parts)
            {
                if (part.State == PartStates.DEAD)
                    continue;

                foreach (PartResource resource in part.Resources)
                {
                    if (resource.info.resourceTransferMode == ResourceTransferMode.NONE ||
                        resource._flowMode == PartResource.FlowMode.None ||
                        !resource._flowState)
                        continue;

                    AvailableResources.Add(resource);
                }
            }

            foreach (var resource in AvailableResources)
            {
                if (!AvailablePool.ContainsKey(resource.info.name))
                    AvailablePool.Add(resource.info.name, resource.amount);
                else
                    AvailablePool[resource.info.name] += resource.amount;
            }
            // Spread resources evenly
            foreach (var resource in AvailablePool)
            {
                try
                {
                    double value = resource.Value;
                    var resList = AvailableResources.FindAll(r => r.info.name == resource.Key);
                    value += VesselRequestList[resource.Key];
                    VesselRequestList[resource.Key] = 0;
                    double spareSpace = 0f;
                    ShareResource(resList, value, spareSpace);
                }
                catch
                {
                    Debug.Log("SimpleLogistivs - Exc" + resource.Key);
                }
            }
        }
        #endregion
        private void InventoryVessel(Vessel vessel)
        {   // vessel lists use common keys
            vesselAmount.Clear();
            vesselMaxAmount.Clear();
            foreach (Part part in vessel.parts)
            {
                foreach (var resource in part.Resources)
                {   // we only care about resource that can be transferred
                    if (resource.info.resourceTransferMode == ResourceTransferMode.NONE ||
                           resource.info.resourceFlowMode == ResourceFlowMode.NO_FLOW ||
                           resource._flowMode == PartResource.FlowMode.None || !resource._flowState)
                        continue;

                    if (vesselAmount.ContainsKey(resource.info.name))
                        vesselAmount[resource.info.name] += resource.amount;
                    else
                        vesselAmount.Add(resource.info.name, resource.amount);

                    if (vesselMaxAmount.ContainsKey(resource.info.name))
                        vesselMaxAmount[resource.info.name] += resource.maxAmount;
                    else
                        vesselMaxAmount.Add(resource.info.name, resource.maxAmount);

                    if (!VesselRequestList.ContainsKey(resource.info.name))
                        VesselRequestList.Add(resource.info.name, 0);
                }
            }
        }

        private void InventoryNetwork()
        {
            emptySpacePool.Clear();
            resourcePool.Clear();
            partResources.Clear();
            NetworkCount = 0;
            NetworkActive = false; // turn off network
            foreach (Vessel vessel in FlightGlobals.VesselsLoaded)
            {   // situation valid
                if (!(vessel.situation == Vessel.Situations.LANDED ||
                      vessel.situation == Vessel.Situations.SPLASHED ||
                      vessel.situation == Vessel.Situations.PRELAUNCH))
                    continue;
                // module activated
                LogisticsModule lm = vessel.FindPartModuleImplementing<LogisticsModule>();
                if (lm != null && !lm.IsActive)
                    continue;
                // resource and parts allow transfer
                foreach (Part part in vessel.parts)
                {
                    if (part.State == PartStates.DEAD)
                        continue;
                    foreach (PartResource resource in part.Resources)
                    {
                        if (resource.info.resourceTransferMode == ResourceTransferMode.NONE ||
                            resource.info.resourceFlowMode == ResourceFlowMode.NO_FLOW ||
                            resource._flowMode == PartResource.FlowMode.None || !resource._flowState)
                            continue;
                        partResources.Add(resource);
                    }
                }
                NetworkCount++;
                NetworkActive = true; // Got through checks above, so network is active
            }

            if (NetworkActive)
            {
                foreach (var resource in partResources)
                {
                    if (!resourcePool.ContainsKey(resource.info.name))
                    {
                        resourcePool.Add(resource.info.name, resource.amount);
                        emptySpacePool.Add(resource.info.name, resource.maxAmount - resource.amount);
                    }
                    else
                    {
                        resourcePool[resource.info.name] += resource.amount;
                        emptySpacePool[resource.info.name] += resource.maxAmount - resource.amount;
                    }
                }
            }
        }
    }
}
