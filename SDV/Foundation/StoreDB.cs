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
        public const string AllOIQuery =
             @"
SELECT AllTI.ID as [ID]
, allti.name as [Наименование ОИ]
, 'I' as [Тип ОИ]
,TICat.Name as [категория]
FROM AllTI inner join  ParTypes p on AllTI.Type=p.ID
left join (SELECT RCV.ID, Тип, RTU,[Addr_Id],inv FROM(
      SELECT 'I'  as 'OI', DefTI.ID as ID,' ТМ' as Тип,RTU.Name as RTU,addr as 'Addr_Id',inv FROM DefTI INNER JOIN RTU ON DefTI.RTUID = RTU.ID
union SELECT 'S', DefTS.ID,' ТМ' as Тип, 	RTU.Name,mask as 'Addr_Id', inv FROM DefTS INNER JOIN RTU ON DefTS.RTUID = RTU.ID
union SELECT p.OI, p.IDOI, ' Набор ТМ', 	RTU.Name,isnull(mask,addr) as 'Addr_Id'	, inv FROM dtParam2 AS p INNER JOIN dtSet AS s ON p.SetID = s.ID INNER JOIN RTU ON s.RTUID = RTU.ID WHERE (s.Enable = 1) AND (s.Trans = 1)
union select p.OI ,p.IDOI, ' Набор ММО', 	RTU.Name,ExtIDOI, iif(ScaleFactor<0,1,0) as inv from dtParam3 p,dtSet s INNER JOIN RTU ON s.RTUID = RTU.ID where p.SetID=s.ID and s.Enable=1 and s.trans=1
) AS RCV WHERE (RCV.OI = 'I')) AS RCV_I on AllTI.ID=RCV_I.ID
inner join TICat on allti.Category=TICat.ID 
 LEFT OUTER  JOIN  ( select  Frml,Result,txt from TIFormulas
 left join (select distinct OI,FID from TIFormulasR) as TIFormulasR on TIFormulas.ID=TIFormulasR.FID where Ftype=0)
   as frm oN frm.Result = AllTI.ID where AllTI.OutOfWork <>0 

union
SELECT DefTS.ID as [ID]
,defts.name as [Наименование ОИ]
,'S' as [Тип ОИ]
,TSCat.Name as [категория]
FROM   DefTS Inner join  TSCat c on defts.Category=c.ID 
Inner join  TSType t on defts.TSType=t.ID 
left join (SELECT RCV.ID, Тип, RTU,[Addr_Id],[inv] FROM(
      SELECT 'I'  as 'OI', DefTI.ID as ID,' ТМ' as Тип,RTU.Name as RTU,addr as 'Addr_Id',inv FROM DefTI INNER JOIN RTU ON DefTI.RTUID = RTU.ID
union SELECT 'S' , DefTS.ID,' ТМ' as Тип, 	RTU.Name,mask as 'Addr_Id', inv FROM DefTS INNER JOIN RTU ON DefTS.RTUID = RTU.ID
union SELECT p.OI, p.IDOI, ' Набор ТМ', 	RTU.Name,isnull(mask,addr) as 'Addr_Id'	, inv FROM dtParam2 AS p INNER JOIN dtSet AS s ON p.SetID = s.ID INNER JOIN RTU ON s.RTUID = RTU.ID WHERE (s.Enable = 1) AND (s.Trans = 1)
union select p.OI ,p.IDOI, ' Набор ММО', 	RTU.Name,ExtIDOI, iif(ScaleFactor<0,1,0) as inv from dtParam3 p,dtSet s INNER JOIN RTU ON s.RTUID = RTU.ID where p.SetID=s.ID and s.Enable=1 and s.trans=1
) AS RCV WHERE (RCV.OI = 'S')) AS RCV_S on defts.ID=RCV_S.ID
 Inner join TSCat on defts.Category=TSCat.ID
 LEFT OUTER  JOIN  ( select  Frml,Result,txt,TSFormulasR.oi from TSFormulas
 left join (select distinct OI,FID from TSFormulasR) as TSFormulasR on TSFormulas.ID=TSFormulasR.FID)
   as frm oN frm.Result = defts.ID where defts.OutOfWork <>0

union
SELECT DefSParam.ID as [ID]
,DefSParam.name as [Наименование ОИ]
,'C' as [Тип ОИ]
,'Специальные параметры вещественные' as [категория]
FROM   DefSParam INNER JOIN  EnObj ON DefSParam.EObject = EnObj.ID
Inner join  EnObjType et on EnObj.Type=et.ID
inner join  ParTypes p on DefSParam.Type=p.ID 
 where DefSParam.OutOfWork <>0 

union
SELECT DefDRParam.ID as [ID]
,DefDRParam.name as [Наименование ОИ]
,'H' as [Тип ОИ]
,'СВ-2 (усредненная)' as [категория]
FROM   DefDRParam INNER JOIN  EnObj ON DefDRParam.EObject = EnObj.ID
Inner join  EnObjType et on EnObj.Type=et.ID
inner join  ParTypes p on DefDRParam.Type=p.ID 

union
SELECT DefSIntParam.ID as [ID]
,DefSIntParam.name as [Наименование ОИ]
,'M' as [Тип ОИ]
,'Специальные параметры целочисленные' as [категория]
FROM   DefSIntParam INNER JOIN  EnObj ON DefSIntParam.EObject = EnObj.ID
Inner join  EnObjType et on EnObj.Type=et.ID
inner join  ParTypes p on DefSIntParam.Type=p.ID 
 where DefSIntParam.OutOfWork <>0 

 union
 SELECT DefPlan.ID as [ID]
,DefPlan.name as [Наименование ОИ]
,'P' as [Тип ОИ]
,'Планы' as [категория]
FROM   DefPlan INNER JOIN  EnObj ON DefPlan.EObject = EnObj.ID
Inner join  EnObjType et on EnObj.Type=et.ID
inner join  ParTypes p on DefPlan.Type=p.ID 
 
 union
SELECT DefDayParam.ID as [ID]
,DefDayParam.name as [Наименование ОИ]
,'U' as [Тип ОИ]
,'Ежедневная информация' as [категория]
FROM   DefDayParam INNER JOIN  EnObj ON DefDayParam.EObject = EnObj.ID
Inner join  EnObjType et on EnObj.Type=et.ID
inner join  ParTypes p on DefDayParam.Type=p.ID 
 
 union
SELECT DEFDRPARAM.ID as [ID]
,DEFDRPARAM.name as [Наименование ОИ]
,'W' as [Тип ОИ]
,'СВ-1 (мгновенная,СДВ)' as [категория]
FROM   DEFDRPARAM INNER JOIN  EnObj ON DEFDRPARAM.EObject = EnObj.ID
Inner join  EnObjType et on EnObj.Type=et.ID
inner join  ParTypes p on DEFDRPARAM.Type=p.ID 
 
 union
SELECT vDefUShour1.ID as [ID]
,vDefUShour1.name as [Наименование ОИ]
,'Л' as [Тип ОИ]
,'Универсальные хранилища 1 час' as [категория]
FROM   vDefUShour1 INNER JOIN  EnObj ON vDefUShour1.EObject = EnObj.ID
Inner join  EnObjType et on EnObj.Type=et.ID
inner join  ParTypes p on vDefUShour1.Type=p.ID 


 union
