using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenSmc.Ifrs17.Utils;

public class Consts
{
    public const double Precision = 1E-5;

    public const double ProjectionPrecision = 1E-3;

    public const double BenchmarkPrecision = 1E-4;

    public const double YieldCurvePrecision = 1E-8;

    public const int CurrentPeriod = 0;

    public const int PreviousPeriod = -1;

    public const int MonthInAYear = 12;

    public const int MonthInAQuarter = 3;

    public const int DefaultDataNodeActivationMonth = 1;
}