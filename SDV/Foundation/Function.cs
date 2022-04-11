using Monitel.Mal;
using SDV.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Monitel.Mal.Context.CIM16;
using System.Threading.Tasks;


namespace SDV.Foundation
{
	public class Function
	{
		public ModelImage ModelImage = null;
		public bool CreateRepVal;
		public Guid UidAnalogForVal;
		public Guid UidDiscreteForVal;
		public Function(ModelImage mImage, bool createRepVal)
		{
			ModelImage = mImage;
			CreateRepVal = createRepVal;

		}

		public OIck11 CreateRBvalue(AnalogValue av)
		{
			try
			{
				MeasurementValueSource mvSource = (MeasurementValueSource)ModelImage.GetObject(new Guid("10000C28-0000-0000-C000-0000006D746C"));//Удаленный ЦУ	
				HISPartition avTwoTime = (HISPartition)ModelImage.GetObject(new Guid("10000064-0000-0000-C000-0000006D746C"));//аналоговые 2 времени
				ModelImage.BeginTransaction();
				var rapidBusVal = (RapidBusIndirectAnalogValue)ModelImage.CreateObject(ModelImage.MetaData.Classes["RapidBusIndirectAnalogValue"]);
				rapidBusVal.name = av.Analog.name + " [RB]";
				rapidBusVal.Analog = av.Analog;
				if (av is ReplicatedAnalogValue rav)
					rapidBusVal.externalId = "RB" + rav.sourceId;
				else
					rapidBusVal.externalId = "RB" + av.externalId;
				rapidBusVal.ParentObject = av.ParentObject;
				rapidBusVal.hisPreserveTime = true;
				rapidBusVal.HISPartition = av.HISPartition;
				rapidBusVal.MeasurementValueSource = mvSource;
				rapidBusVal.MeasurementValueType = av.MeasurementValueType;
				ModelImage.CommitTransaction();
				OIck11 oiRes = new OIck11
				{
					Name = rapidBusVal.name,
					UidMeas = rapidBusVal.Analog.Uid,
					UidVal = rapidBusVal.Uid,
					HISpartition = rapidBusVal.HISPartition.name,
					ValueSource = rapidBusVal.MeasurementValueSource.name,
					Class = "RapidBusIndirectAnalogValue",
					MeasType = rapidBusVal.Analog.MeasurementType.name,
					ValueType = rapidBusVal.MeasurementValueType.name
				};
				return oiRes;
			}
			catch (Exception ex)
			{
				ModelImage.RollbackTransaction();
				throw new ArgumentException("Не удалось создать RB значение. " + ex.Message);
			}
		}
		public OIck11 CreateCalcvalue(OIck11 oi11, Formulas cv, List<OperandFrm> operands, IEnumerable<Formulas> allFormulas, IEnumerable<OperandFrm> allOperands)
		{
			List<(MeasurementValue, string)> operandList = new List<(MeasurementValue, string)>();
			//Проверка все ли есть операнды в ИМ			
			foreach (var operand in operands)
			{
				MetaClass mvClass = ModelImage.MetaData.Classes["MeasurementValue"];
				IEnumerable<MeasurementValue> mvCollect = ModelImage.GetObjects(mvClass).Cast<MeasurementValue>();
				var mvOperand = mvCollect.FirstOrDefault(x => x.externalId?.Replace("Calc", "").Replace("Agr", "").Replace("RB", "") == operand.CatOperand + operand.IdOperand);
				if (mvOperand == null)
				{
					if (operand.CatOperand == "S")
					{
						MetaClass rdvClass = ModelImage.MetaData.Classes["ReplicatedDiscreteValue"];
						IEnumerable<ReplicatedDiscreteValue> rdvCollect = ModelImage.GetObjects(rdvClass).Cast<ReplicatedDiscreteValue>();
						mvOperand = rdvCollect.FirstOrDefault(x => x.sourceId == operand.CatOperand + operand.IdOperand);
					}
					else
					{
						MetaClass ravClass = ModelImage.MetaData.Classes["ReplicatedAnalogValue"];
						IEnumerable<ReplicatedAnalogValue> ravCollect = ModelImage.GetObjects(ravClass).Cast<ReplicatedAnalogValue>();
						mvOperand = ravCollect.FirstOrDefault(x => x.sourceId == operand.CatOperand + operand.IdOperand);
					}
				}
				if (mvOperand == null)
				{
					if (CreateRepVal)
					{
						string id = operand.CatOperand + operand.IdOperand;
						var repValNew = CreateReplicateMeas(id);
						var tuple = (repValNew, id);
						operandList.Add(tuple);
					}
					else
					{
						throw new ArgumentException($"Операнд {operand.CatOperand + operand.IdOperand} для формулы {cv.CatRes}{cv.IdRes} не найден в ИМ");
					}
				}
				else
				{
					string id = operand.CatOperand + operand.IdOperand;
					var tuple = (mvOperand, id);
					operandList.Add(tuple);
				}

			}

			string[] frmRange = cv.Formulafrm.Split(' ');
			foreach (string operandFrm in frmRange)
			{
				string mvOperandSourceId = operandFrm.Remove(operandFrm.Length - 1);
				OperandFrm operand = null;
				try
				{
					if (mvOperandSourceId.Remove(1) == "Z")//Не понятно зачем
					{
						Formulas cvZ = allFormulas.FirstOrDefault(x => x.TypeFrm == cv.TypeFrm && x.FID.ToString() == mvOperandSourceId.Substring(1));
						operand = allOperands.FirstOrDefault(x => x.FID == cv.FID && x.TypeFrm == cv.TypeFrm
						&& (x.CatOperand + x.IdOperand) == (cvZ.CatRes + cvZ.IdRes));
					}
					else
					{
						operand = allOperands.FirstOrDefault(x => x.FID == cv.FID && x.TypeFrm == cv.TypeFrm
						&& (x.CatOperand + x.IdOperand) == mvOperandSourceId);
					}
				}
				catch (ArgumentOutOfRangeException) { continue; }
				if (operand != null)
				{
					var t = operands.First(x => x.CatOperand + x.IdOperand == operand.CatOperand + operand.IdOperand);
					string leng = operandFrm.Substring(operandFrm.Length - 1);
					if (leng == "V")
					{
						t.Field = MeasValueAttribute.value;
					}
					else if (leng == "F")
					{

						t.Field = MeasValueAttribute.qualityCode;
						t.OperandLit += "Ф";
					}
					else if (leng == "T")
					{
						t.Field = MeasValueAttribute.writeTime;
						t.OperandLit += "Т";
					}
				}
			}

			List<string> operandFrmList = new List<string>();
			AnalogValue hCk11 = (AnalogValue)ModelImage.GetObject(oi11.UidVal);
			HISPartition hISPartition = (HISPartition)ModelImage.GetObject(new Guid("1000007D-0000-0000-C000-0000006D746C"));//Аналоговые 1 час
			MeasurementValueType mvt = (MeasurementValueType)ModelImage.GetObject(new Guid("5F42143F-31CF-42FD-AD69-0BA60FE264B4"));

			MetaClass MVSClass = ModelImage.MetaData.Classes["MeasurementValueSource"];
			IEnumerable<MeasurementValueSource> mvsCollect = ModelImage.GetObjects(MVSClass).Cast<MeasurementValueSource>();
			MeasurementValueSource mvs = mvsCollect.First(x => x.name.Equals("Расчет"));
			try
			{
				ModelImage.BeginTransaction();
				CalculatedAnalogValue cavNew = null;

				cavNew = (CalculatedAnalogValue)ModelImage.CreateObject(ModelImage.MetaData.Classes["CalculatedAnalogValue"]);
				cavNew.MeasurementValueType = mvt;
				cavNew.name = "CalcW_" + hCk11.name;
				cavNew.InterpolationParams = hCk11.InterpolationParams;
				cavNew.schedule = CalculationSchedule.byChange;
				cavNew.ParentObject = hCk11.Analog;
				cavNew.Analog = hCk11.Analog;
				cavNew.externalId = "Calc" + oi11.Id.Replace('H', 'W');
				cavNew.MeasurementValueSource = mvs;
				cavNew.HISPartition = hISPartition;

				cavNew.Expression = (AnalogMeasurementExpression)ModelImage.CreateObject(ModelImage.MetaData.Classes["AnalogMeasurementExpression"]);
				cavNew.Expression.definitionMethod = DefinitionMethod.internalFormula;
				cavNew.allowOperatorInput = cv.AllowManual;

				foreach (var operand in operands)
				{
					MeasValueDirectOperand mvOperand = (MeasValueDirectOperand)ModelImage.CreateObject(ModelImage.MetaData.Classes["MeasValueDirectOperand"]);
					//MeasValueOperand mvOperand = (MeasValueOperand)ModelImage.CreateObject(ModelImage.MetaData.Classes["MeasValueOperand"]);
					mvOperand.MeasurementValue = operandList.First(x => x.Item2 == operand.CatOperand + operand.IdOperand).Item1;
					mvOperand.name = operand.OperandLit;
					mvOperand.field = operand.Field;
					if (operand.TShift != 0)
					{
						MetaClass tsClass = ModelImage.MetaData.Classes["TimeShift"];
						var ts = ModelImage.CreateObject(tsClass) as TimeShift;
						var tsABS = Math.Abs(operand.TShift);
						if (tsABS < 60)
						{
							ts.secondValue = operand.TShift;
							mvOperand.name += $"_{Math.Abs(ts.minuteValue)}с";
						}
						else if (tsABS >= 60 && tsABS < 3600)
						{
							ts.minuteValue = operand.TShift / 60;
							mvOperand.name += $"_{Math.Abs(ts.minuteValue)}мин";
						}
						else if (tsABS >= 3600 && tsABS < 86400)
						{
							ts.hourValue = operand.TShift / 60 / 60;
							mvOperand.name += $"_{Math.Abs(ts.hourValue)}ч";
						}
						else if (tsABS >= 86400)
						{
							ts.dayValue = operand.TShift / 24 / 60 / 60;
							mvOperand.name += $"_{Math.Abs(ts.dayValue)}д";
						}
						mvOperand.TimeShift = ts;
					}
					mvOperand.Expression = cavNew.Expression;
					mvOperand.ParentObject = cavNew.Expression;
					cavNew.Expression.AddToOperands(mvOperand);
				}
				var txt = cv.FormulaTxt.Replace("[-", "_").Replace("[", "_").Replace("]", string.Empty);
				txt = txt.Replace(@"%", @"*100/");
				cavNew.Expression.formula = txt;
				cavNew.Expression.ParentObject = cavNew;
				cavNew.Expression.name = "Calc" + oi11.Id.Replace('H', 'W');
				ModelImage.CommitTransaction();
				OIck11 oiRes = new OIck11
				{
					Name = cavNew.name,
					UidMeas = cavNew.Analog.Uid,
					UidVal = cavNew.Uid,
					HISpartition = cavNew.HISPartition.name,
					ValueSource = cavNew.MeasurementValueSource.name,
					Class = "CalculatedAnalogValue",
					MeasType = cavNew.Analog.MeasurementType.name,
					ValueType = cavNew.MeasurementValueType.name
				};
				return oiRes;
			}
			catch(Exception ex)
			{
				{
					ModelImage.RollbackTransaction();
					throw new ArgumentException("Не удалось создать вычисляемое значение. " + ex.Message);
				}
			}
			
		}