SELECT vDefUSmin30.ID as [ID]
,vDefUSmin30.name as [Наименование ОИ]
,'К' as [Тип ОИ]
,'Универсальные хранилища 30 мин' as [категория]
FROM   vDefUSmin30 INNER JOIN  EnObj ON vDefUSmin30.EObject = EnObj.ID
Inner join  EnObjType et on EnObj.Type=et.ID
inner join  ParTypes p on vDefUSmin30.Type=p.ID 

 union
SELECT DefInteg.ID as [ID]
,DefInteg.name as [Наименование ОИ]
,'J' as [Тип ОИ]
,'Интегралы и средние' as [категория]
FROM   DefInteg INNER JOIN  EnObj ON DefInteg.EObject = EnObj.ID
Inner join  EnObjType et on EnObj.Type=et.ID
inner join  ParTypes p on DefInteg.Type=p.ID 


union
SELECT vDefUSmin1.ID as [ID]
,vDefUSmin1.name as [Наименование ОИ]
,'Б' as [Тип ОИ]
,'Универсальные хранилища 1 мин' as [категория]
FROM   vDefUSmin1 INNER JOIN  EnObj ON vDefUSmin1.EObject = EnObj.ID
Inner join  EnObjType et on EnObj.Type=et.ID
inner join  ParTypes p on vDefUSmin1.Type=p.ID 

union
SELECT vDefUSmin5.ID as [ID]
,vDefUSmin5.name as [Наименование ОИ]
,'Г' as [Тип ОИ]
,'Универсальные хранилища 5 мин' as [категория]
FROM   vDefUSmin5 INNER JOIN  EnObj ON vDefUSmin5.EObject = EnObj.ID
Inner join  EnObjType et on EnObj.Type=et.ID
inner join  ParTypes p on vDefUSmin5.Type=p.ID

union
SELECT vDefUSmin10.ID as [ID]
,vDefUSmin10.name as [Наименование ОИ]
,'З' as [Тип ОИ]
,'Универсальные хранилища 10 мин' as [категория]
FROM   vDefUSmin10 INNER JOIN  EnObj ON vDefUSmin10.EObject = EnObj.ID
Inner join  EnObjType et on EnObj.Type=et.ID
inner join  ParTypes p on vDefUSmin10.Type=p.ID

union
SELECT vDefUSmin15.ID as [ID]
,vDefUSmin15.name as [Наименование ОИ]
,'И' as [Тип ОИ]
,'Универсальные хранилища 15 мин' as [категория]
FROM   vDefUSmin15 INNER JOIN  EnObj ON vDefUSmin15.EObject = EnObj.ID
Inner join  EnObjType et on EnObj.Type=et.ID
inner join  ParTypes p on vDefUSmin15.Type=p.ID

union
SELECT vDefUSday1.ID as [ID]
,vDefUSday1.name as [Наименование ОИ]
,'П' as [Тип ОИ]
,'Универсальные хранилища 1 день' as [категория]
FROM   vDefUSday1 INNER JOIN  EnObj ON vDefUSday1.EObject = EnObj.ID
Inner join  EnObjType et on EnObj.Type=et.ID
inner join  ParTypes p on vDefUSday1.Type=p.ID

union
SELECT vDefUSmonth1.ID as [ID]
,vDefUSmonth1.name as [Наименование ОИ]
,'У' as [Тип ОИ]
,'Универсальные хранилища 1 месяц' as [категория]
FROM   vDefUSmonth1 INNER JOIN  EnObj ON vDefUSmonth1.EObject = EnObj.ID
Inner join  EnObjType et on EnObj.Type=et.ID
inner join  ParTypes p on vDefUSmonth1.Type=p.ID

union
SELECT vDefUSmin3.ID as [ID]
,vDefUSmin3.name as [Наименование ОИ]
,'Ф' as [Тип ОИ]
,'Универсальные хранилища 3 мин' as [категория]
FROM   vDefUSmin3 INNER JOIN  EnObj ON vDefUSmin3.EObject = EnObj.ID
Inner join  EnObjType et on EnObj.Type=et.ID
inner join  ParTypes p on vDefUSmin3.Type=p.ID

order by [Тип ОИ],  ID

            ";
        public const string AllOI07Query =
            @"


declare @MAIN_DC varchar(max)
set @MAIN_DC='РДУ Татарстана' 

--INSERT INTO LSA_test..DCReception (DCreceiver, AdressNaborReceiver,  EnergyObject, TypeOI,TypeOTI,NameOtiReceiver,NumbOtiReceiver,DCsource)  

SELECT  @MAIN_DC as [ДЦ-получатель] 
,isnull(RCV_I.[Addr_Id],'') as [Адрес в наборе ДЦ-ДЦ]
, et.Abbr+' '+EnObj.Name as [Объект]
, 'I' as [Категория]
,p.Abbr as [Тип ТИ]
,iif(AllTI.OutOfWork =0,'!_ВЫВЕДЕН_! ','')+allti.name  as [Наименование ТИ в ДЦ-получателе]
,AllTI.ID as [Номер ОИ в ДЦ получателе]
,RCV_I.RTU as [ДЦ-источник]
,RCV_I.DtSetId
FROM          AllTI INNER JOIN  EnObj ON AllTI.EObject = EnObj.ID 
inner join  TICat c on allti.Category=c.ID 
LEFT OUTER JOIN EnObj AS EO_Host ON EO_Host.ID = EnObj.Higher 
inner join  ParTypes p on allti.Type=p.ID
Inner join  EnObjType et on EnObj.Type=et.ID
inner join (SELECT RCV.ID, Тип, RTU,[Addr_Id],inv,DtSetId FROM(
	  select s.ID as DtSetId,p.OI ,p.IDOI as id, ' Набор ТМ' as Тип, RTU.Name as RTU,cast(isnull(mask,addr) as varchar) as 'Addr_Id',inv
		from dtParam2 p,dtSet s INNER JOIN RTU ON s.RTUID = RTU.ID where p.SetID=s.ID and s.Enable=1 and s.trans=2
		union
		--select p.OI  as 'OI',p.IDOI as ID, ' Набор ICCP' as Тип, RTU.Name as RTU,cast(i.RemoteDomain+':'+cast(s.SetID as varchar)+':'+cast(Identifier as varchar) as sql_variant) as 'Addr_Id', inv
		 select s.ID as DtSetId, p.OI  as 'OI',p.IDOI as ID, ' Набор ICCP' as Тип, RTU.Name as RTU,cast(i.RemoteDomain+'::'+cast(Identifier as varchar) as sql_variant) as 'Addr_Id', inv
	  --select p.OI  as 'OI',p.IDOI as ID, ' Набор ICCP' as Тип, RTU.Name as RTU,cast(iif(Scope=1,'::',i.RemoteDomain+'::')+cast(Identifier as varchar) as sql_variant) as 'Addr_Id', inv 
	  from  dtParam6 p INNER JOIN  dtSet s on p.SetID=s.ID  INNER JOIN  RTU ON s.RTUID = RTU.ID INNER JOIN  RTU_ICCP I ON s.RTUID = I.RTUID 
	  where s.Enable=1 and s.trans=1 and rtu.Name not like '%ICCPAVT%'
) AS RCV WHERE (RCV.OI = 'I')) AS RCV_I on AllTI.ID=RCV_I.ID

