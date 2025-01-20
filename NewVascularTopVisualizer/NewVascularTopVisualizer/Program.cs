using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using Fusion;
using Fusion.Development;

namespace NewVascularTopVisualizer
{
    class Program
    {
        static void Main(string[] args)
        {
            //Trace.Listeners.Add(new ColoredTraceListener());
            
            CommonThreadsData commonData = new CommonThreadsData();

            Thread visualizationThread = new Thread(new ParameterizedThreadStart(CommonThreadControl.RunVisualizer));

            Thread controlFormThread = new Thread(new ParameterizedThreadStart(CommonThreadControl.RunControlForm));
            controlFormThread.SetApartmentState(ApartmentState.STA);

            visualizationThread.Start(commonData);
            controlFormThread.Start(commonData);

            visualizationThread.Join();
            controlFormThread.Join();

        }


    }
}
