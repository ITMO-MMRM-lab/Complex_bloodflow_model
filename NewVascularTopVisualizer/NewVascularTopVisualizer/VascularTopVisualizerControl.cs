using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using System.Threading;
using Fusion.Mathematics;
using System.Text.RegularExpressions;
using Fusion.Graphics;
using Fusion.Content;

namespace NewVascularTopVisualizer
{
 
    public partial class VascularTopVisualizerControl : Form
    {
        private CommonThreadsData commonData;
        private MovementSettings movSettingsDefault;
        private Vector3 netCenter;
        private Vector3 selCenter;
        private bool needCenterUpdate;
        private Regex filenamePattern3ds;
        private Scene sceneFBX;
        private bool sceneLoaded;
        private TreeNode root;
        private TreeNode rootSelected;
        private TreeNode root1Dstructure;
        private bool mappingDone;

        private bool partsSet;

        private System.Windows.Forms.ItemCheckEventHandler itemCheckEventHandler;
        private System.Windows.Forms.ItemCheckEventHandler itemCheckPartsEventHandler;
                
        private const int MAX_PREALLOCATED_NODES_ARRAY_SIZE = 1000000;
        private const int MAIN_CONTENT_HEADER_SIZE = 6;
        private const int SECONDARY_HEADER_SIZE = 2;

        private Graph graph;

        LinkedList<Graph> graphs = new LinkedList<Graph>();

        List<AggrStat> aggrNodesStat;

        int pairsCloseIndex;
        int pairsCloseIndexNB;

        public VascularTopVisualizerControl(CommonThreadsData _commonData)
        {
            InitializeComponent();

            filenamePattern3ds = new Regex(@"[\w_]+_in(\d+)_out(\d+)\.ASE");
            sceneFBX = new Scene();
            sceneLoaded = false;
            partsSet = false;

            aggrNodesStat = new List<AggrStat>();

            commonData = _commonData;

            numericUpDownIdOffset.Maximum = Int64.MaxValue;
            numericUpDownMeasurePos.Maximum = Decimal.MaxValue;
            numericUpDownMeasureR.Maximum = Decimal.MaxValue;
            numericUpDownMeasureC.Maximum = Decimal.MaxValue;
            //numericUpDownMeasureC.Enabled = checkBoxLoadCurvature.Checked;
            toolStripStatusLabel.Text = "Vascular net is not loaded.";

            movSettingsDefault = new MovementSettings();
            movSettingsDefault.rot_x_speed = 0.01f;
            movSettingsDefault.rot_y_speed = 0.01f;
            movSettingsDefault.scale_speed = 0.001f;
            movSettingsDefault.x_pos_speed = 0.003f;
            movSettingsDefault.y_pos_speed = 0.003f;
            movSettingsDefault.z_pos_speed = 0.0003f;

            netCenter = Vector3.Zero;
            selCenter = Vector3.Zero;
            needCenterUpdate = false;

            numericUpDownRotXSpeed.Maximum = Decimal.MaxValue;
            numericUpDownRotYSpeed.Maximum = Decimal.MaxValue;
            numericUpDownScalingSpeed.Maximum = Decimal.MaxValue;
            numericUpDownMovXSpeed.Maximum = Decimal.MaxValue;
            numericUpDownMovYSpeed.Maximum = Decimal.MaxValue;
            numericUpDownMovZSpeed.Maximum = Decimal.MaxValue;
            
            numericUpDownSwitchSelectedNodeTo.Maximum = Decimal.MaxValue;
            numericUpDownSelectedNodeCurvature.Maximum = Decimal.MaxValue;
            numericUpDownSelectedNodeRadius.Maximum = Decimal.MaxValue;
            numericUpDownSelectedNodeLumen_sq.Maximum = Decimal.MaxValue;
            numericUpDownSelectedNodeLumen_sq0.Maximum = Decimal.MaxValue;
            numericUpDownSelectedNodePositionX.Maximum = Decimal.MaxValue;
            numericUpDownSelectedNodePositionY.Maximum = Decimal.MaxValue;
            numericUpDownSelectedNodePositionZ.Maximum = Decimal.MaxValue;
            numericUpDownSelectedNodePositionX.Minimum = Decimal.MinValue;
            numericUpDownSelectedNodePositionY.Minimum = Decimal.MinValue;
            numericUpDownSelectedNodePositionZ.Minimum = Decimal.MinValue;
            numericUpDownSelectedNodeBeta.Maximum = Decimal.MaxValue;
            numericUpDownSelectedNodeBeta.Minimum = Decimal.MinValue;
            numericUpDownConnectionFrom.Maximum = Decimal.MaxValue;
            numericUpDownConnectionTo.Maximum = Decimal.MaxValue;

            numericUpDownCenterX.Maximum = Decimal.MaxValue;
            numericUpDownCenterY.Maximum = Decimal.MaxValue;
            numericUpDownCenterZ.Maximum = Decimal.MaxValue;
            numericUpDownCenterX.Minimum = Decimal.MinValue;
            numericUpDownCenterY.Minimum = Decimal.MinValue;
            numericUpDownCenterZ.Minimum = Decimal.MinValue;

            numericUpDownSelCenterX.Maximum = Decimal.MaxValue;
            numericUpDownSelCenterY.Maximum = Decimal.MaxValue;
            numericUpDownSelCenterZ.Maximum = Decimal.MaxValue;
            numericUpDownSelCenterX.Minimum = Decimal.MinValue;
            numericUpDownSelCenterY.Minimum = Decimal.MinValue;
            numericUpDownSelCenterZ.Minimum = Decimal.MinValue;

            numericUpDownCenterXShift.Maximum = Decimal.MaxValue;
            numericUpDownCenterYShift.Maximum = Decimal.MaxValue;
            numericUpDownCenterZShift.Maximum = Decimal.MaxValue;
            numericUpDownCenterXShift.Minimum = Decimal.MinValue;
            numericUpDownCenterYShift.Minimum = Decimal.MinValue;
            numericUpDownCenterZShift.Minimum = Decimal.MinValue;

            numericUpDownSelCenterXShift.Maximum = Decimal.MaxValue;
            numericUpDownSelCenterYShift.Maximum = Decimal.MaxValue;
            numericUpDownSelCenterZShift.Maximum = Decimal.MaxValue;
            numericUpDownSelCenterXShift.Minimum = Decimal.MinValue;
            numericUpDownSelCenterYShift.Minimum = Decimal.MinValue;
            numericUpDownSelCenterZShift.Minimum = Decimal.MinValue;

            numericUpDownMirrorX.Maximum = Decimal.MaxValue;
            numericUpDownMirrorX.Minimum = Decimal.MinValue;

            numericUpDownThreadBetaFrom.Minimum = Decimal.MinValue;
            numericUpDownThreadBetaFrom.Maximum = Decimal.MaxValue;
            numericUpDownThreadA0From.Minimum = Decimal.MinValue;
            numericUpDownThreadA0From.Maximum = Decimal.MaxValue;
            numericUpDownThreadRFrom.Minimum = Decimal.MinValue;
            numericUpDownThreadRFrom.Maximum = Decimal.MaxValue;

            numericUpDownThreadBetaTo.Minimum = Decimal.MinValue;
            numericUpDownThreadBetaTo.Maximum = Decimal.MaxValue;
              numericUpDownThreadA0To.Minimum = Decimal.MinValue;
              numericUpDownThreadA0To.Maximum = Decimal.MaxValue;
               numericUpDownThreadRTo.Minimum = Decimal.MinValue;
               numericUpDownThreadRTo.Maximum = Decimal.MaxValue;

            numericUpDownCenterXShift.Value = 0;
            numericUpDownCenterYShift.Value = 0;
            numericUpDownCenterZShift.Value = 0;

            numericUpDownCenterX3ds.Maximum = Decimal.MaxValue;
            numericUpDownCenterY3ds.Maximum = Decimal.MaxValue;
            numericUpDownCenterZ3ds.Maximum = Decimal.MaxValue;
            numericUpDownCenterX3ds.Minimum = Decimal.MinValue;
            numericUpDownCenterY3ds.Minimum = Decimal.MinValue;
            numericUpDownCenterZ3ds.Minimum = Decimal.MinValue;
            numericUpDownSquare3ds.Maximum = Decimal.MaxValue;
            numericUpDownRadius3ds.Maximum = Decimal.MaxValue;
            numericUpDownVertexIdExcluded.Maximum = Int32.MaxValue;
            numericUpDownVertexIdIncluded.Maximum = Int32.MaxValue;

			numericUpDownMinCurrentRad.Minimum = Decimal.MinValue;
			numericUpDownMaxCurrentRad.Minimum = Decimal.MinValue;
			numericUpDownMinCurrentRad.Maximum = Decimal.MaxValue;
			numericUpDownMaxCurrentRad.Maximum = Decimal.MaxValue;

            numericUpDownMaxPreallocatedNodesCount.Maximum = Int32.MaxValue;
            numericUpDownMaxPreallocatedNodesCount.Value = MAX_PREALLOCATED_NODES_ARRAY_SIZE;

            UpdateMovementSettingsView(movSettingsDefault);

            commonData.MovSettingsAccessMutex.WaitOne();

            commonData.MovSettings = movSettingsDefault;
            commonData.MovSettingsChanged = true;

            commonData.MovSettingsAccessMutex.ReleaseMutex();


            textBoxFBXfilename.Text = getFbxFilenameFromContentFile(@"..\..\..\Content\Content.xml");

            //buttonDefaultMovSettings.Enabled = false;
            //buttonApplyMovSettings.Enabled = false;
            //buttonRevertMovSettings.Enabled = false;

            //Manually set ItemCheck event handler (this allow switch it off to avoid recursion later)
            itemCheckEventHandler = new System.Windows.Forms.ItemCheckEventHandler(this.checkedListBoxTopNodes_ItemCheck);
            itemCheckPartsEventHandler = new System.Windows.Forms.ItemCheckEventHandler(this.checkedListBoxParts_ItemCheck);
            this.checkedListBoxTopNodes.ItemCheck += itemCheckEventHandler;
            this.checkedListBoxParts.ItemCheck += itemCheckPartsEventHandler;

            mappingDone = false;
            root1Dstructure = null;
        }

        private string getFbxFilenameFromContentFile(string contentFilename)
        {
            string fbxFile = "";
            string assetPathValue = "";
            bool assetPathFound = false;
            bool eofFound = false;

            string contentFile = File.ReadAllText(contentFilename);

            using (XmlReader reader = XmlReader.Create(new StringReader(contentFile)))
            {
                while (true)
                {
                    if (!reader.ReadToFollowing("Asset"))
                    {
                        eofFound = true;
                        break;
                    }
                    assetPathFound = reader.ReadToDescendant("AssetPath");
                    if (!assetPathFound)
                        continue;
                    assetPathValue = reader.ReadElementContentAsString();
                    if (assetPathValue.Equals("fbxscene.fbx"))
                        break;
                }

                if (!eofFound)
                {
                    reader.ReadToNextSibling("SourceFile");
                    fbxFile = reader.ReadElementContentAsString();
                }
            }

            return fbxFile;
        }

        private void UpdateMovementSettingsView(MovementSettings ms)
        {
            numericUpDownRotXSpeed.Value = (decimal)ms.rot_x_speed;
            numericUpDownRotYSpeed.Value = (decimal)ms.rot_y_speed;
            numericUpDownScalingSpeed.Value = (decimal)ms.scale_speed;
            numericUpDownMovXSpeed.Value = (decimal)ms.x_pos_speed;
            numericUpDownMovYSpeed.Value = (decimal)ms.y_pos_speed;
            numericUpDownMovZSpeed.Value = (decimal)ms.z_pos_speed;
        }

        private void buttonBrowseInputFile_Click(object sender, EventArgs e)
        {
            DialogResult result = openMeshFileDialog.ShowDialog();
            if (DialogResult.OK == result)
            {
                textBoxInputFile.Text = openMeshFileDialog.FileName;
            }
        }

        private void buttonBrowseOutputFile_Click(object sender, EventArgs e)
        {
            DialogResult result = saveMeshFileDialog.ShowDialog();
            if (DialogResult.OK == result)
            {
                textBoxOutputFile.Text = saveMeshFileDialog.FileName;
            }
        }

        private void buttonLoadMesh_Click(object sender, EventArgs e)
        {
            DialogResult result = System.Windows.Forms.DialogResult.None;
            bool loadingRequested = true;
            String msg = "";
            
            if (!File.Exists(textBoxInputFile.Text))
            {
                MessageBox.Show("File not found.");
                return;
            }

            commonData.VnetAccessMutex.WaitOne();

            if (commonData.VnetLoaded)
            {
                result = MessageBox.Show("Vascular net has been already loaded. Do you want to replace it?", "Vascular net loaded!", MessageBoxButtons.YesNo);
                switch (result)
                {
                    case DialogResult.Yes:
                        loadingRequested = true;
                        break;
                    case DialogResult.No:
                        loadingRequested = false;
                        break;
                    default:
                        loadingRequested = false;
                        MessageBox.Show("Operation cancelled.");
                        break;
                }
            }

            if (loadingRequested)
            {
                msg = VascularNet.LoadFromFile(commonData.Vnet, textBoxInputFile.Text, 
                    //checkBoxLoadCurvature.Checked,
                    (float)numericUpDownMeasurePos.Value,
                    (double)numericUpDownMeasureR.Value, (double)numericUpDownMeasureC.Value, 
                    (double)numericUpDownMeasureBeta.Value,
                    (int)numericUpDownMaxPreallocatedNodesCount.Value);
                commonData.VnetLoaded = true;
                commonData.VnetChanged = true;
                needCenterUpdate = true;
                commonData.VnetFixed = false;
                commonData.VnetAccessMutex.ReleaseMutex();
                toolStripStatusLabel.Text = "Vascular net loaded.";
                MessageBox.Show(msg);
            }
            else
            {
                commonData.VnetAccessMutex.ReleaseMutex();
            }
        }

