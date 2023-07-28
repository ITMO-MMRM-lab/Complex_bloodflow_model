// CppFunc.h
#include <math.h>
#include "alglibinternal.h"
#include "solvers.h"
#include "ap.h"

#pragma once
#pragma comment(lib, "MSCOREE.lib")

using namespace System::Runtime::InteropServices;


public ref class base1DFunction
{
public:
	virtual double getVal(double x) = 0;
};

public ref class baseMDFunction
{
public:
	virtual const double getVal(const double* x) = 0;
};

typedef baseMDFunction^ p_baseMDFunction;
public ref class NewtonSolver
{
public:	
	NewtonSolver(int dim);
	 int solve(const double* init_X, const double eps, double* solution);
	bool addFunc(baseMDFunction^);
	void setDetMatrixEl(int i, int j, bool val);
	void setDxVectorEl (int i, double val);
	void getResidual(double* x, double* residual);	

	[DllImport("c:\\GitHub\\medicine-bloodflow\\BloodFlowModel_0\\Release\\Kramer6Solver.dll")]
	static double Kramer6Solver(double** mat, double* B);

private:
	array<baseMDFunction^>^ funcs;
	double* dX;
	bool** depMatrix;

	int curr_l;
	int dim;

	double** n_m_jacobi;
	double*  n_curr_x;	
	double*  n_B;

	alglib::real_2d_array* m_jacobi;
	alglib::real_1d_array* curr_x;	
	alglib::real_1d_array* B;
};


public ref class McCormackThread
{
	public:	
		McCormackThread(int _length, double* dZ, double density, double viscosity);
		void calc(double* velocity, double* lumen, double* pressure, double dt);
		void setGravity(double* g_energy);
		bool addFunc(base1DFunction^, int i);
	private:
		int length;		
		double density;
		double viscosity;

		array<base1DFunction^>^ pressure_func;

		double*  us_velocity_pred;
		double*  us_lumen_sq_pred;
		double*  us_pressure_pred;
		double*  dZ;
		
		double*  us_velocity;
		double*  us_lumen_sq;
		double*  us_pressure;
		double*  us_g_energy;
};

