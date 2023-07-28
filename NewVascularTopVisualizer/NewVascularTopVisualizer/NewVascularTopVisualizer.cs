using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Mathematics;
using Fusion.Graphics;
using Fusion.Audio;
using Fusion.Input;
using Fusion.Content;
using Fusion.Development;
using System.Runtime.InteropServices;

namespace NewVascularTopVisualizer
{

    public struct MovementSettings
    {
        public float rot_x_speed;
        public float rot_y_speed;
        public float scale_speed;
        public float x_pos_speed;
        public float y_pos_speed;
        public float z_pos_speed;
    }

    class Palette
    {
        public Palette(float min, float max)
        {
            min_value = min;
            max_value = max;
        }

        public Color getColor(float v)
        {
            Color3 result_color = new Color3(1, 1, 1);
            
            float hue = 0.0f;
            if (max_value > min_value)
                hue = (v - min_value) / (max_value - min_value) * 270;
            else
                hue = 0;
            int i = (int)(hue / 60.0);

            float V_min = 0;
            float V = 100;
            float a = (V - V_min) * (hue % 60) / 60;
            float V_inc = V_min + a;
            float V_dec = V - a;

            if (i == 0)
            {
                result_color.Red = V;
                result_color.Green = V_inc;
                result_color.Blue = V_min;
            }
            if (i == 1)
            {
                result_color.Red = V_dec;
                result_color.Green = V;
                result_color.Blue = V_min;
            }

            if (i == 2)
            {
                result_color.Red = V_min;
                result_color.Green = V;
                result_color.Blue = V_inc;
            }

            if (i == 3)
            {
                result_color.Red = V_min;
                result_color.Green = V_dec;
                result_color.Blue = V;
            }

            if (i == 4)
            {
                result_color.Red = V_inc;
                result_color.Green = V_min;
                result_color.Blue = V;
            }

            if (i == 5)
            {
                result_color.Red = V;
                result_color.Green = V_min;
                result_color.Blue = V_dec;
            }

            result_color.Red = result_color.Red / 100;
            result_color.Green = result_color.Green / 100;
            result_color.Blue = result_color.Blue / 100;
            Color c = (Color)result_color;
            c.A = 100;

            return c;
        }

        public float min_value { get; set; }
        public float max_value { get; set; }
    }

    public class NewVascularTopVisualizer : Game
    {
        [StructLayout(LayoutKind.Explicit)]
        struct ConstData
        {
            [FieldOffset(0)]
            public Matrix Transform;
        }
        
        enum UberFlags
        {
            None,
        }
        

        protected struct Vertex
        {
            [Vertex("POSITION")]
            public Vector3 Position;
            [Vertex("NORMAL")]
            public Vector3 Normal;
            [Vertex("TEXCOORD")]
            public Vector2 TexCoord;
            [Vertex("COLOR")]
            public Color Color;

            public void Transform(Matrix m)
            {
                Position = Vector3.TransformCoordinate(Position, m);
                Normal = Vector3.TransformNormal(Normal, m);
            }
        }


        private CommonThreadsData commonData;
        private Palette palette;

        private VertexBuffer vb;
        private IndexBuffer ib;

        private Ubershader us;
        private ConstantBuffer cb;

        private StateFactory linesFactory;
        private StateFactory pointsFactory;
        private StateFactory currentFactory;

        private List<int> indices = new List<int>();
        private List<Vertex> vertices = new List<Vertex>();
        private List<Node> relatedNodes = new List<Node>();

        private int verticesCount;
        private int indicesCount;

        private float rot_x;
        private float rot_y;
        private float scale;
        private float x_pos;
        private float y_pos;
        private float z_pos;

        private Vector2 lastMousePosition;
        private int selectedVertexId;
        private int secondSelectedVertexId;
        private bool tailSelected;

        private MovementSettings movSettingsActual;
        
        private bool performDraw;
        private int drawCircles;
        private int drawSecondCircles;

        private int drawCenterCircles;
        private bool drawCenterStatic;

        private Vector3 netCenter;
        private Color centerColor;
        private Color selectionColor;
        private Color secondSelectionColor;
        private Color toBeDeletedColor;
        private Color hidedColor;

        private bool showHelp;
        private bool silentMode;

        private const int MAX_DRAW_CIRCLES = 20;
        private const float CIRCLE_RADIUS_STEP = 0.2f;
        private const float SMALL_CIRCLE_RADIUS = 0.05f;
        private const float SUPER_SMALL_CIRCLE_RADIUS = 0.005f;
        private const int CIRCLE_POINTS_COUNT_MULT = 20;
        private const float HARD_SELECTION_TOLERANCE = 1e-5f;

        private const float ROT_X_DEF = 0.0f;
        private const float ROT_Y_DEF = 0.0f;
        private const float SCALE_DEF = 1.0f;
        private const float X_POS_DEF = 0.0f;
        private const float Y_POS_DEF = 0.0f;
        private const float Z_POS_DEF = 0.0f;

        private List<int> indicesCircle = new List<int>();
        private List<Vertex> verticesCircle = new List<Vertex>();
        private List<int> indicesSmallCircle = new List<int>();
        private List<Vertex> verticesSmallCircle = new List<Vertex>();
        private List<int> indicesUSmallCircle = new List<int>();
        private List<Vertex> verticesUSmallCircle = new List<Vertex>();
        private List<int> indicesCircle2 = new List<int>();
        private List<Vertex> verticesCircle2 = new List<Vertex>();
        private List<int> indicesSmallCircle2 = new List<int>();
        private List<Vertex> verticesSmallCircle2 = new List<Vertex>();
        private List<int> indicesUSmallCircle2 = new List<int>();
        private List<Vertex> verticesUSmallCircle2 = new List<Vertex>();
        private List<int> indicesCenterCircle = new List<int>();
        private List<Vertex> verticesCenterCircle = new List<Vertex>();
        private List<int> indicesRunningCenterCircle = new List<int>();
        private List<Vertex> verticesRunningCenterCircle = new List<Vertex>();
        private VertexBuffer vbc;
        private IndexBuffer ibc;
        private VertexBuffer vbsc;
        private IndexBuffer ibsc;
        private VertexBuffer vbusc;
        private IndexBuffer ibusc;
        private VertexBuffer vbc2;
        private IndexBuffer ibc2;
        private VertexBuffer vbsc2;
        private IndexBuffer ibsc2;
        private VertexBuffer vbusc2;
        private IndexBuffer ibusc2;
        private VertexBuffer vbcc;
        private IndexBuffer ibcc;
        private VertexBuffer vbrcc;
        private IndexBuffer ibrcc;

        private Vertex[] vacenter;
        private VertexBuffer vbcenter;

        //private int[] indLine;
        //private IndexBuffer indL;
        //private Vertex[] vertLine;
        //private VertexBuffer vertL;

