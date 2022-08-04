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
		public string Prefix;
		public Function(ModelImage mImage, bool createRepVal)
		{
			ModelImage = mImage;
			CreateRepVal = createRepVal;
		}
		/// <summary>
		/// Создание RB значения
		/// </summary>
		/// <param name="oi"></param>
		/// <returns></returns>
		public OIck11 CreateRBvalue(OIck11 oi)
		{
			string idW = oi.Id.Replace('H', 'W');
			AnalogValue av = (AnalogValue)ModelImage.GetObject(oi.UidVal);
			HISPartition hISPartition = (HISPartition)ModelImage.GetObject(new Guid("1000007D-0000-0000-C000-0000006D746C"));//Аналоговые 1 час
			MeasurementValueType mvt = (MeasurementValueType)ModelImage.GetObject(new Guid("5F42143F-31CF-42FD-AD69-0BA60FE264B4"));//СДВ
			MeasurementValueSource mvSource = (MeasurementValueSource)ModelImage.GetObject(new Guid("10000C28-0000-0000-C000-0000006D746C"));//Удаленный ЦУ	
			try
			{
				ModelImage.BeginTransaction();
				var rapidBusVal = (RapidBusIndirectAnalogValue)ModelImage.CreateObject(ModelImage.MetaData.Classes["RapidBusIndirectAnalogValue"]);
				rapidBusVal.name = $"{Prefix}{idW} {mvt.name}  [RB]";
				rapidBusVal.externalId = "RB" + idW;
				rapidBusVal.ParentObject = av.ParentObject;
				rapidBusVal.Analog = av.Analog;
				rapidBusVal.hisPreserveTime = true;
				rapidBusVal.HISPartition = hISPartition;
				rapidBusVal.MeasurementValueSource = mvSource;
				rapidBusVal.MeasurementValueType = mvt;
				ModelImage.CommitTransaction();
				OIck11 oiRes = new OIck11
				{
					Name = rapidBusVal.name,
					UidMeas = rapidBusVal.Analog.Uid,
					UidVal = rapidBusVal.Uid,
					Id = oi.Id.Replace('H', 'W'),
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
		/// <summary>
		/// Создание вычисляемого значения
		/// </summary>
		/// <param name="oi11"></param>
		/// <param name="cv"></param>
		/// <param name="operands"></param>
		/// <param name="allFormulas"></param>
		/// <param name="allOperands"></param>
		/// <returns></returns>
		public OIck11 CreateCalcvalue(OIck11 oi11, char cat, IEnumerable<Formulas> allFormulas, IEnumerable<OperandFrm> allOperands)
		{
			List<(MeasurementValue, string)> operandList = new List<(MeasurementValue, string)>();
			Formulas cv;
			List<OperandFrm> operands;
			try
			{
				cv = allFormulas.First(x => x.CatRes + x.IdRes == cat + oi11.Id.Remove(0, 1));
				operands = (List<OperandFrm>)allOperands.Where(x => x.FID == cv.FID && x.TypeFrm == cv.TypeFrm).ToList();
			}
			catch
			{
				throw new ArgumentException("Не найдена формула в БД СК-07. Возможно контроль отключен.");
			}
			//Проверка все ли есть операнды в ИМ			
			foreach (var operand in operands)
			{
				MetaClass mvClass = ModelImage.MetaData.Classes["MeasurementValue"];
				IEnumerable<MeasurementValue> mvCollect = ModelImage.GetObjects(mvClass).Cast<MeasurementValue>().Where(x => x.MeasurementValueType.name != "Дорасчётное значение" && x.MeasurementValueType.name != "Агрегируемое значение");
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
				OperandFrm operand = operands.FirstOrDefault(x => x.CatOperand + x.IdOperand == mvOperandSourceId && x.Field == 0);
				//try
				//{
				if (operand == null)//Если в формуле операндом является еще одна формула
				{
					try
					{
						if (mvOperandSourceId.Remove(1) == "Z")
						{
							Formulas cvZ = allFormulas.FirstOrDefault(x => x.TypeFrm == cv.TypeFrm && x.FID.ToString() == mvOperandSourceId.Substring(1));
							operand = allOperands.FirstOrDefault(x => x.FID == cv.FID && x.TypeFrm == cv.TypeFrm
							&& (x.CatOperand + x.IdOperand) == (cvZ.CatRes + cvZ.IdRes));
						}
					}
					catch (ArgumentOutOfRangeException) { continue; }
				}
				/*else
				{
					operand = allOperands.FirstOrDefault(x => x.FID == cv.FID && x.TypeFrm == cv.TypeFrm
					&& (x.CatOperand + x.IdOperand) == mvOperandSourceId);
				}*/
				//}
				//catch (ArgumentOutOfRangeException) { continue; }
				if (operand != null)
				{
					//var t = operands.First(x => x.CatOperand + x.IdOperand == operand.CatOperand + operand.IdOperand);
					string leng = operandFrm.Substring(operandFrm.Length - 1);
					if (leng == "V")
					{
						operand.Field = MeasValueAttribute.value;
					}
					else if (leng == "F")
					{

						operand.Field = MeasValueAttribute.qualityCode;
						operand.OperandLit += "Ф";
					}
					else if (leng == "T")
					{
						operand.Field = MeasValueAttribute.writeTime;
						operand.OperandLit += "Т";
					}
				}
				/*else
				{
					throw new ArgumentException($"Проверить операнд {mvOperandSourceId} для формулы {oi11.Id}");
				}*/
			}

			List<string> operandFrmList = new List<string>();
			AnalogValue hCk11 = (AnalogValue)ModelImage.GetObject(oi11.UidVal);
			HISPartition hISPartition = (HISPartition)ModelImage.GetObject(new Guid("1000007D-0000-0000-C000-0000006D746C"));//Аналоговые 1 час
			MeasurementValueType mvt = (MeasurementValueType)ModelImage.GetObject(new Guid("5F42143F-31CF-42FD-AD69-0BA60FE264B4"));//СДВ

			MetaClass MVSClass = ModelImage.MetaData.Classes["MeasurementValueSource"];
			IEnumerable<MeasurementValueSource> mvsCollect = ModelImage.GetObjects(MVSClass).Cast<MeasurementValueSource>();
			MeasurementValueSource mvs = mvsCollect.First(x => x.name.Equals("Расчет"));
			try
			{
				ModelImage.BeginTransaction();
				CalculatedAnalogValue cavNew = null;
				string idW = oi11.Id.Replace('H', 'W');
				cavNew = (CalculatedAnalogValue)ModelImage.CreateObject(ModelImage.MetaData.Classes["CalculatedAnalogValue"]);
				cavNew.MeasurementValueType = mvt;
				cavNew.name = $"{Prefix}{idW} {mvt.name}  [Calc]";
				cavNew.InterpolationParams = hCk11.InterpolationParams;
				cavNew.schedule = CalculationSchedule.byChange;
				cavNew.ParentObject = hCk11.Analog;
				cavNew.Analog = hCk11.Analog;
				cavNew.externalId = "Calc" + idW;
				cavNew.MeasurementValueSource = mvs;
				cavNew.HISPartition = hISPartition;

				cavNew.Expression = (AnalogMeasurementExpression)ModelImage.CreateObject(ModelImage.MetaData.Classes["AnalogMeasurementExpression"]);
				cavNew.Expression.definitionMethod = DefinitionMethod.internalFormula;
				cavNew.allowOperatorInput = cv.AllowManual;
				var t = operands.Distinct();
				foreach (var operand in operands.Distinct().ToList<OperandFrm>())
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
				cavNew.Expression.name = idW;
				ModelImage.CommitTransaction();
				OIck11 oiRes = new OIck11
				{
					Name = cavNew.name,
					UidMeas = cavNew.Analog.Uid,
					UidVal = cavNew.Uid,
					Id = oi11.Id.Replace('H', 'W'),
					HISpartition = cavNew.HISPartition.name,
					ValueSource = cavNew.MeasurementValueSource.name,
					Class = "CalculatedAnalogValue",
					MeasType = cavNew.Analog.MeasurementType.name,
					ValueType = cavNew.MeasurementValueType.name
				};
				return oiRes;
			}
			catch (Exception ex)
			{
				ModelImage.RollbackTransaction();
				throw new ArgumentException("Не удалось создать вычисляемое значение. " + ex.Message);

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
					if (DiscreteForVal == null)
						throw new ArgumentException($"Дискрет для создания операндов не найден в ИМ");
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
					if (AnalogForVal == null)
						throw new ArgumentException($"Аналог для создания операндов не найден в ИМ");
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

		/// <summary>
		/// Создание агрегируемого значения
		/// </summary>
		/// <param name="oi11"></param>
		/// <param name="IntegParamCollect"></param>
		/// <returns></returns>
		public OIck11 CreateAgregateValue(OIck11 oi11, List<IntegParam> IntegParamCollect)
		{
			string idW = oi11.Id.Replace('H', 'W');
			HISPartition hISPartition = (HISPartition)ModelImage.GetObject(new Guid("1000007D-0000-0000-C000-0000006D746C"));//Аналоговые 1 час
			MeasurementValueType mvt = (MeasurementValueType)ModelImage.GetObject(new Guid("5F42143F-31CF-42FD-AD69-0BA60FE264B4"));//СДВ
			IntegParam agregVal;
			string idAgrerSource;
			try
			{
				agregVal = IntegParamCollect.FirstOrDefault(x => x.CategoryOI + x.IdOI == oi11.Id);
				if (agregVal == null)
					agregVal = IntegParamCollect.FirstOrDefault(x => x.CategoryOI + x.IdOI == idW);
				idAgrerSource = agregVal.CategorySource + agregVal.IdSource;
			}
			catch
			{
				throw new ArgumentException("Не найдены параметры агрегирования. Возможно контроль в СК-07 отключен.");
			}
			MetaClass mvClass = ModelImage.MetaData.Classes["MeasurementValue"];
			IEnumerable<MeasurementValue> mvCollect = ModelImage.GetObjects(mvClass).Cast<MeasurementValue>().Where(x => x.MeasurementValueType.name != "Дорасчётное значение" && x.MeasurementValueType.name != "Агрегируемое значение");
			var sourceAgreg = mvCollect.FirstOrDefault(x => x.externalId?.Replace("Calc", "").Replace("Agr", "").Replace("RB", "") == idAgrerSource);
			if (sourceAgreg == null)
			{
				if (agregVal.CategorySource == "S")
				{
					MetaClass rdvClass = ModelImage.MetaData.Classes["ReplicatedDiscreteValue"];
					IEnumerable<ReplicatedDiscreteValue> rdvCollect = ModelImage.GetObjects(rdvClass).Cast<ReplicatedDiscreteValue>();
					sourceAgreg = rdvCollect.FirstOrDefault(x => x.sourceId == idAgrerSource);
				}
				else
				{
					MetaClass ravClass = ModelImage.MetaData.Classes["ReplicatedAnalogValue"];
					IEnumerable<ReplicatedAnalogValue> ravCollect = ModelImage.GetObjects(ravClass).Cast<ReplicatedAnalogValue>();
					sourceAgreg = ravCollect.FirstOrDefault(x => x.sourceId == idAgrerSource);
				}
			}
			if (sourceAgreg == null)
			{
				if (CreateRepVal)
				{
					sourceAgreg = CreateReplicateMeas(idAgrerSource);
				}
				else if (sourceAgreg == null)
				{
					throw new ArgumentException($"Для агрегируемого параметра {oi11.Id} не найден источник {idAgrerSource} ");
				}
			}
			AnalogValue oiCk11 = (AnalogValue)ModelImage.GetObject(oi11.UidVal);
			MetaClass MVSClass = ModelImage.MetaData.Classes["MeasurementValueSource"];
			IEnumerable<MeasurementValueSource> mvsCollect = ModelImage.GetObjects(MVSClass).Cast<MeasurementValueSource>();
			//AllOi oiAgreg = AllOiCollect.FirstOrDefault(x => x.idOI == agregVal.IdOI && x.Category == agregVal.CategoryOI);
			MeasurementValueSource mvs = mvsCollect.First(x => x.name.Equals("Расчет"));
			try
			{
				ModelImage.BeginTransaction();
				Analog analog = oiCk11.Analog;

				AggregatedAnalogValue aavNew = (AggregatedAnalogValue)ModelImage.CreateObject(ModelImage.MetaData.Classes["AggregatedAnalogValue"]);
				aavNew.name = $"{Prefix}{idW} {mvt.name}  [Agr]";
				aavNew.Source = sourceAgreg;
				aavNew.MeasurementValueType = mvt;
				if (oiCk11 is ReplicatedAnalogValue rv)
				{
					aavNew.InterpolationParams = rv.InterpolationParams;
				}
				if (oiCk11 is RapidBusIndirectAnalogValue rb)
				{
					aavNew.InterpolationParams = rb.InterpolationParams;
				}

				aavNew.valueFilterType = GetFilterType(agregVal.DFilter);
				aavNew.schedule = GetSchedule(agregVal.Periodic);
				if (agregVal.Periodic == 1)
				{
					aavNew.regularPeriod = agregVal.IStart;
				}
				else if (agregVal.Periodic == 0)
				{
					aavNew.intervalStart = new DateTime();
					aavNew.intervalEnd = new DateTime();
					aavNew.intervalStart += TimeSpan.FromSeconds(agregVal.IStart);
					aavNew.intervalEnd += TimeSpan.FromSeconds(agregVal.IEnd);
				}
				/*else if (agregVal.Periodic == 7)
				{
					Log("Необходимо вручную заполнить временную зону для " + aavNew.name);
				}*/
				aavNew.Method = GetMethod(agregVal.IMethod);
				aavNew.intermediateCalcStep = GetStep(agregVal.IStep);
				aavNew.inverse = agregVal.Inv;
				aavNew.queryStep = agregVal.ParamStep;//TODO: задается не всегда
				aavNew.ParentObject = analog;
				aavNew.Analog = analog;
				aavNew.externalId = "Agr" + oi11.Id.Replace('H', 'W');
				aavNew.MeasurementValueSource = mvs;
				aavNew.HISPartition = hISPartition;
				ModelImage.CommitTransaction();
				OIck11 oiRes = new OIck11
				{
					Name = aavNew.name,
					UidMeas = aavNew.Analog.Uid,
					UidVal = aavNew.Uid,
					Id = oi11.Id.Replace('H', 'W'),
					HISpartition = aavNew.HISPartition.name,
					ValueSource = aavNew.MeasurementValueSource.name,
					Class = "AggregatedAnalogValue",
					MeasType = aavNew.Analog.MeasurementType.name,
					ValueType = aavNew.MeasurementValueType.name
				};
				return oiRes;
			}
			catch (Exception ex)
			{
				ModelImage.RollbackTransaction();
				throw new ArgumentException($"Не удалось создать агрегируемое значение {oi11.Id} в ИА: " + ex.Message);
			}


		}
		public OIck11 CreateAgregateValue(OIck11 oi11, DrSource drSource)
		{
			HISPartition hISPartition = (HISPartition)ModelImage.GetObject(new Guid("1000007D-0000-0000-C000-0000006D746C"));//Аналоговые 1 час
			MeasurementValueType mvt = (MeasurementValueType)ModelImage.GetObject(new Guid("5F42143F-31CF-42FD-AD69-0BA60FE264B4"));//СДВ
			AggregationMethod am = (AggregationMethod)ModelImage.GetObject(new Guid("10001634-0000-0000-C000-0000006D746C"));//Среднее по правилу трапеции
			IntegParam agregVal;
			string idAgrerSource = drSource.IdSource;

			MetaClass mvClass = ModelImage.MetaData.Classes["AnalogValue"];
			IEnumerable<AnalogValue> mvCollect = ModelImage.GetObjects(mvClass).Cast<AnalogValue>().Where(x => x.MeasurementValueType.name != "Дорасчётное значение" && x.MeasurementValueType.name != "Агрегируемое значение");


			var sourceAgreg = mvCollect.FirstOrDefault(x => x.externalId?.Replace("Calc", "").Replace("Agr", "").Replace("RB", "") == idAgrerSource);
			if (sourceAgreg == null)
			{
				MetaClass ravClass = ModelImage.MetaData.Classes["ReplicatedAnalogValue"];
				IEnumerable<ReplicatedAnalogValue> ravCollect = ModelImage.GetObjects(ravClass).Cast<ReplicatedAnalogValue>();
				sourceAgreg = ravCollect.FirstOrDefault(x => x.sourceId == idAgrerSource);
			}
			if (sourceAgreg == null)
			{
				if (CreateRepVal)
				{
					sourceAgreg = (AnalogValue)CreateReplicateMeas(idAgrerSource);
				}
				else if (sourceAgreg == null)
				{
					throw new ArgumentException($"Для агрегируемого параметра {oi11.Id} не найден источник {idAgrerSource} ");
				}
			}

			AnalogValue oiCk11 = (AnalogValue)ModelImage.GetObject(oi11.UidVal);
			MetaClass MVSClass = ModelImage.MetaData.Classes["MeasurementValueSource"];
			IEnumerable<MeasurementValueSource> mvsCollect = ModelImage.GetObjects(MVSClass).Cast<MeasurementValueSource>();
			MeasurementValueSource mvs = mvsCollect.First(x => x.name.Equals("Расчет"));
			string idW = oi11.Id.Replace('H', 'W');
			try
			{
				ModelImage.BeginTransaction();
				Analog analog = oiCk11.Analog;
				AggregatedAnalogValue aavNew = (AggregatedAnalogValue)ModelImage.CreateObject(ModelImage.MetaData.Classes["AggregatedAnalogValue"]);
				aavNew.name = $"{Prefix}{idW} {mvt.name}  [Agr]";
				aavNew.Source = sourceAgreg;
				aavNew.MeasurementValueType = mvt;
				/*if (oiCk11 is ReplicatedAnalogValue rv)
				{
					aavNew.InterpolationParams = rv.InterpolationParams;
				}
				if (oiCk11 is RapidBusIndirectAnalogValue rb)
				{
					aavNew.InterpolationParams = rb.InterpolationParams;
				}*/
				//TODO: тут возможно придется менять
				aavNew.valueFilterType = AggregationValueFilterType.all;
				aavNew.schedule = AggregationSchedule.regular;
				aavNew.regularPeriod = -60;
				aavNew.Method = am;
				//aavNew.intermediateCalcStep = 3600;
				aavNew.inverse = drSource.Inv;
				//aavNew.queryStep = 0;

				aavNew.ParentObject = analog;
				aavNew.Analog = analog;
				aavNew.externalId = "Agr" + oi11.Id.Replace('H', 'W');
				aavNew.MeasurementValueSource = mvs;
				aavNew.HISPartition = hISPartition;
				ModelImage.CommitTransaction();
				OIck11 oiRes = new OIck11
				{
					Name = aavNew.name,
					UidMeas = aavNew.Analog.Uid,
					UidVal = aavNew.Uid,
					Id = oi11.Id.Replace('H', 'W'),
					HISpartition = aavNew.HISPartition.name,
					ValueSource = aavNew.MeasurementValueSource.name,
					Class = "AggregatedAnalogValue",
					MeasType = aavNew.Analog.MeasurementType.name,
					ValueType = aavNew.MeasurementValueType.name
				};
				return oiRes;
			}
			catch (Exception ex)
			{
				ModelImage.RollbackTransaction();
				throw new ArgumentException($"Не удалось создать агрегируемое значение {oi11.Id} в ИА: " + ex.Message);
			}


		}
		public OIck11 CreateRepeatedValue(OIck11 oi11)
		{
			HISPartition hISPartition = (HISPartition)ModelImage.GetObject(new Guid("1000007D-0000-0000-C000-0000006D746C"));//Аналоговые 1 час
			MeasurementValueType mvt = (MeasurementValueType)ModelImage.GetObject(new Guid("5F42143F-31CF-42FD-AD69-0BA60FE264B4"));//СДВ			
			string idW = oi11.Id.Replace('H', 'W');
			AnalogValue oiCk11 = (AnalogValue)ModelImage.GetObject(oi11.UidVal);
			MetaClass MVSClass = ModelImage.MetaData.Classes["MeasurementValueSource"];
			IEnumerable<MeasurementValueSource> mvsCollect = ModelImage.GetObjects(MVSClass).Cast<MeasurementValueSource>();
			MeasurementValueSource mvs = mvsCollect.First(x => x.name.Equals("Расчет"));

			try
			{
				ModelImage.BeginTransaction();
				Analog analog = oiCk11.Analog;
				RepeatedAnalogValue ravNew = (RepeatedAnalogValue)ModelImage.CreateObject(ModelImage.MetaData.Classes["RepeatedAnalogValue"]);
				ravNew.name = $"{Prefix}{idW} [Rep]";
				ravNew.MeasurementValueType = mvt;

				ravNew.fillTimeShift = 0;
				ravNew.incrementUnit = RepeateIncrementUnit.number;
				ravNew.incrementValue = 0;

				ravNew.ParentObject = analog;
				ravNew.Analog = analog;
				ravNew.externalId = "Agr" + oi11.Id.Replace('H', 'W');
				ravNew.MeasurementValueSource = mvs;
				ravNew.HISPartition = hISPartition;
				ModelImage.CommitTransaction();
				OIck11 oiRes = new OIck11
				{
					Name = ravNew.name,
					UidMeas = ravNew.Analog.Uid,
					UidVal = ravNew.Uid,
					Id = oi11.Id.Replace('H', 'W'),
					HISpartition = ravNew.HISPartition.name,
					ValueSource = ravNew.MeasurementValueSource.name,
					Class = "RepeatedAnalogValue",
					MeasType = ravNew.Analog.MeasurementType.name,
					ValueType = ravNew.MeasurementValueType.name
				};
				return oiRes;
			}
			catch (Exception ex)
			{
				ModelImage.RollbackTransaction();
				throw new ArgumentException($"Не удалось создать повторяемое значение {oi11.Id} в ИА: " + ex.Message);
			}


		}
		private AggregationValueFilterType GetFilterType(string filter)
		{
			AggregationValueFilterType valueFilterType = new AggregationValueFilterType();
			switch (filter)
			{
				case "*":
					valueFilterType = AggregationValueFilterType.all;
					break;
				case "+":
					valueFilterType = AggregationValueFilterType.positive;
					break;
				case "-":
					valueFilterType = AggregationValueFilterType.negative;
					break;
			}
			return valueFilterType;
		}
		private AggregationSchedule GetSchedule(int periodic)
		{
			AggregationSchedule schedule = new AggregationSchedule();
			switch (periodic)
			{
				case 0:
					schedule = AggregationSchedule.interval;
					break;
				case 1:
					schedule = AggregationSchedule.regular;
					break;
				case 2:
					schedule = AggregationSchedule.day;
					break;
				case 3:
					schedule = AggregationSchedule.energyWeek;
					break;
				case 4:
					schedule = AggregationSchedule.month;
					break;
				case 5:
					schedule = AggregationSchedule.quarter;
					break;
				case 6:
					schedule = AggregationSchedule.year;
					break;
				case 7:
					schedule = AggregationSchedule.timeZone;
					break;
			}
			return schedule;
		}
		private AggregationMethod GetMethod(int idMethod)
		{
			Guid methodGuid = new Guid();
			switch (idMethod)
			{
				case 1:
					methodGuid = new Guid("10001636-0000-0000-C000-0000006D746C");//Интеграл по правилу трапеций
					break;
				case 2:
					methodGuid = new Guid("10001634-0000-0000-C000-0000006D746C");//Среднее по правилу трапеций
					break;
				case 3:
					methodGuid = new Guid("10001635-0000-0000-C000-0000006D746C");//Интеграл по правилу прямоугольников
					break;
				case 4:
					methodGuid = new Guid("10001633-0000-0000-C000-0000006D746C");//Среднее по правилу прямоугольников
					break;
				case 5:
					methodGuid = new Guid("1000163B-0000-0000-C000-0000006D746C");//Сумма
					break;
				case 6:
					methodGuid = new Guid("10001637-0000-0000-C000-0000006D746C");//Максимум
					break;
				case 7:
					methodGuid = new Guid("10001639-0000-0000-C000-0000006D746C");//Минимум
					break;
				case 8:
					methodGuid = new Guid("10001632-0000-0000-C000-0000006D746C");//Среднее арифметическое
					break;
			}
			AggregationMethod method = (AggregationMethod)ModelImage.GetObject(methodGuid);
			return method;
		}
		private double GetStep(int step)
		{
			if (step == 0)
			{
				return 0;
			}
			else if (step <= 60)
			{
				return 60;
			}
			else if (step <= 120)
			{
				return 120;
			}
			else if (step <= 180)
			{
				return 180;
			}
			else if (step <= 300)
			{
				return 300;
			}
			else if (step <= 360)
			{
				return 360;
			}
			else if (step <= 600)
			{
				return 600;
			}
			else if (step <= 900)
			{
				return 900;
			}
			else if (step <= 1800)
			{
				return 1800;
			}
			else { return step; }
		}
	}
}
