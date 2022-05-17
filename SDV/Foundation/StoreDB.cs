using SDV.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDV.Foundation
{
	public class StoreDB//TODO: проверить запросы 
	{
		public string serverName;
		public string dbName;

		#region Запросы sql
		public const string SDVQuery =
			 @"
SELECT DefDRParam.ID as [ID]
--,'H' as [Тип ОИ]
,DefDRParam.name as [Наименование ОИ]
,p.Name as [Type]
,f2.Name as [h]
,f.Name as [w]
FROM   DefDRParam INNER JOIN  EnObj ON DefDRParam.EObject = EnObj.ID
inner join  ParTypes p on DefDRParam.Type=p.ID 
inner join  FillType f2 on DefDRParam.Fill2=f2.ID 
inner join  FillType f on DefDRParam.Fill=f.ID 
order by  [ID]

            ";
		public const string DrSourceQuery =
			 @"
select * from DRSource
            ";

		public const string TransmitQuery =
			 @"


SELECT et.Abbr+' '+EnObj.Name  as [Объект] 
,'W' as [Тип ОИ]------!!!!!
,DEFDRPARAM.ID as [Номер ТС в ДЦ источнике] 
,p.Abbr as [Тип]
,DEFDRPARAM.name as [Наименование ТС в ДЦ-получателе]

,RCV_S.RTU as [ДЦ-источник]

FROM   DEFDRPARAM INNER JOIN  EnObj ON DEFDRPARAM.EObject = EnObj.ID 
--Inner join  TSCat c on DEFDRPARAM.Category=c.ID
inner join  ParTypes p on DEFDRPARAM.Type=p.ID
Inner join  EnObjType et on EnObj.Type=et.ID
inner join (SELECT SND.ID, Тип, RTU,[Addr_Id],[inv],DtSetId FROM(
	  select s.ID as DtSetId, p.OI ,p.IDOI as id, ' Набор ТМ' as Тип, RTU.Name as RTU,cast(isnull(mask,addr) as sql_variant) as 'Addr_Id',inv from  dtParam2 p, dtSet s INNER JOIN  RTU ON s.RTUID = RTU.ID where p.SetID=s.ID and s.Enable=1 and s.trans=1
union select s.ID as DtSetId, p.OI  as 'OI',p.IDOI as ID, ' Набор ICCP' as Тип, RTU.Name as RTU,cast(i.RemoteDomain+':'+cast(s.SetID as varchar)+':'+cast(Identifier as varchar) as sql_variant) as 'Addr_Id', inv
--select p.OI  as 'OI',p.IDOI as ID, ' Набор ICCP' as Тип, RTU.Name as RTU,cast(i.RemoteDomain+'::'+cast(Identifier as varchar) as sql_variant) as 'Addr_Id', inv
--select p.OI  as 'OI',p.IDOI as ID, ' Набор ICCP' as Тип, RTU.Name as RTU,cast(/*iif(Scope=1,'::',i.RemoteDomain+'::')+*/cast(Identifier as varchar) as sql_variant) as 'Addr_Id', inv 
	  from  dtParam6 p INNER JOIN  dtSet s on p.SetID=s.ID INNER JOIN  RTU ON s.RTUID = RTU.ID INNER JOIN  RTU_ICCP I ON s.RTUID = I.RTUID where 
	  s.Enable=1 and s.trans=1 and rtu.Name not like '%ICCPAVT%'
) AS SND WHERE (snd.OI = 'W')) AS RCV_S on DEFDRPARAM.ID=RCV_S.ID------!!!!!

union
SELECT et.Abbr+' '+EnObj.Name  as [Объект] 
,'H' as [Тип ОИ]------!!!!!
,DefDRParam.ID as [Номер ТС в ДЦ источнике] 
,p.Abbr as [Тип]
,DefDRParam.name as [Наименование ТС в ДЦ-получателе]
,RCV_S.RTU as [ДЦ-источник]
FROM   DefDRParam INNER JOIN  EnObj ON DefDRParam.EObject = EnObj.ID 
inner join  ParTypes p on DefDRParam.Type=p.ID
Inner join  EnObjType et on EnObj.Type=et.ID
inner join (SELECT SND.ID, Тип, RTU,[Addr_Id],[inv],DtSetId FROM(
	  select  s.ID as DtSetId,p.OI ,p.IDOI as id, ' Набор ТМ' as Тип, RTU.Name as RTU,cast(isnull(mask,addr) as sql_variant) as 'Addr_Id',inv from  dtParam2 p, dtSet s INNER JOIN  RTU ON s.RTUID = RTU.ID where p.SetID=s.ID and s.Enable=1 and s.trans=1
union select  s.ID as DtSetId,p.OI  as 'OI',p.IDOI as ID, ' Набор ICCP' as Тип, RTU.Name as RTU,cast(i.RemoteDomain+':'+cast(s.SetID as varchar)+':'+cast(Identifier as varchar) as sql_variant) as 'Addr_Id', inv
--select p.OI  as 'OI',p.IDOI as ID, ' Набор ICCP' as Тип, RTU.Name as RTU,cast(i.RemoteDomain+'::'+cast(Identifier as varchar) as sql_variant) as 'Addr_Id', inv
	  from  dtParam6 p INNER JOIN  dtSet s on p.SetID=s.ID INNER JOIN  RTU ON s.RTUID = RTU.ID INNER JOIN  RTU_ICCP I ON s.RTUID = I.RTUID where 
	  s.Enable=1 and s.trans=1 and rtu.Name not like '%ICCPAVT%'
) AS SND WHERE (snd.OI = 'H')) AS RCV_S on DefDRParam.ID=RCV_S.ID------!!!!!


order by   [Тип ОИ], [Номер ТС в ДЦ источнике]

            ";

		public const string CalcValueQuery =
			@"
select 304,cast(Formulas.Txt as varchar(max)), Formulas.ID
	   ,Formulas.OI as [Тип],Formulas.Result as [ОИ], AllowManual as [рв]
	   ,cast(Formulas.Frml as varchar(max))
	    from Formulas WHERE (Formulas.OutOfWork = 1) 
            ";


		public const string OperandsQuery =
		   @"
 select Formulasr.OI as [Тип],Formulasr.Sid as [ОИ],replace(OICat.Abbr+cast(formulasr.SID as varchar),' ','') AS [Operand lit],
       304, Formulasr.FID, 	  Formulasr.TShift as [сдвиг времени]
	    from formulasr INNER JOIN Formulas ON FormulasR.FID = Formulas.ID INNER JOIN OICat ON formulasr.OI = OICat.Letter
		WHERE (Formulas.OutOfWork = 1)
            ";
		public const string AgregateQuery =
			@"
SELECT  [ID]
      ,[SourceOI]      ,[SourceID]
      -- ,  oik.dbo.fn_GetNameOI([SourceOI],[SourceID]) as [имяS] -- Имя параметра источника
      ,[ResultOI]      ,[ResultID]
       --,  oik.dbo.fn_GetNameOI([ResultOI],[ResultID]) as [имяR] -- Имя параметра результата
      ,cast ([IMethod] as int) as [IMethod]
      ,cast ([Periodic] as int) as [Periodic]
      ,[IStart]      ,[IEnd]      ,[IStep]
      ,[DFilter]
      ,[BadQuality]
      ,[Inv]
      ,[DFilterValue]
      ,[Enable]
      ,[ParamStep]
      ,[TimeZone]
      ,[ListDayType]
      ,[NoCalcFuture]
  FROM [IntegParam]
  where [enable] =1
            ";

		#endregion

		public void GetValueOI(DateTime dateStart, DateTime dateEnd, SdvMeas sdv)
		{
			string ids = sdv.H.Id.Remove(0, 1);			
			var strBuilder = new SqlConnectionStringBuilder()
			{
				DataSource = serverName,
				IntegratedSecurity = true,
				InitialCatalog = dbName
			};
			var connectionString = strBuilder.ConnectionString;
			using (SqlConnection connection = new SqlConnection(connectionString))
			{
				connection.Open();
				SqlCommand command = new SqlCommand("StepLt", connection);
				command.CommandType = CommandType.StoredProcedure;
				command.Parameters.Add("@Cat", SqlDbType.VarChar, 1);
				command.Parameters.Add("@Ids", SqlDbType.VarChar);
				command.Parameters.Add("@Start", SqlDbType.DateTime);
				command.Parameters.Add("@StartIsSummer", SqlDbType.Bit);
				command.Parameters.Add("@Stop", SqlDbType.DateTime);
				command.Parameters.Add("@StopIsSummer", SqlDbType.Bit);
				command.Parameters.Add("@Step", SqlDbType.Int);
				command.Parameters.Add("@ShowSystemTime", SqlDbType.Bit);
				command.Parameters.Add("@ResultForReports", SqlDbType.Bit);

				command.Parameters["@Cat"].SqlValue = "H";
				command.Parameters["@Ids"].SqlValue = ids;
				command.Parameters["@Start"].SqlValue = dateStart;
				command.Parameters["@StartIsSummer"].SqlValue = 1;
				command.Parameters["@Stop"].SqlValue = dateEnd; 
				command.Parameters["@StopIsSummer"].SqlValue = 0;
				command.Parameters["@Step"].SqlValue = 3600;
				command.Parameters["@ShowSystemTime"].SqlValue = 1;
				command.Parameters["@ResultForReports"].SqlValue = 1;
				try
				{
					SqlDataReader dataReader_oic = command.ExecuteReader();
					while (dataReader_oic.Read())
					{

						var measValue = new MeasValue
						{
							Value = Convert.ToDouble(dataReader_oic["Value"]),
							Date = Convert.ToDateTime(dataReader_oic["TimeLt"]),
							QualityCode = Convert.ToInt32(dataReader_oic["QC"])
						};
						var id = Convert.ToInt32(dataReader_oic["id"]);
						sdv.W.MeasValueList.Add(measValue);
					}
					dataReader_oic.Close();
				}
				catch (Exception ex)
				{
					throw new ArgumentException("Ошибка чтения данных:" + ex.Message);
				}
			}
		}
		 
		public ObservableCollection<OIck07> GetAllOI()
		{
			var query = SDVQuery;
			var strBuilder = new SqlConnectionStringBuilder()
			{
				DataSource = serverName,
				IntegratedSecurity = true,
				InitialCatalog = dbName
			};
			var connString = strBuilder.ConnectionString;
			ObservableCollection<OIck07> oiCollect = new ObservableCollection<OIck07>();
			using (var connection = new SqlConnection(connString))
			{
				connection.Open();
				using (SqlCommand com = new SqlCommand(query, connection))
				{
					var reader = com.ExecuteReader();
					while (reader.Read())
					{
						var t = 'H' + reader[0].ToString();
						oiCollect.Add(new OIck07()
						{
							Id = 'H' + reader[0].ToString(),
							Name = (string)reader[2],
							CategoryH = (string)reader[3],
							CategoryW = (string)reader[4]
						});
					}
				}
				return oiCollect;
			}
		}
		public ObservableCollection<DrSource> GetDrSource()
		{
			var query = DrSourceQuery;
			var strBuilder = new SqlConnectionStringBuilder()
			{
				DataSource = serverName,
				IntegratedSecurity = true,
				InitialCatalog = dbName
			};
			var connString = strBuilder.ConnectionString;
			ObservableCollection<DrSource> oiCollect = new ObservableCollection<DrSource>();
			using (var connection = new SqlConnection(connString))
			{
				connection.Open();
				using (SqlCommand com = new SqlCommand(query, connection))
				{
					var reader = com.ExecuteReader();
					while (reader.Read())
					{
						string id;
						var drnum = Convert.ToInt32(reader[1]);
						if (drnum == 2)
							id = "H" + reader[0].ToString();
						else
							id = "W" + reader[0].ToString();
						oiCollect.Add(new DrSource()
						{
							Id = id,
							IdSource = reader[2].ToString() + reader[3].ToString(),
							Inv = Convert.ToBoolean(reader[4]),
							NumbParam = Convert.ToInt32(reader[5])
						});
					}
				}
				return oiCollect;
			}
		}
		public ObservableCollection<IntegParam> GetIntegParam()
		{
			ObservableCollection<IntegParam> integParamCollect = new ObservableCollection<IntegParam>();
			var query = AgregateQuery;
			var strBuilder = new SqlConnectionStringBuilder()
			{
				DataSource = serverName,
				IntegratedSecurity = true,
				InitialCatalog = dbName
			};
			var connString = strBuilder.ConnectionString;

			using (var connection = new SqlConnection(connString))
			{
				connection.Open();
				using (SqlCommand com = new SqlCommand(query, connection))
				{
					var reader = com.ExecuteReader();
					while (reader.Read())
					{
						IntegParam param = new IntegParam();
						param.CategorySource = (string)reader[1];
						param.IdSource = (int)reader[2];
						param.CategoryOI = (string)reader[3];
						param.IdOI = (int)reader[4];
						param.IMethod = (int)reader[5];
						param.Periodic = (int)reader[6];
						param.IStart = (reader[7] != DBNull.Value) ? (int)reader[7] : 0;
						param.IEnd = (int)reader[8];
						param.IStep = (reader[9] != DBNull.Value) ? (int)reader[9] : 0;
						param.DFilter = (string)reader[10];
						param.Inv = (bool)reader[12];
						param.DFilterValue = (reader[13] is double readerVal) ? readerVal : 0;
						//param.DFilterValue = (reader[13] is DBNull.Value) ? (double)reader[13] : 0;
						param.ParamStep = (reader[15] != DBNull.Value) ? (int)reader[15] : 0;
						param.TimeZone = (reader[16] != DBNull.Value) ? (int)reader[16] : 0;
						integParamCollect.Add(param);
					}
				}
			}

			return integParamCollect;
		}
		public ObservableCollection<OperandFrm> GetOperands()
		{
			ObservableCollection<OperandFrm> formulasCollect = new ObservableCollection<OperandFrm>();
			var query = OperandsQuery;
			var strBuilder = new SqlConnectionStringBuilder()
			{
				DataSource = serverName,
				IntegratedSecurity = true,
				InitialCatalog = dbName
			};
			var connString = strBuilder.ConnectionString;

			using (var connection = new SqlConnection(connString))
			{
				connection.Open();
				using (SqlCommand com = new SqlCommand(query, connection))
				{
					var reader = com.ExecuteReader();
					while (reader.Read())
					{
						formulasCollect.Add(new OperandFrm()
						{
							CatOperand = (string)reader[0],
							IdOperand = (int)reader[1],
							FID = (int)reader[4],
							OperandLit = (string)reader[2],
							TypeFrm = (int)reader[3],
							TShift = (int)reader[5]
						});
					}
				}

			}

			return formulasCollect;
		}
		public ObservableCollection<OIck07> GetTransmitOi()
		{
			var query = TransmitQuery;
			var strBuilder = new SqlConnectionStringBuilder()
			{
				DataSource = serverName,
				IntegratedSecurity = true,
				InitialCatalog = dbName
			};
			var connString = strBuilder.ConnectionString;
			ObservableCollection<OIck07> oiCollect = new ObservableCollection<OIck07>();
			using (var connection = new SqlConnection(connString))
			{
				connection.Open();
				using (SqlCommand com = new SqlCommand(query, connection))
				{
					var reader = com.ExecuteReader();
					while (reader.Read())
					{
						oiCollect.Add(new OIck07()
						{
							Id = reader[1].ToString() + reader[2].ToString(),
							Name = (string)reader[4]
						});
					}
				}
				return oiCollect;
			}
		}
		public ObservableCollection<Formulas> GetCalcValue()
		{
			ObservableCollection<Formulas> calcValueCollect = new ObservableCollection<Formulas>();
			var query = CalcValueQuery;
			var strBuilder = new SqlConnectionStringBuilder()
			{
				DataSource = serverName,
				IntegratedSecurity = true,
				InitialCatalog = dbName
			};
			var connString = strBuilder.ConnectionString;

			using (var connection = new SqlConnection(connString))
			{
				connection.Open();
				using (SqlCommand com = new SqlCommand(query, connection))
				{
					var reader = com.ExecuteReader();
					while (reader.Read())
					{
						calcValueCollect.Add(new Formulas()
						{
							TypeFrm = (int)reader[0],
							FormulaTxt = (string)reader[1],
							FID = (int)reader[2],
							CatRes = (string)reader[3],
							IdRes = (int)reader[4],
							AllowManual = (bool)reader[5],
							Formulafrm = (string)reader[6]
						});
					}
				}
			}

			return calcValueCollect;
		}

	}
}
