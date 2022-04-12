using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDV.Model
{
    public class IntegParam
	{
        public string CategorySource;
        public int IdSource;
        public string CategoryOI;
        public int IdOI;
        public int IMethod;
        public int Periodic;
        public int IStart;
        public int IEnd;
        public int IStep;
        public string DFilter;
        public bool Inv;
        public double DFilterValue;
        public int ParamStep;
        public int TimeZone;
    }
}