        private void buttonWriteMesh_Click(object sender, EventArgs e)
        {
            DialogResult result = System.Windows.Forms.DialogResult.None;
            bool savingRequested = true;

            if (File.Exists(textBoxOutputFile.Text))
            {
                result = MessageBox.Show("Specified file already exists. Do you want to overwrite it?", "File exists!", MessageBoxButtons.YesNo);
                switch (result)
                {
                    case DialogResult.Yes:
                        savingRequested = true;
                        break;
                    case DialogResult.No:
                        savingRequested = false;
                        break;
                    default:
                        savingRequested = false;
                        MessageBox.Show("Operation cancelled.");
                        break;
                }
            }

            if (savingRequested)
            {

                commonData.VnetAccessMutex.WaitOne();
                if (commonData.VnetLoaded)
                {
                    string report = VascularNet.WriteToFile(commonData.Vnet, (int)numericUpDownIdOffset.Value, textBoxOutputFile.Text,
                    (float)numericUpDownMeasurePos.Value,
                    (double)numericUpDownMeasureR.Value, (double)numericUpDownMeasureC.Value, 
                    (double)numericUpDownMeasureBeta.Value, checkBoxPrintBeta.Checked);
                    commonData.VnetAccessMutex.ReleaseMutex();
                    if (report.Equals(""))
                    {
                        toolStripStatusLabel.Text = "Vascular net saved.";
                        MessageBox.Show("Vascular net was saved successfully.");
                    }
                    else
                    {
                        MessageBox.Show(report);
                    }
                }
                else
                {
                    commonData.VnetAccessMutex.ReleaseMutex();
                    MessageBox.Show("Vascular net is not loaded. Nothing to save.");
                }
            }
        }

        private void buttonFix_Click(object sender, EventArgs e)
        {
            DialogResult result = System.Windows.Forms.DialogResult.None;
            bool fixingRequested = true;
            String msg = "";
            int nodesBefore = 0;

            commonData.VnetAccessMutex.WaitOne();
            if (!commonData.VnetLoaded)
            {
                commonData.VnetAccessMutex.ReleaseMutex();
                MessageBox.Show("Vascular net is not loaded. Nothing to fix.");
                return;
            }
            if (commonData.VnetFixed)
            {
                result = MessageBox.Show("Loaded vascular net resolution has already been set. Do you want to set it again?", "Vascular net fixed!", MessageBoxButtons.YesNo);
                switch (result)
                {
                    case DialogResult.Yes:
                        fixingRequested = true;
                        break;
                    case DialogResult.No:
                        fixingRequested = false;
                        break;
                    default:
                        fixingRequested = false;
                        MessageBox.Show("Operation cancelled.");
                        break;
                }
            }

            if (fixingRequested)
            {
                nodesBefore = commonData.Vnet.NodesCount;
                VascularNet.Fix(commonData.Vnet, (double)numericUpDownResolution.Value, 
                    (int)numericUpDownMaxTerminalBranchLengthN.Value,
                    (double)numericUpDownMaxTerminalBranchRadius.Value,
                    (double)numericUpDownMaxTerminalNodeRadius.Value, 
                    checkBoxEnableSimplification.Checked, checkBoxSaveTopology.Checked, 
                    checkBoxSetBifurcationNodes.Checked,
                    radioButtonSetResLinearityA0.Checked, checkBoxConserveLengths.Checked);
                commonData.VnetFixed = true;
                commonData.VnetChanged = true;
                needCenterUpdate = true;
                commonData.VnetAccessMutex.ReleaseMutex();
                msg = "Vascular net resolution was set successfully. \n" +
                "Nodes count: " + nodesBefore.ToString() + " -> " + commonData.Vnet.NodesCount.ToString();
                toolStripStatusLabel.Text = "Vascular net resolution set.";
                MessageBox.Show(msg);
            }
            else
            {
                commonData.VnetAccessMutex.ReleaseMutex();
            }
        }

        private void checkBoxLoadCurvature_CheckedChanged(object sender, EventArgs e)
        {
            //numericUpDownMeasureC.Enabled = checkBoxLoadCurvature.Checked;
        }

        private void buttonDefaultMovSettings_Click(object sender, EventArgs e)
        {
            UpdateMovementSettingsView(movSettingsDefault);

            //buttonDefaultMovSettings.Enabled = false;
            //buttonApplyMovSettings.Enabled = true;
            //buttonRevertMovSettings.Enabled = true;
        }

        private void buttonRevertMovSettings_Click(object sender, EventArgs e)
        {
            MovementSettings tmp;

            commonData.MovSettingsAccessMutex.WaitOne();

            tmp = commonData.MovSettings;

            commonData.MovSettingsAccessMutex.ReleaseMutex();

            UpdateMovementSettingsView(tmp);

            //buttonDefaultMovSettings.Enabled = true;
            //buttonApplyMovSettings.Enabled = false;
            //buttonRevertMovSettings.Enabled = false;
        }

        private void buttonApplyMovSettings_Click(object sender, EventArgs e)
        {
            MovementSettings tmp;

            tmp.rot_x_speed = (float)numericUpDownRotXSpeed.Value;
            tmp.rot_y_speed = (float)numericUpDownRotYSpeed.Value;
            tmp.scale_speed = (float)numericUpDownScalingSpeed.Value;
            tmp.x_pos_speed = (float)numericUpDownMovXSpeed.Value;
            tmp.y_pos_speed = (float)numericUpDownMovYSpeed.Value;
            tmp.z_pos_speed = (float)numericUpDownMovZSpeed.Value;

            commonData.MovSettingsAccessMutex.WaitOne();

            commonData.MovSettings = tmp;
            commonData.MovSettingsChanged = true;

            commonData.MovSettingsAccessMutex.ReleaseMutex();

            //buttonDefaultMovSettings.Enabled = true;
            //buttonApplyMovSettings.Enabled = false;
            //buttonRevertMovSettings.Enabled = false;
        }

        private void numericUpDownRotXSpeed_ValueChanged(object sender, EventArgs e)
        {
            //buttonDefaultMovSettings.Enabled = true;
            //buttonApplyMovSettings.Enabled = true;
            //buttonRevertMovSettings.Enabled = true;
        }

        private void numericUpDownRotYSpeed_ValueChanged(object sender, EventArgs e)
        {
            //buttonDefaultMovSettings.Enabled = true;
            //buttonApplyMovSettings.Enabled = true;
            //buttonRevertMovSettings.Enabled = true;
        }

        private void numericUpDownScalingSpeed_ValueChanged(object sender, EventArgs e)
        {
            //buttonDefaultMovSettings.Enabled = true;
            //buttonApplyMovSettings.Enabled = true;
            //buttonRevertMovSettings.Enabled = true;
        }

        private void numericUpDownMovXSpeed_ValueChanged(object sender, EventArgs e)
        {
            //buttonDefaultMovSettings.Enabled = true;
            //buttonApplyMovSettings.Enabled = true;
            //buttonRevertMovSettings.Enabled = true;
        }

        private void numericUpDownMovYSpeed_ValueChanged(object sender, EventArgs e)
        {
            //buttonDefaultMovSettings.Enabled = true;
            //buttonApplyMovSettings.Enabled = true;
            //buttonRevertMovSettings.Enabled = true;
        }

        private void numericUpDownMovZSpeed_ValueChanged(object sender, EventArgs e)
        {
            //buttonDefaultMovSettings.Enabled = true;
            //buttonApplyMovSettings.Enabled = true;
            //buttonRevertMovSettings.Enabled = true;
        }

        private void buttonStyleApply_Click(object sender, EventArgs e)
        {

            commonData.StyleAccessMutex.WaitOne();

            if (radioButtonPoints.Checked)
                commonData.Style = MeshStyle.MSTYLE_POINTS;
            if (radioButtonLines.Checked)
                commonData.Style = MeshStyle.MSTYLE_LINES;
            commonData.StyleChanged = true;

            commonData.StyleAccessMutex.ReleaseMutex();
        }

        private void LoadSelectedThreadData()
        {
            commonData.SelectedNodeAccessMutex.WaitOne();

            Node selectedNode = commonData.SelectedNode;
            Node secondSelectedNode = commonData.SecondSelectedNode;

            commonData.SelectedNodeAccessMutex.ReleaseMutex();

            if ((selectedNode == null) || (secondSelectedNode == null))
            {
                numericUpDownBSIDfrom.Value = 0;
                numericUpDownBSIDtowards.Value = 0;
                numericUpDownIDfrom.Value = 0;
                numericUpDownIDtowards.Value = 0;
                numericUpDownLength.Value = 0;
                return;
            }
            if (!selectedNode.getNeighbours().Contains(secondSelectedNode))
            {
                numericUpDownBSIDfrom.Value = 0;
                numericUpDownBSIDtowards.Value = 0;
                numericUpDownIDfrom.Value = 0;
                numericUpDownIDtowards.Value = 0;
                numericUpDownLength.Value = 0;
                return;
            }
            numericUpDownBSIDfrom.Value = selectedNode.getId();
            numericUpDownBSIDtowards.Value = secondSelectedNode.getId();
            numericUpDownIDfrom.Value = selectedNode.getId();
            numericUpDownIDtowards.Value = secondSelectedNode.getId();
            VascularThread thread = new VascularThread();
            thread = commonData.Vnet.getThread(selectedNode, secondSelectedNode);
            if (thread.nodes == null)
            {
                numericUpDownLength.Value = 0;
                numericUpDownThreadA0From.Value = 0;
                numericUpDownThreadA0To.Value = 0;
                numericUpDownThreadBetaFrom.Value = 0;
                numericUpDownThreadBetaTo.Value = 0;
            }
            else
            {
                bool switchBack = false;
                if (numericUpDownThreadA0From.ReadOnly)
                {
                    numericUpDownThreadA0From.ReadOnly = false;
                    numericUpDownThreadA0To.ReadOnly = false;
                    switchBack = true;
                }

                numericUpDownLength.Value = (decimal)thread.getLength() * 100;
                numericUpDownThreadA0From.Value = (decimal)thread.nodes.First().getLumen_sq0() * 10000;
                numericUpDownThreadA0To.Value = (decimal)thread.nodes.Last().getLumen_sq0() * 10000;
                numericUpDownThreadBetaFrom.Value = (decimal)thread.nodes.First().Beta / 10000000;
                numericUpDownThreadBetaTo.Value = (decimal)thread.nodes.Last().Beta / 10000000;

                if (switchBack)
                {
                    numericUpDownThreadA0From.ReadOnly = true;
                    numericUpDownThreadA0To.ReadOnly = true;
                }
            }
        }

        private void LoadSelectedNodeData()
        {
            commonData.SelectedNodeAccessMutex.WaitOne();

            Node selectedNode = commonData.SelectedNode;
            Node secondSelectedNode = commonData.SecondSelectedNode;

            commonData.SelectedNodeAccessMutex.ReleaseMutex();

            if (selectedNode == null)
            {
                textBoxSelectedNodeId.Text = "";
                textBoxNeighbours.Text = "";
                numericUpDownSelectedNodeCurvature.Value = 0;
                numericUpDownSelectedNodeRadius.Value = 0;
                numericUpDownSelectedNodeLumen_sq.Value = 0;
                numericUpDownSelectedNodeLumen_sq0.Value = 0;
                numericUpDownSelectedNodePositionX.Value = 0;
                numericUpDownSelectedNodePositionY.Value = 0;
                numericUpDownSelectedNodePositionZ.Value = 0;
                numericUpDownSelectedNodeBeta.Value = 0;

                numericUpDownConnectionFrom.Value = 0;
            }
            else
            {
                textBoxSelectedNodeId.Text = selectedNode.getId().ToString();
                String neighbours = "";
                foreach (Node n in selectedNode.getNeighbours())
                    neighbours += n.getId().ToString() + " ";
                textBoxNeighbours.Text = neighbours;

                numericUpDownSelectedNodeCurvature.Value = (decimal)selectedNode.curvature;
                numericUpDownSelectedNodeRadius.Value = (decimal)selectedNode.Rad;
                numericUpDownSelectedNodeLumen_sq.Value = (decimal)selectedNode.getLumen_sq();
                numericUpDownSelectedNodeLumen_sq0.Value = (decimal)selectedNode.getLumen_sq0();
                numericUpDownSelectedNodePositionX.Value = (decimal)selectedNode.Position.X;
                numericUpDownSelectedNodePositionY.Value = (decimal)selectedNode.Position.Y;
                numericUpDownSelectedNodePositionZ.Value = (decimal)selectedNode.Position.Z;
                numericUpDownSelectedNodeBeta.Value = (decimal)(selectedNode.Beta / 10000000);

                numericUpDownConnectionFrom.Value = selectedNode.getId();
            }

            if (secondSelectedNode == null)
            {
                numericUpDownConnectionTo.Value = 0;
            }
            else
            {
                numericUpDownConnectionTo.Value = secondSelectedNode.getId();
            }
        }

        private void LoadNetCenterData()
        {
            if (needCenterUpdate)
            {
                commonData.VnetAccessMutex.WaitOne();
                if (commonData.VnetLoaded)
                    netCenter = commonData.Vnet.getCenter();
                commonData.VnetAccessMutex.ReleaseMutex();
                needCenterUpdate = false;
            }

            numericUpDownCenterX.Value = (decimal)netCenter.X;
            numericUpDownCenterY.Value = (decimal)netCenter.Y;
            numericUpDownCenterZ.Value = (decimal)netCenter.Z;
        }

        private void LoadSelCenterData()
        {
            commonData.VnetAccessMutex.WaitOne();
            if (commonData.VnetLoaded)
                selCenter = commonData.Vnet.getSelCenter();
            commonData.VnetAccessMutex.ReleaseMutex();

            numericUpDownSelCenterX.Value = (decimal)selCenter.X;
            numericUpDownSelCenterY.Value = (decimal)selCenter.Y;
            numericUpDownSelCenterZ.Value = (decimal)selCenter.Z;
        }