        private int nearestVertexId;
        private ConstData cdata;
        private float selectionTolerance;

        private VascularThread selectedThread;
        private bool threadSelected;

        private string errorMessage;

        private Node lastClonedFrom;
        private Node lastClonedTo;

        /// <summary>
        /// NewVascularTopVisualizer constructor
        /// </summary>
        public NewVascularTopVisualizer(CommonThreadsData _commonData)
            : base()
        {
            //	enable object tracking :
            Parameters.TrackObjects = true;

            //	uncomment to enable debug graphics device:
            //	(MS Platform SDK must be installed)
            //	Parameters.UseDebugDevice	=	true;

            //	add services :
            AddService(new SpriteBatch(this), false, false, 0, 0);
            AddService(new DebugStrings(this), true, true, 9999, 9999);
            AddService(new DebugRender(this), true, true, 9998, 9998);

            //	add here additional services :

            //	load configuration for each service :
            LoadConfiguration();

            //	make configuration saved on exit :
            Exiting += Game_Exiting;

            

            commonData = _commonData;
            palette = new Palette(float.MinValue, float.MaxValue);
           
            rot_x = ROT_X_DEF;
            rot_y = ROT_Y_DEF;
            scale = SCALE_DEF;
            x_pos = X_POS_DEF;
            y_pos = Y_POS_DEF;
            z_pos = Z_POS_DEF;

            performDraw = false;

            lastMousePosition = Vector2.Zero;
            selectedVertexId = -1;
            secondSelectedVertexId = -1;
            verticesCount = 0;
            indicesCount = 0;
            drawCircles = -1;
            drawSecondCircles = -1;
            drawCenterCircles = -1;
            drawCenterStatic = true;
            netCenter = Vector3.Zero;
            centerColor = Color.Red;
            selectionColor = Color.White;
            secondSelectionColor = Color.Yellow;
            hidedColor = Color.Black;

            toBeDeletedColor = Color.LightGray;

            threadSelected = false;
            tailSelected = false;
            showHelp = true;
            silentMode = false;
            errorMessage = "";

            lastClonedFrom = null;
            lastClonedTo = null;
        }

        private void SelectTail()
        {
            if (tailSelected)
                ClearTailSelection();
            if ((selectedVertexId != -1) && (secondSelectedVertexId != -1) &&
                (selectedVertexId != secondSelectedVertexId))
            {
                commonData.VnetAccessMutex.WaitOne();

                commonData.Vnet.selectTail(relatedNodes[selectedVertexId], relatedNodes[secondSelectedVertexId]);

                commonData.VnetAccessMutex.ReleaseMutex();

                tailSelected = true;

                UpdateColors();
            }
        }

        private void ClearTailSelection()
        {

            commonData.VnetAccessMutex.WaitOne();

            commonData.Vnet.clearTailSelection();

            commonData.VnetAccessMutex.ReleaseMutex();

            tailSelected = false;
            threadSelected = false;

            UpdateColors();
        }

        private void DeleteSelectedTail()
        {
            if (tailSelected)
            {
                commonData.VnetAccessMutex.WaitOne();

                commonData.Vnet.deleteSelectedTail(relatedNodes[selectedVertexId], relatedNodes[secondSelectedVertexId]);

                commonData.VnetChanged = true;

                commonData.VnetAccessMutex.ReleaseMutex();

                ReloadData();
            }
        }

        private void MergeSelectedTail()
        {
            if (tailSelected)
            {
                commonData.VnetAccessMutex.WaitOne();

                List<Node> except = new List<Node>();
                except.Add(relatedNodes[secondSelectedVertexId]);
                commonData.Vnet.mergeSelectedTail(relatedNodes[selectedVertexId], except);

                commonData.VnetChanged = true;

                commonData.VnetAccessMutex.ReleaseMutex();

                ReloadData();
            }
        }

        /// <summary>
        /// Initializes game :
        /// </summary>
        protected override void Initialize()
        {
            //	initialize services :
            base.Initialize();

            //	add keyboard handler :
            InputDevice.KeyDown += InputDevice_KeyDown;
            InputDevice.KeyUp += InputDevice_KeyUp;
            InputDevice.MouseMove += InputDevice_MouseMove;
            InputDevice.MouseScroll += InputDevice_MouseScroll;

            GraphicsDevice device = GraphicsDevice;

            //	load content & create graphics and audio resources here:
            cb = new ConstantBuffer(device, typeof(ConstData));
            us = Content.Load<Ubershader>("render.hlsl");
            linesFactory = new StateFactory(us, typeof(UberFlags), Primitive.LineList, VertexInputElement.FromStructure(typeof(Vertex)));
            pointsFactory = new StateFactory(us, typeof(UberFlags), Primitive.PointList, VertexInputElement.FromStructure(typeof(Vertex)));
            currentFactory = pointsFactory;

            nearestVertexId = -1;
            selectionTolerance = 0.1f;

            vacenter = new Vertex[1];
            vbcenter = new VertexBuffer(device, typeof(Vertex), 1);

            //vertLine = new Vertex[2];
            //vertL = new VertexBuffer(device, typeof(Vertex), 2);
            //indLine = new int[2];
            //indL = new IndexBuffer(device, 2);
            //indLine[0] = 0;
            //indLine[1] = 1;
            //indL.SetData(indLine);
            ReloadData();
        }

        protected void UpdateVB()
        {
            if (vb != null)
                SafeDispose(ref vb);
            vb = new VertexBuffer(GraphicsDevice, typeof(Vertex), verticesCount);
            vb.SetData(vertices.ToArray(), 0, verticesCount);

            vbcenter.SetData(vacenter, 0, 1);
        }

        protected void UpdateIB()
        {
            if (ib != null)
                SafeDispose(ref ib);
            ib = new IndexBuffer(GraphicsDevice, indicesCount);
            ib.SetData(indices.ToArray(), 0, indicesCount);
        }

        protected void UpdatePalette(List<Node> nodes)
        {
            float min = float.MaxValue;
            float max = float.MinValue;

            foreach (Node node in nodes)
            {
                if (node.ValueF > max)
                {
                    max = node.ValueF;
                }
                if (node.ValueF < min)
                {
                    min = node.ValueF;
                }
            }

            if (max >= min)
            {
                palette.max_value = max;
                palette.min_value = min;
            }
        }

        protected void UpdateScene(List<Node> nodes)
        {
            vertices.Clear();
            indices.Clear();
            relatedNodes.Clear();

            foreach (Node node in nodes)
                {
                    Vertex v = new Vertex();
                    v.Position  = node.Position - netCenter;
                    v.Color     = palette.getColor(node.ValueF);

                    vertices.Add(v);
                    relatedNodes.Add(node);
                    node.setId(vertices.Count - 1);
                }

            foreach (Node node in nodes)
                foreach(Node n in node.getNeighbours())
                    {
                        indices.Add(node.getId());
                        indices.Add(n.getId());
                    }

            verticesCount = vertices.Count;
            indicesCount = indices.Count;
            selectedVertexId = -1;
            secondSelectedVertexId = -1;

            vacenter[0].Position = -netCenter;
            vacenter[0].Color = centerColor;

            UpdateVB();
            UpdateIB();
        }

