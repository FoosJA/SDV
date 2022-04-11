using SDV.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
                            Id = 'H'+ reader[0].ToString(),
                            Name = (string)reader[2],
                            CategoryH = (string)reader[3],
                            CategoryW = (string)reader[4]
                        });
                    }
                }
                return oiCollect;
            }
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
