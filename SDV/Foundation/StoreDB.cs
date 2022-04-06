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
,'H' as [Тип ОИ]
,DefDRParam.name as [Наименование ОИ]
,p.Name as [Type]
,f2.Name as [fill2]
FROM   DefDRParam INNER JOIN  EnObj ON DefDRParam.EObject = EnObj.ID
inner join  ParTypes p on DefDRParam.Type=p.ID 
inner join  FillType f2 on DefDRParam.Fill2=f2.ID 

union
SELECT DefDRParam.ID as [ID]
,'W' as [Тип ОИ]
,DefDRParam.name as [Наименование ОИ]
,p.Name as [Type]
,f.Name as [fill]
FROM   DefDRParam INNER JOIN  EnObj ON DefDRParam.EObject = EnObj.ID
inner join  ParTypes p on DefDRParam.Type=p.ID 
inner join  FillType f on DefDRParam.Fill=f.ID 
order by  [ID]

            ";
       
        public const string FormulasQuery =
            @"
select Formulas.OI as [Тип результата],Formulas.Result as [id результата],
cast(Formulas.Txt as varchar(max)) as [Формула текст], 
cast(Formulas.Frml as varchar(max)) as [Формула расш]
, Formulas.AllowManual as [рв]		   
,Formulasr.OI as [Тип операнда],Formulasr.Sid as [id операнда], replace(OICat.Abbr+cast(formulasr.SID as varchar),' ','') AS [Operand lit],
Formulasr.TShift as [сдвиг времени] 	  
from  formulasr INNER JOIN Formulas ON FormulasR.FID = Formulas.ID INNER JOIN OICat ON formulasr.OI = OICat.Letter 		
WHERE (Formulas.OutOfWork = 1) 
order by Formulas.ID
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
                        oiCollect.Add(new OIck07()
                        {
                            Id = (string)reader[1] + (int)reader[0],
                            Name = (string)reader[2],
                            Category= (string)reader[4]
                        });
                    }
                }
                return oiCollect;
            }
        }
        
    }
}