        private void LoadRadiusData()
        {
            if (!commonData.VnetLoaded)
            {
                numericUpDownMinCurrentRad.Value = 0.0M;
                numericUpDownMaxCurrentRad.Value = 0.0M;
            }
            else
            {
                double minRad = double.MaxValue;
                double maxRad = 0.0;
                foreach (var n in commonData.Vnet.Nodes)
                {
                    if (n.Rad < minRad)
                        minRad = n.Rad;
                    if (n.Rad > maxRad)
                        maxRad = n.Rad;
                }
                numericUpDownMinCurrentRad.Value = (decimal)minRad;
                numericUpDownMaxCurrentRad.Value = (decimal)maxRad;
            }
        }

        private void LoadSelectionTolerance()
        {
            commonData.SelectionToleranceAccessMutex.WaitOne();

            numericUpDownSelectionTolerance.Value = (decimal)commonData.SelectionTolerance;

            commonData.SelectionToleranceAccessMutex.ReleaseMutex();
        }

        private void VascularTopVisualizerControl_Activated(object sender, EventArgs e)
        {
            LoadSelectedNodeData();

            LoadNetCenterData();

            LoadSelectionTolerance();

            LoadRadiusData();

            LoadSelectedThreadData();
        }

        private void buttonSortNodes_Click(object sender, EventArgs e)
        {
            String msg = "";

            commonData.VnetAccessMutex.WaitOne();
            if (!commonData.VnetLoaded)
            {
                commonData.VnetAccessMutex.ReleaseMutex();
                MessageBox.Show("Vascular net is not loaded. Nothing to sort.");
                return;
            }

            commonData.Vnet.SortByXYZ();
            commonData.VnetChanged = true;
            commonData.VnetAccessMutex.ReleaseMutex();
            toolStripStatusLabel.Text = "Vascular nodes sorted.";
            msg = "Vascular net nodes were sorted successfully.";
            MessageBox.Show(msg);
        }

        private void buttonSwitchSelectedNodeTo_Click(object sender, EventArgs e)
        {
            commonData.SelectedNodeAccessMutex.WaitOne();

            commonData.SwitchSelectionTo = (int)numericUpDownSwitchSelectedNodeTo.Value;

            commonData.SelectedNodeAccessMutex.ReleaseMutex();
        }

        private void buttonConnectionAdd_Click(object sender, EventArgs e)
        {
            String msg = "";

            commonData.VnetAccessMutex.WaitOne();

            if (commonData.Vnet.AddConnection(
                (int)numericUpDownConnectionFrom.Value,
                (int)numericUpDownConnectionTo.Value))
            {
                commonData.VnetChanged = true;
                msg = "Connection was added successfully.";
                toolStripStatusLabel.Text = "Connection added.";
            }
            else
            {
                msg = "Failed to add the connection.";
            }
            
            commonData.VnetAccessMutex.ReleaseMutex();

            MessageBox.Show(msg);
        }

        private void buttonConnectionRemove_Click(object sender, EventArgs e)
        {

            String msg = "";

            commonData.VnetAccessMutex.WaitOne();

            if (commonData.Vnet.RemoveConnection(
                (int)numericUpDownConnectionFrom.Value,
                (int)numericUpDownConnectionTo.Value))
            {
                commonData.VnetChanged = true;
                msg = "Connection was removed successfully.";
                toolStripStatusLabel.Text = "Connection removed.";
            }
            else
            {
                msg = "Failed to remove the connection.";
            }

            commonData.VnetAccessMutex.ReleaseMutex();

            MessageBox.Show(msg);
        }

        private void numericUpDownSelectedNodeRadius_ValueChanged(object sender, EventArgs e)
        {
            numericUpDownSelectedNodeLumen_sq.Value = (decimal)Math.PI * numericUpDownSelectedNodeRadius.Value *
                numericUpDownSelectedNodeRadius.Value;
        }

        private void numericUpDownSelectedNodeLumen_sq_ValueChanged(object sender, EventArgs e)
        {
            numericUpDownSelectedNodeRadius.Value = (decimal)Math.Sqrt((float)(numericUpDownSelectedNodeLumen_sq.Value / (decimal)Math.PI));
            numericUpDownSelectedNodeLumen_sq0.Value = numericUpDownSelectedNodeLumen_sq.Value;
        }

        private void buttonSelectedNodeRevert_Click(object sender, EventArgs e)
        {
            LoadSelectedNodeData();
        }

        private void buttonSelectedNodeApply_Click(object sender, EventArgs e)
        {
            commonData.SelectedNodeAccessMutex.WaitOne();

            Node selectedNode = commonData.SelectedNode;

            commonData.SelectedNodeAccessMutex.ReleaseMutex();

            if (selectedNode != null)
            {

                commonData.VnetAccessMutex.WaitOne();

                selectedNode.curvature = (float)numericUpDownSelectedNodeCurvature.Value;
                selectedNode.Rad = (float)numericUpDownSelectedNodeRadius.Value;
                selectedNode.setLumen_sq0((float)numericUpDownSelectedNodeLumen_sq0.Value);
                selectedNode.Position = new Fusion.Mathematics.Vector3(
                    (float)numericUpDownSelectedNodePositionX.Value,
                    (float)numericUpDownSelectedNodePositionY.Value,
                    (float)numericUpDownSelectedNodePositionZ.Value);
                selectedNode.Beta = (double)numericUpDownSelectedNodeBeta.Value * 10000000;
                commonData.VnetChanged = true;
                needCenterUpdate = true;
                toolStripStatusLabel.Text = "Selected node updated.";

                commonData.VnetAccessMutex.ReleaseMutex();
            }

        }

        private void buttonSelectedNodeDelete_Click(object sender, EventArgs e)
        {

            String msg = "";

            commonData.SelectedNodeAccessMutex.WaitOne();

            Node selectedNode = commonData.SelectedNode;

            commonData.SelectedNodeAccessMutex.ReleaseMutex();


            if (selectedNode != null)
            {
                commonData.VnetAccessMutex.WaitOne();

                if (commonData.Vnet.removeNode(selectedNode))
                {
                    commonData.VnetChanged = true;
                    needCenterUpdate = true;
                    msg = "Node was removed successfully.";
                    toolStripStatusLabel.Text = "Selected node removed.";
                }
                else
                {
                    msg = "Failed to remove the node.";
                }

                commonData.VnetAccessMutex.ReleaseMutex();
                MessageBox.Show(msg);
            }

        }

        private void buttonSelectedNodeClone_Click(object sender, EventArgs e)
        {
            String msg = "Node added with id ";

            commonData.SelectedNodeAccessMutex.WaitOne();

            Node selectedNode = commonData.SelectedNode;

            commonData.SelectedNodeAccessMutex.ReleaseMutex();


            if (selectedNode != null)
            {
                commonData.VnetAccessMutex.WaitOne();
                int newId = commonData.Vnet.NodesCount;
                Node clone = new Node(newId, selectedNode.Position, selectedNode.Rad);
                clone.curvature = selectedNode.curvature;
                commonData.Vnet.Nodes.Add(clone);

                commonData.VnetChanged = true;
                needCenterUpdate = true;
                toolStripStatusLabel.Text = "Selected node cloned.";

                commonData.VnetAccessMutex.ReleaseMutex();
                MessageBox.Show(msg + newId.ToString());
            }
        }

        private void buttonSetCenterTo0_Click(object sender, EventArgs e)
        {
            numericUpDownCenterX.Value = 0;
            numericUpDownCenterY.Value = 0;
            numericUpDownCenterZ.Value = 0;
        }

        private void buttonRevertCenter_Click(object sender, EventArgs e)
        {
            LoadNetCenterData();
        }

        private void buttonApplyCenter_Click(object sender, EventArgs e)
        {
            Vector3 newNetCenter;

            newNetCenter.X = (float)numericUpDownCenterX.Value;
            newNetCenter.Y = (float)numericUpDownCenterY.Value;
            newNetCenter.Z = (float)numericUpDownCenterZ.Value;

            commonData.VnetAccessMutex.WaitOne();

            commonData.Vnet.moveAllNodes(newNetCenter - netCenter);
            netCenter = newNetCenter;
            commonData.VnetChanged = true;
            toolStripStatusLabel.Text = "Vascular net center moved.";

            commonData.VnetAccessMutex.ReleaseMutex();
        }

        private void tabControlMain_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (tabControlMain.SelectedIndex)
            {
                case 0:
                    // Load, fix, save.
                    break;
                case 1:
                    // Graphic control.
                    LoadSelectedNodeData();
                    LoadSelectionTolerance();
                    break;
                case 2:
                    // Center control.
                    LoadNetCenterData();
                    LoadSelCenterData();
                    break;
                case 6:
                    // Set res.
                    LoadRadiusData();
                    break;
                case 7:
                    // Threads.
                    LoadSelectedThreadData();
                    break;
            }
        }

        private void buttonSelectionToleranceSetDefault_Click(object sender, EventArgs e)
        {
            numericUpDownSelectionTolerance.Value = (decimal)0.1f;
        }

        private void buttonSelectionToleranceReload_Click(object sender, EventArgs e)
        {
            LoadSelectionTolerance();
        }

        private void buttonSelectionToleranceApply_Click(object sender, EventArgs e)
        {

            commonData.SelectionToleranceAccessMutex.WaitOne();

            commonData.SelectionTolerance = (float)numericUpDownSelectionTolerance.Value;
            commonData.SelectionToleranceChanged = true;

            commonData.SelectionToleranceAccessMutex.ReleaseMutex();
        }

        private void buttonLoadAndMerge_Click(object sender, EventArgs e)
        {
            DialogResult result = System.Windows.Forms.DialogResult.None;
            String msg = "";

            if (!File.Exists(textBoxInputFile.Text))
            {
                MessageBox.Show("File not found.");
                return;
            }

            VascularNet vnetToBeMerged = new VascularNet("NetToBeMerged");

            msg = VascularNet.LoadFromFile(vnetToBeMerged, textBoxInputFile.Text, 
                //checkBoxLoadCurvature.Checked,
                (float)numericUpDownMeasurePos.Value, (float)numericUpDownMeasureR.Value,
                (float)numericUpDownMeasureC.Value, (float)numericUpDownMeasureBeta.Value,
                (int)numericUpDownMaxPreallocatedNodesCount.Value);

            commonData.VnetAccessMutex.WaitOne();

            if (!commonData.VnetLoaded)
            {
                commonData.Vnet = vnetToBeMerged;
                commonData.VnetLoaded = true;
                commonData.VnetChanged = true;
                needCenterUpdate = true;
                commonData.VnetAccessMutex.ReleaseMutex();
                toolStripStatusLabel.Text = "Vascular net loaded.";
                MessageBox.Show("Vascular net hasn't been loaded yet. Nothing to merge. Specified vascular net loaded.");
                MessageBox.Show(msg);
                return;
            }
            commonData.Vnet.mergeWithNet(vnetToBeMerged);

            commonData.VnetLoaded = true;
            commonData.VnetChanged = true;
            needCenterUpdate = true;
            commonData.VnetAccessMutex.ReleaseMutex();
            toolStripStatusLabel.Text = "Vascular net loaded.";
            MessageBox.Show(msg);
        }

        private void buttonShift_Click(object sender, EventArgs e)
        {
            numericUpDownCenterX.Value += numericUpDownCenterXShift.Value;
            numericUpDownCenterY.Value += numericUpDownCenterYShift.Value;
            numericUpDownCenterZ.Value += numericUpDownCenterZShift.Value;

            numericUpDownCenterXShift.Value = 0;
            numericUpDownCenterYShift.Value = 0;
            numericUpDownCenterZShift.Value = 0;
        }

        private void buttonMirrorOYZ_Click(object sender, EventArgs e)
        {
            commonData.VnetAccessMutex.WaitOne();

            if (commonData.VnetLoaded)
            {
                foreach (Node n in commonData.Vnet.Nodes)
                {
                    n.Position = new Vector3(-n.Position.X, n.Position.Y, n.Position.Z);
                }
                commonData.VnetChanged = true;
                MessageBox.Show("Vascular net mirrored.");
                toolStripStatusLabel.Text = "Vascular net mirrored.";
            }

            commonData.VnetAccessMutex.ReleaseMutex();
        }

        private void numericUpDownSelectedNodeLumen_sq0_ValueChanged(object sender, EventArgs e)
        {
            numericUpDownSelectedNodeLumen_sq.Value = numericUpDownSelectedNodeLumen_sq0.Value;
        }

        private void buttonBrowse3ds_Click(object sender, EventArgs e)
        {
            DialogResult result = open3dsFileDialog.ShowDialog();
            if (DialogResult.OK == result)
            {
                textBoxInputFile3ds.Text = open3dsFileDialog.FileName;
            }
        }

        private void buttonProcess3ds_Click(object sender, EventArgs e)
        {
            // Check if name of the file contains vertices' indices
            Match m = filenamePattern3ds.Match(textBoxInputFile3ds.Text);
            if (m.Success)
            {
                checkBoxForceNormal.Checked = true;
                numericUpDownVertexIdIncluded.Value = Int32.Parse(m.Groups[1].Value);
                numericUpDownVertexIdExcluded.Value = Int32.Parse(m.Groups[2].Value);
            }
            // Load data from the 3ds exported file.
            if (!File.Exists(textBoxInputFile3ds.Text))
            {
                MessageBox.Show("File not found!");
                return;
            }
            String[] data = File.ReadAllLines(textBoxInputFile3ds.Text);
            Vector3 forcedNormal = Vector3.Zero;
            List<Vector3> vertices = Parser3ds.Parse3dsData(data, (float)numericUpDownMeasure3ds.Value,
                checkBoxForceNormal.Checked, (int)numericUpDownVertexIdIncluded.Value - 1, 
                (int)numericUpDownVertexIdExcluded.Value - 1, out forcedNormal);
            if (vertices.Count == 0)
            {
                MessageBox.Show("Vertices description not found!");
                return;
            }
            Section section = new Section(vertices, checkBoxForceNormal.Checked, forcedNormal, 
                radioButtonRadiusByMeanDiameter.Checked);
            numericUpDownCenterX3ds.Value = (decimal)section.center.X;
            numericUpDownCenterY3ds.Value = (decimal)section.center.Z;
            numericUpDownCenterZ3ds.Value = -(decimal)section.center.Y;
            numericUpDownRadius3ds.Value = (decimal)section.radius;
            numericUpDownSquare3ds.Value = (decimal)section.square;
        }

