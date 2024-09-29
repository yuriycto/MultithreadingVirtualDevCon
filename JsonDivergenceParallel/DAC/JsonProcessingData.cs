using PX.Data;
using System;

namespace JsonDivergenceParallel.DAC
{
    [Serializable]
    [PXCacheName("Json Processing Data")]
    public class JsonProcessingData : PXBqlTable, IBqlTable
    {
        #region Selected
        [PXBool]
        [PXUIField(DisplayName = "Selected")]
        public virtual bool? Selected { get; set; }
        public abstract class selected : PX.Data.BQL.BqlBool.Field<selected> { }
        #endregion

        #region LineNbr
        [PXInt(IsKey = true)]
        [PXUIField(DisplayName = "Line Nbr.")]
        public virtual int? LineNbr { get; set; }
        public abstract class lineNbr : PX.Data.BQL.BqlInt.Field<lineNbr> { }
        #endregion

        #region JsonData
        [PXString]
        [PXUIField(DisplayName = "Json Data")]
        public virtual string JsonData { get; set; }
        public abstract class jsonData : PX.Data.BQL.BqlString.Field<jsonData> { }
        #endregion
    }
}
