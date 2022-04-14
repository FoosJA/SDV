using Monitel.Mal.Context.CIM16;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDV.Model
{
	public class OperandFrm : IEquatable<OperandFrm>
	{
		public OperandFrm()
		{
			Field = MeasValueAttribute.value;

		}
		public string CatOperand;
		public int IdOperand;
		public int FID;
		public string OperandLit;
		public int TypeFrm;
		public int TShift;
		public MeasurementValue MvOperand;
		public MeasValueAttribute Field;

		public bool Equals(OperandFrm other)
		{
			//Check whether the compared object is null.
			if (Object.ReferenceEquals(other, null)) return false;

			//Check whether the compared object references the same data.
			if (Object.ReferenceEquals(this, other)) return true;

			//Check whether the products' properties are equal.
			return OperandLit.Equals(other.OperandLit) && TShift.Equals(other.TShift) && IdOperand.Equals(other.IdOperand) && Field.Equals(other.Field);
		}
		public override int GetHashCode()
		{

			//Get hash code for the Name field if it is not null.
			int hashOperandLit = OperandLit == null ? 0 : OperandLit.GetHashCode();

			int hashTShift = TShift.GetHashCode();
			int hashIdOperand = IdOperand.GetHashCode();
			int hashField = Field.GetHashCode();

			//Calculate the hash code for the product.
			return hashOperandLit ^ hashTShift ^ hashIdOperand ^ hashField;
		}
	}
}