        protected void UpdateColors()
        {
            Vertex v;
            for (int i = 0; i < verticesCount; i++)
            {
                v = vertices[i];

                if (selectedVertexId == i)
                {
                    v.Color = selectionColor;
                    vertices[i] = v;
                    continue;
                }
                if (secondSelectedVertexId == i)
                {
                    v.Color = secondSelectionColor;
                    vertices[i] = v;
                    continue;
                }

                if (!relatedNodes[i].SelectedToShow)
                {
                    v.Color = hidedColor;
                    vertices[i] = v;
                    continue;
                }

                if (relatedNodes[i].TailSelectionFlag)
                {
                    v.Color = toBeDeletedColor;
                    vertices[i] = v;
                    continue;
                }

                v.Color = palette.getColor(relatedNodes[i].ValueF);
                vertices[i] = v;
            }

            UpdateVB();
        }

        protected void ReloadData()
        {
            commonData.VnetAccessMutex.WaitOne();

            if (commonData.VnetLoaded && commonData.VnetChanged)
            {
                netCenter = commonData.Vnet.getCenter();

                List<Node> nodes = commonData.Vnet.Nodes;
                UpdatePalette(nodes);
                UpdateScene(nodes);

                commonData.VnetChanged = false;

                commonData.VnetAccessMutex.ReleaseMutex();

                UpdateVB();
                UpdateIB();

                performDraw = true;
            }
            else
            {
                commonData.VnetAccessMutex.ReleaseMutex();
            }

            commonData.MovSettingsAccessMutex.WaitOne();

            if (commonData.MovSettingsChanged)
            {
                movSettingsActual = commonData.MovSettings;
                commonData.MovSettingsChanged = false;
            }

            commonData.MovSettingsAccessMutex.ReleaseMutex();

            commonData.SelectionToleranceAccessMutex.WaitOne();

            if (commonData.SelectionToleranceChanged)
            {
                selectionTolerance = commonData.SelectionTolerance;
                commonData.SelectionToleranceChanged = false;
            }

            commonData.SelectionToleranceAccessMutex.ReleaseMutex();
        }

        protected void ShareSelection()
        {
            commonData.SelectedNodeAccessMutex.WaitOne();

            if (selectedVertexId == -1)
            {
                commonData.SelectedNode = null;
            }
            else
            {
                commonData.SelectedNode = relatedNodes[selectedVertexId];
            } 
            
            if (secondSelectedVertexId == -1)
            {
                commonData.SecondSelectedNode = null;
            }
            else
            {
                commonData.SecondSelectedNode = relatedNodes[secondSelectedVertexId];
            }

            commonData.SelectedNodeAccessMutex.ReleaseMutex();
        }

        //protected float GetDistanceFromPointToLine(Vector3 point, Vector3 pointOnLine, Vector3 directingVector)
        //{
        //    return Vector3.Multiply((pointOnLine - point), directingVector).Length() / directingVector.Length();
        //}

