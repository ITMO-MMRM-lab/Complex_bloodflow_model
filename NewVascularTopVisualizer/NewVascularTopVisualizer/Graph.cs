using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewVascularTopVisualizer
{
    class SparseMatrixElem
    {
        private int indexI;
        private int indexJ;
        private float value;

        public SparseMatrixElem(int i, int j, float val)
        {
            indexI = i;
            indexJ = j;
            value = val;
        }

        public float Value
        {
            get
            {
                return value;
            }
            set
            {
                this.value = value;
            }
        }

        public int IndexI
        {
            get
            {
                return indexI;
            }
        }

        public int IndexJ
        {
            get
            {
                return indexJ;
            }
        }

        public override bool Equals(object obj)
        {
            if (!(obj is SparseMatrixElem))
                return false;
            SparseMatrixElem sobj = (SparseMatrixElem)obj;
            if (sobj.indexI != indexI)
                return false;
            if (sobj.indexJ != indexJ)
                return false;
            return true;
        }

        public override int GetHashCode()
        {
            return indexI.GetHashCode() ^ indexJ.GetHashCode();
        }
    }

    class SparseMatrix
    {
        private HashSet<SparseMatrixElem> nonzeros;
        private int n;
        private int m;

        public SparseMatrix(int _n, int _m)
        {
            n = _n;
            m = _m;
            nonzeros = new HashSet<SparseMatrixElem>();
        }

        public void SetDimensions(int _n, int _m)
        {
            n = _n;
            m = _m;
        }

        public bool SetValue(int i, int j, float value)
        {
            SparseMatrixElem newElem = new SparseMatrixElem(i, j, value);
            SparseMatrixElem oldElem = nonzeros.FirstOrDefault(x => x.Equals(newElem));
            if (oldElem != null)
            {
                oldElem.Value = value;
                return false;
            }
            else
            {
                nonzeros.Add(newElem);
                return true;
            }
        }

        public bool SetValueForceAddition(int i, int j, float value)
        {
            SparseMatrixElem newElem = new SparseMatrixElem(i, j, value);
            nonzeros.Add(newElem);
            return true;
        }

        public SparseMatrix AddValue(int i, int j, float value)
        {
            SparseMatrixElem newElem = new SparseMatrixElem(i, j, value);
            SparseMatrixElem oldElem = nonzeros.FirstOrDefault(x => x.Equals(newElem));
            if (oldElem != null)
            {
                oldElem.Value += value;
            }
            else
            {
                nonzeros.Add(newElem);
            }
            return this;
        }

        public float GetValue(int i, int j)
        {
            SparseMatrixElem newElem = new SparseMatrixElem(i, j, 0.0f);
            SparseMatrixElem oldElem = nonzeros.FirstOrDefault(x => x.Equals(newElem));
            if (oldElem != null)
            {
                return oldElem.Value;
            }
            else
            {
                return 0.0f;
            }
        }

        public SparseMatrix ScaleBy(float factor)
        {
            foreach (var elem in nonzeros)
            {
                elem.Value *= factor;
            }
            return this;
        }

        public int N
        {
            get
            {
                return n;
            }
        }

        public int M
        {
            get
            {
                return m;
            }
        }

        public SparseMatrixElem[] NonzerosArray
        {
            get
            {
                return nonzeros.ToArray();
            }
        }

        public IEnumerable<SparseMatrixElem> Nonzeros
        {
            get
            {
                return nonzeros;
            }
        }

        public SparseMatrix DiagMultA(DiagMatrix D)
        {
            float [] diagElems = D.ValuesArray;
            foreach (var elem in nonzeros)
            {
                elem.Value *= diagElems[elem.IndexI];
            }
            return this;
        }

        public SparseMatrix DiagAddA(DiagMatrix D)
        {
            float[] diagElems = D.ValuesArray;
            bool[] foundElems = new bool[diagElems.Length];
            for (int i = 0; i < diagElems.Length; i++)
                foundElems[i] = false;
            foreach (SparseMatrixElem elem in nonzeros)
            {
                if (elem.IndexI == elem.IndexJ)
                {
                    elem.Value += diagElems[elem.IndexI];
                    foundElems[elem.IndexI] = true;
                }
            }
            for (int i = 0; i < diagElems.Length; i++)
            {
                if (!foundElems[i])
                    SetValueForceAddition(i, i, diagElems[i]);
            }
            return this;
        }

        public float[] AMultX(float[] X)
        {
            float[] R = new float[N];
            for (int i = 0; i < N; i++)
                R[i] = 0.0f;
            foreach (var elem in nonzeros)
            {
                R[elem.IndexI] += elem.Value * X[elem.IndexJ];
            }
             
            return R;
        }

        public static SparseMatrix AMultB(SparseMatrix A, SparseMatrix B)
        {
            SparseMatrix C = new SparseMatrix(A.N, B.M);
            foreach (var elemA in A.Nonzeros)
            {
                foreach (var elemB in B.Nonzeros)
                {
                    if (elemA.IndexJ == elemB.IndexI)
                    {
                        C.AddValue(elemA.IndexI, elemB.IndexJ, elemA.Value * elemB.Value);
                    }
                }
            }
            return C;
        }

        public SparseMatrix Clone()
        {
            SparseMatrix newMatrix = new SparseMatrix(N, M);
            foreach (var elem in nonzeros)
            {
                newMatrix.SetValueForceAddition(elem.IndexI, elem.IndexJ, elem.Value);
            }
            return newMatrix;
        }
    }

    class SparseMatrixRows
    {
        int n;
        int m;
        List<VertexMeasure>[] rows;

        public SparseMatrixRows(int _n, int _m)
        {
            n = _n;
            m = _m;

            rows = new List<VertexMeasure>[n];
        }

        public void SetDimensions(int _n, int _m)
        {
            n = _n;
            m = _m;
        }

        public void SetValueForceAddition(int i, int j, float value)
        {
            if (rows[i] == null)
                rows[i] = new List<VertexMeasure>();
            rows[i].Add(new VertexMeasure(j, value));
        }

        public List<VertexMeasure>[] Rows
        {
            get
            {
                return rows;
            }
        }
    }

    class DiagMatrix
    {
        private float[] diag;
        private int n;

        public DiagMatrix(int _n)
        {
            n = _n;
            diag = new float[n];
        }

        public DiagMatrix(int _n, float initValue)
            : this(_n)
        {
            for (int i = 0; i < n; i++)
                diag[i] = initValue;
        }

        public DiagMatrix SetValue(int i, float value)
        {
            diag[i] = value;
            return this;
        }

        public DiagMatrix ScaleBy(float factor)
        {
            for (int i = 0; i < n; i++)
                diag[i] *= factor;
            return this;
        }

        public DiagMatrix Invert()
        {
            for (int i = 0; i < n; i++)
                diag[i] = 1.0f / diag[i];
            return this;
        }

        public int N
        {
            get
            {
                return n;
            }
        }

        public float[] ValuesArray
        {
            get
            {
                return diag;
            }
        }

        public DiagMatrix Clone()
        {
            DiagMatrix newMatrix = new DiagMatrix(n);
            for (int i = 0; i < n; i++)
            {
                newMatrix.SetValue(i, diag[i]);
            }
            return newMatrix;
        }
    }

    abstract class GVertex
    {
        //protected int id;
        protected float weight;
        protected List<int> neighboursIds;
        protected List<float> edgesWeights;
        protected int groupNumber;

        public int GroupNumber
        {
            get
            {
                return groupNumber;
            }
        }

        public float Weight
        {
            get
            {
                return weight;
            }
            set
            {
                weight = value;
            }
        }

        public List<int> NeighboursIds
        {
            get
            {
                return neighboursIds;
            }
        }
        public List<float> EdgesWeights
        {
            get
            {
                return edgesWeights;
            }
        }

        abstract public void SetGroupNumber(int _groupNumber);
    }

    class FVertex : GVertex
    {
        public FVertex(/*int _id,*/ Node _node, float _weight)
        {
            //id = _id;
            node = _node;
            weight = _weight;
            edgesWeights = new List<float>();
            neighboursIds = new List<int>();
            groupNumber = -1;
        }

        public FVertex(/*int _id,*/ Node _node)
            : this(/*_id,*/ _node, 1.0f)
        {
        }

        private Node node;

        public Node Node
        {
            get
            {
                return node;
            }
        }

        override public void SetGroupNumber(int _groupNumber)
        {
            groupNumber = _groupNumber;
            node.GroupId = groupNumber;
        }
    }

    class AVertex : GVertex
    {
        public AVertex(Aggregate _aggregate, float w)
        {
            //id = _id;
            aggregate = _aggregate;
            weight = w;
            neighboursIds = new List<int>();
            edgesWeights = new List<float>();
            groupNumber = -1;
        }

        private Aggregate aggregate;

        public Aggregate Aggregate
        {
            get
            {
                return aggregate;
            }
        }

        override public void SetGroupNumber(int _groupNumber)
        {
            groupNumber = _groupNumber;
        }
    }

    struct VertexMeasure
    {
        public int vertexIndex;
        public float vertexMeasure;
        public bool testFlag;

        public VertexMeasure(int _vertexIndex, float _vertexMeasure)
        {
            vertexIndex = _vertexIndex;
            vertexMeasure = _vertexMeasure;
            testFlag = false;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is VertexMeasure))
                return false;
            VertexMeasure vm = (VertexMeasure)obj;
            return (this.vertexIndex.Equals(vm.vertexIndex));
        }

        public override int GetHashCode()
        {
            return vertexIndex.GetHashCode();
        }
    }

    class Aggregate
    {
        private LinkedList<VertexMeasure> verticesProbabilities;

        public Aggregate()
        {
            verticesProbabilities = new LinkedList<VertexMeasure>();
        }

        public LinkedList<VertexMeasure> VerticesProbabilities
        {
            get
            {
                return verticesProbabilities;
            }
        }
    }

    class Graph
    {
        private List<GVertex> vertices;
        private SparseMatrix edgesWeights;
        private DiagMatrix sumWeights;
        private List<VertexMeasure> futureVolumes;
        private SparseMatrixRows P;
        private List<Aggregate> aggregates;

        // Random-walk matrix.
        private SparseMatrix H;
        // Test vectors.
        private float[][] X;
        // Count of test vectors.
        private int R;
        // Matrix size.
        private int n;

        private float alpha;
        private int K;

        private bool XKvalid;

        public List<GVertex> Vertices
        {
            get
            {
                return vertices;
            }
        }

        public SparseMatrix EdgesWeights
        {
            get
            {
                return edgesWeights;
            }
        }

        public DiagMatrix SumWeights
        {
            get
            {
                return sumWeights;
            }
        }

        // First iteration graph.
        public Graph(VascularNet vnet, float alpha, int R, int K)
        {
            XKvalid = false;
            vnet.reindexNodes();
            n = vnet.NodesCount;
            this.R = R;
            this.alpha = alpha;
            this.K = K;

            AllocateMemory();
            InitializeX();

            // Fill vertices, edgesWeights, sumWeights variables.
            FillInitialData(vnet);

            CalculateHandXK();
        }

        // Next iterations graphs.
        public Graph(int n, float alpha, int R, int K)
        {
            this.n = n;
            this.R = R;
            this.alpha = alpha;
            this.K = K;
            XKvalid = false;
            AllocateMemory();
            InitializeX();

            // Fill vertices, edgesWeights, sumWeights.
            //FillInitialData(vnet);

            //CalculateHandXK(alpha, K));
        }

        private float GetAggregateWeight(Aggregate a)
        {
            float weight = 0;
            foreach (VertexMeasure prob in a.VerticesProbabilities)
            {
                weight += vertices[prob.vertexIndex].Weight * prob.vertexMeasure;
            }
            return weight;
        }

        public Graph Coarse(float nu, float Q, bool safro, float overload)
        {
            if (!XKvalid)
                return null;
            UpdateFutureVolumes();
            List<int> C = new List<int>();
            List<int> F = new List<int>();
            FillCF(C, F, nu, Q, safro);
            P = new SparseMatrixRows(n, n);
            aggregates = new List<Aggregate>(C.Count);
            FillP_Safro(C, P, aggregates, overload);
            Graph newGraph = CreateGraphByP(P, aggregates);
            return newGraph;
        }

        private void FillCF(List<int> C, List<int> F, float nu, float Q, bool safro)
        {
            // !safro:
            // According to RELAXATION-BASED GRAPH COARSENING by DORIT RON, ILYA SAFRO, AND ACHI BRANDT, p. 9/415 Alg. 2
            float fvAverage = 0.0f;
            int initialFStartsFrom = 0;

            if (!safro)
            {
                for (int i = 0; i < n; i++)
                {
                    fvAverage += futureVolumes[i].vertexMeasure;
                }
                fvAverage /= n;

                // Initializing C & F
                while (futureVolumes[initialFStartsFrom].vertexMeasure > nu * fvAverage)
                {
                    initialFStartsFrom++;
                }
                // Now  [0 .. initialFStartsFrom) -> C
                //      [initialFStartsFrom .. n) -> F
                for (int i = 0; i < initialFStartsFrom; i++)
                    C.Add(futureVolumes[i].vertexIndex);
            }

            // Updating C & F
            for (int i = initialFStartsFrom; i < n; i++)
            {
                if (safro)
                {
                    if (NeedMoveIfromFtoC_Safro(futureVolumes[i].vertexIndex, C, Q))
                        C.Add(futureVolumes[i].vertexIndex);
                    else
                        F.Add(futureVolumes[i].vertexIndex);
                }
                else
                {
                    if (NeedMoveIfromFtoC_Ron(futureVolumes[i].vertexIndex, C, Q))
                        C.Add(futureVolumes[i].vertexIndex);
                    else
                        F.Add(futureVolumes[i].vertexIndex);
                }
            }
        }

        private bool NeedMoveIfromFtoC_Ron(int i, List<int> C, float Q)
        {
            float sumcC = 0.0f;
            float sumcVf = 0.0f;
            float sumwC = 0.0f;
            float sumwVf = 0.0f;
            for (int j = 0; j < vertices[i].NeighboursIds.Count; j++)
            {
                int neighbId = vertices[i].NeighboursIds[j];
                float edgeWeight = vertices[i].EdgesWeights[j];
                if (C.Contains(neighbId))
                {
                    sumcC += GetRoValueInv(i, neighbId);
                    sumwC += edgeWeight;
                }
                sumcVf += GetRoValueInv(i, neighbId);
                sumwVf += edgeWeight;
            }
            return ((sumcC <= Q * sumcVf) || (sumwC <= Q * sumwVf));
        }

        private bool NeedMoveIfromFtoC_Safro(int i, List<int> C, float theta)
        {
            float sumC = 0.0f;
            float sumVf = 0.0f;
            foreach (int j in C)
            {
                sumC += GetRoValueInv(i, j);
            }
            for (int j = 0; j < n; j++)
            {
                if (i == j)
                    continue;
                sumVf += GetRoValueInv(i, j);
            }
            return (sumC < theta * sumVf);
        }

        private float getLmax(int Ccount, float totalC, float maxC, float overload)
        {
            return (1.0f + overload) * totalC / Ccount + maxC;
        }

        private Tuple<VertexMeasure, VertexMeasure> getCNodesToAssign(int i, List<VertexMeasure> neighbCouplings, List<int> C,
            List<Aggregate> aggregates, float totalC, float maxC, float overload)
        {
            Tuple<VertexMeasure, VertexMeasure> result;
            // Try to find 2 C:
            int CneighbCount = neighbCouplings.Count;
            List<VertexMeasure> pairs = new List<VertexMeasure>(CneighbCount * (CneighbCount - 1) / 2);
            for (int i1 = 0; i1 < CneighbCount; i1++)
                for (int i2 = i1 + 1; i2 < CneighbCount; i2++)
                {
                    pairs.Add(new VertexMeasure(i1 * CneighbCount + i2, neighbCouplings[i1].vertexMeasure +
                        neighbCouplings[i2].vertexMeasure));
                }
            pairs.Sort(new VerticesMeasuresComparerDesc());
            foreach (VertexMeasure pair in pairs)
            {
                int i1 = pair.vertexIndex / CneighbCount;
                int i2 = pair.vertexIndex % CneighbCount;
                float p1 = 0.0f;
                float p2 = 0.0f;

                if (float.IsInfinity(neighbCouplings[i1].vertexMeasure))
                {
                    p1 = 1.0f;
                    p2 = 0.0f;
                }
                else
                {
                    if (float.IsInfinity(neighbCouplings[i2].vertexMeasure))
                    {
                        p2 = 1.0f;
                        p1 = 0.0f;
                    }
                    else
                    {

                        p1 = neighbCouplings[i1].vertexMeasure /
                             (neighbCouplings[i1].vertexMeasure + neighbCouplings[i2].vertexMeasure);

                        p2 = neighbCouplings[i2].vertexMeasure /
                            (neighbCouplings[i1].vertexMeasure + neighbCouplings[i2].vertexMeasure);
                    }
                }



                Aggregate ag1 = aggregates[neighbCouplings[i1].vertexIndex];
                Aggregate ag2 = aggregates[neighbCouplings[i2].vertexIndex];
                VertexMeasure vm1 = new VertexMeasure(i, p1);
                //vm1.testFlag = true;
                ag1.VerticesProbabilities.AddLast(vm1);
                if (GetAggregateWeight(ag1) > getLmax(C.Count, totalC, maxC, overload))
                {
                    ag1.VerticesProbabilities.RemoveLast();
                    continue;
                }
                VertexMeasure vm2 = new VertexMeasure(i, p2);
                //vm2.testFlag = true;
                ag2.VerticesProbabilities.AddLast(vm2);
                if (GetAggregateWeight(ag2) > getLmax(C.Count, totalC, maxC, overload))
                {
                    ag1.VerticesProbabilities.RemoveLast();
                    ag2.VerticesProbabilities.RemoveLast();
                    continue;
                }
                else
                {
                    // No overload.
                    result = new Tuple<VertexMeasure, VertexMeasure>(
                        new VertexMeasure(i1, p1), new VertexMeasure(i2, p2));
                    return result;
                }
            }
            // Try to find 1 C:
            for (int i1 = 0; i1 < CneighbCount; i1++)
            {
                Aggregate ag1 = aggregates[neighbCouplings[i1].vertexIndex];
                VertexMeasure vm1 = new VertexMeasure(i, 1.0f);
                ag1.VerticesProbabilities.AddLast(vm1);
                if (GetAggregateWeight(ag1) > getLmax(C.Count, totalC, maxC, overload))
                {
                    ag1.VerticesProbabilities.RemoveLast();
                    continue;
                }
                else
                {
                    // No overload.
                    result = new Tuple<VertexMeasure, VertexMeasure>(
                        new VertexMeasure(i1, 1.0f), new VertexMeasure(-1, 0.0f));
                    return result;
                }
            }
            // Failed to found 1 or 2C:
            result = new Tuple<VertexMeasure, VertexMeasure>(
                new VertexMeasure(-1, 0.0f), new VertexMeasure(-1, 0.0f));
            return result;
        }

        private void FillP_Safro(List<int> C, SparseMatrixRows P, List<Aggregate> aggregates, float overload)
        {
            float totalC = 0.0f;
            float maxC = 0.0f;
            foreach (GVertex v in vertices)
            {
                totalC += v.Weight;
                if (v.Weight > maxC)
                    maxC = v.Weight;
            }

            bool[] pSet = new bool[n];
            for (int i = 0; i < n; i++)
            {
                pSet[i] = false;
            }
            for (int iC = 0; iC < C.Count; iC++)
            {
                int iCfine = C[iC];
                P.SetValueForceAddition(iCfine, iC, 1.0f);
                pSet[iCfine] = true;
                Aggregate newAggegate = new Aggregate();
                newAggegate.VerticesProbabilities.AddLast(new VertexMeasure(iCfine, 1.0f));
                aggregates.Add(newAggegate);
            }
            for (int ii = 0; ii < n; ii++)
            {
                int i = futureVolumes[ii].vertexIndex;
                if (pSet[i])
                    continue;
                List<VertexMeasure> neighbCouplings = new List<VertexMeasure>();
                // indices in C
                foreach (int id in vertices[i].NeighboursIds)
                {
                    int iC = C.FindIndex(x => x == id);
                    if (iC != -1)
                    {
                        float ro = GetRoValueInv(i, id);

                        neighbCouplings.Add(new VertexMeasure(iC, ro));
                    }
                }
                neighbCouplings.Sort(new VerticesMeasuresComparerDesc());
                // Try to find 1 or 2 C-neighbours to assign.
                Tuple<VertexMeasure, VertexMeasure> nodes = 
                    getCNodesToAssign(i, neighbCouplings, C, aggregates, totalC, maxC, overload);
                if (nodes.Item2.vertexIndex != -1)
                {
                    // 2 nodes found.
                    P.SetValueForceAddition(i, neighbCouplings[nodes.Item1.vertexIndex].vertexIndex,
                        nodes.Item1.vertexMeasure);
                    P.SetValueForceAddition(i, neighbCouplings[nodes.Item2.vertexIndex].vertexIndex,
                        nodes.Item2.vertexMeasure);
                }
                else
                {
                    if (nodes.Item1.vertexIndex != -1)
                    {
                        // 1 node found.
                        P.SetValueForceAddition(i, neighbCouplings[nodes.Item1.vertexIndex].vertexIndex,
                            nodes.Item1.vertexMeasure);
                    }
                    else
                    {
                        // Add a node to C.
                        int Ccount = C.Count;
                        C.Add(i);
                        P.SetValueForceAddition(i, Ccount, 1.0f);
                        pSet[i] = true;
                        Aggregate newAggegate = new Aggregate();
                        newAggegate.VerticesProbabilities.AddLast(new VertexMeasure(i, 1.0f));
                        aggregates.Add(newAggegate);

                    }
                }
            }
            P.SetDimensions(n, C.Count);
        }

        private Graph CreateGraphByP(SparseMatrixRows P, List<Aggregate> aggregates)
        {
            Graph newGraph = new Graph(aggregates.Count, alpha, R, K);
            
            // Set vertices
            for (int ia = 0; ia < aggregates.Count; ia++)
            {
                Aggregate a = aggregates[ia];
                AVertex avertex = new AVertex(a, GetAggregateWeight(a));
                newGraph.Vertices.Add(avertex);
            }
            // Find weights, set neighbourhood.
            for (int ia = 0; ia < aggregates.Count; ia++)
            {
                Aggregate a = aggregates[ia];
                foreach (VertexMeasure vm in a.VerticesProbabilities)
                {
                    for (int inb = 0; inb < vertices[vm.vertexIndex].NeighboursIds.Count; inb++)
                    {
                        int fneighbour = vertices[vm.vertexIndex].NeighboursIds[inb];
                        float fneighbourW = vertices[vm.vertexIndex].EdgesWeights[inb];
                        foreach (VertexMeasure neibAggr in P.Rows[fneighbour])
                        {
                            if (neibAggr.vertexIndex == ia)
                                continue;

                            ///////////////////////////
                            // Normalization removed.
                            float newW = vm.vertexMeasure * fneighbourW * neibAggr.vertexMeasure;
								// / 
                                //(float)Math.Sqrt(newGraph.Vertices[ia].Weight * newGraph.Vertices[neibAggr.vertexIndex].Weight);

                            int awid = newGraph.Vertices[ia].NeighboursIds.FindIndex(x => x == neibAggr.vertexIndex);
                            if (awid == -1)
                            {
                                newGraph.Vertices[ia].NeighboursIds.Add(neibAggr.vertexIndex);
                                newGraph.Vertices[ia].EdgesWeights.Add(newW);
                                newGraph.Vertices[neibAggr.vertexIndex].NeighboursIds.Add(ia);
                                newGraph.Vertices[neibAggr.vertexIndex].EdgesWeights.Add(newW);
                            }
                            else
                            {
                                newGraph.Vertices[ia].EdgesWeights[awid] += newW;
                                awid = newGraph.Vertices[neibAggr.vertexIndex].NeighboursIds.FindIndex(x => x == ia);
                                newGraph.Vertices[neibAggr.vertexIndex].EdgesWeights[awid] += newW;
                            }
                        }
                    }
                }
            }

            for (int iv = 0; iv < newGraph.Vertices.Count; iv++)
            {
                float sum = 0.0f;
                for (int ini = 0; ini < newGraph.Vertices[iv].NeighboursIds.Count; ini++)
                {
                    int jv = newGraph.Vertices[iv].NeighboursIds[ini];
                    float w = newGraph.Vertices[iv].EdgesWeights[ini];
                    sum += w;
                    if (jv > iv)
                    {
                        newGraph.edgesWeights.SetValueForceAddition(iv, jv, w);
                        newGraph.edgesWeights.SetValueForceAddition(jv, iv, w);
                    }
                }
                newGraph.SumWeights.SetValue(iv, sum);
            }

            newGraph.CalculateHandXK();

            return newGraph;
        }

        //private void AssignFNodesStraightForward()
        //{
        //    //TODO?
        //}

        //private void AssignFNodesAMG()
        //{
        //    //TODO?
        //}

        public void SetGroupNumbers()
        {
            for (int iv = 0; iv < vertices.Count; iv++)
            {
                vertices[iv].SetGroupNumber(iv);
            }
        }

        public void SetGroupNumbers(List<GVertex> prevVertices, Random rnd)
        {
            for (int iv = 0; iv < vertices.Count; iv++)
            {
                if (P.Rows[iv].Count == 1)
                {
                    vertices[iv].SetGroupNumber(prevVertices[P.Rows[iv][0].vertexIndex].GroupNumber);
                    continue;
                }
                // P.Rows[iv].Count == 2
                float frnd = (float)rnd.Next() / Int32.MaxValue;
                int ii = 0;
                if (frnd < P.Rows[iv][0].vertexMeasure)
                {
                    ii = P.Rows[iv][0].vertexIndex;
                }
                else
                {
                    ii = P.Rows[iv][1].vertexIndex;
                }
                vertices[iv].SetGroupNumber(prevVertices[ii].GroupNumber);
            }
        }

        private void UpdateFutureVolumes()
        {
            if (!XKvalid)
                return;
            futureVolumes.Clear();
            VerticesMeasuresComparerDesc fvComparer = new VerticesMeasuresComparerDesc();
            for (int i = 0; i < n; i++)
            {
                futureVolumes.Add(new VertexMeasure(i, GetFutureVolume(i)));
            }
            futureVolumes.Sort(fvComparer);
        }

        // Not optimal, not used
        private void UpdateFutureFor(IEnumerable<int> ids)
        {
            if (!XKvalid)
                return;
            //futureVolumes.Clear();
            VerticesMeasuresComparerDesc fvComparer = new VerticesMeasuresComparerDesc();
            foreach (int i in ids)
            {
                int fvId = futureVolumes.FindIndex(x => x.vertexIndex == i);
                VertexMeasure fv = new VertexMeasure(i, GetFutureVolume(i));
                futureVolumes[fvId] = fv;
            }
            //futureVolumes.Sort(fvComparer);
        }

        private void AllocateMemory()
        {
            vertices = new List<GVertex>(n);
            edgesWeights = new SparseMatrix(n, n);
            sumWeights = new DiagMatrix(n);
            futureVolumes = new List<VertexMeasure>(n);
            H = new SparseMatrix(n, n);
            X = new float[R][];
            for (int r = 0; r < R; r++)
                X[r] = new float[n];
        }

        private void InitializeX()
        {
            ///////////////////////
            // Initialize X with one standard PRNG.
            // Is it better to use R different PRNGs?
            ///////////////////////
            Random rnd = new Random();
            for (int r = 0; r < R; r++)
                for (int i = 0; i < n; i++)
                {
                    X[r][i] = (float)rnd.Next() / Int32.MaxValue - 0.5f;
                }
        }

        public void FillInitialData(VascularNet vnet)
        {
            for (int i = 0; i < n; i++)
            {
                Node node = vnet.Nodes[i];
                FVertex newVertex = new FVertex(/*i,*/ node, 1.0f);

                float sum = 0.0f;
                foreach (var neighbour in node.getNeighbours())
                {
                    int j = neighbour.getId();
                    newVertex.EdgesWeights.Add(1.0f);
                    newVertex.NeighboursIds.Add(j);
                    if (j > i)
                    {
                        edgesWeights.SetValueForceAddition(i, j, 1.0f);
                        edgesWeights.SetValueForceAddition(j, i, 1.0f);
                    }
                    sum += 1.0f;
                }
                vertices.Add(newVertex);
                sumWeights.SetValue(i, sum);
            }
        }

        public void CalculateHandXK()
        {
            DiagMatrix I = new DiagMatrix(n, 1.0f);
            DiagMatrix Dinv = sumWeights.Clone().Invert();
            H = edgesWeights.Clone().DiagMultA(Dinv.ScaleBy(alpha));
            H = H.DiagAddA(I.ScaleBy(1.0f - alpha));

            for (int r = 0; r < R; r++)
                for (int k = 0; k < K; k++)
                {
                    X[r] = H.AMultX(X[r]);
                }
            XKvalid = true;
        }

        public float GetRoValue(int i, int j)
        {
            if (!XKvalid)
                return 1.0f;
            float ro = 0.0f;
            float diff = 0.0f;
            for (int r = 0; r < R; r++)
            {
                diff = (X[r][i] - X[r][j]);
                ro += diff * diff;
            }
            return ro;
        }

        public float GetRoValueInv(int i, int j)
        {
            float ro = GetRoValue(i, j);
            if (ro == 0.0f)
                return float.PositiveInfinity;
            return 1.0f / ro;
        }

        public float GetFutureVolume(int i)
        {
            float vol = vertices[i].Weight;
            float sum;
            foreach (int neighbIdJ in vertices[i].NeighboursIds)
            {
                sum = 0.0f;
                foreach (int neighbIdK in vertices[neighbIdJ].NeighboursIds)
                {
                    sum += GetRoValueInv(neighbIdJ, neighbIdK);
                }
                vol += vertices[neighbIdJ].Weight * GetRoValueInv(i, neighbIdJ) / sum;
            }
            return vol;
        }
    }

    class VerticesMeasuresComparerDesc : IComparer<VertexMeasure>
    {
        public int Compare(VertexMeasure x, VertexMeasure y)
        {
            return -x.vertexMeasure.CompareTo(y.vertexMeasure);
        }
    }
}