union

SELECT @MAIN_DC as [ДЦ-получатель] 
,isnull(RCV_S.[Addr_Id],'') as [Адрес в наборе ДЦ-ДЦ]
,et.Abbr+' '+EnObj.Name as [Объект] 
,'S' as [Категория]
, t.Abbr as [Тип]
,iif(defts.OutOfWork =0,'!_ВЫВЕДЕН_! ','')+defts.name  as [Наименование ТC в ДЦ-получателе] 
,defts.ID as [Номер ОИ в ДЦ получателе]
,RCV_S.RTU as [ДЦ-источник] 
,RCV_S.DtSetId
FROM         defts INNER JOIN EnObj ON defts.EObject = EnObj.ID Inner join oik..TSCat c on defts.Category=c.ID Inner join oik..TSType t on defts.TSType=t.ID Inner join oik..EnObjType et on EnObj.Type=et.ID
inner join (SELECT RCV.ID, Тип, RTU,[Addr_Id],[inv],DtSetId FROM(
	  select  s.ID as DtSetId,p.OI ,p.IDOI as id, ' Набор ТМ' as Тип, RTU.Name as RTU,cast(isnull(mask,addr) as varchar) as 'Addr_Id',inv
		from dtParam2 p,dtSet s INNER JOIN RTU ON s.RTUID = RTU.ID where p.SetID=s.ID and s.Enable=1 and s.trans=2
		union
		--select p.OI  as 'OI',p.IDOI as ID, ' Набор ICCP' as Тип, RTU.Name as RTU,cast(i.RemoteDomain+':'+cast(s.SetID as varchar)+':'+cast(Identifier as varchar) as sql_variant) as 'Addr_Id', inv
		select  s.ID as DtSetId,p.OI  as 'OI',p.IDOI as ID, ' Набор ICCP' as Тип, RTU.Name as RTU,cast(i.RemoteDomain+'::'+cast(Identifier as varchar) as sql_variant) as 'Addr_Id', inv
	  --select p.OI  as 'OI',p.IDOI as ID, ' Набор ICCP' as Тип, RTU.Name as RTU,cast(iif(Scope=1,'::',i.RemoteDomain+'::')+cast(Identifier as varchar) as sql_variant) as 'Addr_Id', inv 
	  --select p.OI  as 'OI',p.IDOI as ID, ' Набор ICCP' as Тип, RTU.Name as RTU,cast(Identifier as varchar) as 'Addr_Id', inv 
	  from  dtParam6 p INNER JOIN  dtSet s on p.SetID=s.ID INNER JOIN  RTU ON s.RTUID = RTU.ID INNER JOIN  RTU_ICCP I ON s.RTUID = I.RTUID 
	  where s.Enable=1 and s.trans=1 and rtu.Name not like '%ICCPAVT%'
) AS RCV WHERE (RCV.OI = 'S')) AS RCV_S on defts.ID=RCV_S.ID

UNION

SELECT @MAIN_DC as [ДЦ-получатель]
,RCV_S.[Addr_Id] as [Адрес в наборе ДЦ-ДЦ]
,et.Abbr+' '+EnObj.Name  as [Объект] 
,'J' as [Тип ОИ]------!!!!!
,p.Abbr as [Тип]
,iif(DefSParam.OutOfWork =0,'!_ВЫВЕДЕН_! ','')+DefSParam.name as [Наименование ТС в ДЦ-получателе]
,DefSParam.ID as [Номер ТС в ДЦ источнике] 
,RCV_S.RTU as [ДЦ-источник]
,RCV_S.DtSetId
FROM   DefSParam INNER JOIN  EnObj ON DefSParam.EObject = EnObj.ID 
--Inner join  TSCat c on DefSParam.Category=c.ID
inner join  ParTypes p on DefSParam.Type=p.ID
Inner join  EnObjType et on EnObj.Type=et.ID
inner join (SELECT SND.ID, Тип, RTU,[Addr_Id],[inv],DtSetId FROM(
	  select  s.ID as DtSetId,p.OI ,p.IDOI as id, ' Набор ТМ' as Тип, RTU.Name as RTU,cast(isnull(mask,addr) as sql_variant) as 'Addr_Id',inv from  dtParam2 p, dtSet s INNER JOIN  RTU ON s.RTUID = RTU.ID where p.SetID=s.ID and s.Enable=1 and s.trans=1
union select  s.ID as DtSetId,p.OI  as 'OI',p.IDOI as ID, ' Набор ICCP' as Тип, RTU.Name as RTU,cast(i.RemoteDomain+':'+cast(s.SetID as varchar)+':'+cast(Identifier as varchar) as sql_variant) as 'Addr_Id', inv
--select p.OI  as 'OI',p.IDOI as ID, ' Набор ICCP' as Тип, RTU.Name as RTU,cast(i.RemoteDomain+'::'+cast(Identifier as varchar) as sql_variant) as 'Addr_Id', inv
--select p.OI  as 'OI',p.IDOI as ID, ' Набор ICCP' as Тип, RTU.Name as RTU,cast(/*iif(Scope=1,'::',i.RemoteDomain+'::')+*/cast(Identifier as varchar) as sql_variant) as 'Addr_Id', inv 
	  from  dtParam6 p INNER JOIN  dtSet s on p.SetID=s.ID INNER JOIN  RTU ON s.RTUID = RTU.ID INNER JOIN  RTU_ICCP I ON s.RTUID = I.RTUID where 
	  s.Enable=1 and s.trans=1 and rtu.Name not like '%ICCPAVT%'
) AS SND WHERE (snd.OI = 'J')) AS RCV_S on DefSParam.ID=RCV_S.ID------!!!!!

union

SELECT @MAIN_DC as [ДЦ-получатель]
,RCV_S.[Addr_Id] as [Адрес в наборе ДЦ-ДЦ]
,et.Abbr+' '+EnObj.Name  as [Объект] 
,'W' as [Тип ОИ]------!!!!!
,p.Abbr as [Тип]
,DEFDRPARAM.name as [Наименование ТС в ДЦ-получателе]
,DEFDRPARAM.ID as [Номер ТС в ДЦ источнике] 
,RCV_S.RTU as [ДЦ-источник]
,RCV_S.DtSetId
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

