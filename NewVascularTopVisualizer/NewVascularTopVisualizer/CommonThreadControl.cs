using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Fusion;
using Fusion.Development;
using System.Windows.Forms;
using Fusion.Mathematics;
using Fusion.Content;
using Fusion.Graphics;

namespace NewVascularTopVisualizer
{
    public enum MeshStyle
    {
        MSTYLE_POINTS, MSTYLE_LINES
    }

    public class CommonThreadsData
    {
        private VascularNet vnet;
        private bool vnetLoaded;
        private bool vnetFixed;
        private bool vnetChanged;
        private Mutex vnetAccessMutex;
        private MovementSettings movSettings;
        private bool movSettingsChanged;
        private Mutex movSettingsAccessMutex;
        private MeshStyle style;
        private bool styleChanged;
        private Mutex styleAccessMutex;

        private Node selectedNode;
        private int switchSelectionTo;
        private int switchSecondSelectionTo;
        private bool selectionChanged;
        private Node secondSelectedNode;
        private Mutex selectedNodeAccessMutex;

        private Vector3 netCenter;
        private bool netCenterChanged;
        private Mutex netCenterAccessMutex;

        private float selectionTolerance;
        private bool selectionToleranceChanged;
        private Mutex selectionToleranceAccessMutex;

        private ContentManager contentManager;
        private Scene scene;
        private Mutex sceneAccessMutex;

        private Vector3 storedVector;

        public CommonThreadsData()
        {
            vnet = new VascularNet("VnetNameStub");
            vnetLoaded = false;
            vnetFixed = false;
            vnetChanged = false;
            vnetAccessMutex = new Mutex();
            movSettings = new MovementSettings();
            movSettingsChanged = false;
            movSettingsAccessMutex = new Mutex();
            style = MeshStyle.MSTYLE_POINTS;
            styleChanged = false;
            styleAccessMutex = new Mutex();
            selectedNode = null;
            secondSelectedNode = null;
            switchSelectionTo = -1;
            switchSecondSelectionTo = -1;
            selectionChanged = false;
            selectedNodeAccessMutex = new Mutex();

            netCenter = Vector3.Zero;
            netCenterChanged = false;
            netCenterAccessMutex = new Mutex();

            selectionTolerance = 0.1f;
            selectionToleranceChanged = false;
            selectionToleranceAccessMutex = new Mutex();

            contentManager = null;
            scene = null;
            sceneAccessMutex = new Mutex();

            storedVector = Vector3.Zero;
        }

        ~CommonThreadsData()
        {
            vnetAccessMutex.Dispose();
            movSettingsAccessMutex.Dispose();
            styleAccessMutex.Dispose();
            selectedNodeAccessMutex.Dispose();
            netCenterAccessMutex.Dispose();
            selectionToleranceAccessMutex.Dispose();
        }

        public VascularNet Vnet
        {
            get
            {
                return vnet;
            }
            set
            {
                vnet = value;
            }
        }

        public bool VnetLoaded
        {
            get
            {
                return vnetLoaded;
            }
            set
            {
                vnetLoaded = value;
            }
        }

        public bool VnetFixed
        {
            get
            {
                return vnetFixed;
            }
            set
            {
                vnetFixed = value;
            }
        }

        public bool VnetChanged
        {
            get
            {
                return vnetChanged;
            }
            set
            {
                vnetChanged = value;
            }
        }

        public MovementSettings MovSettings
        {
            get
            {
                return movSettings;
            }
            set
            {
                movSettings = value;
            }
        }

        public bool MovSettingsChanged
        {
            get
            {
                return movSettingsChanged;
            }
            set
            {
                movSettingsChanged = value;
            }
        }

        public MeshStyle Style
        {
            get
            {
                return style;
            }
            set
            {
                style = value;
            }
        }

        public bool StyleChanged
        {
            get
            {
                return styleChanged;
            }
            set
            {
                styleChanged = value;
            }
        }

        public Node SelectedNode
        {
            get
            {
                return selectedNode;
            }
            set
            {
                selectedNode = value;
            }
        }

        public Node SecondSelectedNode
        {
            get
            {
                return secondSelectedNode;
            }
            set
            {
                secondSelectedNode = value;
            }
        }

        public int SwitchSelectionTo
        {

            get
            {
                return switchSelectionTo;
            }
            set
            {
                switchSelectionTo = value;
            }
        }

        public int SwitchSecondSelectionTo
        {

            get
            {
                return switchSecondSelectionTo;
            }
            set
            {
                switchSecondSelectionTo = value;
            }
        }

        public bool SelectionChanged
        {
            get
            {
                return selectionChanged;
            }
            set
            {
                selectionChanged = value;
            }
        }

        public Vector3 NetCenter
        {
            get
            {
                return netCenter;
            }
            set
            {
                netCenter = value;
            }
        }

        public bool NetCenterChanged
        {
            get
            {
                return netCenterChanged;
            }
            set
            {
                netCenterChanged = value;
            }
        }

        public float SelectionTolerance
        {
            get
            {
                return selectionTolerance;
            }
            set
            {
                selectionTolerance = value;
            }
        }

        public bool SelectionToleranceChanged
        {
            get
            {
                return selectionToleranceChanged;
            }
            set
            {
                selectionToleranceChanged = value;
            }
        }

        public Mutex VnetAccessMutex
        {
            get
            {
                return vnetAccessMutex;
            }
        }

        public Mutex MovSettingsAccessMutex
        {
            get
            {
                return movSettingsAccessMutex;
            }
        }

        public Mutex StyleAccessMutex
        {
            get
            {
                return styleAccessMutex;
            }
        }

        public Mutex SelectedNodeAccessMutex
        {
            get
            {
                return selectedNodeAccessMutex;
            }
        }

        public Mutex NetCenterAccessMutex
        {
            get
            {
                return netCenterAccessMutex;
            }
        }

        public Mutex SelectionToleranceAccessMutex
        {
            get
            {
                return selectionToleranceAccessMutex;
            }
        }

        public Mutex SceneAccessMutex
        {
            get
            {
                return sceneAccessMutex;
            }
        }

        public ContentManager ContentManager
        {
            get
            {
                return contentManager;
            }
            set
            {
                contentManager = value;
            }
        }

        public Scene Scene
        {
            get
            {
                return scene;
            }
            set
            {
                scene = value;
            }
        }

        public Vector3 StoredVector
        {
            get
            {
                return storedVector;
            }
            set
            {
                storedVector = value;
            }
        }
    }

    class CommonThreadControl
    {

        public static void RunVisualizer(Object commonData)
        {
            String [] argsStub = new String[0];
            using (var game = new NewVascularTopVisualizer((CommonThreadsData)commonData))
            {
                if (DevCon.Prepare(game, @"..\..\..\Content\Content.xml", "Content"))
                {
                    ((CommonThreadsData)commonData).ContentManager = game.Content;
                    game.Parameters.Height = 700;
                    game.Parameters.Width = 700;
                    game.Run(argsStub);
                }
            }
        }

        public static void RunControlForm(Object commonData)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new VascularTopVisualizerControl((CommonThreadsData)commonData));
        }
    }
}