        /// <summary>
        /// Handle keys
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void InputDevice_KeyDown(object sender, Fusion.Input.InputDevice.KeyEventArgs e)
        {
            if (e.Key == Keys.F1)
            {
                DevCon.Show(this);
            }

            if (e.Key == Keys.F2)
            {
                silentMode = !silentMode;
            }

            if (e.Key == Keys.F5)
            {
                ReloadData();
            }

            if (e.Key == Keys.F6)
            {
                ShareSelection();
            }

            if (e.Key == Keys.F12)
            {
                GraphicsDevice.Screenshot();
            }

            if (e.Key == Keys.Escape)
            {
                Exit();
            }

            if (e.Key == Keys.Space)
            {
                if (InputDevice.IsKeyDown(Keys.LeftAlt) || InputDevice.IsKeyDown(Keys.RightAlt))
                    if (selectedVertexId != -1)
                        drawCircles = 1;
                if (InputDevice.IsKeyDown(Keys.LeftControl) || InputDevice.IsKeyDown(Keys.RightControl))
                    if (secondSelectedVertexId != -1)
                        drawSecondCircles = 1;
            }

            if (e.Key == Keys.O)
            {
                if (InputDevice.IsKeyDown(Keys.LeftControl) || InputDevice.IsKeyDown(Keys.RightControl))
                {
                    drawCenterStatic = !drawCenterStatic;
                }
                drawCenterCircles = 1;
            }

            if (e.Key == Keys.B)
            {
                if (secondSelectionColor.Equals(Color.Yellow))
                {
                    secondSelectionColor = Color.Blue;
                }
                else
                {
                    secondSelectionColor = Color.Yellow;
                }
            }

            if (e.Key == Keys.C)
            {
                if ((selectedVertexId != -1) && (secondSelectedVertexId != -1) &&
                    (selectedVertexId != secondSelectedVertexId))
                {
                    commonData.VnetAccessMutex.WaitOne();

                    if (relatedNodes[selectedVertexId].getNeighbours().Contains(relatedNodes[secondSelectedVertexId]))
                    {
                        commonData.Vnet.RemoveConnection(relatedNodes[selectedVertexId].getId(),
                            relatedNodes[secondSelectedVertexId].getId());
                    }
                    else
                    {
                        commonData.Vnet.AddConnection(relatedNodes[selectedVertexId].getId(),
                            relatedNodes[secondSelectedVertexId].getId());
                    }

                    commonData.VnetChanged = true;

                    commonData.VnetAccessMutex.ReleaseMutex();

                    ReloadData();
                }
            }

            if (e.Key == Keys.F)
            {
                commonData.SceneAccessMutex.WaitOne();

                try
                {
                    commonData.Scene = Content.Load<Scene>("fbxscene");
                    // DEBUG FBX PARSING
                    errorMessage = "Scene loaded. "; //+ commonData.Scene.Meshes[commonData.Scene.Nodes[1].MeshIndex].VertexCount.ToString();
                }
                catch (Exception ex)
                {
                    errorMessage = ex.Message;
                }

                commonData.SceneAccessMutex.ReleaseMutex();
            }

            if (e.Key == Keys.Q)
            {
                ClearTailSelection();
                UpdateColors();
            }

            if (e.Key == Keys.S)
            {
                if (selectedVertexId != -1)
                {
                    Node n = relatedNodes[selectedVertexId];
                    if (n.getNeighbours().Count > 2)
                    {
                        n.TailSelectionFlag = !n.TailSelectionFlag;
                    }
                    else if (n.getNeighbours().Count == 2)
                    {
                        bool value = !n.TailSelectionFlag;
                        commonData.Vnet.getThread(n, n.getNeighbours()[0]).setSelectionFlag(value);
                        commonData.Vnet.getThread(n, n.getNeighbours()[1]).setSelectionFlag(value);
                    }
                    else if (n.getNeighbours().Count == 1)
                    {
                        bool value = !n.TailSelectionFlag;
                        commonData.Vnet.getThread(n, n.getNeighbours()[0]).setSelectionFlag(value);
                    }
                    UpdateColors();
                }
            }

            if (e.Key == Keys.OemMinus)
            {
                commonData.Vnet.deleteSelected();
                commonData.VnetChanged = true;

                ReloadData();
            }
            if (e.Key == Keys.T)
            {
                if ((selectedVertexId != -1) && (secondSelectedVertexId != -1) &&
                    (selectedVertexId != secondSelectedVertexId))
                {
                    Node nodeToMove = null;
                    TreeNode moveFrom = null;
                    TreeNode moveTo = null;
                    if (InputDevice.IsKeyDown(Keys.LeftAlt) || InputDevice.IsKeyDown(Keys.RightAlt))
                    {
                        nodeToMove = relatedNodes[selectedVertexId];
                        moveFrom = relatedNodes[selectedVertexId].StructureLeafContainer;
                        moveTo = relatedNodes[secondSelectedVertexId].StructureLeafContainer;
                    }
                    if (InputDevice.IsKeyDown(Keys.LeftControl) || InputDevice.IsKeyDown(Keys.RightControl))
                    {
                        nodeToMove = relatedNodes[secondSelectedVertexId];
                        moveFrom = relatedNodes[secondSelectedVertexId].StructureLeafContainer;
                        moveTo = relatedNodes[selectedVertexId].StructureLeafContainer;
                    }
                    if (nodeToMove != null)
                    {
                        nodeToMove.StructureLeafContainer = moveTo;
                        moveFrom.Nodes1D.Remove(nodeToMove);
                        moveTo.Nodes1D.Add(nodeToMove);
                    }
                }
                if (!(InputDevice.IsKeyDown(Keys.LeftAlt) || InputDevice.IsKeyDown(Keys.RightAlt) ||
                    InputDevice.IsKeyDown(Keys.LeftControl) || InputDevice.IsKeyDown(Keys.RightControl)))
                    SelectTail();
            }

            if (e.Key == Keys.R)
            {
                if (selectedVertexId != -1)
                {
                    commonData.VnetAccessMutex.WaitOne();

                    relatedNodes[selectedVertexId].setRadiusToMean();
                    commonData.VnetChanged = true;

                    commonData.VnetAccessMutex.ReleaseMutex();
                    ReloadData();
                }
            }

            if (e.Key == Keys.X)
            {
                if (selectedVertexId != -1)
                {
                    commonData.VnetAccessMutex.WaitOne();

                    if (commonData.Vnet.removeNode(relatedNodes[selectedVertexId]))
                    {
                        commonData.VnetChanged = true;
                    }

                    commonData.VnetAccessMutex.ReleaseMutex();

                    ReloadData();
                }
            }

            if (e.Key == Keys.N)
            {
                /////////////////////////////////
                // BEWARE: A0 linear, not radius!
                if ((selectedVertexId != -1) && (secondSelectedVertexId != -1))
                {
                    commonData.VnetAccessMutex.WaitOne();

                    Node sn1 = relatedNodes[selectedVertexId];
                    Node sn2 = relatedNodes[secondSelectedVertexId];

                    int newId = commonData.Vnet.NodesCount;
                    Vector3 newPosition = (sn1.Position + sn2.Position) / 2.0f;
                    double newS0 = (sn1.getLumen_sq0() + sn2.getLumen_sq0()) / 2.0;

                    Node newNode = new Node(newId, newPosition, Math.Sqrt(newS0 / Math.PI));

                    commonData.Vnet.Nodes.Add(newNode);

                    if (sn1.getNeighbours().Contains(sn2))
                    {
                        commonData.Vnet.RemoveConnection(selectedVertexId, secondSelectedVertexId);
                        commonData.Vnet.AddConnection(selectedVertexId, newId);
                        commonData.Vnet.AddConnection(newId, secondSelectedVertexId);
                    }

                    commonData.VnetChanged = true;

                    commonData.VnetAccessMutex.ReleaseMutex();
                    ReloadData();
                }
            }

            if (e.Key == Keys.OemPeriod)
            {
                if ((selectedVertexId != -1) && (secondSelectedVertexId != -1))
                {
                    commonData.VnetAccessMutex.WaitOne();

                    Node sn1 = relatedNodes[selectedVertexId];
                    Node sn2 = relatedNodes[secondSelectedVertexId];

                    int newId = commonData.Vnet.NodesCount;
                    Vector3 newPosition = (sn1.Position + sn2.Position) / 2.0f;
                    double newS0 = Math.Min(sn1.getLumen_sq0(), sn2.getLumen_sq0());

                    Node newNode = new Node(newId, newPosition, Math.Sqrt(newS0 / Math.PI));

                    commonData.Vnet.Nodes.Add(newNode);

                    if (sn1.getNeighbours().Contains(sn2))
                    {
                        commonData.Vnet.RemoveConnection(selectedVertexId, secondSelectedVertexId);
                        commonData.Vnet.AddConnection(selectedVertexId, newId);
                        commonData.Vnet.AddConnection(newId, secondSelectedVertexId);
                    }

                    commonData.VnetChanged = true;

                    commonData.VnetAccessMutex.ReleaseMutex();
                    ReloadData();
                }
            }

            if (e.Key == Keys.OemSemicolon)
            {
                commonData.VnetAccessMutex.WaitOne();

                Node sn1 = relatedNodes[selectedVertexId];

                int newId = commonData.Vnet.NodesCount;

                Node newNode = new Node(newId, sn1.Position, sn1.Rad);

                if (lastClonedFrom != null)
                {
                    if (sn1.getNeighbours().Contains(lastClonedFrom))
                    {
                        newNode.addNeighbours(new Node[] { lastClonedTo });
                        lastClonedTo.addNeighbours(new Node[] { newNode });
                    }
                }

                lastClonedFrom = sn1;
                lastClonedTo = newNode;

                commonData.Vnet.Nodes.Add(newNode);

                commonData.VnetChanged = true;

                commonData.VnetAccessMutex.ReleaseMutex();
                ReloadData();
            }

            if (e.Key == Keys.L)
            {
                commonData.StyleAccessMutex.WaitOne();

                if (commonData.Style == MeshStyle.MSTYLE_LINES)
                {
                    commonData.Style = MeshStyle.MSTYLE_POINTS;
                }
                else
                {
                    commonData.Style = MeshStyle.MSTYLE_LINES;
                }

                commonData.StyleChanged = true;

                commonData.StyleAccessMutex.ReleaseMutex();
            }

            if (e.Key == Keys.H)
            {
                showHelp = !showHelp;
            }

            if (e.Key == Keys.D0)
            {
                rot_x = ROT_X_DEF;
                rot_y = ROT_Y_DEF;
                scale = SCALE_DEF;
                x_pos = X_POS_DEF;
                y_pos = Y_POS_DEF;
                z_pos = Z_POS_DEF;
            }

            if (e.Key == Keys.Y)
            {
                if (selectedVertexId == -1)
                    return;
                if (secondSelectedVertexId == -1)
                    return;
                
                commonData.VnetAccessMutex.WaitOne();

                selectedThread = commonData.Vnet.getThread(
                    relatedNodes[selectedVertexId], relatedNodes[secondSelectedVertexId]);
                commonData.Vnet.selectThread(selectedThread);
                relatedNodes[selectedVertexId].TailSelectionFlag = false;
                commonData.VnetAccessMutex.ReleaseMutex();

                threadSelected = true;
                tailSelected = true;

                UpdateColors();
            }

            if (e.Key == Keys.V)
            {
                if ((selectedVertexId != -1) && (secondSelectedVertexId != -1))
                {
                    commonData.StoredVector = relatedNodes[secondSelectedVertexId].Position - relatedNodes[selectedVertexId].Position;
                }
            }
        }

