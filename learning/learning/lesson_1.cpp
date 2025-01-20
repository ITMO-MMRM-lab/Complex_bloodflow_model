#include <stdio.h>
#include <math.h>

typedef unsigned int UINT;

int myFunc(double arg_1);

int main(int argc, char* argv[])
{
	int* a = new int;
	int  b = 0;

	*a = 10;

	for(int i=0; i<argc; i++)	
		printf("%s\n",argv[i]);
	

return *a+b;
}