        private void buttonAddPoint3ds_Click(object sender, EventArgs e)
        {
            // Add calculated point to the mesh.
            commonData.VnetAccessMutex.WaitOne();
            int newId = commonData.Vnet.NodesCount;
            Node newNode = new Node(newId, new Vector3(
                (float)numericUpDownCenterX3ds.Value, 
                (float)numericUpDownCenterY3ds.Value, 
                (float)numericUpDownCenterZ3ds.Value),
                (double)numericUpDownRadius3ds.Value);
            newNode.curvature = 0.0;
            commonData.Vnet.Nodes.Add(newNode);

            commonData.VnetChanged = true;
            needCenterUpdate = true;
            toolStripStatusLabel.Text = "New node added with id " + newId.ToString() + ".";
            commonData.VnetAccessMutex.ReleaseMutex();

            commonData.SelectedNodeAccessMutex.WaitOne();

            commonData.SwitchSelectionTo = newId;

            commonData.SelectedNodeAccessMutex.ReleaseMutex();
        }

        private void buttonFixTNdebug_Click(object sender, EventArgs e)
        {
            commonData.VnetAccessMutex.WaitOne();

            if (commonData.VnetLoaded)
            {
                List<Node> nt = new List<Node>();
                commonData.Vnet.simplifyTerminalNodes((double)numericUpDownMaxTerminalBranchLengthN.Value,
                    (double)numericUpDownMaxTerminalNodeRadius.Value);
                String msg = "";
                foreach (Node n in nt)
                    msg += n.getId().ToString() + " ";
                MessageBox.Show(msg);

                commonData.VnetChanged = true;
            }


            commonData.VnetAccessMutex.ReleaseMutex();
        }

        private void buttonLoadFBXFile_Click(object sender, EventArgs e)
        {
            //if (contentManager == null)
            //{
            //    contentManager = commonData.ContentManager;
            //}

            //if (contentManager == null)
            //{
            //    MessageBox.Show("ContentManager not found. Please run Visualisation part of application.");
            //    return;
            //}
            //try
            //{
            //    sceneFBX = contentManager.Load<Scene>("testScene");
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show(ex.Message);
            //    return;
            //}

            commonData.SceneAccessMutex.WaitOne();

            if (commonData.Scene == null)
            {
                MessageBox.Show("Scene is not ready. Use F key in Visualisation window to load it.");
            }
            else
            {
                sceneFBX = commonData.Scene;
                sceneLoaded = true;
            }

            commonData.SceneAccessMutex.ReleaseMutex();

            root = null;
            mappingDone = false;

            if (sceneLoaded)
            {
                toolStripStatusLabel.Text = "Scene loaded (" + sceneFBX.Nodes.Count.ToString() + " nodes).";

                root = ArterialTreeBuilder.buildArterialTree(sceneFBX, (float)numericUpDownMeasureFBX.Value);

                checkedListBoxTopNodes.Items.Clear();
                TreeNodeNamesComparer tnnc = new TreeNodeNamesComparer();
                foreach (TreeNode partNode in root.Children)
                {
                    checkedListBoxTopNodes.Items.Add(partNode);
                    partNode.Children.Sort(tnnc);
                    foreach (TreeNode leave in partNode.Children)
                    {
                        checkedListBoxTopNodes.Items.Add(leave);
                    }
                }
                
            }
        }


        private void checkedListBoxTopNodes_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            //Temporarly switch off event handling to avoid recursion
            this.checkedListBoxTopNodes.ItemCheck -= itemCheckEventHandler;

            // Assume that only selected item can change its state
            if ((TreeNode)checkedListBoxTopNodes.SelectedItem == null)
                throw new Exception("Assumption about selection & checking in CheckedListBox is wrong (see ItemCheck method).");
            TreeNode selectedNode = (TreeNode)checkedListBoxTopNodes.SelectedItem;
            if (selectedNode.Children.Count != 0)
            {
                for (int index = 0; index < checkedListBoxTopNodes.Items.Count; index++)
                {
                    if (((TreeNode)checkedListBoxTopNodes.Items[index]).Parent.Equals(selectedNode))
                    {
                        checkedListBoxTopNodes.SetItemCheckState(index, e.NewValue);
                    }
                }
            }

            if (selectedNode.Parent != root)
            {
                if (e.NewValue == CheckState.Checked)
                {
                    for (int i = 0; i < checkedListBoxTopNodes.Items.Count; i++)
                    {
                        if (checkedListBoxTopNodes.Items[i].Equals(selectedNode.Parent))
                        {
                            checkedListBoxTopNodes.SetItemCheckState(i, e.NewValue);
                            break;
                        }
                    }
                }
            }