SELECT @MAIN_DC as [ДЦ-получатель]
,RCV_S.[Addr_Id] as [Адрес в наборе ДЦ-ДЦ]
,et.Abbr+' '+EnObj.Name  as [Объект] 
,'P' as [Тип ОИ]------!!!!!
,p.Abbr as [Тип]
,DefPlan.name as [Наименование ТС в ДЦ-получателе]
,DefPlan.ID as [Номер ТС в ДЦ источнике] 
,RCV_S.RTU as [ДЦ-источник]
,RCV_S.DtSetId
FROM   DefPlan INNER JOIN  EnObj ON DefPlan.EObject = EnObj.ID 
--Inner join  TSCat c on DefPlan.Category=c.ID
inner join  ParTypes p on DefPlan.Type=p.ID
Inner join  EnObjType et on EnObj.Type=et.ID
inner join (SELECT SND.ID, Тип, RTU,[Addr_Id],[inv],DtSetId FROM(
	  select  s.ID as DtSetId,p.OI ,p.IDOI as id, ' Набор ТМ' as Тип, RTU.Name as RTU,cast(isnull(mask,addr) as sql_variant) as 'Addr_Id',inv from  dtParam2 p, dtSet s INNER JOIN  RTU ON s.RTUID = RTU.ID where p.SetID=s.ID and s.Enable=1 and s.trans=1
union select  s.ID as DtSetId,p.OI  as 'OI',p.IDOI as ID, ' Набор ICCP' as Тип, RTU.Name as RTU,cast(i.RemoteDomain+':'+cast(s.SetID as varchar)+':'+cast(Identifier as varchar) as sql_variant) as 'Addr_Id', inv
--select p.OI  as 'OI',p.IDOI as ID, ' Набор ICCP' as Тип, RTU.Name as RTU,cast(i.RemoteDomain+'::'+cast(Identifier as varchar) as sql_variant) as 'Addr_Id', inv
--select p.OI  as 'OI',p.IDOI as ID, ' Набор ICCP' as Тип, RTU.Name as RTU,cast(/*iif(Scope=1,'::',i.RemoteDomain+'::')+*/cast(Identifier as varchar) as sql_variant) as 'Addr_Id', inv 
	  from  dtParam6 p INNER JOIN  dtSet s on p.SetID=s.ID INNER JOIN  RTU ON s.RTUID = RTU.ID INNER JOIN  RTU_ICCP I ON s.RTUID = I.RTUID where 
	  s.Enable=1 and s.trans=1 and rtu.Name not like '%ICCPAVT%'
) AS SND WHERE (snd.OI = 'P')) AS RCV_S on DefPlan.ID=RCV_S.ID------!!!!!

union

SELECT @MAIN_DC as [ДЦ-получатель]
,RCV_S.[Addr_Id] as [Адрес в наборе ДЦ-ДЦ]
,et.Abbr+' '+EnObj.Name  as [Объект] 
,'U' as [Тип ОИ]------!!!!!
,p.Abbr as [Тип]
,DefDayParam.name as [Наименование ТС в ДЦ-получателе]
,DefDayParam.ID as [Номер ТС в ДЦ источнике] 
,RCV_S.RTU as [ДЦ-источник]
,RCV_S.DtSetId
FROM   DefDayParam INNER JOIN  EnObj ON DefDayParam.EObject = EnObj.ID 
--Inner join  TSCat c on DefDayParam.Category=c.ID
inner join  ParTypes p on DefDayParam.Type=p.ID
Inner join  EnObjType et on EnObj.Type=et.ID
inner join (SELECT SND.ID, Тип, RTU,[Addr_Id],[inv],DtSetId FROM(
	  select  s.ID as DtSetId,p.OI ,p.IDOI as id, ' Набор ТМ' as Тип, RTU.Name as RTU,cast(isnull(mask,addr) as sql_variant) as 'Addr_Id',inv from  dtParam2 p, dtSet s INNER JOIN  RTU ON s.RTUID = RTU.ID where p.SetID=s.ID and s.Enable=1 and s.trans=1
union select  s.ID as DtSetId,p.OI  as 'OI',p.IDOI as ID, ' Набор ICCP' as Тип, RTU.Name as RTU,cast(i.RemoteDomain+':'+cast(s.SetID as varchar)+':'+cast(Identifier as varchar) as sql_variant) as 'Addr_Id', inv
--select p.OI  as 'OI',p.IDOI as ID, ' Набор ICCP' as Тип, RTU.Name as RTU,cast(i.RemoteDomain+'::'+cast(Identifier as varchar) as sql_variant) as 'Addr_Id', inv
--select p.OI  as 'OI',p.IDOI as ID, ' Набор ICCP' as Тип, RTU.Name as RTU,cast(/*iif(Scope=1,'::',i.RemoteDomain+'::')+*/cast(Identifier as varchar) as sql_variant) as 'Addr_Id', inv 
	  from  dtParam6 p INNER JOIN  dtSet s on p.SetID=s.ID INNER JOIN  RTU ON s.RTUID = RTU.ID INNER JOIN  RTU_ICCP I ON s.RTUID = I.RTUID where 
	  s.Enable=1 and s.trans=1 and rtu.Name not like '%ICCPAVT%'
) AS SND WHERE (snd.OI = 'U')) AS RCV_S on DefDayParam.ID=RCV_S.ID------!!!!!

UNION

SELECT @MAIN_DC as [ДЦ-получатель]
,RCV_S.[Addr_Id] as [Адрес в наборе ДЦ-ДЦ]
,et.Abbr+' '+EnObj.Name  as [Объект] 
,'C' as [Тип ОИ]------!!!!!
,p.Abbr as [Тип]
,iif(DefSParam.OutOfWork =0,'!_ВЫВЕДЕН_! ','')+DefSParam.name as [Наименование ТС в ДЦ-получателе]
,DefSParam.ID as [Номер ТС в ДЦ источнике] 
,RCV_S.RTU as [ДЦ-источник]
,RCV_S.DtSetId
FROM   DefSParam INNER JOIN  EnObj ON DefSParam.EObject = EnObj.ID 
--Inner join  TSCat c on DefSParam.Category=c.ID
inner join  ParTypes p on DefSParam.Type=p.ID
Inner join  EnObjType et on EnObj.Type=et.ID
inner join (SELECT SND.ID, Тип, RTU,[Addr_Id],[inv],DtSetId FROM(
	  select  s.ID as DtSetId,p.OI ,p.IDOI as id, ' Набор ТМ' as Тип, RTU.Name as RTU,cast(isnull(mask,addr) as sql_variant) as 'Addr_Id',inv from  dtParam2 p, dtSet s INNER JOIN  RTU ON s.RTUID = RTU.ID where p.SetID=s.ID and s.Enable=1 and s.trans=1
union select  s.ID as DtSetId,p.OI  as 'OI',p.IDOI as ID, ' Набор ICCP' as Тип, RTU.Name as RTU,cast(i.RemoteDomain+':'+cast(s.SetID as varchar)+':'+cast(Identifier as varchar) as sql_variant) as 'Addr_Id', inv
--select p.OI  as 'OI',p.IDOI as ID, ' Набор ICCP' as Тип, RTU.Name as RTU,cast(i.RemoteDomain+'::'+cast(Identifier as varchar) as sql_variant) as 'Addr_Id', inv
--select p.OI  as 'OI',p.IDOI as ID, ' Набор ICCP' as Тип, RTU.Name as RTU,cast(/*iif(Scope=1,'::',i.RemoteDomain+'::')+*/cast(Identifier as varchar) as sql_variant) as 'Addr_Id', inv 
	  from  dtParam6 p INNER JOIN  dtSet s on p.SetID=s.ID INNER JOIN  RTU ON s.RTUID = RTU.ID INNER JOIN  RTU_ICCP I ON s.RTUID = I.RTUID where 
	  s.Enable=1 and s.trans=1 and rtu.Name not like '%ICCPAVT%'
) AS SND WHERE (snd.OI = 'C')) AS RCV_S on DefSParam.ID=RCV_S.ID------!!!!!


