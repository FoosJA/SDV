using Monitel.Mal.Context.CIM16;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDV.Model
{
	public class OperandFrm
    {
        public string CatOperand;
        public int IdOperand;
        public int FID;
        public string OperandLit;
        public int TypeFrm;
        public int TShift;
        public MeasurementValue MvOperand;
        public MeasValueAttribute Field;
    }
}