        int getNearestVertexId()
        {
            Vector2 mouseFloat2DC = new Vector2(
    ((float)InputDevice.MousePosition.X / GraphicsDevice.DisplayBounds.Width * 2.0f - 1.0f),
    (-(float)InputDevice.MousePosition.Y / GraphicsDevice.DisplayBounds.Height * 2.0f + 1.0f));
            Vector3 transf = Vector3.Zero;
            Vector2 transf2 = Vector2.Zero;
            float nearestDistance = 0.0f;
            nearestVertexId = -1;
            float distance = 0.0f;
            for (int i = 0; i < vertices.Count; i++)
            {
                transf = Vector3.TransformCoordinate(vertices[i].Position, cdata.Transform);
                transf2.X = transf.X;
                transf2.Y = transf.Y;
                distance = Vector2.Distance(mouseFloat2DC, transf2);
                if (distance < selectionTolerance)
                {
                    if (nearestVertexId == -1)
                    {
                        nearestVertexId = i;
                        nearestDistance = distance;
                    }
                    else
                    {
                        if (Math.Abs(distance - nearestDistance) < HARD_SELECTION_TOLERANCE)
                        {
                            if (vertices[nearestVertexId].Position.Z < vertices[i].Position.Z)
                            {
                                nearestVertexId = i;
                                nearestDistance = distance;
                            }
                            continue;
                        }
                        if (distance < nearestDistance)
                        {
                            nearestVertexId = i;
                            nearestDistance = distance;
                        }
                    }
                }
            }
            return nearestVertexId;
        }

        void InputDevice_KeyUp(object sender, InputDevice.KeyEventArgs e)
        {
            if (InputDevice.IsKeyDown(Keys.LeftAlt) || InputDevice.IsKeyDown(Keys.RightAlt))
            {
                if (e.Key == Keys.LeftButton)
                {
                    selectedVertexId = getNearestVertexId();
                    //ClearTailSelection();
                    UpdateColors();
                    ShareSelection();
                }
            }
            if (InputDevice.IsKeyDown(Keys.LeftControl) || InputDevice.IsKeyDown(Keys.RightControl))
            {
                if (e.Key == Keys.LeftButton)
                {
                    secondSelectedVertexId = getNearestVertexId();
                    //ClearTailSelection();
                    UpdateColors();
                    ShareSelection();
                }
            }
        }

        void InputDevice_MouseMove(object sender, InputDevice.MouseMoveEventArgs e)
        {
            Vector2 shift = e.Position - lastMousePosition;

            if (InputDevice.IsKeyDown(Keys.MiddleButton))
            {
                // Move
                float scaleFactor = 1.0f;
                if (Math.Abs(z_pos) > 1.0)
                {
                    if (z_pos > 0.0)
                    {
                        scaleFactor = Math.Abs(z_pos);
                    }
                    else
                    {
                        scaleFactor = (float)1.0 / Math.Abs(z_pos);
                    }
                }
                x_pos -= shift.X * movSettingsActual.x_pos_speed * scaleFactor;// / scale;
                y_pos += shift.Y * movSettingsActual.y_pos_speed * scaleFactor;// / scale;
            }

            if (InputDevice.IsKeyDown(Keys.RightButton))
            {
                // Rotate
                rot_x += shift.Y * movSettingsActual.rot_x_speed;
                rot_y += shift.X * movSettingsActual.rot_y_speed;
            }

            lastMousePosition = e.Position;
        }

        void InputDevice_MouseScroll(object sender, InputDevice.MouseScrollEventArgs e)
        {
            int step = 1;
            if (InputDevice.IsKeyDown(Keys.D1))
                step *= 10;
            if (InputDevice.IsKeyDown(Keys.D2))
                step *= 100;
            if (InputDevice.IsKeyDown(Keys.LeftAlt) || InputDevice.IsKeyDown(Keys.RightAlt))
            {
                // Selection changing.
                if (e.WheelDelta > 0)
                {
                    selectedVertexId = (selectedVertexId + step) % verticesCount;
                }
                else
                {
                    selectedVertexId -= step;
                    while (selectedVertexId < 0)
                        selectedVertexId += verticesCount;
                    selectedVertexId %= verticesCount;
                }
                ClearTailSelection();
                UpdateColors();
                ShareSelection();
                return;
            }
            if (InputDevice.IsKeyDown(Keys.LeftControl) || InputDevice.IsKeyDown(Keys.RightControl))
            {
                // Selection changing.
                if (e.WheelDelta > 0)
                {
                    secondSelectedVertexId = (secondSelectedVertexId + step) % verticesCount;
                }
                else
                {
                    secondSelectedVertexId -= step;
                    while (secondSelectedVertexId < 0)
                        secondSelectedVertexId += verticesCount;
                    secondSelectedVertexId %= verticesCount;
                }
                ClearTailSelection();
                UpdateColors();
                ShareSelection();
                return;
            }

            //if (InputDevice.IsKeyDown(Keys.LeftButton))
            //{
            //    // Moving (z).
            //    z_pos += e.WheelDelta * movSettingsActual.z_pos_speed;
            //}
            //else
            //{
            //    // Scaling.
            //    scale *= (float)Math.Pow(10.0f, e.WheelDelta * movSettingsActual.scale_speed);
            //}
            
            // Moving (z).
            z_pos += e.WheelDelta * movSettingsActual.z_pos_speed;
        }