union
SELECT @MAIN_DC as [ДЦ-получатель]
,RCV_S.[Addr_Id] as [Адрес в наборе ДЦ-ДЦ]
,et.Abbr+' '+EnObj.Name  as [Объект] 
,'H' as [Тип ОИ]------!!!!!
,p.Abbr as [Тип]
,DefDRParam.name as [Наименование ТС в ДЦ-получателе]
,DefDRParam.ID as [Номер ТС в ДЦ источнике] 
,RCV_S.RTU as [ДЦ-источник]
,RCV_S.DtSetId
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

union

SELECT @MAIN_DC as [ДЦ-получатель]
,RCV_S.[Addr_Id] as [Адрес в наборе ДЦ-ДЦ]
,et.Abbr+' '+EnObj.Name  as [Объект] 
,'M' as [Тип ОИ]------!!!!!
,p.Abbr as [Тип]
,iif(DefSIntParam.OutOfWork =0,'!_ВЫВЕДЕН_! ','')+DefSIntParam.name as [Наименование ТС в ДЦ-получателе]
,DefSIntParam.ID as [Номер ТС в ДЦ источнике] 
,RCV_S.RTU as [ДЦ-источник]
,RCV_S.DtSetId
FROM   DefSIntParam INNER JOIN  EnObj ON DefSIntParam.EObject = EnObj.ID 
--Inner join  TSCat c on DefSIntParam.Category=c.ID
inner join  ParTypes p on DefSIntParam.Type=p.ID
Inner join  EnObjType et on EnObj.Type=et.ID
inner join (SELECT SND.ID, Тип, RTU,[Addr_Id],[inv],DtSetId FROM(
	  select  s.ID as DtSetId,p.OI ,p.IDOI as id, ' Набор ТМ' as Тип, RTU.Name as RTU,cast(isnull(mask,addr) as sql_variant) as 'Addr_Id',inv from  dtParam2 p, dtSet s INNER JOIN  RTU ON s.RTUID = RTU.ID where p.SetID=s.ID and s.Enable=1 and s.trans=1
union select  s.ID as DtSetId,p.OI  as 'OI',p.IDOI as ID, ' Набор ICCP' as Тип, RTU.Name as RTU,cast(i.RemoteDomain+':'+cast(s.SetID as varchar)+':'+cast(Identifier as varchar) as sql_variant) as 'Addr_Id', inv
--select p.OI  as 'OI',p.IDOI as ID, ' Набор ICCP' as Тип, RTU.Name as RTU,cast(i.RemoteDomain+'::'+cast(Identifier as varchar) as sql_variant) as 'Addr_Id', inv
--select p.OI  as 'OI',p.IDOI as ID, ' Набор ICCP' as Тип, RTU.Name as RTU,cast(/*iif(Scope=1,'::',i.RemoteDomain+'::')+*/cast(Identifier as varchar) as sql_variant) as 'Addr_Id', inv 
	  from  dtParam6 p INNER JOIN  dtSet s on p.SetID=s.ID INNER JOIN  RTU ON s.RTUID = RTU.ID INNER JOIN  RTU_ICCP I ON s.RTUID = I.RTUID where 
	  s.Enable=1 and s.trans=1 and rtu.Name not like '%ICCPAVT%'
) AS SND WHERE (snd.OI = 'M')) AS RCV_S on DefSIntParam.ID=RCV_S.ID------!!!!!

union

SELECT @MAIN_DC as [ДЦ-получатель]
,RCV_S.[Addr_Id] as [Адрес в наборе ДЦ-ДЦ]
,et.Abbr+' '+EnObj.Name  as [Объект] 
,'Б' as [Тип ОИ]------!!!!!
,p.Abbr as [Тип]
,vDefUShour1.name as [Наименование ТС в ДЦ-получателе]
,vDefUShour1.ID as [Номер ТС в ДЦ источнике] 
,RCV_S.RTU as [ДЦ-источник]
,RCV_S.DtSetId
FROM   vDefUShour1 INNER JOIN  EnObj ON vDefUShour1.EObject = EnObj.ID 
--Inner join  TSCat c on vDefUShour1.Category=c.ID
inner join  ParTypes p on vDefUShour1.Type=p.ID
Inner join  EnObjType et on EnObj.Type=et.ID
inner join (SELECT SND.ID, Тип, RTU,[Addr_Id],[inv],DtSetId FROM(
	  select  s.ID as DtSetId,p.OI ,p.IDOI as id, ' Набор ТМ' as Тип, RTU.Name as RTU,cast(isnull(mask,addr) as sql_variant) as 'Addr_Id',inv from  dtParam2 p, dtSet s INNER JOIN  RTU ON s.RTUID = RTU.ID where p.SetID=s.ID and s.Enable=1 and s.trans=1
union select  s.ID as DtSetId,p.OI  as 'OI',p.IDOI as ID, ' Набор ICCP' as Тип, RTU.Name as RTU,cast(i.RemoteDomain+':'+cast(s.SetID as varchar)+':'+cast(Identifier as varchar) as sql_variant) as 'Addr_Id', inv
--select p.OI  as 'OI',p.IDOI as ID, ' Набор ICCP' as Тип, RTU.Name as RTU,cast(i.RemoteDomain+'::'+cast(Identifier as varchar) as sql_variant) as 'Addr_Id', inv
--select p.OI  as 'OI',p.IDOI as ID, ' Набор ICCP' as Тип, RTU.Name as RTU,cast(/*iif(Scope=1,'::',i.RemoteDomain+'::')+*/cast(Identifier as varchar) as sql_variant) as 'Addr_Id', inv 
	  from  dtParam6 p INNER JOIN  dtSet s on p.SetID=s.ID INNER JOIN  RTU ON s.RTUID = RTU.ID INNER JOIN  RTU_ICCP I ON s.RTUID = I.RTUID where 
	  s.Enable=1 and s.trans=1 and rtu.Name not like '%ICCPAVT%'
) AS SND WHERE (snd.OI = 'Б')) AS RCV_S on vDefUShour1.ID=RCV_S.ID------!!!!!

union

