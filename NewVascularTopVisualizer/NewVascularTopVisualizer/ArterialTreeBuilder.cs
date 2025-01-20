using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Graphics;
using Fusion.Mathematics;

namespace NewVascularTopVisualizer
{
    //public class TreeNode1D
    //{
    //    private TreeNode1D parent;
    //    private List<TreeNode1D> children;
    //    private List<Node> nodes;
    //    private TreeNode referencedNode;

    //    public TreeNode1D(TreeNode _referencedNode)
    //    {
    //        parent = null;
    //        children = new List<TreeNode1D>();
    //        nodes = new List<Node>();
    //        referencedNode = _referencedNode;
    //    }

    //    public TreeNode1D Parent
    //    {
    //        get
    //        {
    //            return parent;
    //        }
    //        set
    //        {
    //            parent = value;
    //        }
    //    }

    //    public List<TreeNode1D> Children
    //    {
    //        get
    //        {
    //            return children;
    //        }
    //    }

    //    public List<Node> Nodes
    //    {
    //        get
    //        {
    //            return nodes;
    //        }
    //    }

    //    public string Name
    //    {
    //        get
    //        {
    //            return referencedNode.SceneNode.Name;
    //        }
    //    }

    //    public TreeNode ReferencedNode
    //    {
    //        get
    //        {
    //            return referencedNode;
    //        }
    //    }

    //    public override string ToString()
    //    {
    //        return ReferencedNode.ToString();
    //    }

    //    public override bool Equals(object obj)
    //    {
    //        if (!(obj is TreeNode1D))
    //            return false;
    //        TreeNode1D tn = (TreeNode1D)obj;
    //        return (tn.ReferencedNode.Equals(ReferencedNode));
    //    }

    //    public override int GetHashCode()
    //    {
    //        return ReferencedNode.GetHashCode();
    //    }
    //}

    public struct UniqueVertexIdentifier
    {
        public TreeNode treeNode;
        public int vertexIndex;

