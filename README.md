Introduction
============

Blood Flow Model is an open source software that provides simulations of hemodynamics in arterial systems. Such simulations may be useful in cases of scientific researches and educational applications. We assume that scientists working in the field of computational biomedicine will be interested in this software in first row. 
The description of the mathematical basis of this model can be found in paper ([A. Svitenkov et al., 2018](https://doi.org/10.1016/j.procs.2018.08.272)). This model was used many times in scientific works ([Shramko et al., 2021](https://vestnik.pstu.ru/get/_res/fs/file2.pdf/10224/Shramko+O.A.%2C+Svitenkov+A.I.+%28St.+Petersburg%2C+Russian+Federation%29%2C+Zun+P.S.+%28St.+Petersburg%2C+Russian+Federation%3B+Amsterdam%2C+The+Netherlands%29.+Can+collateral+flow+index+have+an+influence+on+restenosis+growth+dynamics%3Ffile2.pdf), [A. Svitenkov et al., 2016](https://doi.org/10.1016/j.procs.2016.05.393), [A. I. Svitenkov et al., 2021](https://doi.org/10.1007/978-3-030-77967-2_59); [Zun et al., 2017](https://doi.org/10.3389/fphys.2017.00284); [Zun & Hoekstra, 2015](https://doi.org/10.1016/j.procs.2015.11.047)), so it proved its quality.

Description
===========
The software includes three main applications: the blood flow model, the topology manager and the optimizer of outlet boundary conditions.


Blood flow model   
================

Installation
------------

It is recommended to use Visual Studio (2015 or later version).

Application logic
-----------------

The application performs simulations of blood flow in an arterial network. For these simulations some input files are needed. After a simulation output files are generated.

Input files
-----------

1. The run file. Here are examples:

```
<Test>
Topology: new_lca_025.top
InletFlux: 33 LCA_inlet_flux.txt
OutletParams: new_lca_par.par
Task: 74:0.2 74:0.3 74:0.4
```

```
<Test>
Topology: new_lca_025.top
InletFlux: 33 LCA_inlet_flux.txt
OutletParams: new_lca_par.par
DownDirection: 0.0 1.0 0.0
```

```
<Test>
Topology: new_lca_025.top
InletFlux: 33 LCA_inlet_flux.txt
OutletParams: new_lca_par.par
Agent: 33
```
    
In the topology line a topology file name is given. In the inlet flux line an inlet point id and a inlet flux file name is given. In the outlet parameters line a outlet parameters file name is given. In the task line tasks are given in such way: <point id>:<stenosis degree>. In the down direction line the direction of g is given corresponding to the topology coordinates: <X> <Y> <Z>. In the agent line the endpoint of the artery, which the agent is injected in.

2. The file containing a topology of an arterial network (*.top). Here is an example:

```
Name: System_0
Coordinates:
0 X:0.007916 Y:1.428580 Z:0.010909 R:14.498250 C:0.000000
1 X:-0.141348 Y:1.460138 Z:-0.044640 R:1.132713 C:0.000000
2 X:-0.230791 Y:1.289018 Z:-0.056437 R:1.535108 C:0.000000
.
.
.

.
.
.
5655 X:0.018233 Y:1.631310 Z:0.015014 R:1.150796 C:0.000000
5656 X:0.016458 Y:1.632985 Z:0.015680 R:1.150796 C:0.000000

Bonds:
0 2339 
1 287 288 289 
2 311 330 338 
3 294 310 505 
.
.
.
```
    
There are lines after "Coordinates:". In each line id of a point, X coordinate of a point, Y coordinate of a point, Z coordinate of a point, radius of an artery in a point, curvature of an artery in a point are specified. The are lines after "Bonds:". In each line a list of neighbours of a point is specified.

3. The file containing an inlet flux. Here is an example:

```    
1.0e-6
0.0	0.0
0.02193	0.0
0.041305	105.089
0.06068	273.23
0.080055	404.592
0.09943	535.952
0.118805	580.479
0.128492	600.734
0.13818	575.479
0.157555	545.952
0.17693	488.663
0.196305	451.882
0.244742	315.266
0.322242	178.651
0.351305	136.615
0.390055	0.0
0.415117	-162.888
0.437555	-21.0178
0.48693	0.0
0.515992	5.25444
0.583805	0.0
1.0	0.0
```
    
Inlet flux file contains timestamps and inlet flux values in corresponding timestamps. In the first line a multiplication coeffecient for inlet flux values is set.  

4. The file containing outlet parameters. Here is an example:

```    
75 R1:0.549235844455994 R2:303.529671289095 C:3.41542281911504
286 R1:1.71199342637368 R2:338.11175177571 C:3.05616677144763
```
    
In each line a terminal point id, first resistance of a corresponding terminal artery, second resistance of a corresponding terminal artery, compliance of a corresponding terminal artery are given.

Output files
------------

1. The file containing a blood flow dynamics (*.dyn). Here is an example:

```
WT: 3.0000999242111
0	-0.13801	13702.44065	-0.0609
1	-0.24820	13394.22339	-0.0826
2	-0.09924	13524.00789	-0.0613
.
.
.

.
.
.
186	-1.26959	13861.22046	-0.1395
187	-1.25442	13877.13729	-0.1378
188	1.26811	13877.13729	0.1393
WT: 3.02009992370586
0	-0.13539	13628.18215	-0.0599
1	-0.24356	13322.72102	-0.0811
2	-0.09739	13451.91353	-0.0602
.
.
.
```

In the lines with "WT:" timesteps are show. After each timestep line dynamics tables are shown. In each row of a table a point id, a flux value, a pressure value, a velocity value are shown.

2. The file containing stenosis simulation data (*.out). Here is an example:

```
100 R0: 1.000 R: 1.000 R real: 1.05866 ref flux: 0.7480 degree: 0.00 depressed flux: 0.7480 Pp: 15297.42 Pd: 15244.42 delta P: 53.0027 Pav: 15270.94
100 R0: 1.000 R: 0.949 R real: 1.00142 ref flux: 0.7480 degree: 0.10 depressed flux: 0.7471 Pp: 15298.81 Pd: 15231.58 delta P: 67.2373 Pav: 15265.23
```
    
In each line the stenosis point id, the initial radius, the estimated radius, the calculated (during the simulation) radius, the reference flux (with no stenosis), the flux with stenosis, the proximal pressure, the distal pressure, the pressure drop, the average pressure are shown.

Hardcoded parameters
--------------------    
    
Some important parameters for simulations can not be set as command line arguments, so, the only way to set these parameters is to change their values directly in the code.

1. Parameters of writing

Program.cs:
```
public static float TIMESTEP = 0.5e-4f;
        public static float AV_TIME = 0.0f;
        public static float END_TIME = 150.0f; // end time of the simulation, redefined for stenosis case
        public static float WRITE_TIME = 1.0f; // time to start writing output dynamics file

        public static float clot_set_time = 0.0f;
        public static float STABILISATION_TIME = 10.0f;

        public static float CLOT_RELAXATION_PERIOD = 10.0f;
        public static float CLOT_REMOVE_RELAXATION_PERIOD = 1.0f;
        public static float OUTPUT_PERIOD = 0.02f;
        public static BFSimulator bf_simulation;
```

TIMESTEP is the variable for the step of the simulation in time (in seconds). AV_TIME is the variable for the averaging in time during the simulation (in seconds). END_TIME is the variable that defines time of the end of the simulation (in seconds). WRITE_TIME is the variable that defines time of the start of writing data to the output dynamics file (in seconds). STABILISATION_TIME is the variable that defines time after that the blood flow becomes stationary (in seconds). OUTPUT_PERIOD is the variable that defines the period of writing the output data in the dynamics file (in seconds).
    
2. Simulation parameters

```
BFSimulation.cs:
public static float YOUNG_MODULUS = 225.0e+3f;//Pa        
        public static double BLOOD_DENSITY = 1040f; //kg/m^3 
        public static double BLOOD_VISC = 4.0e-3f; //Pa*s
        public static double DIASTOLIC_PRESSURE = 10.0e+3f; //Pa
        public static double OUT_PRESSURE = 0.0f;
        public static double DIASTOLIC_PRESSURE_1 = 0; //Pa
        public static double SISTOLIC_PRESSURE = 15.9e+3f; //Pa
        public static double GRAVITY = 9.8f;// m/s^2
        public static double FRICTION_C = 8.0f;
        public static double HEART_PERIOD = 1.0;
```
    
YOUNG_MODULUS is the variable that defines the value of the Young's modulus during the simulation (in Pa). BLOOD_DENSITY is the variable that defines the value of the blood density during the simulation (in kg/m^3). BLOOD_VISC is the variable that defines the value of the blood viscosity during the simulation (in Pa*s). DIASTOLIC_PRESSURE is the variable that defines the diastolic pressure in the start of the simulation (in Pa). GRAVITY is the variable that defines the value of the acceleration of gravity during the simulation (in m/s^2). FRICTION_C is the variable that defines the value of the multiplier in the frictional term during the simulation (dimensionless). HEART_PERIOD is the variable that defines the period of heartbeat during the simulation (in seconds)? it is used only for the dissipation calculation.
    
Running
-------
    
To run application command line arguments should be given. For example:

``..\..\examples\aorta_femoral_1_0\run_10.txt ..\..\examples\aorta_femoral_1_0``

The first argument is a path to a run file, the second argument is a path to a directory, containing input files.

Topology manager
================

For running, you can use PyCharm on Windows (tested with Python 3.9.7, venv, numpy 1.21.2, matplotlib 3.4.3, PyQt 5.15.5, PyQtChart 5.15.4, pyopengl 3.1.5, networkx 2.6.3). Overall, any recent version should work fine.

For visualization, first open a .top file (Topology -> Load), then use the "Load dynamics" button to add a corresponding .dyn file. An example is "0_2_cutoff.top" and "0_2_static.dyn".

Optimizer of boundary conditions
================
    
The optimizer is located in BCOptimizer folder.
    
For setting input arguments, GlobalDefs of Program.cs should be changed.

For running, there should be TestModel.exe, CppCalculations.dll, CppCalculations_x64.dll in BCOptimizer\bin folder.

Information about the finished optimization can be found in out_args.txt (the values of the parameters) and summary.txt (the gradients). Also, the .par files will be changes corresponding to the latest iteration.