SELECT @MAIN_DC as [ДЦ-получатель]
,RCV_S.[Addr_Id] as [Адрес в наборе ДЦ-ДЦ]
,et.Abbr+' '+EnObj.Name  as [Объект] 
,'Г' as [Тип ОИ]------!!!!!
,p.Abbr as [Тип]
,vDefUShour1.name as [Наименование ТС в ДЦ-получателе]
,vDefUShour1.ID as [Номер ТС в ДЦ источнике] 
,RCV_S.RTU as [ДЦ-источник]
,RCV_S.DtSetId
FROM   vDefUShour1 INNER JOIN  EnObj ON vDefUShour1.EObject = EnObj.ID 
--Inner join  TSCat c on vDefUShour1.Category=c.ID
inner join  ParTypes p on vDefUShour1.Type=p.ID
Inner join  EnObjType et on EnObj.Type=et.ID
inner join (SELECT SND.ID, Тип, RTU,[Addr_Id],[inv],DtSetId FROM(
	  select  s.ID as DtSetId,p.OI ,p.IDOI as id, ' Набор ТМ' as Тип, RTU.Name as RTU,cast(isnull(mask,addr) as sql_variant) as 'Addr_Id',inv from  dtParam2 p, dtSet s INNER JOIN  RTU ON s.RTUID = RTU.ID where p.SetID=s.ID and s.Enable=1 and s.trans=1
union select  s.ID as DtSetId,p.OI  as 'OI',p.IDOI as ID, ' Набор ICCP' as Тип, RTU.Name as RTU,cast(i.RemoteDomain+':'+cast(s.SetID as varchar)+':'+cast(Identifier as varchar) as sql_variant) as 'Addr_Id', inv
--select p.OI  as 'OI',p.IDOI as ID, ' Набор ICCP' as Тип, RTU.Name as RTU,cast(i.RemoteDomain+'::'+cast(Identifier as varchar) as sql_variant) as 'Addr_Id', inv
--select p.OI  as 'OI',p.IDOI as ID, ' Набор ICCP' as Тип, RTU.Name as RTU,cast(/*iif(Scope=1,'::',i.RemoteDomain+'::')+*/cast(Identifier as varchar) as sql_variant) as 'Addr_Id', inv 
	  from  dtParam6 p INNER JOIN  dtSet s on p.SetID=s.ID INNER JOIN  RTU ON s.RTUID = RTU.ID INNER JOIN  RTU_ICCP I ON s.RTUID = I.RTUID where 
	  s.Enable=1 and s.trans=1 and rtu.Name not like '%ICCPAVT%'
) AS SND WHERE (snd.OI = 'Г')) AS RCV_S on vDefUShour1.ID=RCV_S.ID------!!!!!

union

SELECT @MAIN_DC as [ДЦ-получатель]
,RCV_S.[Addr_Id] as [Адрес в наборе ДЦ-ДЦ]
,et.Abbr+' '+EnObj.Name  as [Объект] 
,'З' as [Тип ОИ]------!!!!!
,p.Abbr as [Тип]
,vDefUShour1.name as [Наименование ТС в ДЦ-получателе]
,vDefUShour1.ID as [Номер ТС в ДЦ источнике] 
,RCV_S.RTU as [ДЦ-источник]
,RCV_S.DtSetId
FROM   vDefUShour1 INNER JOIN  EnObj ON vDefUShour1.EObject = EnObj.ID 
--Inner join  TSCat c on vDefUShour1.Category=c.ID
inner join  ParTypes p on vDefUShour1.Type=p.ID
Inner join  EnObjType et on EnObj.Type=et.ID
inner join (SELECT SND.ID, Тип, RTU,[Addr_Id],[inv],DtSetId FROM(
	  select  s.ID as DtSetId,p.OI ,p.IDOI as id, ' Набор ТМ' as Тип, RTU.Name as RTU,cast(isnull(mask,addr) as sql_variant) as 'Addr_Id',inv from  dtParam2 p, dtSet s INNER JOIN  RTU ON s.RTUID = RTU.ID where p.SetID=s.ID and s.Enable=1 and s.trans=1
union select  s.ID as DtSetId,p.OI  as 'OI',p.IDOI as ID, ' Набор ICCP' as Тип, RTU.Name as RTU,cast(i.RemoteDomain+':'+cast(s.SetID as varchar)+':'+cast(Identifier as varchar) as sql_variant) as 'Addr_Id', inv
--select p.OI  as 'OI',p.IDOI as ID, ' Набор ICCP' as Тип, RTU.Name as RTU,cast(i.RemoteDomain+'::'+cast(Identifier as varchar) as sql_variant) as 'Addr_Id', inv
--select p.OI  as 'OI',p.IDOI as ID, ' Набор ICCP' as Тип, RTU.Name as RTU,cast(/*iif(Scope=1,'::',i.RemoteDomain+'::')+*/cast(Identifier as varchar) as sql_variant) as 'Addr_Id', inv 
	  from  dtParam6 p INNER JOIN  dtSet s on p.SetID=s.ID INNER JOIN  RTU ON s.RTUID = RTU.ID INNER JOIN  RTU_ICCP I ON s.RTUID = I.RTUID where 
	  s.Enable=1 and s.trans=1 and rtu.Name not like '%ICCPAVT%'
) AS SND WHERE (snd.OI = 'З')) AS RCV_S on vDefUShour1.ID=RCV_S.ID------!!!!!

union

SELECT @MAIN_DC as [ДЦ-получатель]
,RCV_S.[Addr_Id] as [Адрес в наборе ДЦ-ДЦ]
,et.Abbr+' '+EnObj.Name  as [Объект] 
,'И' as [Тип ОИ]------!!!!!
,p.Abbr as [Тип]
,vDefUShour1.name as [Наименование ТС в ДЦ-получателе]
,vDefUShour1.ID as [Номер ТС в ДЦ источнике] 
,RCV_S.RTU as [ДЦ-источник]
,RCV_S.DtSetId
FROM   vDefUShour1 INNER JOIN  EnObj ON vDefUShour1.EObject = EnObj.ID 
--Inner join  TSCat c on vDefUShour1.Category=c.ID
inner join  ParTypes p on vDefUShour1.Type=p.ID
Inner join  EnObjType et on EnObj.Type=et.ID
inner join (SELECT SND.ID, Тип, RTU,[Addr_Id],[inv],DtSetId FROM(
	  select  s.ID as DtSetId,p.OI ,p.IDOI as id, ' Набор ТМ' as Тип, RTU.Name as RTU,cast(isnull(mask,addr) as sql_variant) as 'Addr_Id',inv from  dtParam2 p, dtSet s INNER JOIN  RTU ON s.RTUID = RTU.ID where p.SetID=s.ID and s.Enable=1 and s.trans=1
union select  s.ID as DtSetId,p.OI  as 'OI',p.IDOI as ID, ' Набор ICCP' as Тип, RTU.Name as RTU,cast(i.RemoteDomain+':'+cast(s.SetID as varchar)+':'+cast(Identifier as varchar) as sql_variant) as 'Addr_Id', inv
--select p.OI  as 'OI',p.IDOI as ID, ' Набор ICCP' as Тип, RTU.Name as RTU,cast(i.RemoteDomain+'::'+cast(Identifier as varchar) as sql_variant) as 'Addr_Id', inv
--select p.OI  as 'OI',p.IDOI as ID, ' Набор ICCP' as Тип, RTU.Name as RTU,cast(/*iif(Scope=1,'::',i.RemoteDomain+'::')+*/cast(Identifier as varchar) as sql_variant) as 'Addr_Id', inv 
	  from  dtParam6 p INNER JOIN  dtSet s on p.SetID=s.ID INNER JOIN  RTU ON s.RTUID = RTU.ID INNER JOIN  RTU_ICCP I ON s.RTUID = I.RTUID where 
	  s.Enable=1 and s.trans=1 and rtu.Name not like '%ICCPAVT%'
) AS SND WHERE (snd.OI = 'И')) AS RCV_S on vDefUShour1.ID=RCV_S.ID------!!!!!