            //Switch on event handling back
            this.checkedListBoxTopNodes.ItemCheck += itemCheckEventHandler;
        }

        private void checkedListBoxParts_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            //Temporarly switch off event handling to avoid recursion
            this.checkedListBoxParts.ItemCheck -= itemCheckPartsEventHandler;

            // Assume that only selected item can change its state
            if ((TreeNode)checkedListBoxParts.SelectedItem == null)
                throw new Exception("Assumption about selection & checking in CheckedListBox is wrong (see ItemCheck method).");
            TreeNode selectedNode = (TreeNode)checkedListBoxParts.SelectedItem;
            if (selectedNode.Children.Count != 0)
            {
                for (int index = 0; index < checkedListBoxParts.Items.Count; index++)
                {
                    if (((TreeNode)checkedListBoxParts.Items[index]).Parent.Equals(selectedNode))
                    {
                        checkedListBoxParts.SetItemCheckState(index, e.NewValue);
                    }
                }
            }

            if (!selectedNode.Parent.Equals(rootSelected))
            {
                if (e.NewValue == CheckState.Checked)
                {
                    for (int i = 0; i < checkedListBoxParts.Items.Count; i++)
                    {
                        if (checkedListBoxParts.Items[i].Equals(selectedNode.Parent))
                        {
                            checkedListBoxParts.SetItemCheckState(i, e.NewValue);
                            break;
                        }
                    }
                }
            }

            //Switch on event handling back
            this.checkedListBoxParts.ItemCheck += itemCheckPartsEventHandler;
        }

        private void copySelectedFbxStructureToNewTree()
        {
            // Copy only selected nodes of tree.
            if (root == null)
                return;

            if (checkedListBoxTopNodes.CheckedItems.Count == 0)
                return;

            rootSelected = new TreeNode(root.SceneListId, root.SceneNode, root.Vertices);

            foreach (var item in checkedListBoxTopNodes.CheckedItems)
            {
                TreeNode currentItem = ((TreeNode)item);
                if (currentItem.Parent.Equals(root))
                {
                    TreeNode newNode = new TreeNode(currentItem.SceneListId, currentItem.SceneNode,
                        currentItem.Vertices);
                    newNode.Parent = rootSelected;
                    rootSelected.Children.Add(newNode);
                }
            }

            foreach (var item in checkedListBoxTopNodes.CheckedItems)
            {
                TreeNode currentItem = ((TreeNode)item);
                if (currentItem.Parent.Equals(rootSelected))
                    continue;
                TreeNode parent = rootSelected.Children.Find(x => x.Equals(currentItem.Parent));
                if (parent == null)
                {
                    throw new ArgumentException("Unchecked parent with checked child found.");
                }
                TreeNode newNode = new TreeNode(currentItem.SceneListId, currentItem.SceneNode,
                    currentItem.Vertices);
                newNode.Parent = parent;
                parent.Children.Add(newNode);

                //DEBUG FBX PARSING
                //StringBuilder ss = new StringBuilder();
                //for (int i = 0; i < newNode.Vertices.Length; i++)
                //{
                //    if (i > 20)
                //        break;
                //    Vector3 v3 = newNode.Vertices[i];
                //    ss.AppendFormat("{0} X: {1}, Y: {2}, Z: {3}\n", i, v3.X, v3.Y, v3.Z);
                //}
                //MessageBox.Show("Vertices:\n" + ss.ToString());
            }
        }

        private void mapVertices()
        {
            if (rootSelected == null)
                return;

            commonData.VnetAccessMutex.WaitOne();

            if (!commonData.VnetLoaded)
            {
                commonData.VnetAccessMutex.ReleaseMutex();
                return;
            }

            int nodeIndex;
            TreeNode nearestLeave;
            float distanceSq, newDistanceSq;

            // Get 1D node for each FBX node
            foreach (var part in rootSelected.Children)
            {
                foreach (var leaf in part.Children)
                {
                    if (leaf.Vertices == null)
                        continue;
                    for (int vi = 0; vi < leaf.Vertices.Length; vi++)
                    {
                        nodeIndex = 0;
                        distanceSq = Vector3.DistanceSquared(commonData.Vnet.Nodes[0].Position, leaf.Vertices[vi]);
                        for (int i = 1; i < commonData.Vnet.Nodes.Count; i++)
                        {
                            newDistanceSq = Vector3.DistanceSquared(commonData.Vnet.Nodes[i].Position, leaf.Vertices[vi]);
                            if (newDistanceSq < distanceSq)
                            {
                                distanceSq = newDistanceSq;
                                nodeIndex = i;
                            }
                        }

                        leaf.FbxVerticesTo1DverticesMap[vi] = new List<int>(
                            new int[] { commonData.Vnet.Nodes[nodeIndex].getId() });

                        commonData.Vnet.Nodes[nodeIndex].MappedVertices.Add(new UniqueVertexIdentifier(leaf, vi));
                    }
                }
            }

            // If all neighbours of an unused 1D node are used,
            // then assign the node to the treenode that contains the majority of neighbours.
            bool allNodesUsed = true;
            Dictionary<TreeNode, int> treeNodesOccurencies = new Dictionary<TreeNode,int>();
            int interpolated1D = 0;
            for (int i = 0; i < commonData.Vnet.Nodes.Count; i++)
            {
                if (commonData.Vnet.Nodes[i].MappedVertices.Count > 0)
                    continue;
                if (commonData.Vnet.Nodes[i].getNeighbours().Count == 0)
                    continue;
                allNodesUsed = true;
                treeNodesOccurencies.Clear();
                foreach (var neighbour in commonData.Vnet.Nodes[i].getNeighbours())
                {
                    if (neighbour.MappedVertices.Count == 0)
                    {
                        allNodesUsed = false;
                        break;
                    }
                    TreeNode mostFqNode = ArterialTreeBuilder.getMostFrequentTreeNode(neighbour);
                    if (treeNodesOccurencies.ContainsKey(mostFqNode))
                    {
                        treeNodesOccurencies[mostFqNode]++;
                    }
                    else
                    {
                        treeNodesOccurencies[mostFqNode] = 1;
                    }
                }
                if (allNodesUsed)
                {
                    int maxCount = -1;
                    TreeNode nodeToAssign = null;
                    foreach (var key in treeNodesOccurencies.Keys)
                    {
                        if (treeNodesOccurencies[key] > maxCount)
                        {
                            maxCount = treeNodesOccurencies[key];
                            nodeToAssign = key;
                        }
                    }
                    int nearestVertexIndex = nodeToAssign.getNearestVertexIndex(commonData.Vnet.Nodes[i]);

                    commonData.Vnet.Nodes[i].MappedVertices.Add(new UniqueVertexIdentifier(nodeToAssign, nearestVertexIndex));
                    nodeToAssign.FbxVerticesTo1DverticesMap[nearestVertexIndex].Add(commonData.Vnet.Nodes[i].getId());
                    interpolated1D++;
                }
            }

            // If there is a thread of unused nodes, bounded with used nodes contained by the same treenode,
            // then assign all the nodes of the thread to the treenode that contains the bounds.
            int longInterpolated1D = 0;
            List<Node> nodesToAssign = new List<Node>();
            bool unassignedThreadEndFound = false;
            for (int i = 0; i < commonData.Vnet.Nodes.Count; i++)
            {
                if (commonData.Vnet.Nodes[i].MappedVertices.Count > 0)
                    continue;
                if (commonData.Vnet.Nodes[i].getNeighbours().Count != 2)
                    continue;
                nodesToAssign.Clear();
                Node neighbourLeft = commonData.Vnet.Nodes[i].getNeighbours()[0];
                Node prevNeighbourLeft = commonData.Vnet.Nodes[i];
                Node neighbourRight = commonData.Vnet.Nodes[i].getNeighbours()[1];
                Node prevNeighbourRight = commonData.Vnet.Nodes[i];
                Node tmpNode;
                unassignedThreadEndFound = false;
                nodesToAssign.Add(commonData.Vnet.Nodes[i]);
                while (neighbourLeft.MappedVertices.Count == 0)
                {
                    if (neighbourLeft.getNeighbours().Count != 2)
                    {
                        unassignedThreadEndFound = true;
                        break;
                    }
                    nodesToAssign.Add(neighbourLeft);
                    tmpNode = neighbourLeft;
                    neighbourLeft = neighbourLeft.getNeighbours().Find(x => !x.Equals(prevNeighbourLeft));
                    prevNeighbourLeft = tmpNode;
                }
                if (unassignedThreadEndFound)
                {
                    // Long interpolation is not applicable.
                    continue;
                }
                while (neighbourRight.MappedVertices.Count == 0)
                {
                    if (neighbourRight.getNeighbours().Count != 2)
                    {
                        unassignedThreadEndFound = true;
                        break;
                    }
                    nodesToAssign.Add(neighbourRight);
                    tmpNode = neighbourRight;
                    neighbourRight = neighbourRight.getNeighbours().Find(x => !x.Equals(prevNeighbourRight));
                    prevNeighbourRight = tmpNode;
                }
                if (unassignedThreadEndFound)
                {
                    // Long interpolation is not applicable.
                    continue;
                }
                if (!ArterialTreeBuilder.getMostFrequentTreeNode(neighbourLeft).Equals(
                    ArterialTreeBuilder.getMostFrequentTreeNode(neighbourRight)))
                {
                    // Long interpolation is not applicable.
                    continue;
                }
                TreeNode commonTreeNode = ArterialTreeBuilder.getMostFrequentTreeNode(neighbourLeft);
                // All's OK, assigning all nodes.
                foreach (Node node in nodesToAssign)
                {
                    int nearestVertexIndex = commonTreeNode.getNearestVertexIndex(node);
                    node.MappedVertices.Add(new UniqueVertexIdentifier(commonTreeNode, nearestVertexIndex));
                    commonTreeNode.FbxVerticesTo1DverticesMap[nearestVertexIndex].Add(node.getId());
                    longInterpolated1D++;
                }
            }

            // Get 20 nearest FBX vertices for each (unused) 1D node.
            int setToNearest1D = 0;
            UVIWDSbyDistanceComparer uviwdCmp = new UVIWDSbyDistanceComparer();
            SortedSet<UniqueVertexIdentifierWithDistanceSq> nearestFBXVertices =
                new SortedSet<UniqueVertexIdentifierWithDistanceSq>(uviwdCmp);
            for (int i = 0; i < commonData.Vnet.Nodes.Count; i++)
            {
                if (commonData.Vnet.Nodes[i].MappedVertices.Count > 0)
                    continue;

                nearestFBXVertices.Clear();
                nearestLeave = null;
                nodeIndex = -1;
                distanceSq = float.PositiveInfinity;

                foreach (var part in rootSelected.Children)
                {
                    foreach (var leaf in part.Children)
                    {
                        if (leaf.Vertices == null)
                            continue;

                        for (int vi = 0; vi < leaf.Vertices.Length; vi++)
                        {
                            newDistanceSq = Vector3.DistanceSquared(commonData.Vnet.Nodes[i].Position,
                                leaf.Vertices[vi]);
                            nearestFBXVertices.Add(new UniqueVertexIdentifierWithDistanceSq(
                                new UniqueVertexIdentifier(leaf, vi), newDistanceSq));
                            if (nearestFBXVertices.Count > 20)
                            {
                                nearestFBXVertices.Remove(nearestFBXVertices.Max);
                            }
                        }
                    }
                }
                //string ss = "";
                foreach (UniqueVertexIdentifierWithDistanceSq uviwd in nearestFBXVertices)
                {
                    //ss += Math.Sqrt(uviwd.distanceSq).ToString() + " ";
                    commonData.Vnet.Nodes[i].MappedVertices.Add(uviwd.uVertexId);
                    uviwd.uVertexId.treeNode.FbxVerticesTo1DverticesMap[uviwd.uVertexId.vertexIndex].
                        Add(commonData.Vnet.Nodes[i].getId());
                }
                //if (setToNearest1D < 10)
                //    MessageBox.Show(ss);
                setToNearest1D++;
            }

            commonData.VnetAccessMutex.ReleaseMutex();

            mappingDone = true;
            toolStripStatusLabel.Text = string.Format(
                "Vertices mapped (normal {0}, interpolated {1}, long-interpolated {2}, nearest {3}).", 
                commonData.Vnet.Nodes.Count - interpolated1D - longInterpolated1D - setToNearest1D, 
                interpolated1D, longInterpolated1D, setToNearest1D);
        }

        private void buttonMapStructureToPoints_Click(object sender, EventArgs e)
        {
            copySelectedFbxStructureToNewTree();

            mapVertices();
            CheckIfNotMapped1DNodesExist();
        }

        private void CheckIfNotMapped1DNodesExist()
        {
            List<Node> notMapped = get1DnodesNotMapped();

            if (notMapped.Count > 0)
            {
                StringBuilder msg = new StringBuilder("1D nodes not mapped (total " + notMapped.Count + "):");
                int ii = 0;
                foreach (Node n in notMapped)
                {
                    msg.Append(" " + n.getId());
                    ii++;
                    if (ii > 100)
                    {
                        msg.Append("...");
                        break;
                    }
                }
                MessageBox.Show(msg.ToString());
            }
        }

        private List<Node> get1DnodesNotMapped()
        {
            List<Node> notMapped = new List<Node>();
            commonData.VnetAccessMutex.WaitOne();

            if (commonData.VnetLoaded)
            {
                foreach (var node in commonData.Vnet.Nodes)
                {
                    if (node.MappedVertices.Count == 0)
                    {
                        notMapped.Add(node);
                    }
                }
            }

            commonData.VnetAccessMutex.ReleaseMutex();

            return notMapped;
        }

        // DEBUG FBX PARSING
        private void buttonGetVerticesDeb_Click(object sender, EventArgs e)
        {
            commonData.SceneAccessMutex.WaitOne();

            if (commonData.Scene == null)
            {
                MessageBox.Show("Scene is not ready. Use F key in Visualisation window to load it.");
            }
            else
            {
                sceneFBX = commonData.Scene;
                sceneLoaded = true;
            }

            commonData.SceneAccessMutex.ReleaseMutex();

            Fusion.Graphics.Node node = sceneFBX.Nodes[1];
            Matrix[] wm = new Matrix[sceneFBX.Nodes.Count];
            sceneFBX.CopyAbsoluteTransformsTo(wm);
            Vector3[] vertices = ArterialTreeBuilder.getVertices(sceneFBX.Meshes[node.MeshIndex], ref wm[1], false);
            StringBuilder ss = new StringBuilder("Vertices:\n");
            string[] vertStrings = new string[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 v3 = vertices[i];
                vertStrings[i] = string.Format("X: {0}, Y: {1}, Z: {2} ({3})\n", v3.X, v3.Y, v3.Z, i);
            }
            IOrderedEnumerable<string> vertOrdStrings = vertStrings.OrderByDescending(x => x);
            foreach (var sss in vertOrdStrings)
            {
                ss.Append(sss);
            }
            File.WriteAllText(@"D:\Temp\fbxLoadedVertices.txt", ss.ToString());

            MessageBox.Show(vertices.Length.ToString() + " " + 
                sceneFBX.Meshes[node.MeshIndex].Vertices.Count.ToString());

            commonData.VnetAccessMutex.WaitOne();

            commonData.Vnet.Nodes.Clear();

            int ii = 0;
            foreach (var v in vertices)
            {
                commonData.Vnet.Nodes.Add(new Node(ii, v, 1.0 * ii));
                if (ii > 0)
                {
                    commonData.Vnet.Nodes[ii-1].addNeighbours(new Node[] { commonData.Vnet.Nodes[ii] });
                    commonData.Vnet.Nodes[ii].addNeighbours(new Node[] { commonData.Vnet.Nodes[ii-1] });
                }
                ii++;
            }
            commonData.VnetLoaded = true;
            commonData.VnetChanged = true;

            commonData.VnetAccessMutex.ReleaseMutex();
        }

        private void buttonReindexNodes_Click(object sender, EventArgs e)
        {
            commonData.VnetAccessMutex.WaitOne();

            if (commonData.VnetLoaded)
            {
                commonData.Vnet.reindexNodes();
                commonData.VnetChanged = true;
            }

            commonData.VnetAccessMutex.ReleaseMutex();
        }

        private void buttonBrowseSaveMapping_Click(object sender, EventArgs e)
        {
            DialogResult result = saveMapFileDialog.ShowDialog();
            if (DialogResult.OK == result)
            {
                textBoxSaveMappingFilename.Text = saveMapFileDialog.FileName;
            }
        }

        private void buttonBrowseLoadMapping_Click(object sender, EventArgs e)
        {
            DialogResult result = openMapFileDialog.ShowDialog();
            if (DialogResult.OK == result)
            {
                textBoxLoadMappingFilename.Text = openMapFileDialog.FileName;
            }
        }

        //private void addContentIdentifiersHeader(StringBuilder content)
        //{
        //}

        //private bool checkContentIdentifiersHeader(string[] content)
        //{
        //    string line;
        //    Match match;

        //}

        private void buttonSaveMapping_Click(object sender, EventArgs e)
        {
            if (!mappingDone)
                return;
            if (!commonData.VnetLoaded)
                return;
            StringBuilder content = new StringBuilder();
            content.Append("FBX to 1D vertices correspondence.\n");
            content.Append("FBX scene:\n");
            content.Append(string.Format("{0}, {1} nodes.\n", textBoxFBXfilename.Text, sceneFBX.Nodes.Count));
            content.Append("1D mesh:\n");
            content.Append(string.Format("{0} nodes.\n", commonData.Vnet.Nodes.Count));
            content.Append("Selected FBX scene nodes:\n");

            foreach (var part in rootSelected.Children)
            {
                content.Append(string.Format("ID: {0} NAME: {1}\n", part.SceneListId, part.SceneNode.Name));
                foreach (var leave in part.Children)
                {
                    content.Append(string.Format("ID: {0} NAME: {1}\n", leave.SceneListId, leave.SceneNode.Name));
                }
            }
            content.Append("Vertices correspondence:\n");
            content.Append("(<SceneListId>,<VertexIndex>) <1DVertexId> ...\n");

            foreach (var part in rootSelected.Children)
            {
                foreach (var leave in part.Children)
                {
                    if (leave.Vertices == null)
                        continue;
                    for (int i = 0; i < leave.Vertices.Length; i++)
                    {
                        content.Append(string.Format("({0},{1})", leave.SceneListId, i));
                        foreach (int id in leave.FbxVerticesTo1DverticesMap[i])
                        {
                            content.Append(String.Format(" {0}", id));
                        }
                        content.Append("\n");
                    }
                }
            }

            File.WriteAllText(textBoxSaveMappingFilename.Text, content.ToString());
        }

        private void buttonLoadMapping_Click(object sender, EventArgs e)
        {
            if (!commonData.VnetLoaded)
                return;
            if (!sceneLoaded)
                return;
            if (root == null)
                return;
            if (!commonData.Vnet.UpdatePreallocatedVascularSystem(MAX_PREALLOCATED_NODES_ARRAY_SIZE))
            {
                MessageBox.Show("Preallocated VNet array not updated.");
                return;
            }

            string[] content = File.ReadAllLines(textBoxLoadMappingFilename.Text);
            string line;
            Match match;
            MatchCollection innerMatches;
            Regex fbxFileDescription = new Regex(@"([^,\n]+), (\d+) nodes.");
            Regex meshDescription = new Regex(@"(\d+) nodes.");
            Regex fbxNodeDescription = new Regex(@"ID: (\d+) NAME: ([^\s]+)");
            Regex mappingLine = new Regex(@"\((\d+),(\d+)\)([\s\d]+)");
            Regex mappingLineListItem = new Regex(@"(\s\d+)");
            Dictionary<int, TreeNode> sceneListIdsToTreeNodesMap = new Dictionary<int, TreeNode>();


            if (content.Length < MAIN_CONTENT_HEADER_SIZE + SECONDARY_HEADER_SIZE)
            {
                MessageBox.Show("Wrong format of the mapping file.");
                return;
            }
            line = content[2];
            match = fbxFileDescription.Match(line);

            if (!match.Success)
            {
                MessageBox.Show("Wrong format of the mapping file.");
                return;
            }

            string fbxFilename = match.Groups[1].Value;
            int fbxNodesCount = int.Parse(match.Groups[2].Value);

            if ((!textBoxFBXfilename.Text.Equals(fbxFilename)) ||
                (sceneFBX.Nodes.Count != fbxNodesCount))
            {
                MessageBox.Show("Loaded FBX scene doesn't match to the mapped one.");
                return;
            }

            line = content[4];
            match = meshDescription.Match(line);

            if (!match.Success)
            {
                MessageBox.Show("Wrong format of the mapping file.");
                return;
            }
            int meshNodesCount = int.Parse(match.Groups[1].Value);

            if (commonData.Vnet.Nodes.Count != meshNodesCount)
            {
                MessageBox.Show("Loaded 1D mesh doesn't match to the mapped one.");
                return;
            }

            int i = MAIN_CONTENT_HEADER_SIZE;
            match = fbxNodeDescription.Match(content[i]);

            rootSelected = new TreeNode(root.SceneListId, root.SceneNode, null);

            bool nodeFound = false;
            TreeNode lastParent = null;
            TreeNode lastParentSelected = null;
            TreeNode newNode;
            while (match.Success)
            {
                nodeFound = false;
                int nodeId = int.Parse(match.Groups[1].Value);
                string nodeName = match.Groups[2].Value;

                // Search among root's children.
                for (int ii = 0; ii < root.Children.Count; ii++)
                {
                    if ((root.Children[ii].SceneListId == nodeId) &&
                        (root.Children[ii].SceneNode.Name.Equals(nodeName)))
                    {
                        nodeFound = true;
                        newNode = new TreeNode(root.Children[ii].SceneListId, root.Children[ii].SceneNode, null);
                        rootSelected.Children.Add(newNode);
                        newNode.Parent = rootSelected;
                        lastParent = root.Children[ii];
                        lastParentSelected = newNode;
                    }
                }

                if ((!nodeFound) && (lastParent != null))
                {
                    // Search among last parent's children.
                    for (int ii = 0; ii < lastParent.Children.Count; ii++)
                    {
                        if ((lastParent.Children[ii].SceneListId == nodeId) &&
                            (lastParent.Children[ii].SceneNode.Name.Equals(nodeName)))
                        {
                            nodeFound = true;
                            newNode = new TreeNode(lastParent.Children[ii].SceneListId,
                                lastParent.Children[ii].SceneNode, lastParent.Children[ii].Vertices);
                            lastParentSelected.Children.Add(newNode);
                            newNode.Parent = lastParentSelected;
                            sceneListIdsToTreeNodesMap[lastParent.Children[ii].SceneListId] = newNode;
                        }
                    }
                }

                if (!nodeFound)
                {
                    MessageBox.Show("FBX node not found.");
                    rootSelected = null;
                    return;
                }

                i++;
                if (i >= content.Length)
                    break;
                match = fbxNodeDescription.Match(content[i]);
            }

            i += SECONDARY_HEADER_SIZE;

            commonData.Vnet.clearMappingData();
            while (true)
            {
                if (i >= content.Length)
                    break;
                match = mappingLine.Match(content[i]);
                if (!match.Success)
                {
                    MessageBox.Show("Wrong format of mapping lines.");
                    rootSelected = null;
                    commonData.Vnet.clearMappingData();
                    return;
                }

                int sceneListId = int.Parse(match.Groups[1].Value);
                int fbxVertexIndex = int.Parse(match.Groups[2].Value);
                string meshNodesIndicesList = match.Groups[3].Value;

                TreeNode tn = sceneListIdsToTreeNodesMap[sceneListId];
                if (tn == null)
                {
                    MessageBox.Show("Invalid sceneNodeId in mapping lines.");
                    rootSelected = null;
                    commonData.Vnet.clearMappingData();
                    return;
                }
                if (tn.Vertices == null)
                {
                    MessageBox.Show("SceneNode without mesh in mapping lines.");
                    rootSelected = null;
                    commonData.Vnet.clearMappingData();
                    return;
                }
                if (tn.Vertices.Length <= fbxVertexIndex)
                {
                    MessageBox.Show("Invalid fbx vertex index in mapping lines.");
                    rootSelected = null;
                    commonData.Vnet.clearMappingData();
                    return;
                }
                // Parse node's ids list
                innerMatches = mappingLineListItem.Matches(meshNodesIndicesList);
                tn.FbxVerticesTo1DverticesMap[fbxVertexIndex] = new List<int>();
                foreach (Match im in innerMatches)
                {
                    int meshVertexIndex = int.Parse(im.Groups[1].Value);
                    Node meshNode = commonData.Vnet.PreallocatedVascularSystem[meshVertexIndex];
                    if (meshNode == null)
                    {
                        MessageBox.Show("Invalid 1D mesh node id in mapping lines.");
                        rootSelected = null;
                        commonData.Vnet.clearMappingData();
                        return;
                    }
                    tn.FbxVerticesTo1DverticesMap[fbxVertexIndex].Add(meshNode.getId());
                    meshNode.MappedVertices.Add(new UniqueVertexIdentifier(tn, fbxVertexIndex));
                }
                i++;
            }

            // Finally update checks in list on FBX processor tabpage.
            UpdateChecksInFBXProcessorNodesList();
            mappingDone = true;
            toolStripStatusLabel.Text = "Mapping loaded.";
            CheckIfNotMapped1DNodesExist();
        }

        private void UpdateChecksInFBXProcessorNodesList()
        {
            //Switch off event handling
            this.checkedListBoxTopNodes.ItemCheck -= itemCheckEventHandler;

            TreeNode lastParent = null;
            for (int i = 0; i < checkedListBoxTopNodes.Items.Count; i++ )
            {
                if (root.Children.Contains(checkedListBoxTopNodes.Items[i]))
                {
                    lastParent = rootSelected.Children.Find(x => x.Equals(checkedListBoxTopNodes.Items[i]));
                    if (lastParent != null)
                    {
                        checkedListBoxTopNodes.SetItemCheckState(i, CheckState.Checked);
                    }
                    else
                    {
                        checkedListBoxTopNodes.SetItemCheckState(i, CheckState.Unchecked);
                    }
                    continue;
                }
                if (lastParent == null)
                {
                    checkedListBoxTopNodes.SetItemCheckState(i, CheckState.Unchecked);
                    continue;
                }
                if (lastParent.Children.Contains(checkedListBoxTopNodes.Items[i]))
                {
                    checkedListBoxTopNodes.SetItemCheckState(i, CheckState.Checked);
                }
                else
                {
                    checkedListBoxTopNodes.SetItemCheckState(i, CheckState.Unchecked);
                }
            }

            //Switch on event handling back
            this.checkedListBoxTopNodes.ItemCheck += itemCheckEventHandler;
        }

        private void buttonBuildStructured1DMesh_Click(object sender, EventArgs e)
        {
            if (rootSelected == null)
                return;
            if (!mappingDone)
                return;

            commonData.VnetAccessMutex.WaitOne();

            if (!commonData.VnetLoaded)
            {
                commonData.VnetAccessMutex.ReleaseMutex();
                return;
            }

            root1Dstructure = ArterialTreeBuilder.build1DTree(rootSelected, commonData.Vnet.Nodes);
            
            commonData.VnetAccessMutex.ReleaseMutex();

            checkedListBoxParts.Items.Clear();
            TreeNodeNamesComparer tnnc = new TreeNodeNamesComparer();
            foreach (TreeNode partNode in root1Dstructure.Children)
            {
                checkedListBoxParts.Items.Add(partNode);
                partNode.Children.Sort(tnnc);
                foreach (TreeNode leave in partNode.Children)
                {
                    checkedListBoxParts.Items.Add(leave);
                }
            }

            toolStripStatusLabel.Text = "Structured 1D mesh built.";
        }

        private void buttonShowSelected_Click(object sender, EventArgs e)
        {
            commonData.VnetAccessMutex.WaitOne();

            if (!commonData.VnetLoaded)
            {
                commonData.VnetAccessMutex.ReleaseMutex();
                return;
            }

            for (int i = 0; i < checkedListBoxParts.Items.Count; i++)
            {
                foreach (Node n in ((TreeNode)checkedListBoxParts.Items[i]).Nodes1D)
                {
                    n.SelectedToShow = checkedListBoxParts.GetItemChecked(i);
                }
            }
            commonData.SelectionChanged = true;

            commonData.VnetAccessMutex.ReleaseMutex();
        }

        private void buttonRecombineScraps_Click(object sender, EventArgs e)
        {
            if (rootSelected == null)
                return;
            if (!mappingDone)
                return;

            commonData.VnetAccessMutex.WaitOne();

            if (!commonData.VnetLoaded)
            {
                commonData.VnetAccessMutex.ReleaseMutex();
                return;
            }
            float singleNodePart = 0.0f;//(float)numericUpDownSingleNodePart.Value;

            int count = ArterialTreeBuilder.recombine1DTreeScraps(commonData.Vnet, singleNodePart);

            commonData.VnetAccessMutex.ReleaseMutex();

            //checkedListBoxParts.Items.Clear();
            //foreach (TreeNode partNode in root1Dstructure.Children)
            //{
            //    checkedListBoxParts.Items.Add(partNode);
            //    foreach (TreeNode leave in partNode.Children)
            //    {
            //        checkedListBoxParts.Items.Add(leave);
            //    }
            //}

            toolStripStatusLabel.Text = "Structured 1D mesh recombined. ("+count+")";
        }

        private void buttonBrowseSave1Dtree_Click(object sender, EventArgs e)
        {
            DialogResult result = save1DtreeFileDialog.ShowDialog();
            if (DialogResult.OK == result)
            {
                textBoxSave1DtreeFilename.Text = save1DtreeFileDialog.FileName;
            }
        }

        private void buttonBrowseLoad1DtreeFile_Click(object sender, EventArgs e)
        {
            DialogResult result = open1DtreeFileDialog.ShowDialog();
            if (DialogResult.OK == result)
            {
                textBoxLoad1DtreeFilename.Text = open1DtreeFileDialog.FileName;
            }
        }

        private void buttonSave1DtreeFile_Click(object sender, EventArgs e)
        {
            if (!mappingDone)
                return;
            if (!commonData.VnetLoaded)
                return;
            if (rootSelected == null)
                return;
            if (root1Dstructure == null)
                return;
            StringBuilder content = new StringBuilder();
            content.Append("1D vertices to TreeNodes correspondence.\n");
            content.Append("FBX scene:\n");
            content.Append(string.Format("{0}, {1} nodes.\n", textBoxFBXfilename.Text, sceneFBX.Nodes.Count));
            content.Append("1D mesh:\n");
            content.Append(string.Format("{0} nodes.\n", commonData.Vnet.Nodes.Count));
            content.Append("\n");
            content.Append("Vertices correspondence:\n");
            content.Append("<1DVertexId> <SceneListId>\n");

            foreach (var node in commonData.Vnet.Nodes)
            {
                content.Append(string.Format("{0} {1}\n", node.getId(), node.StructureLeafContainer.SceneListId));
            }

            File.WriteAllText(textBoxSave1DtreeFilename.Text, content.ToString());
            toolStripStatusLabel.Text = "1D structure saved.";
        }

        private void buttonLoad1Dtree_Click(object sender, EventArgs e)
        {
            if (!commonData.VnetLoaded)
                return;
            if (!sceneLoaded)
                return;
            if (rootSelected == null)
                return;
            if (!commonData.Vnet.UpdatePreallocatedVascularSystem(MAX_PREALLOCATED_NODES_ARRAY_SIZE))
            {
                MessageBox.Show("Preallocated VNet array not updated.");
                return;
            }

            string[] content = File.ReadAllLines(textBoxLoad1DtreeFilename.Text);
            string line;
            Match match;
            Regex fbxFileDescription = new Regex(@"([^,\n]+), (\d+) nodes.");
            Regex meshDescription = new Regex(@"(\d+) nodes.");
            Regex mappingLine = new Regex(@"(\d+) (\d+)");
            Dictionary<int, TreeNode> sceneListIdsToTreeNodesMap = new Dictionary<int, TreeNode>();


            if (content.Length < MAIN_CONTENT_HEADER_SIZE + SECONDARY_HEADER_SIZE)
            {
                MessageBox.Show("Wrong format of the mapping file.");
                return;
            }
            line = content[2];
            match = fbxFileDescription.Match(line);

            if (!match.Success)
            {
                MessageBox.Show("Wrong format of the mapping file.");
                return;
            }

            string fbxFilename = match.Groups[1].Value;
            int fbxNodesCount = int.Parse(match.Groups[2].Value);

            if ((!textBoxFBXfilename.Text.Equals(fbxFilename)) ||
                (sceneFBX.Nodes.Count != fbxNodesCount))
            {
                MessageBox.Show("Loaded FBX scene doesn't match to the mapped one.");
                return;
            }

            line = content[4];
            match = meshDescription.Match(line);

            if (!match.Success)
            {
                MessageBox.Show("Wrong format of the mapping file.");
                return;
            }
            int meshNodesCount = int.Parse(match.Groups[1].Value);

            if (commonData.Vnet.Nodes.Count != meshNodesCount)
            {
                MessageBox.Show("Loaded 1D mesh doesn't match to the mapped one.");
                return;
            }

            int i = MAIN_CONTENT_HEADER_SIZE;

            i += SECONDARY_HEADER_SIZE;

            root1Dstructure = rootSelected;
            // Create sceneListId -> treeNode map.
            foreach (var part in root1Dstructure.Children)
            {
                foreach (var leave in part.Children)
                {
                    sceneListIdsToTreeNodesMap[leave.SceneListId] = leave;
                    leave.Nodes1D.Clear();
                }
            }
            while (true)
            {
                if (i >= content.Length)
                    break;
                match = mappingLine.Match(content[i]);
                if (!match.Success)
                {
                    MessageBox.Show("Wrong format of mapping lines.");
                    return;
                }

                int vertexIndex = int.Parse(match.Groups[1].Value);
                int sceneListId = int.Parse(match.Groups[2].Value);

                TreeNode tn = sceneListIdsToTreeNodesMap[sceneListId];
                if (tn == null)
                {
                    MessageBox.Show("Invalid sceneNodeId in mapping lines.");
                    return;
                }
                Node meshNode = commonData.Vnet.PreallocatedVascularSystem[vertexIndex];
                if (meshNode == null)
                {
                    MessageBox.Show("Invalid 1D mesh node id in mapping lines.");
                    return;
                }
                tn.Nodes1D.Add(meshNode);
                meshNode.StructureLeafContainer = tn;
                i++;
            }
            toolStripStatusLabel.Text = "1D structure loaded.";
        }

        private void buttonInvertCoords_Click(object sender, EventArgs e)
        {
            foreach (Node n in commonData.Vnet.Nodes)
            {
                n.Position = -n.Position;
            }
            commonData.VnetChanged = true;
        }

        private void buttonCheckNodesConsistency_Click(object sender, EventArgs e)
        {
            if (!commonData.VnetLoaded)
                return;
            MessageBox.Show("Neighbours are " + (commonData.Vnet.hasNeighboursConsistency() ? "" : "not") + " consistent.");
        }

        private List<Node> getInitialNodesList()
        {
            Regex neighbour_id = new Regex(@"(\d+)\s*", RegexOptions.IgnoreCase);
            MatchCollection matches = neighbour_id.Matches(textBoxSetOfStartingNodes.Text);
            List<Node> nodes = new List<Node>();
            foreach (Match m in matches)
            {
                int id = Int32.Parse(m.Groups[1].Value);
                Node n = commonData.Vnet.Nodes.Find(x => x.getId() == id);
                if (n == null)
                {
                    MessageBox.Show("Incorrect id in initial set list.");
                    return nodes;
                }
                nodes.Add(n);
            }
            return nodes;
        }

        private void buttonSelectBranches_Click(object sender, EventArgs e)
        {
            double hardRadBound = (double) numericUpDownHardRadToCut.Value;
            double softRadBound = (double) numericUpDownSoftRadToCut.Value;
            int hardThreadsBound = (int)numericUpDownHardThreadsToCut.Value;
            int softThreadsBound = (int)numericUpDownSoftThreadsToCut.Value;
            double hardLengthBound = (double)numericUpDownHardLengthToCut.Value;
            double softLengthBound = (double)numericUpDownSoftLengthToCut.Value;

            Queue<DeepVascularThread> threadsToProcess = new Queue<DeepVascularThread>();
            List<Node> nodes = getInitialNodesList();
            foreach (Node n in nodes)
            {
                threadsToProcess.Enqueue(new DeepVascularThread(0, commonData.Vnet.getThread(n, n.getNeighbours()[0])));
            }

            commonData.Vnet.selectBranches(threadsToProcess, softRadBound, hardRadBound, softThreadsBound, hardThreadsBound,
                softLengthBound, hardLengthBound);

            commonData.SelectionChanged = true;
        }

        private void buttonBrowseSelectionFile_Click(object sender, EventArgs e)
        {
            DialogResult result = openSelectionFileDialog.ShowDialog();
            if (DialogResult.OK == result)
            {
                textBoxSelectionFileName.Text = openSelectionFileDialog.FileName;
            }
        }

        private void buttonSaveSelection_Click(object sender, EventArgs e)
        {
            if (!commonData.VnetLoaded)
                return;
            StringBuilder content = new StringBuilder();
            content.Append("1D nodes selection state.\n");

            foreach (var node in commonData.Vnet.Nodes)
            {
                content.Append(string.Format("{0} {1}\n", node.getId(), (node.TailSelectionFlag ? "1" : "0")));
            }

            File.WriteAllText(textBoxSelectionFileName.Text, content.ToString());
            toolStripStatusLabel.Text = "1D selection saved.";
        }

        private void buttonLoadSelection_Click(object sender, EventArgs e)
        {
            if (!commonData.VnetLoaded)
                return;

            if (!commonData.Vnet.UpdatePreallocatedVascularSystem(MAX_PREALLOCATED_NODES_ARRAY_SIZE))
            {
                MessageBox.Show("Preallocated VNet array not updated.");
                return;
            }

            string[] content = File.ReadAllLines(textBoxSelectionFileName.Text);
            Match match;
            Regex mappingLine = new Regex(@"(\d+) (\d)");

            if (content.Length != commonData.Vnet.Nodes.Count + 1)
            {
                MessageBox.Show("Count of nodes mismatch.");
                return;
            }

            for (int i = 1; i < content.Length; i++)
            {
                match = mappingLine.Match(content[i]);
                int vertexIndex = int.Parse(match.Groups[1].Value);
                int stateCode = int.Parse(match.Groups[2].Value);
                Node meshNode = commonData.Vnet.PreallocatedVascularSystem[vertexIndex];
                if (meshNode == null)
                {
                    MessageBox.Show("Invalid 1D mesh node id in mapping lines.");
                    return;
                }
                switch (stateCode)
                {
                    case 0:
                        meshNode.TailSelectionFlag = false;
                        break;
                    case 1:
                        meshNode.TailSelectionFlag = true;
                        break;
                    default:
                        MessageBox.Show("Invalid 1D mesh node id in mapping lines.");
                        return;
                }
            }

            toolStripStatusLabel.Text = "1D selection loaded.";

            commonData.SelectionChanged = true;
        }

        private void buttonMergeThreads_Click(object sender, EventArgs e)
        {
            if (!commonData.VnetLoaded)
                return;

            if (!commonData.Vnet.UpdatePreallocatedVascularSystem(MAX_PREALLOCATED_NODES_ARRAY_SIZE))
                return;

            Node nodeTh1From = commonData.Vnet.PreallocatedVascularSystem[(int)numericUpDownNodeIdThread1From.Value];
            Node nodeTh1To = commonData.Vnet.PreallocatedVascularSystem[(int)numericUpDownNodeIdThread1To.Value];
            Node nodeTh2From = commonData.Vnet.PreallocatedVascularSystem[(int)numericUpDownNodeIdThread2From.Value];
            Node nodeTh2To = commonData.Vnet.PreallocatedVascularSystem[(int)numericUpDownNodeIdThread2To.Value];

            if (nodeTh1From == null || nodeTh1To == null || nodeTh2From == null || nodeTh2To == null)
                return;

            VascularThread th1 = commonData.Vnet.getThread(nodeTh1From, nodeTh1To);
            VascularThread th2 = commonData.Vnet.getThread(nodeTh2From, nodeTh2To);

            if ((th1.nodes == null) || (th2.nodes == null))
                return;

            commonData.Vnet.mergeThreads(th1, th2);
            commonData.VnetChanged = true;

            toolStripStatusLabel.Text = "Threads merged.";
        }

        private void buttonKill1LengthThreads_Click(object sender, EventArgs e)
        {
            List<Node> core3 = new List<Node>();
            List<Tuple<Node, Node>> pairs = new List<Tuple<Node, Node>>();
            foreach (Node n in commonData.Vnet.Nodes)
            {
                n.is_processed = false;
                if (n.getNeighbours().Count > 2)
                {
                    core3.Add(n);
                }
            }
            foreach (Node c in core3)
            {
                foreach (Node n in c.getNeighbours())
                {
                    if (n.is_processed)
                        continue;
                    if (n.getNeighbours().Count > 2)
                    {
                        //1 length thread
                        pairs.Add(new Tuple<Node, Node>(c, n));
                    }
                }
                c.is_processed = true;
            }
            foreach (Tuple<Node, Node> pair in pairs)
            {
                commonData.VnetAccessMutex.WaitOne();

                Node sn1 = pair.Item1;
                Node sn2 = pair.Item2;

                int newId = commonData.Vnet.NodesCount;
                Vector3 newPosition = (sn1.Position + sn2.Position) / 2.0f;
                double newRadius = (sn1.Rad + sn2.Rad) / 2.0;

                Node newNode = new Node(newId, newPosition, newRadius);

                commonData.Vnet.Nodes.Add(newNode);

                if (sn1.getNeighbours().Contains(sn2))
                {
                    commonData.Vnet.RemoveConnection(sn1.getId(), sn2.getId());
                    commonData.Vnet.AddConnection(sn1.getId(), newId);
                    commonData.Vnet.AddConnection(newId, sn2.getId());
                }

                commonData.VnetChanged = true;

                commonData.VnetAccessMutex.ReleaseMutex();
            }
        }

        private void buttonSetSelCenterTo0_Click(object sender, EventArgs e)
        {
            numericUpDownSelCenterX.Value = 0;
            numericUpDownSelCenterY.Value = 0;
            numericUpDownSelCenterZ.Value = 0;
        }

        private void buttonReloadSelCenter_Click(object sender, EventArgs e)
        {
            LoadSelCenterData();
        }

        private void buttonApplySelCenter_Click(object sender, EventArgs e)
        {
            Vector3 newSelCenter;

            newSelCenter.X = (float)numericUpDownSelCenterX.Value;
            newSelCenter.Y = (float)numericUpDownSelCenterY.Value;
            newSelCenter.Z = (float)numericUpDownSelCenterZ.Value;

            commonData.VnetAccessMutex.WaitOne();

            commonData.Vnet.moveSelectedNodes(newSelCenter - selCenter);
            selCenter = newSelCenter;
            commonData.VnetChanged = true;
            toolStripStatusLabel.Text = "Selected nodes moved.";

            commonData.VnetAccessMutex.ReleaseMutex();
        }

        private void buttonShiftSelCenter_Click(object sender, EventArgs e)
        {
            numericUpDownSelCenterX.Value += numericUpDownSelCenterXShift.Value;
            numericUpDownSelCenterY.Value += numericUpDownSelCenterYShift.Value;
            numericUpDownSelCenterZ.Value += numericUpDownSelCenterZShift.Value;

            numericUpDownSelCenterXShift.Value = 0;
            numericUpDownSelCenterYShift.Value = 0;
            numericUpDownSelCenterZShift.Value = 0;
        }

        private void buttonPasteStoredVector_Click(object sender, EventArgs e)
        {
            numericUpDownSelCenterXShift.Value = (decimal)commonData.StoredVector.X;
            numericUpDownSelCenterYShift.Value = (decimal)commonData.StoredVector.Y;
            numericUpDownSelCenterZShift.Value = (decimal)commonData.StoredVector.Z;
        }

        private bool getThreadChecked(int idFrom, int idTo, ref VascularThread thread)
        {
            if (!commonData.VnetLoaded)
            {
                MessageBox.Show("Not loaded.");
                return false;
            }
            Node nodeFrom = commonData.Vnet.Nodes.Find(x => x.getId() == idFrom);
            Node nodeTo = commonData.Vnet.Nodes.Find(x => x.getId() == idTo);
            if ((nodeFrom == null) || (nodeTo == null))
            {
                MessageBox.Show("Nodes not found.");
                return false;
            }
            thread = commonData.Vnet.getThread(nodeFrom, nodeTo);
            if (thread.nodes == null)
            {
                MessageBox.Show("Thread not found.");
                return false;
            }
            return true;
        }

        private void buttonAddEndPoint_Click(object sender, EventArgs e)
        {
            int idFrom = (int)numericUpDownIDfrom.Value;
            int idTo = (int)numericUpDownIDtowards.Value;
            double newLength = (double)numericUpDownLength.Value / 100;
            VascularThread thread = new VascularThread();
            if (!getThreadChecked(idFrom, idTo, ref thread))
            {
                return;
            }
            int newId = 0;
            foreach (Node n in commonData.Vnet.Nodes)
            {
                if (n.getId() > newId)
                    newId = n.getId();
            }
            newId++;
            if (thread.getLength() < newLength)
            {
                Node newNode = thread.getNodeBehindTheEnd(newLength, newId);
                int idBefore = thread.nodes.Length - 1;
                Node nodeBefore = thread.nodes[idBefore];
                commonData.Vnet.Nodes.Add(newNode);
                commonData.Vnet.AddConnection(nodeBefore, newNode);
                MessageBox.Show("New node id: " + newNode.getId());
                commonData.VnetChanged = true;
            }
            else
            { //thread.getLength() >= newLength
                int idBefore = 0;
                int idAfter = 0;
                Node newNode = thread.getInterNode(newLength, false, newId, ref idBefore, ref idAfter);
                Node nodeBefore = thread.nodes[idBefore];
                Node nodeAfter = thread.nodes[idAfter];
                commonData.Vnet.Nodes.Add(newNode);
                commonData.Vnet.RemoveConnection(nodeBefore, nodeAfter);
                commonData.Vnet.AddConnection(nodeBefore, newNode);
                commonData.Vnet.AddConnection(newNode, nodeAfter);
                MessageBox.Show("New node id: " + newNode.getId());
                commonData.VnetChanged = true;
            }
        }

        private void numericUpDownThreadA0From_ValueChanged(object sender, EventArgs e)
        {
            if (radioButtonSetA0.Checked)
            {
                numericUpDownThreadRFrom.Value = (decimal)(Math.Sqrt((double)numericUpDownThreadA0From.Value / Math.PI) * 10);
            }
        }

        private void numericUpDownThreadA0to_ValueChanged(object sender, EventArgs e)
        {
            if (radioButtonSetA0.Checked)
            {
                numericUpDownThreadRTo.Value = (decimal)(Math.Sqrt((double)numericUpDownThreadA0To.Value / Math.PI) * 10);
            }
        }

        private void buttonSetThread_Click(object sender, EventArgs e)
        {
            int idFrom = (int)numericUpDownBSIDfrom.Value;
            int idTo = (int)numericUpDownBSIDtowards.Value;
            VascularThread thread = new VascularThread();
            if (!getThreadChecked(idFrom, idTo, ref thread))
            {
                MessageBox.Show("Thread not found.");
                return;
            }
            double betaFrom = (double)numericUpDownThreadBetaFrom.Value;
            double betaTo = (double)numericUpDownThreadBetaTo.Value;
            double RFrom = (double)numericUpDownThreadRFrom.Value;
            double RTo = (double)numericUpDownThreadRTo.Value;
            double A0From = (double)numericUpDownThreadA0From.Value;
            double A0To = (double)numericUpDownThreadA0To.Value;
            double totalLength = thread.getLength();
            double length = 0.0;
            double currentR = 0.0;
            for (int i = 0; i < thread.nodes.Length; i++)
            {
                if (i != 0)
                    length += Vector3.Distance(thread.nodes[i-1].Position, thread.nodes[i].Position);
                thread.nodes[i].Beta = (betaFrom + (betaTo - betaFrom) * length / totalLength) * 10000000;
                if (radioButtonLinearA0.Checked)
                    thread.nodes[i].setLumen_sq0((A0From + (A0To - A0From) * length / totalLength) / 10000);
                if (radioButtonLinearR.Checked)
                {
                    currentR = RFrom + (RTo - RFrom) * length / totalLength;
                    thread.nodes[i].setLumen_sq0(Math.PI * currentR * currentR * 1E-6);
                }
            }
            MessageBox.Show("Beta and LumenSq0 set.");
            commonData.VnetChanged = true;
        }

        private void groupBoxBetaSquare_Enter(object sender, EventArgs e)
        {

        }

        private void buttonBifurcationFix_Click(object sender, EventArgs e)
        {
            commonData.Vnet.fillBifurcationNodes();
        }

        private void buttonChangeTo0123_Click(object sender, EventArgs e)
        {
            List<Node> nodes = getInitialNodesList();
            for (int i = 0; i < nodes.Count; i++)
            {
                commonData.Vnet.moveNode(nodes[i].getId(), i);
            }
            commonData.VnetChanged = true;
        }

        private void buttonSetRto0_Click(object sender, EventArgs e)
        {
            if (!commonData.VnetLoaded)
                return;
            foreach (var n in commonData.Vnet.Nodes)
            {
                n.GroupId = 0;
            }
            commonData.VnetChanged = true;
            toolStripStatusLabel.Text = "Groups set to 0.";
        }

        private void buttonBuildGraph_Click(object sender, EventArgs e)
        {
            if (!commonData.VnetLoaded)
                return;
            graph = new Graph(commonData.Vnet, 0.5f, 5, 20);
            graphs.Clear();
            graphs.AddLast(graph);
            toolStripStatusLabel.Text = "Fine graph built.";
        }

        private void buttonCoarse_Click(object sender, EventArgs e)
        {
            String msg = graph.Vertices.Count + "\n";
            int levels = 1;
            partsSet = false;
            Graph g = null;
            do
            {
                g = graphs.Last.Value.Coarse((float)numericUpDownPartNu.Value, (float)numericUpDownPartQ.Value,
                    checkBoxPartSafro.Checked, (float)numericUpDownPartOverload.Value);
                graphs.AddLast(g);
                levels++;
                msg += g.Vertices.Count + "\n";
                if (g.Vertices.Count < 10)
                {
                    msg += "// ";
                    foreach (var v in g.Vertices)
                        msg += v.Weight + " ";
                }
                msg += "\n";
            }
            while (g.Vertices.Count > (int)numericUpDownPauseAt.Value);

			float minw = float.MaxValue;
			float maxw = 0.0f;

			foreach (var v in g.Vertices)
			{
				if (v.Weight < minw)
					minw = v.Weight;
				if (v.Weight > maxw)
					maxw = v.Weight;
			}

            MessageBox.Show(msg + "levels = " + levels + "\nmin vcnt = " + g.Vertices.Count);
            toolStripStatusLabel.Text = "Coarsening done (vcnt = "+g.Vertices.Count +")." +
				"minW = " +minw +" maxW = "+maxw;
        }

        private void buttonCoarsePlus_Click(object sender, EventArgs e)
        {
            Graph g = null;
            g = graphs.Last.Value.Coarse((float)numericUpDownPartNu.Value, (float)numericUpDownPartQ.Value,
                checkBoxPartSafro.Checked, (float)numericUpDownPartOverload.Value);
            graphs.AddLast(g);
            String msg = "min vcnt = " + g.Vertices.Count;
            if (g.Vertices.Count < 10)
            {
                msg += "\n// ";
                foreach (var v in g.Vertices)
                    msg += v.Weight + " ";
            }
			MessageBox.Show(msg);
			float minw = float.MaxValue;
			float maxw = 0.0f;

			foreach (var v in g.Vertices)
			{
				if (v.Weight < minw)
					minw = v.Weight;
				if (v.Weight > maxw)
					maxw = v.Weight;
			}
			toolStripStatusLabel.Text = "Coarsening done (vcnt = " + g.Vertices.Count + ")." +
				"minW = " + minw + " maxW = " + maxw;
        }

        private void buttonSetGroups_Click(object sender, EventArgs e)
        {
            if (!partsSet)
                graphs.Last.Value.SetGroupNumbers();
            var gg = graphs.Last.Previous;
            Random rnd = new Random();
            while (gg != null)
            {
                gg.Value.SetGroupNumbers(gg.Next.Value.Vertices, rnd);
                gg = gg.Previous;
            }
            commonData.VnetChanged = true;
            toolStripStatusLabel.Text = "Groups numbers set.";
        }

        private void buttonMinusGraph_Click(object sender, EventArgs e)
        {
            if (graphs.Count > 1)
            {
                graphs.RemoveLast();
			}
			float minw = float.MaxValue;
			float maxw = 0.0f;

			foreach (var v in graphs.Last.Value.Vertices)
			{
				if (v.Weight < minw)
					minw = v.Weight;
				if (v.Weight > maxw)
					maxw = v.Weight;
			}
			toolStripStatusLabel.Text = "Coarse level removed (vcnt = " + graphs.Last.Value.Vertices.Count + ")." +
				"minW = " + minw + " maxW = " + maxw;
        }

        private void buttonBlocksSaveBrowse_Click(object sender, EventArgs e)
        {
            DialogResult result = saveMeshFileDialog.ShowDialog();
            if (DialogResult.OK == result)
            {
                textBoxBlocksSave.Text = saveMeshFileDialog.FileName;
            }
        }

        private void buttonBlocksLoadBrowse_Click(object sender, EventArgs e)
        {
            DialogResult result = openMeshFileDialog.ShowDialog();
            if (DialogResult.OK == result)
            {
                textBoxBlocksLoad.Text = openMeshFileDialog.FileName;
            }
        }

        private void buttonBlocksSave_Click(object sender, EventArgs e)
        {
            StringBuilder output = new StringBuilder();
            Graph gr = graphs.Last.Value;
            output.Append(gr.Vertices.Count.ToString() + "\n");
            for (int iv = 0; iv < gr.Vertices.Count; iv++)
            {
                output.AppendFormat("{0} {1}", iv, gr.Vertices[iv].Weight.ToString("F8"));
                for (int inb = 0; inb < gr.Vertices[iv].NeighboursIds.Count; inb++)
                {
                    output.AppendFormat(" {0} {1}", gr.Vertices[iv].NeighboursIds[inb], 
                        gr.Vertices[iv].EdgesWeights[inb].ToString("F8"));
                }
                output.Append("\n");
            }
            File.WriteAllText(textBoxBlocksSave.Text, output.ToString());
        }

        private void buttonBlocksLoad_Click(object sender, EventArgs e)
        {
            String[] lines = File.ReadAllLines(textBoxBlocksLoad.Text);
            Regex rpair = new Regex(@"(\d+)\s+(\d+)");
            Graph g = graphs.Last.Value;
            foreach (String line in lines)
            {
                Match m = rpair.Match(line);
                int ai = Int32.Parse(m.Groups[1].Value);
                int ap = Int32.Parse(m.Groups[2].Value);
                g.Vertices[ai].SetGroupNumber(ap);
            }
            partsSet = true;
        }

        private void buttonBrowseStatFile_Click(object sender, EventArgs e)
        {
            DialogResult result = saveMeshFileDialog.ShowDialog();
            if (DialogResult.OK == result)
            {
                textBoxStatFile.Text = saveMeshFileDialog.FileName;
            }
        }

        private void buttonSaveStatFile_Click(object sender, EventArgs e)
        {
            StringBuilder output = new StringBuilder();

            int totalNodes = 0;
            int totalBorderNodes = 0;

            for (int listPos = 0; listPos < aggrNodesStat.Count; listPos++)
            {
                totalNodes += aggrNodesStat[listPos].nodesCount;
                totalBorderNodes += aggrNodesStat[listPos].borderNodesConut;
            }

            float meanNodes = (float)totalNodes / aggrNodesStat.Count;
            float meanBorderNodes = (float)totalBorderNodes / aggrNodesStat.Count;

            float sumDiffSqNodes = 0.0f;
            float sumDiffSqBorderNodes = 0.0f;

            for (int listPos = 0; listPos < aggrNodesStat.Count; listPos++)
            {
                sumDiffSqNodes += (aggrNodesStat[listPos].nodesCount - meanNodes) * 
                                  (aggrNodesStat[listPos].nodesCount - meanNodes);
                sumDiffSqBorderNodes += (aggrNodesStat[listPos].borderNodesConut - meanBorderNodes) *
                                        (aggrNodesStat[listPos].borderNodesConut - meanBorderNodes);
            }

            float Dnodes = (aggrNodesStat.Count > 1) ?
                sumDiffSqNodes / (aggrNodesStat.Count - 1)
                : 0.0f;
            float Dbordernodes = (aggrNodesStat.Count > 1) ?
                sumDiffSqBorderNodes / (aggrNodesStat.Count - 1)
                : 0.0f;

            float rmsqNodes = (float)Math.Sqrt(Dnodes);
            float rmsqBorderNodes = (float)Math.Sqrt(Dbordernodes);

            for (int listPos = 0; listPos < aggrNodesStat.Count; listPos++)
            {
                output.AppendFormat("{0}\t{1}\n", aggrNodesStat[listPos].nodesCount, aggrNodesStat[listPos].borderNodesConut);
            }
            output.Append("-------------\n");
            output.AppendFormat("{0}\t{1}\n", meanNodes.ToString("F8"), meanBorderNodes.ToString("F8"));
            output.AppendFormat("{0}\t{1}\n", rmsqNodes.ToString("F8"), rmsqBorderNodes.ToString("F8"));
            File.WriteAllText(textBoxStatFile.Text, output.ToString());
        }

        private void buttonCalculateStat_Click(object sender, EventArgs e)
        {
            if (!commonData.VnetLoaded)
                return;
            aggrNodesStat.Clear();
            foreach (var v in commonData.Vnet.Nodes)
            {
                int gid = v.GroupId;
                // Assume gid = 1, 2, ...
                if (gid > aggrNodesStat.Count)
                {
                    for (int aid = aggrNodesStat.Count + 1; aid <= gid; aid++)
                    {
                        aggrNodesStat.Add(new AggrStat(aid));
                    }
                }
                int listPos = gid - 1;
                aggrNodesStat[listPos].nodesCount++;
                bool diffGidFound = false;
                foreach (var n in v.getNeighbours())
                {
                    if (n.GroupId != gid)
                    {
                        diffGidFound = true;
                        break;
                    }
                }
                if (diffGidFound)
                {
                    aggrNodesStat[listPos].borderNodesConut++;
                }
            }
        }

        private void radioButtonSetR_CheckedChanged(object sender, EventArgs e)
        {
            numericUpDownThreadA0From.ReadOnly = !radioButtonSetA0.Checked;
            numericUpDownThreadA0To.ReadOnly = !radioButtonSetA0.Checked;
            numericUpDownThreadRFrom.ReadOnly = !radioButtonSetR.Checked;
            numericUpDownThreadRTo.ReadOnly = !radioButtonSetR.Checked;
        }

        private void numericUpDownThreadRFrom_ValueChanged(object sender, EventArgs e)
        {
            if (radioButtonSetR.Checked)
            {
                numericUpDownThreadA0From.Value = (decimal)
                    ((double)numericUpDownThreadRFrom.Value * (double)numericUpDownThreadRFrom.Value / 100 * Math.PI);
            }
        }

        private void numericUpDownThreadRTo_ValueChanged(object sender, EventArgs e)
        {
            if (radioButtonSetR.Checked)
            {
                numericUpDownThreadA0To.Value = (decimal)
                    ((double)numericUpDownThreadRTo.Value * (double)numericUpDownThreadRTo.Value / 100 * Math.PI);
            }
        }

        private void buttonMirrorSelected_Click(object sender, EventArgs e)
        {
            float atX;

            atX = (float)numericUpDownMirrorX.Value;

            commonData.VnetAccessMutex.WaitOne();

            commonData.Vnet.mirrorSelectedNodesOYZ(atX);

            commonData.VnetChanged = true;
            toolStripStatusLabel.Text = "Selected nodes mirrored.";

            commonData.VnetAccessMutex.ReleaseMutex();
        }

        private void buttonPrintMurray_Click(object sender, EventArgs e)
        {
            commonData.Vnet.PrintMurrayStats(textBoxInputFile.Text + ".murray");
        }

        private void buttonRemoveTerminalsShorterThanRes_Click(object sender, EventArgs e)
        {
            commonData.VnetAccessMutex.WaitOne();
            commonData.Vnet.RemoveShortTerminals((float)numericUpDownResolution.Value);
            
            commonData.VnetChanged = true;
            needCenterUpdate = true;
            
            commonData.VnetAccessMutex.ReleaseMutex();
        }

        private void buttonResetThreadsIteration_Click(object sender, EventArgs e)
        {
            commonData.Vnet.reindexNodes();
            pairsCloseIndex = 0;
            pairsCloseIndexNB = 0;
            commonData.Vnet.Nodes.ForEach(x => x.is_processed = false);
            float minDist = (float)numericUpDownResolution.Value;
            int node1 = -1;
            int node2 = -1;
            int count = 0;
            commonData.Vnet.Nodes.ForEach(x => {
                x.getNeighbours().ForEach(xx =>
                {
                    if ((x.Position - xx.Position).Length() < minDist)
                    {
                        minDist = (x.Position - xx.Position).Length();
                        node1 = x.getId();
                        node2 = xx.getId();
                    }
                    if ((x.Position - xx.Position).Length() < (float)(numericUpDownResFactor.Value * numericUpDownResolution.Value)) {
                        if (x.getId() < xx.getId())
                        {
                            Console.WriteLine("dist = " + (x.Position - xx.Position).Length() + ", nodes = " + x.getId() + " " + xx.getId());
                            count++;
                        }
                    }
                });
            });
            Console.WriteLine("count = " + count);
            Console.WriteLine("Min dist = " + minDist + ", nodes = " + node1 + " " + node2);
        }

        private void buttonSelectNextThread_Click(object sender, EventArgs e)
        {
            for (; pairsCloseIndex < commonData.Vnet.Nodes.Count; pairsCloseIndex++)
            {
                for (; pairsCloseIndexNB < commonData.Vnet.Nodes[pairsCloseIndex].getNeighbours().Count; pairsCloseIndexNB++)
                {
                    Node n = commonData.Vnet.Nodes[pairsCloseIndex];
                    Node nb = n.getNeighbours()[pairsCloseIndexNB];
                    //Console.WriteLine(n.getId() + " " + nb.getId());
                    if (n.getId() > nb.getId())
                        continue;
                    float dist = (n.Position - nb.Position).Length();
                    if (dist > (float)(numericUpDownResFactor.Value * numericUpDownResolution.Value))
                        continue;
                    Console.WriteLine(n.getId() + " " + nb.getId() + " " + dist);
                    if (n.is_processed)
                        continue;
                    // Too close.
                    commonData.SelectedNodeAccessMutex.WaitOne();

                    commonData.SwitchSelectionTo = n.getId();
                    commonData.SwitchSecondSelectionTo = nb.getId();

                    commonData.SelectedNodeAccessMutex.ReleaseMutex();
                    nb.is_processed = true;
                    n.is_processed = true;
                    break;
                }
                pairsCloseIndexNB = 0;
            }
        }
    }
    public class AggrStat
    {
        public int aggrId;
        public int nodesCount;
        public int borderNodesConut;

        public AggrStat(int _aggrId)
        {
            aggrId = _aggrId;
            nodesCount = 0;
            borderNodesConut = 0;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is AggrStat))
                return false;
            AggrStat ao = (AggrStat)obj;
            return aggrId == ao.aggrId;
        }

        public override int GetHashCode()
        {
            return aggrId.GetHashCode();
        }
    }


}