        /// <summary>
        /// Disposes game
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                //	dispose disposable stuff here
                //	Do NOT dispose objects loaded using ContentManager.
                if (ib != null)
                    SafeDispose(ref ib);
                if (vb != null)
                    SafeDispose(ref vb);
                if (ibc != null)
                    SafeDispose(ref ibc);
                if (vbc != null)
                    SafeDispose(ref vbc);
                if (ibsc != null)
                    SafeDispose(ref ibsc);
                if (vbsc != null)
                    SafeDispose(ref vbsc);
                if (ibusc != null)
                    SafeDispose(ref ibusc);
                if (vbusc != null)
                    SafeDispose(ref vbusc);
                if (ibc2 != null)
                    SafeDispose(ref ibc2);
                if (vbc2 != null)
                    SafeDispose(ref vbc2);
                if (ibsc2 != null)
                    SafeDispose(ref ibsc2);
                if (vbsc2 != null)
                    SafeDispose(ref vbsc2);
                if (ibusc2 != null)
                    SafeDispose(ref ibusc2);
                if (vbusc2 != null)
                    SafeDispose(ref vbusc2);
                if (ibcc != null)
                    SafeDispose(ref ibcc);
                if (vbcc != null)
                    SafeDispose(ref vbcc);
                if (ibrcc != null)
                    SafeDispose(ref ibrcc);
                if (vbrcc != null)
                    SafeDispose(ref vbrcc);

                //SafeDispose(ref vertL);
                //SafeDispose(ref indL);

