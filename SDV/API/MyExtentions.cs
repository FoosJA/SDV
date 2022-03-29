using Newtonsoft.Json.Linq;
using SDV.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDV.API
{
    public static class MyExtentions
	{
        public static List<OIck11> ToMVRT(this Response6 source)
        {
            var respList = source.Value.ToList();
            JObject jObject = JObject.Parse(respList[0].ToString());
            JToken lisTokent = jObject["mVals"];
            List<OIck11> mvList = lisTokent.ToObject<List<OIck11>>();
            return mvList;
        }
    }
}