		/// <summary>
		/// СОздание реплицируемого аналогового значения
		/// </summary>
		/// <param name="externalId"></param>
		/// <returns></returns>
		private MeasurementValue CreateReplicateMeas(string externalId)
		{
			if (externalId[0] == 'S')
			{
				Discrete DiscreteForVal;
				try
				{
					DiscreteForVal = (Discrete)ModelImage.GetObject(UidDiscreteForVal);
				}
				catch
				{
					throw new ArgumentException($"Дискрет для создания операндов не найден в ИМ");
				}
				try
				{
					ModelImage.BeginTransaction();
					ReplicatedDiscreteValue repValNew = (ReplicatedDiscreteValue)ModelImage.CreateObject(this.ModelImage.MetaData.Classes["ReplicatedDiscreteValue"]);
					repValNew.ParentObject = DiscreteForVal;
					repValNew.Discrete = DiscreteForVal;
					repValNew.name = externalId;
					repValNew.sourceId = externalId;
					repValNew.HISPartition = (HISPartition)ModelImage.GetObject(new Guid("1000006C-0000-0000-C000-0000006D746C"));//Дискретные 2 времени
					repValNew.MeasurementValueSource = (MeasurementValueSource)ModelImage.GetObject(new Guid("10001335-0000-0000-C000-0000006D746C"));//репликация
					repValNew.MeasurementValueType = (MeasurementValueType)ModelImage.GetObject(new Guid("10000051-0000-0000-C000-0000006D746C"));//фактическое значение
					ModelImage.CommitTransaction();
					return repValNew;
				}
				catch
				{
					throw new ArgumentException($"Ошибка создания {externalId}");
				}
			}
			else
			{
				Analog AnalogForVal;
				try
				{
					AnalogForVal = (Analog)ModelImage.GetObject(UidAnalogForVal);
				}
				catch
				{
					throw new ArgumentException($"Аналог для создания операндов не найден в ИМ");
				}
				try
				{
					ModelImage.BeginTransaction();
					ReplicatedAnalogValue repValNew = (ReplicatedAnalogValue)ModelImage.CreateObject(this.ModelImage.MetaData.Classes["ReplicatedAnalogValue"]);
					repValNew.ParentObject = AnalogForVal;
					repValNew.Analog = AnalogForVal;
					repValNew.name = externalId;
					repValNew.sourceId = externalId;
					repValNew.HISPartition = (HISPartition)ModelImage.GetObject(new Guid("10000064-0000-0000-C000-0000006D746C"));//Аналоговые 2 времени
					repValNew.MeasurementValueSource = (MeasurementValueSource)ModelImage.GetObject(new Guid("10001335-0000-0000-C000-0000006D746C"));//репликация
					repValNew.MeasurementValueType = (MeasurementValueType)ModelImage.GetObject(new Guid("10000051-0000-0000-C000-0000006D746C"));//фактическое значение 
					ModelImage.CommitTransaction();
					return repValNew;
				}
				catch
				{
					throw new ArgumentException($"Ошибка создания {externalId}");
				}
			}

		}

	}
}