                SafeDispose(ref vbcenter);
                SafeDispose(ref cb);
                SafeDispose(ref linesFactory);
                SafeDispose(ref pointsFactory);
                //SafeDispose(ref us);
            }
            base.Dispose(disposing);
        }



        /// <summary>
        /// Saves configuration on exit.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Game_Exiting(object sender, EventArgs e)
        {
            SaveConfiguration();
        }

        protected void UpdateCircleBuffers(List<Vertex> _vertices, List<int> _indices, Vertex center, 
            int pointsCount, float radius, Color color, ref VertexBuffer _vb, ref IndexBuffer _ib)
        {
            _vertices.Clear();
            _indices.Clear();

            //Vertex center = vertices[selectedVertexId];
            Vertex point;
            float alphaStep = 2.0f * (float)Math.PI / pointsCount; // (drawCircles * CIRCLE_POINTS_COUNT_MULT);
            float alpha;
            //float radius = drawCircles * CIRCLE_RADIUS_STEP;

            for (int i = 0; i < pointsCount; i++)
            {
                alpha = i * alphaStep;
                point = new Vertex();
                point.Color = color;// Color.White;
                point.Position = center.Position + 
                    new Vector3((float)Math.Cos(alpha) * radius, (float)Math.Sin(alpha) * radius, 0.0f);
                _vertices.Add(point);
                if (i == 0)
                {
                    _indices.Add(pointsCount - 1);
                    _indices.Add(0);
                }
                else
                {
                    _indices.Add(i-1);
                    _indices.Add(i);
                }
            }

            if (_ib != null)
                SafeDispose(ref _ib);
            if (_vb != null)
                SafeDispose(ref _vb);

            _vb = new VertexBuffer(GraphicsDevice, typeof(Vertex), _vertices.Count);
            _vb.SetData(_vertices.ToArray(), 0, _vertices.Count);

            _ib = new IndexBuffer(GraphicsDevice, _indices.Count);
            _ib.SetData(_indices.ToArray(), 0, _indices.Count);
        }

        /// <summary>
        /// Updates game
        /// </summary>
        /// <param name="gameTime"></param>
        protected override void Update(GameTime gameTime)
        {

            //ConstData newTransform;
            //newTransform.Transform =
            //    Matrix.RotationZ(rot_x) * Matrix.RotationY(rot_y) *
            //    Matrix.Translation(x_pos, y_pos, z_pos) *
            //    Matrix.Scaling(scale) *
            //    Matrix.LookAtRH(new Vector3(0.0f, 0.0f, 1.0f), Vector3.Zero, new Vector3(0.0f, 1.0f, 0.0f)) *
            //    Matrix.PerspectiveFovRH(MathUtil.Pi / 2, 1.0f, 0.1f, 1000f);
            //ConstData inverseTransform;
            //inverseTransform.Transform = newTransform.Transform;
            //inverseTransform.Transform.Invert();
            //Vector3 transf = Vector3.Zero;

            //if (selectedVertexId >= 0)
            //{
            //    transf = Vector3.TransformCoordinate(vertices[selectedVertexId].Position, newTransform.Transform);
            //}

            var ds = GetService<DebugStrings>();
            if (!silentMode)
            {
                ds.Add(Color.Orange, "FPS {0}", gameTime.Fps);
                if (showHelp)
                {
                    ds.Add("F1   - show developer console");
                    ds.Add("F2   - toggle silent mode");
                    ds.Add("F5   - reload mesh data");
                    ds.Add("F6   - share selection data");
                    ds.Add("F12  - make screenshot");
                    ds.Add("F    - load FBX scene");
                    ds.Add(errorMessage);
                    ds.Add("ESC  - exit");
                    ds.Add("H    - show/hide this list of keys");
                    ds.Add("O    - show origin position");
                    ds.Add("C    - add or remove connection");
                    ds.Add("T    - select tails except");
                    ds.Add("M    - merge selected tails (not implemented)");
                    ds.Add("D    - delete selected tails (disabled)");
                    ds.Add("R    - set radius to a mean of neighbours'");
                    ds.Add("X    - delete selected node");
                    ds.Add("-    - delete all selected nodes");
                    ds.Add("N    - add node between selected nodes");
                    ds.Add(";    - clone selected node");
                    ds.Add("L    - show/hide lines");
                    ds.Add("0    - set camera pos. to default");
                    ds.Add("Y    - select thread towards");
                    ds.Add("S    - change thread's selection state");
                    ds.Add("Q    - clear selection");
                    ds.Add("B    - change second selection color (Blue/Yellow)");
                    ds.Add("V    - store selected vector");
                    ds.Add("Mouse middle- move");
                    ds.Add("Mouse right - rotate");
                    ds.Add("Scroll      - scale");
                    //ds.Add("MLeft+Scroll- scale by Zshift");
                    ds.Add("ALT+T       - copy TreeNode data from second to first selection");
                    ds.Add("ALT+SPACE   - show point position");
                    ds.Add("ALT+MLeft   - select");
                    ds.Add("ALT+Scroll  - select change");
                    ds.Add("ALT+1+Scroll- fast select change");
                    ds.Add("ALT+2+Scroll- very fast select change");
                    ds.Add("CTRL+ --\"-- - second selection control");
                    ds.Add("-----------------------------");
                }
                if (threadSelected)
                {
                    ds.Add("Thread nodes count: {0}", selectedThread.nodes.Length);
                    ds.Add("-----------------------------");
                }
                if ((selectedVertexId != -1) && (secondSelectedVertexId != -1))
                {
                    ds.Add("Distance: {0}",
                        Vector3.Distance(relatedNodes[selectedVertexId].Position,
                        relatedNodes[secondSelectedVertexId].Position));
                    Vector3 vector = relatedNodes[secondSelectedVertexId].Position - relatedNodes[selectedVertexId].Position;
                    ds.Add("Vector: ({0}, {1}, {2})", vector.X, vector.Y, vector.Z);
                    ds.Add("-----------------------------");
                }

                ds.Add("cam pos: {0} {1} {2}", x_pos, y_pos, z_pos);
                ds.Add("Scale = {0}", scale);
                ds.Add("Zshift = {0}", z_pos);
                if ((selectedVertexId != -1))
                {
                    ds.Add("Selected node ID = {0} ({1} nbs)", selectedVertexId, 
                        relatedNodes[selectedVertexId].getNeighbours().Count);
                    if (relatedNodes[selectedVertexId].StructureLeafContainer != null)
                        ds.Add(relatedNodes[selectedVertexId].StructureLeafContainer.SceneNode.Name);
                }
                else
                {
                    ds.Add("Selected node ID = {0}", selectedVertexId);
                }
                if ((secondSelectedVertexId != -1))
                {
                    ds.Add("2nd selected node ID = {0} ({1} nbs)",
                        secondSelectedVertexId,
                        relatedNodes[secondSelectedVertexId].getNeighbours().Count);
                    if (relatedNodes[secondSelectedVertexId].StructureLeafContainer != null)
                        ds.Add(relatedNodes[secondSelectedVertexId].StructureLeafContainer.SceneNode.Name);
                }
                else
                {
                    ds.Add("2nd selected node ID = {0}", secondSelectedVertexId);
                }



            }


            //if (selectedVertexId >= 0)
            //{
            //    ds.Add("Selected node 3DC  = {0}, {1}, {2}",
            //        vertices[selectedVertexId].Position.X, vertices[selectedVertexId].Position.Y,
            //        vertices[selectedVertexId].Position.Z);
            //    ds.Add("Selected node 3DTC = {0}, {1}, {2}",
            //        transf.X, transf.Y, transf.Z);
            //}
            //ds.Add("Mouse 2DC = {0}, {1}", InputDevice.MousePosition.X, InputDevice.MousePosition.Y);
            //ds.Add("DispBounds= {0}, {1}", GraphicsDevice.DisplayBounds.Width, GraphicsDevice.DisplayBounds.Height);
            //Vector3 mouseFloat3DC = new Vector3(
            //    ((float)InputDevice.MousePosition.X / GraphicsDevice.DisplayBounds.Width * 2.0f - 1.0f),
            //    (-(float)InputDevice.MousePosition.Y / GraphicsDevice.DisplayBounds.Height * 2.0f + 1.0f),
            //    0.0f);
            //Vector3 mouseInversed3DC = Vector3.TransformCoordinate(mouseFloat3DC, inverseTransform.Transform);
            //mouseFloat3DC.Z = 1.0f;
            //Vector3 mouseInversed3DC2 = Vector3.TransformCoordinate(mouseFloat3DC, inverseTransform.Transform);
            //Vector3 directingVector = mouseInversed3DC2 - mouseInversed3DC;
            //ds.Add("Mouse 3DC = {0}, {1}, {2}", mouseFloat3DC.X, mouseFloat3DC.Y, mouseFloat3DC.Z);
            //ds.Add("MouseI3DC = {0}, {1}, {2}", mouseInversed3DC.X, mouseInversed3DC.Y, mouseInversed3DC.Z);
            //ds.Add("MouseI3DC2= {0}, {1}, {2}", mouseInversed3DC2.X, mouseInversed3DC2.Y, mouseInversed3DC2.Z);
            //ds.Add("MouseDirV = {0}, {1}, {2}", directingVector.X, directingVector.Y, directingVector.Z);
            //if (selectedVertexId >=0)
            //    ds.Add("Distance P 2 MLine = {0}", GetDistanceFromPointToLine(vertices[selectedVertexId].Position,
            //        mouseInversed3DC, directingVector));

            //vertLine[0] = new Vertex();
            //vertLine[0].Position = mouseInversed3DC; //+ new Vector3(0.01f, 0.01f, 0.01f) 
            //vertLine[0].Color = Color.Red;
            //vertLine[1] = new Vertex();
            //vertLine[1].Position = mouseInversed3DC2;
            //vertLine[1].Color = Color.Red;

            //vertL.SetData(vertLine);

            //	Update stuff here :
            commonData.MovSettingsAccessMutex.WaitOne();

            if (commonData.MovSettingsChanged)
            {
                movSettingsActual = commonData.MovSettings;
            }

            commonData.MovSettingsAccessMutex.ReleaseMutex();


            commonData.StyleAccessMutex.WaitOne();

            switch (commonData.Style)
            {
                case MeshStyle.MSTYLE_POINTS:
                    currentFactory = pointsFactory;
                    break;
                case MeshStyle.MSTYLE_LINES:
                    currentFactory = linesFactory;
                    break;
                default:
                    currentFactory = pointsFactory;
                    break;
            }
            commonData.StyleAccessMutex.ReleaseMutex();

            commonData.SelectedNodeAccessMutex.WaitOne();

            if ((commonData.SwitchSelectionTo >= 0) &&
                (commonData.SwitchSelectionTo) < verticesCount)
            {
                selectedVertexId = commonData.SwitchSelectionTo;
                commonData.SwitchSelectionTo = -1;
                UpdateColors();
                ShareSelection();
            } 
            
            if ((commonData.SwitchSecondSelectionTo >= 0) &&
                (commonData.SwitchSecondSelectionTo) < verticesCount)
            {
                secondSelectedVertexId = commonData.SwitchSecondSelectionTo;
                commonData.SwitchSecondSelectionTo = -1;
                UpdateColors();
                ShareSelection();
            }

            if (commonData.SelectionChanged)
            {
                UpdateColors();
                commonData.SelectionChanged = false;
            }

            commonData.SelectedNodeAccessMutex.ReleaseMutex();

            if ((drawCircles > 0) && (selectedVertexId != -1))
                UpdateCircleBuffers(verticesCircle, indicesCircle, vertices[selectedVertexId],
                    (drawCircles * CIRCLE_POINTS_COUNT_MULT), drawCircles * CIRCLE_RADIUS_STEP, 
                    selectionColor, ref vbc, ref ibc);

            if (selectedVertexId != -1)
            {
                UpdateCircleBuffers(verticesSmallCircle, indicesSmallCircle, vertices[selectedVertexId],
                    CIRCLE_POINTS_COUNT_MULT, SMALL_CIRCLE_RADIUS, selectionColor, ref vbsc, ref ibsc);
                UpdateCircleBuffers(verticesUSmallCircle, indicesUSmallCircle, vertices[selectedVertexId],
                    CIRCLE_POINTS_COUNT_MULT, SUPER_SMALL_CIRCLE_RADIUS, selectionColor, ref vbusc, ref ibusc);
            }

            if ((drawSecondCircles > 0) && (secondSelectedVertexId != -1))
                UpdateCircleBuffers(verticesCircle2, indicesCircle2, vertices[secondSelectedVertexId],
                    (drawSecondCircles * CIRCLE_POINTS_COUNT_MULT), drawSecondCircles * CIRCLE_RADIUS_STEP,
                    secondSelectionColor, ref vbc2, ref ibc2);

            if (secondSelectedVertexId != -1)
            {
                UpdateCircleBuffers(verticesSmallCircle2, indicesSmallCircle2, vertices[secondSelectedVertexId],
                    CIRCLE_POINTS_COUNT_MULT, SMALL_CIRCLE_RADIUS, secondSelectionColor, ref vbsc2, ref ibsc2);
                UpdateCircleBuffers(verticesUSmallCircle2, indicesUSmallCircle2, vertices[secondSelectedVertexId],
                    CIRCLE_POINTS_COUNT_MULT, SUPER_SMALL_CIRCLE_RADIUS, secondSelectionColor, ref vbusc2, ref ibusc2);
            }

            UpdateCircleBuffers(verticesCenterCircle, indicesCenterCircle, vacenter[0],
                CIRCLE_POINTS_COUNT_MULT, SMALL_CIRCLE_RADIUS, centerColor, ref vbcc, ref ibcc);

            if (drawCenterCircles > 0)
                UpdateCircleBuffers(verticesRunningCenterCircle, indicesRunningCenterCircle, vacenter[0],
                    (drawCenterCircles * CIRCLE_POINTS_COUNT_MULT), drawCenterCircles * CIRCLE_RADIUS_STEP,
                    centerColor, ref vbrcc, ref ibrcc);

            base.Update(gameTime);
        }



        /// <summary>
        /// Draws game
        /// </summary>
        /// <param name="gameTime"></param>
        /// <param name="stereoEye"></param>
        protected override void Draw(GameTime gameTime, StereoEye stereoEye)
        {
            //	Draw stuff here :
            
            GraphicsDevice.ClearBackbuffer(Color4.Black);
            ConstData newTransform;

            newTransform.Transform =
                //Matrix.Translation(x_pos, y_pos, z_pos) *
                Matrix.RotationX(rot_x) * Matrix.RotationY(rot_y) *
                //Matrix.Translation(-x_pos, -y_pos, -z_pos) * // x_pos, y_pos, z_pos
                Matrix.Scaling(1.0f) * // scale
                Matrix.LookAtRH(new Vector3(x_pos, y_pos, z_pos + 1.0f),
                new Vector3(x_pos, y_pos, z_pos), 
                new Vector3(0.0f, 1.0f, 0.0f)) * 
                Matrix.PerspectiveFovRH(MathUtil.Pi / 2, 1.0f, 0.1f, 1000f);
            cdata = newTransform;
            cb.SetData(newTransform);
            
            GraphicsDevice.VertexShaderConstants[0] = cb;
            GraphicsDevice.PixelShaderConstants[0] = cb;

            //	Setup device state :
            GraphicsDevice.PixelShaderSamplers[0] = SamplerState.LinearWrap;

            if (performDraw)
            {
                // Net
                GraphicsDevice.PipelineState = currentFactory[0];
                GraphicsDevice.SetupVertexInput(vb, ib);
                GraphicsDevice.DrawIndexed(ib.Capacity, 0, 0);
                // Center
                GraphicsDevice.PipelineState = pointsFactory[0];
                GraphicsDevice.SetupVertexInput(vbcenter, ibcc);
                GraphicsDevice.Draw(1, 0);
                // Center circle
                if (drawCenterStatic)
                {
                    GraphicsDevice.PipelineState = linesFactory[0];
                    GraphicsDevice.SetupVertexInput(vbcc, ibcc);
                    GraphicsDevice.DrawIndexed(ibcc.Capacity, 0, 0);
                }
                // Running center circle
                if (drawCenterCircles > 0)
                {
                    GraphicsDevice.PipelineState = linesFactory[0];
                    GraphicsDevice.SetupVertexInput(vbrcc, ibrcc);
                    GraphicsDevice.DrawIndexed(ibrcc.Capacity, 0, 0);
                    drawCenterCircles++;
                    if (drawCenterCircles == MAX_DRAW_CIRCLES)
                        drawCenterCircles = -1;
                }
            }

            if (drawCircles > 0)
            {
                GraphicsDevice.PipelineState = linesFactory[0];
                GraphicsDevice.SetupVertexInput(vbc, ibc);
                GraphicsDevice.DrawIndexed(ibc.Capacity, 0, 0);
                drawCircles++;
                if (drawCircles == MAX_DRAW_CIRCLES)
                    drawCircles = -1;
            }

            if (selectedVertexId != -1)
            {
                GraphicsDevice.PipelineState = linesFactory[0];
                GraphicsDevice.SetupVertexInput(vbsc, ibsc);
                GraphicsDevice.DrawIndexed(ibsc.Capacity, 0, 0);
                GraphicsDevice.SetupVertexInput(vbusc, ibusc);
                GraphicsDevice.DrawIndexed(ibusc.Capacity, 0, 0);
            }

            if (drawSecondCircles > 0)
            {
                GraphicsDevice.PipelineState = linesFactory[0];
                GraphicsDevice.SetupVertexInput(vbc2, ibc2);
                GraphicsDevice.DrawIndexed(ibc2.Capacity, 0, 0);
                drawSecondCircles++;
                if (drawSecondCircles == MAX_DRAW_CIRCLES)
                    drawSecondCircles = -1;
            }

            if (secondSelectedVertexId != -1)
            {
                GraphicsDevice.PipelineState = linesFactory[0];
                GraphicsDevice.SetupVertexInput(vbsc2, ibsc2);
                GraphicsDevice.DrawIndexed(ibsc2.Capacity, 0, 0);
                GraphicsDevice.SetupVertexInput(vbusc2, ibusc2);
                GraphicsDevice.DrawIndexed(ibusc2.Capacity, 0, 0);
            }

            //GraphicsDevice.PipelineState = linesFactory[0];
            //GraphicsDevice.SetupVertexInput(vertL, indL);
            //GraphicsDevice.DrawIndexed(indL.Capacity, 0, 0);
            
            base.Draw(gameTime, stereoEye);
        }
    }
}