union

SELECT @MAIN_DC as [ДЦ-получатель]
,RCV_S.[Addr_Id] as [Адрес в наборе ДЦ-ДЦ]
,et.Abbr+' '+EnObj.Name  as [Объект] 
,'К' as [Тип ОИ]------!!!!!
,p.Abbr as [Тип]
,vDefUShour1.name as [Наименование ТС в ДЦ-получателе]
,vDefUShour1.ID as [Номер ТС в ДЦ источнике] 
,RCV_S.RTU as [ДЦ-источник]
,RCV_S.DtSetId
FROM   vDefUShour1 INNER JOIN  EnObj ON vDefUShour1.EObject = EnObj.ID 
--Inner join  TSCat c on vDefUShour1.Category=c.ID
inner join  ParTypes p on vDefUShour1.Type=p.ID
Inner join  EnObjType et on EnObj.Type=et.ID
inner join (SELECT SND.ID, Тип, RTU,[Addr_Id],[inv],DtSetId FROM(
	  select  s.ID as DtSetId,p.OI ,p.IDOI as id, ' Набор ТМ' as Тип, RTU.Name as RTU,cast(isnull(mask,addr) as sql_variant) as 'Addr_Id',inv from  dtParam2 p, dtSet s INNER JOIN  RTU ON s.RTUID = RTU.ID where p.SetID=s.ID and s.Enable=1 and s.trans=1
union select  s.ID as DtSetId,p.OI  as 'OI',p.IDOI as ID, ' Набор ICCP' as Тип, RTU.Name as RTU,cast(i.RemoteDomain+':'+cast(s.SetID as varchar)+':'+cast(Identifier as varchar) as sql_variant) as 'Addr_Id', inv
--select p.OI  as 'OI',p.IDOI as ID, ' Набор ICCP' as Тип, RTU.Name as RTU,cast(i.RemoteDomain+'::'+cast(Identifier as varchar) as sql_variant) as 'Addr_Id', inv
--select p.OI  as 'OI',p.IDOI as ID, ' Набор ICCP' as Тип, RTU.Name as RTU,cast(/*iif(Scope=1,'::',i.RemoteDomain+'::')+*/cast(Identifier as varchar) as sql_variant) as 'Addr_Id', inv 
	  from  dtParam6 p INNER JOIN  dtSet s on p.SetID=s.ID INNER JOIN  RTU ON s.RTUID = RTU.ID INNER JOIN  RTU_ICCP I ON s.RTUID = I.RTUID where 
	  s.Enable=1 and s.trans=1 and rtu.Name not like '%ICCPAVT%'
) AS SND WHERE (snd.OI = 'К')) AS RCV_S on vDefUShour1.ID=RCV_S.ID------!!!!!

union

SELECT @MAIN_DC as [ДЦ-получатель]
,RCV_S.[Addr_Id] as [Адрес в наборе ДЦ-ДЦ]
,et.Abbr+' '+EnObj.Name  as [Объект] 
,'Л' as [Тип ОИ]------!!!!!
,p.Abbr as [Тип]
,vDefUShour1.name as [Наименование ТС в ДЦ-получателе]
,vDefUShour1.ID as [Номер ТС в ДЦ источнике] 
,RCV_S.RTU as [ДЦ-источник]
,RCV_S.DtSetId
FROM   vDefUShour1 INNER JOIN  EnObj ON vDefUShour1.EObject = EnObj.ID 
--Inner join  TSCat c on vDefUShour1.Category=c.ID
inner join  ParTypes p on vDefUShour1.Type=p.ID
Inner join  EnObjType et on EnObj.Type=et.ID
inner join (SELECT SND.ID, Тип, RTU,[Addr_Id],[inv],DtSetId FROM(
	  select  s.ID as DtSetId,p.OI ,p.IDOI as id, ' Набор ТМ' as Тип, RTU.Name as RTU,cast(isnull(mask,addr) as sql_variant) as 'Addr_Id',inv from  dtParam2 p, dtSet s INNER JOIN  RTU ON s.RTUID = RTU.ID where p.SetID=s.ID and s.Enable=1 and s.trans=1
union select  s.ID as DtSetId,p.OI  as 'OI',p.IDOI as ID, ' Набор ICCP' as Тип, RTU.Name as RTU,cast(i.RemoteDomain+':'+cast(s.SetID as varchar)+':'+cast(Identifier as varchar) as sql_variant) as 'Addr_Id', inv
--select p.OI  as 'OI',p.IDOI as ID, ' Набор ICCP' as Тип, RTU.Name as RTU,cast(i.RemoteDomain+'::'+cast(Identifier as varchar) as sql_variant) as 'Addr_Id', inv
--select p.OI  as 'OI',p.IDOI as ID, ' Набор ICCP' as Тип, RTU.Name as RTU,cast(/*iif(Scope=1,'::',i.RemoteDomain+'::')+*/cast(Identifier as varchar) as sql_variant) as 'Addr_Id', inv 
	  from  dtParam6 p INNER JOIN  dtSet s on p.SetID=s.ID INNER JOIN  RTU ON s.RTUID = RTU.ID INNER JOIN  RTU_ICCP I ON s.RTUID = I.RTUID where 
	  s.Enable=1 and s.trans=1 and rtu.Name not like '%ICCPAVT%'
) AS SND WHERE (snd.OI = 'Л')) AS RCV_S on vDefUShour1.ID=RCV_S.ID------!!!!!

union

SELECT @MAIN_DC as [ДЦ-получатель]
,RCV_S.[Addr_Id] as [Адрес в наборе ДЦ-ДЦ]
,et.Abbr+' '+EnObj.Name  as [Объект] 
,'П' as [Тип ОИ]------!!!!!
,p.Abbr as [Тип]
,vDefUShour1.name as [Наименование ТС в ДЦ-получателе]
,vDefUShour1.ID as [Номер ТС в ДЦ источнике] 
,RCV_S.RTU as [ДЦ-источник]
,RCV_S.DtSetId
FROM   vDefUShour1 INNER JOIN  EnObj ON vDefUShour1.EObject = EnObj.ID 
--Inner join  TSCat c on vDefUShour1.Category=c.ID
inner join  ParTypes p on vDefUShour1.Type=p.ID
Inner join  EnObjType et on EnObj.Type=et.ID
inner join (SELECT SND.ID, Тип, RTU,[Addr_Id],[inv],DtSetId FROM(
	  select  s.ID as DtSetId,p.OI ,p.IDOI as id, ' Набор ТМ' as Тип, RTU.Name as RTU,cast(isnull(mask,addr) as sql_variant) as 'Addr_Id',inv from  dtParam2 p, dtSet s INNER JOIN  RTU ON s.RTUID = RTU.ID where p.SetID=s.ID and s.Enable=1 and s.trans=1
union select  s.ID as DtSetId,p.OI  as 'OI',p.IDOI as ID, ' Набор ICCP' as Тип, RTU.Name as RTU,cast(i.RemoteDomain+':'+cast(s.SetID as varchar)+':'+cast(Identifier as varchar) as sql_variant) as 'Addr_Id', inv
--select p.OI  as 'OI',p.IDOI as ID, ' Набор ICCP' as Тип, RTU.Name as RTU,cast(i.RemoteDomain+'::'+cast(Identifier as varchar) as sql_variant) as 'Addr_Id', inv
--select p.OI  as 'OI',p.IDOI as ID, ' Набор ICCP' as Тип, RTU.Name as RTU,cast(/*iif(Scope=1,'::',i.RemoteDomain+'::')+*/cast(Identifier as varchar) as sql_variant) as 'Addr_Id', inv 
	  from  dtParam6 p INNER JOIN  dtSet s on p.SetID=s.ID INNER JOIN  RTU ON s.RTUID = RTU.ID INNER JOIN  RTU_ICCP I ON s.RTUID = I.RTUID where 
	  s.Enable=1 and s.trans=1 and rtu.Name not like '%ICCPAVT%'
) AS SND WHERE (snd.OI = 'П')) AS RCV_S on vDefUShour1.ID=RCV_S.ID------!!!!!