        public UniqueVertexIdentifier(TreeNode _treeNode, int _vertexIndex)
        {
            treeNode = _treeNode;
            vertexIndex = _vertexIndex;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is UniqueVertexIdentifier))
                return false;
            UniqueVertexIdentifier uvi = (UniqueVertexIdentifier)obj;
            return (treeNode.Equals(uvi.treeNode) && vertexIndex == uvi.vertexIndex);
        }

        public override int GetHashCode()
        {
            return vertexIndex.GetHashCode() ^ treeNode.GetHashCode();
        }
    }

    public struct UniqueVertexIdentifierWithDistanceSq
    {
        public UniqueVertexIdentifier uVertexId;
        public float distanceSq;

        public UniqueVertexIdentifierWithDistanceSq(UniqueVertexIdentifier uvi, float distSq)
        {
            uVertexId = uvi;
            distanceSq = distSq;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is UniqueVertexIdentifierWithDistanceSq))
                return false;
            UniqueVertexIdentifierWithDistanceSq uvi = (UniqueVertexIdentifierWithDistanceSq)obj;
            return (uVertexId.Equals(uvi.uVertexId) && distanceSq == uvi.distanceSq);
        }

        public override int GetHashCode()
        {
            return uVertexId.GetHashCode() ^ distanceSq.GetHashCode();
        }
    }

    public class UVIWDSbyDistanceComparer : IComparer<UniqueVertexIdentifierWithDistanceSq>
    {
        public int Compare(UniqueVertexIdentifierWithDistanceSq x, UniqueVertexIdentifierWithDistanceSq y)
        {
            if (x.distanceSq < y.distanceSq)
                return -1;
            if (x.distanceSq > y.distanceSq)
                return 1;
            return 0;
        }
    }


    public class UniqueVertexIdentifierValueEqComparer : IEqualityComparer<UniqueVertexIdentifier>
    {
        public bool Equals(UniqueVertexIdentifier obj1, UniqueVertexIdentifier obj2)
        {
            if (!obj1.treeNode.Equals(obj2.treeNode))
                return false;
            if (!obj1.treeNode.Vertices[obj1.vertexIndex].Equals(
                obj2.treeNode.Vertices[obj2.vertexIndex]))
                return false;
            return true;
        }

        public int GetHashCode(UniqueVertexIdentifier obj)
        {
            return obj.treeNode.GetHashCode() ^ obj.treeNode.Vertices[obj.vertexIndex].GetHashCode();
        }
    }

    public class TreeNode
    {
        private TreeNode parent;
        private List<TreeNode> children;
        private Fusion.Graphics.Node sceneNode;
        private int sceneListId;
        private Vector3[] vertices;
        private Dictionary<int, List<int>> fbxVerticesTo1DverticesMap;
        private List<Node> nodes1D;

        public TreeNode(int id, Fusion.Graphics.Node _sceneNode, Vector3[] _vertices)
        {
            sceneListId = id;
            parent = null;
            children = new List<TreeNode>();
            sceneNode = _sceneNode;
            vertices = _vertices;
            fbxVerticesTo1DverticesMap = new Dictionary<int, List<int>>();
            nodes1D = new List<Node>();
        }

        public int SceneListId
        {
            get
            {
                return sceneListId;
            }
        }

        public TreeNode Parent
        {
            get
            {
                return parent;
            }
            set
            {
                parent = value;
            }
        }

        public List<TreeNode> Children
        {
            get
            {
                return children;
            }
        }

        public Fusion.Graphics.Node SceneNode
        {
            get
            {
                return sceneNode;
            }
        }

        public Vector3[] Vertices
        {
            get
            {
                return vertices;
            }
        }

        public void scaleVerticesPositionBy(float factor)
        {
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] *= factor;
            }
        }

        public Dictionary<int, List<int>> FbxVerticesTo1DverticesMap
        {
            get
            {
                return fbxVerticesTo1DverticesMap;
            }
        }

        public List<Node> Nodes1D
        {
            get
            {
                return nodes1D;
            }
        }

        public int getNearestVertexIndex(Node node)
        {
            int index = -1;
            float distanceSq = float.PositiveInfinity;
            float newDistanceSq;
            for (int i = 0; i < Vertices.Length; i++)
            {
                newDistanceSq = Vector3.DistanceSquared(node.Position, Vertices[i]);
                if (newDistanceSq < distanceSq)
                {
                    index = i;
                }
            }
            return index;
        }

        public override string ToString()
        {
            string indent = "";
            bool oneLevelSkipped = false;
            TreeNode parent = this.Parent;
            while (parent != null)
            {
                if (oneLevelSkipped)
                    indent += "    ";
                else
                    oneLevelSkipped = true;
                parent = parent.Parent;
            }
            return indent + SceneNode.Name.Replace("Circulatory", "C-y").Replace("Textured", "T-d") + " (" +
                Nodes1D.Count + ")";
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (!(obj is TreeNode))
            {
                return false;
            }
            return ((TreeNode)obj).SceneListId == this.SceneListId;
        }

        public override int GetHashCode()
        {
            return SceneListId.GetHashCode();
        }
    }

    public class TreeNodeNamesComparer : Comparer<TreeNode>
    {
        public override int Compare(TreeNode x, TreeNode y)
        {
            return x.SceneNode.Name.CompareTo(y.SceneNode.Name);
        }
    }

    class ArterialTreeBuilder
    {
        public static Vector3[] getVertices(Mesh mesh, ref Matrix worldMatrix, bool doTransform)
        {
            Vector3[] verticesLocal = mesh.Vertices.Select(x => x.Position).ToArray();
            if (doTransform)
            {
                Vector3[] verticesGlobal = new Vector3[verticesLocal.Length];
                Vector3.TransformCoordinate(verticesLocal, ref worldMatrix, verticesGlobal);
                return verticesGlobal;
            }
            else
            {
                return verticesLocal;
            }
        }

        public static TreeNode buildArterialTree(Scene scene, float measure)
        {
            TreeNode root;
            // List of "..Arteries.." nodes.
            List<TreeNode> vesselTypeNodes = new List<TreeNode>();
            // World matrices for nodes.
            Matrix[] worldMatrices = new Matrix[scene.Nodes.Count];
            scene.CopyAbsoluteTransformsTo(worldMatrices);

            // Get root of circulatory nodes.
            int circulatoryRoot = -1;
            for (int i = 0; i < scene.Nodes.Count; i++)
            {
                // Go through root node's children.
                if (scene.Nodes[i].ParentIndex != 0)
                    continue;
                if (scene.Nodes[i].Name.Contains("Circulatory"))
                {
                    circulatoryRoot = i;
                    break;
                }
            }

            if (circulatoryRoot < 0)
                throw new ArgumentException("Root node not found.");

            root = new TreeNode(circulatoryRoot, scene.Nodes[circulatoryRoot], null);

            // Get circulatory root node's children.
            for (int i = 0; i < scene.Nodes.Count; i++)
            {
                if (scene.Nodes[i].ParentIndex != circulatoryRoot)
                    continue;
                if (scene.Nodes[i].Name.Contains("Heart"))
                    continue;
                TreeNode partNode = new TreeNode(i, scene.Nodes[i], null);
                root.Children.Add(partNode);
                partNode.Parent = root;
            }

            if (root.Children.Count == 0)
                throw new ArgumentException("Sealed root node.");

            //Get circulatory root node's Arteria grandchildren.
            for (int i = 0; i < scene.Nodes.Count; i++)
            {
                if (!scene.Nodes[i].Name.Contains("Arteries"))
                    continue;
                TreeNode parent = root.Children.Find(x => (x.SceneListId == scene.Nodes[i].ParentIndex));
                if (parent != null)
                {
                    TreeNode partNode = new TreeNode(i, scene.Nodes[i], null);
                    vesselTypeNodes.Add(partNode);
                    partNode.Parent = parent;
                }
            }

            if (vesselTypeNodes.Count == 0)
                throw new ArgumentException("Arteries nodes not found.");

            //Get Arteries node's children.
            for (int i = 0; i < scene.Nodes.Count; i++)
            {
                TreeNode parent = vesselTypeNodes.Find(x => (x.SceneListId == scene.Nodes[i].ParentIndex));
                if (parent != null)
                {
                    int meshIndex = scene.Nodes[i].MeshIndex;
                    TreeNode partNode;
                    if (meshIndex == -1)
                        partNode = new TreeNode(i, scene.Nodes[i], null);
                    else
                    {
                        partNode = new TreeNode(i, scene.Nodes[i], getVertices(scene.Meshes[meshIndex], 
                            ref worldMatrices[i], true));
                        partNode.scaleVerticesPositionBy(measure);
                    }
                    parent.Parent.Children.Add(partNode);
                    partNode.Parent = parent.Parent;
                }
            }

            return root;
        }

        public static TreeNode getMostFrequentTreeNode(Node node1D)
        {
            UniqueVertexIdentifierValueEqComparer uviviec = new UniqueVertexIdentifierValueEqComparer();
            HashSet<UniqueVertexIdentifier> uniqueVertices = new HashSet<UniqueVertexIdentifier>(uviviec);
            Dictionary<TreeNode, int> uniqueFBXVerticesCount = new Dictionary<TreeNode, int>();

            if (node1D.MappedVertices.Count == 0)
                return null;

            uniqueVertices.Clear();
            uniqueFBXVerticesCount.Clear();
            foreach (var vertexId in node1D.MappedVertices)
            {
                if (uniqueVertices.Add(vertexId))
                {
                    if (uniqueFBXVerticesCount.ContainsKey(vertexId.treeNode))
                    {
                        uniqueFBXVerticesCount[vertexId.treeNode]++;
                    }
                    else
                    {
                        uniqueFBXVerticesCount[vertexId.treeNode] = 1;
                    }
                }
            }

            int maxValue = -1;
            TreeNode maxValueKey = null;
            foreach (var key in uniqueFBXVerticesCount.Keys)
            {
                if (uniqueFBXVerticesCount[key] > maxValue)
                {
                    maxValue = uniqueFBXVerticesCount[key];
                    maxValueKey = key;
                }
            }

            return maxValueKey;
        }

        public static Tuple<TreeNode, int> getMostFrequentTreeNode(VascularThread thread)
        {
            Dictionary<TreeNode, int> uniqueTreeNodesCount = new Dictionary<TreeNode, int>();

            if (thread.nodes.Length == 0)
                return null;

            uniqueTreeNodesCount.Clear();
            foreach (var node in thread.nodes)
            {
                if (uniqueTreeNodesCount.ContainsKey(node.StructureLeafContainer))
                {
                    uniqueTreeNodesCount[node.StructureLeafContainer]++;
                }
                else
                {
                    uniqueTreeNodesCount[node.StructureLeafContainer] = 1;
                }
            }

            int maxValue = -1;
            TreeNode maxValueKey = null;
            foreach (var key in uniqueTreeNodesCount.Keys)
            {
                if (uniqueTreeNodesCount[key] > maxValue)
                {
                    maxValue = uniqueTreeNodesCount[key];
                    maxValueKey = key;
                }
            }

            return new Tuple<TreeNode, int>(maxValueKey, maxValue);
        }

        public static TreeNode build1DTree(TreeNode rootSelected, List<Node> vascular_system)
        {
            foreach (var part in rootSelected.Children)
            {
                foreach (var leaf in part.Children)
                {
                    leaf.Nodes1D.Clear();
                }
            }

            // Put 1D nodes to "most frequent" treenodes.
            foreach (var vnetNode in vascular_system)
            {
                TreeNode mostFqNode = getMostFrequentTreeNode(vnetNode);

                mostFqNode.Nodes1D.Add(vnetNode);
                vnetNode.StructureLeafContainer = mostFqNode;
            }

            //Move T-ends of length 1 to other treenodes.
            for (int i = 0; i < vascular_system.Count; i++)
            {
                Node currentNode = vascular_system[i];
                List<Node> neighbours = currentNode.getNeighbours();
                if (neighbours.Count < 3)
                    continue;
                TreeNode x1DnodeAssignedTo = currentNode.StructureLeafContainer;
                foreach (Node n in neighbours)
                {
                    TreeNode n1DnodeAssignedTo = n.StructureLeafContainer;
                    if (!n1DnodeAssignedTo.Equals(x1DnodeAssignedTo))
                    {
                        // Neighbour is already in different TreeNode, nothing to do.
                        continue;
                    }
                    Node nextNode = n.getNeighbours().Find(x => (!x.Equals(currentNode)));
                    if (nextNode == null)
                    {
                        // Terminal thread, nothing to do.
                        continue;
                    }
                    TreeNode next1DnodeAssignedTo = nextNode.StructureLeafContainer;
                    if (!next1DnodeAssignedTo.Equals(x1DnodeAssignedTo))
                    {
                        // T-end of length 1 found.
                        TreeNode nodeToMoveFrom = null;
                        TreeNode nodeToMoveTo = null;

                        // Check for >- < situation.
                        if (nextNode.getNeighbours().Count > 2)
                        {
                            // >- < situation found. 
                            // Possibly mid-node >-o < (Node n) need to be moved.
                            
                            // Moving mid-node to the X node with less radius.
                            if (nextNode.Rad < currentNode.Rad)
                            {
                                // Move mid-node to nextNode's TreeNode.
                                nodeToMoveFrom = currentNode.StructureLeafContainer;
                                nodeToMoveTo = nextNode.StructureLeafContainer;
                                nodeToMoveFrom.Nodes1D.Remove(n);
                                nodeToMoveTo.Nodes1D.Add(n);
                                n.StructureLeafContainer = nodeToMoveTo;
                            }
                        }

                        // Check if there is second T-end on the other side.
                        if (nextNode.getNeighbours().Count == 2)
                        {
                            Node possiblyXNode = nextNode.getNeighbours().Find(x => (!x.Equals(n)));
                            if ((possiblyXNode.getNeighbours().Count > 2) &&
                                (possiblyXNode.StructureLeafContainer.Equals(next1DnodeAssignedTo)))
                            {
                                // >- -< situation found (double T-node).
                                // Moving nodes to the X node with less radius.
                                if (currentNode.Rad < possiblyXNode.Rad)
                                {
                                    // Move mid-node >- o-< (Node nextNode) to currentNode's TreeNode.
                                    nodeToMoveFrom = possiblyXNode.StructureLeafContainer;
                                    nodeToMoveTo = currentNode.StructureLeafContainer;

                                    nodeToMoveFrom.Nodes1D.Remove(nextNode);
                                    nodeToMoveTo.Nodes1D.Add(nextNode);
                                    nextNode.StructureLeafContainer = nodeToMoveTo;
                                }
                                else
                                {
                                    // Move mid-node >-o -< (Node n) to possiblyXNode's TreeNode.
                                    nodeToMoveFrom = currentNode.StructureLeafContainer;
                                    nodeToMoveTo = possiblyXNode.StructureLeafContainer;

                                    nodeToMoveFrom.Nodes1D.Remove(n);
                                    nodeToMoveTo.Nodes1D.Add(n);
                                    n.StructureLeafContainer = nodeToMoveTo;
                                }
                                // Moving done, continue.
                                continue;
                            }
                        }

                        // Ordinary >- -- or >- - situation.
                        // Moving the neighbour to the different treenode.
                        n1DnodeAssignedTo.Nodes1D.Remove(n);
                        next1DnodeAssignedTo.Nodes1D.Add(n);
                        n.StructureLeafContainer = next1DnodeAssignedTo;
                    }
                }
            }

            return rootSelected;
        }

        public static int recombine1DTreeScraps(VascularNet vnet, float singleNodePart)
        {
            List<Node> vascular_system = vnet.Nodes;
            List<Tuple<VascularThread, TreeNode>> threadsToRecombine = new List<Tuple<VascularThread, TreeNode>>();
            Dictionary<TreeNode, int> nodesCount = new Dictionary<TreeNode,int>();
            int recombined = 0;

            foreach (Node n in vascular_system)
            {
                n.is_processed = false;
            }

            for (int i = 0; i < vascular_system.Count; i++)
            {
                Node currentNode = vascular_system[i];
                //if ((currentNode.getId() == 19273) || (currentNode.getId() == 19274))
                //{
                //    recombined++;
                //}
                List<Node> neighbours = currentNode.getNeighbours();
                if (neighbours.Count > 2)
                {
                    foreach (Node n in neighbours)
                    {
                        if (n.is_processed)
                            continue;
                        VascularThread th = vnet.getThread(currentNode, n);
                        //recombined++;
                        foreach (Node nth in th.nodes)
                        {
                            nth.is_processed = true;
                        }

                        // Most frequent TreeNode
                        //var mostFreqTreeNode = getMostFrequentTreeNode(th);
                        //if (((float)mostFreqTreeNode.Item2 / th.nodes.Length) > singleNodePart)
                        //{
                        //    // Dominant TreeNode exist.
                        //    threadsToRecombine.Add(new Tuple<VascularThread, TreeNode>(th, mostFreqTreeNode.Item1));
                        //}

                        // Marginal nodes placed in the same TreeNode.
                        //if (th.getLength() < 3)
                        //    continue;
                        //var firstTreeNode = th.nodes[0].StructureLeafContainer;
                        //var lastTreeNode = th.nodes[th.nodes.Length - 1].StructureLeafContainer;
                        //if (firstTreeNode == null)
                        //    continue;
                        //if (firstTreeNode.Equals(lastTreeNode))
                        //{
                        //    // Marginal nodes placed in the same TreeNode.
                        //    threadsToRecombine.Add(new Tuple<VascularThread, TreeNode>(th, firstTreeNode));
                        //}

                        // o-o-x-x-o state search.
                        if (th.nodes.Length < 3)
                            continue;
                        int ii = 0;
                        int jj = 1;
                        int kk = 0;
                        TreeNode firstTreeNode = th.nodes[ii].StructureLeafContainer;
                        TreeNode nextTreeNode = th.nodes[jj].StructureLeafContainer;
                        TreeNode lastTreeNode;
                        while (nextTreeNode.Equals(firstTreeNode))
                        {
                            //if ((th.nodes[jj].getId() == 11776) || (th.nodes[jj].getId() == 11766) ||
                            //    (th.nodes[jj].getId() == 11778))
                            //{
                            //    recombined++;
                            //}
                            jj++;

                            if (jj < th.nodes.Length)
                            {
                                nextTreeNode = th.nodes[jj].StructureLeafContainer;
                            }
                            else
                                break;
                        }
                        while (true)
                        {
                            kk = jj + 1;
                            if (kk >= th.nodes.Length)
                                break;
                            lastTreeNode = th.nodes[kk].StructureLeafContainer;
                            while (lastTreeNode.Equals(nextTreeNode))
                            {
                                //if ((th.nodes[kk].getId() == 11776) || (th.nodes[kk].getId() == 11766) ||
                                //    (th.nodes[kk].getId() == 11778))
                                //{
                                //    recombined++;
                                //}
                                kk++;
                                if (kk < th.nodes.Length)
                                {
                                    lastTreeNode = th.nodes[kk].StructureLeafContainer;
                                }
                                else
                                    break;
                            }
                            if (kk < th.nodes.Length)
                            {
                                if (lastTreeNode.Equals(firstTreeNode))
                                {
                                    // Moving nodes "in the middle".
                                    for (int tt = jj; tt < kk; tt++)
                                    {
                                        nextTreeNode.Nodes1D.Remove(th.nodes[tt]);
                                        firstTreeNode.Nodes1D.Add(th.nodes[tt]);
                                        th.nodes[tt].StructureLeafContainer = firstTreeNode;
                                    }
                                    recombined++;
                                    // Go on.
                                    ii = kk;
                                    firstTreeNode = lastTreeNode;
                                    jj = kk + 1;
                                    if (jj < th.nodes.Length)
                                    {
                                        nextTreeNode = th.nodes[jj].StructureLeafContainer;
                                    }
                                    else
                                        break;
                                    while (nextTreeNode.Equals(firstTreeNode))
                                    {
                                        jj++;
                                        if (jj < th.nodes.Length)
                                        {
                                            nextTreeNode = th.nodes[jj].StructureLeafContainer;
                                        }
                                        else
                                            break;
                                    }
                                }
                                else
                                {
                                    // Go on.
                                    ii = jj;
                                    firstTreeNode = nextTreeNode;
                                    jj = kk;
                                    nextTreeNode = lastTreeNode;
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                        //if (firstTreeNode == null)
                        //    continue;
                        //if (firstTreeNode.Equals(lastTreeNode))
                        //{
                        //    // Marginal nodes placed in the same TreeNode.
                        //    threadsToRecombine.Add(new Tuple<VascularThread, TreeNode>(th, firstTreeNode));
                        //}
                    }
                }
            }

            //foreach (var tuple in threadsToRecombine)
            //{
            //    foreach (var node in tuple.Item1.nodes)
            //    {
            //        if (node.StructureLeafContainer.Equals(tuple.Item2))
            //            continue;
            //        // Move node to the dominant TreeNode.
            //        node.StructureLeafContainer.Nodes1D.Remove(node);
            //        tuple.Item2.Nodes1D.Add(node);
            //        node.StructureLeafContainer = tuple.Item2;
            //    }
            //}

            return recombined;
        }
    }
}