union

SELECT @MAIN_DC as [ДЦ-получатель]
,RCV_S.[Addr_Id] as [Адрес в наборе ДЦ-ДЦ]
,et.Abbr+' '+EnObj.Name  as [Объект] 
,'У' as [Тип ОИ]------!!!!!
,p.Abbr as [Тип]
,vDefUShour1.name as [Наименование ТС в ДЦ-получателе]
,vDefUShour1.ID as [Номер ТС в ДЦ источнике] 
,RCV_S.RTU as [ДЦ-источник]
,RCV_S.DtSetId
FROM   vDefUShour1 INNER JOIN  EnObj ON vDefUShour1.EObject = EnObj.ID 
--Inner join  TSCat c on vDefUShour1.Category=c.ID
inner join  ParTypes p on vDefUShour1.Type=p.ID
Inner join  EnObjType et on EnObj.Type=et.ID
inner join (SELECT SND.ID, Тип, RTU,[Addr_Id],[inv],DtSetId FROM(
	  select  s.ID as DtSetId,p.OI ,p.IDOI as id, ' Набор ТМ' as Тип, RTU.Name as RTU,cast(isnull(mask,addr) as sql_variant) as 'Addr_Id',inv from  dtParam2 p, dtSet s INNER JOIN  RTU ON s.RTUID = RTU.ID where p.SetID=s.ID and s.Enable=1 and s.trans=1
union select  s.ID as DtSetId,p.OI  as 'OI',p.IDOI as ID, ' Набор ICCP' as Тип, RTU.Name as RTU,cast(i.RemoteDomain+':'+cast(s.SetID as varchar)+':'+cast(Identifier as varchar) as sql_variant) as 'Addr_Id', inv
--select p.OI  as 'OI',p.IDOI as ID, ' Набор ICCP' as Тип, RTU.Name as RTU,cast(i.RemoteDomain+'::'+cast(Identifier as varchar) as sql_variant) as 'Addr_Id', inv
--select p.OI  as 'OI',p.IDOI as ID, ' Набор ICCP' as Тип, RTU.Name as RTU,cast(/*iif(Scope=1,'::',i.RemoteDomain+'::')+*/cast(Identifier as varchar) as sql_variant) as 'Addr_Id', inv 
	  from  dtParam6 p INNER JOIN  dtSet s on p.SetID=s.ID INNER JOIN  RTU ON s.RTUID = RTU.ID INNER JOIN  RTU_ICCP I ON s.RTUID = I.RTUID where 
	  s.Enable=1 and s.trans=1 and rtu.Name not like '%ICCPAVT%'
) AS SND WHERE (snd.OI = 'У')) AS RCV_S on vDefUShour1.ID=RCV_S.ID------!!!!!

union

SELECT @MAIN_DC as [ДЦ-получатель]
,RCV_S.[Addr_Id] as [Адрес в наборе ДЦ-ДЦ]
,et.Abbr+' '+EnObj.Name  as [Объект] 
,'Ф' as [Тип ОИ]------!!!!!
,p.Abbr as [Тип]
,vDefUShour1.name as [Наименование ТС в ДЦ-получателе]
,vDefUShour1.ID as [Номер ТС в ДЦ источнике] 
,RCV_S.RTU as [ДЦ-источник]
,RCV_S.DtSetId
FROM   vDefUShour1 INNER JOIN  EnObj ON vDefUShour1.EObject = EnObj.ID 
--Inner join  TSCat c on vDefUShour1.Category=c.ID
inner join  ParTypes p on vDefUShour1.Type=p.ID
Inner join  EnObjType et on EnObj.Type=et.ID
inner join (SELECT SND.ID, Тип, RTU,[Addr_Id],[inv],DtSetId FROM(
	  select  s.ID as DtSetId,p.OI ,p.IDOI as id, ' Набор ТМ' as Тип, RTU.Name as RTU,cast(isnull(mask,addr) as sql_variant) as 'Addr_Id',inv from  dtParam2 p, dtSet s INNER JOIN  RTU ON s.RTUID = RTU.ID where p.SetID=s.ID and s.Enable=1 and s.trans=1
union select  s.ID as DtSetId,p.OI  as 'OI',p.IDOI as ID, ' Набор ICCP' as Тип, RTU.Name as RTU,cast(i.RemoteDomain+':'+cast(s.SetID as varchar)+':'+cast(Identifier as varchar) as sql_variant) as 'Addr_Id', inv
--select p.OI  as 'OI',p.IDOI as ID, ' Набор ICCP' as Тип, RTU.Name as RTU,cast(i.RemoteDomain+'::'+cast(Identifier as varchar) as sql_variant) as 'Addr_Id', inv
--select p.OI  as 'OI',p.IDOI as ID, ' Набор ICCP' as Тип, RTU.Name as RTU,cast(/*iif(Scope=1,'::',i.RemoteDomain+'::')+*/cast(Identifier as varchar) as sql_variant) as 'Addr_Id', inv 
	  from  dtParam6 p INNER JOIN  dtSet s on p.SetID=s.ID INNER JOIN  RTU ON s.RTUID = RTU.ID INNER JOIN  RTU_ICCP I ON s.RTUID = I.RTUID where 
	  s.Enable=1 and s.trans=1 and rtu.Name not like '%ICCPAVT%'
) AS SND WHERE (snd.OI = 'Ф')) AS RCV_S on vDefUShour1.ID=RCV_S.ID------!!!!!

order by  [Номер ОИ в ДЦ получателе],[Категория], [Адрес в наборе ДЦ-ДЦ]

";
        #endregion

        public ObservableCollection<OIck07> GetAllOI07()
        {
            var query = AllOI07Query;
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
                            Id = (string)reader[3] + (int)reader[6],
                            Name = (string)reader[5],
                            Type = (string)reader[7],
                        });
                    }
                }
                return oiCollect;
            }
        }
        public ObservableCollection<OIck07> GetAllOI()
        {
            var query = AllOIQuery;
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
                            Id = (string)reader[2] + (int)reader[0],
                            Name = (string)reader[1],
                            Type = (string)reader[3],

                        });
                    }
                }
                return oiCollect;
            }
        }
    }
}
