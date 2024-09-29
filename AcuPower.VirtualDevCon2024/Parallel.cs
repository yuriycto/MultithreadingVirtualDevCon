using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.AR;
using PX.Objects.SO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace rt
{
    public class EmptyTarget : PXBqlTable, IBqlTable
    {
        #region Selected
        [PXBool]
        public bool? Selected => true;
        public abstract class selected : BqlBool.Field<selected> { }
        #endregion
    }

    public class EmptyProcessing<TFilter> : SelectFrom<EmptyTarget>.ProcessingView.FilteredBy<TFilter> where TFilter : class, IBqlTable, new()
    {
        public EmptyProcessing(PXGraph graph) : base(graph, new PXSelectDelegate(SelectHandler))
        {
            TrySetRefIdGetter(DummyRefIdGetter, "");
        }

        public EmptyProcessing(PXGraph graph, Delegate handler) : base(graph, handler)
        {
            TrySetRefIdGetter(DummyRefIdGetter, "");
        }

        public void RegisterParallelProcesses(int count)
        {
            ParallelProcessingOptions = options =>
            {
                options.AutoBatchSize = false;
                options.BatchSize = 1;
                options.IsEnabled = true;
                options.SplitToBatches = (_, _) => SplitProcess(count);
            };
        }

        public void SetProcessDelegate(Action func)
        {
            SetProcessDelegate((List<EmptyTarget> _) => func());
        }

        protected override List<EmptyTarget> _PendingList(object[] parameters, string[] sorts, bool[] descendings, PXFilterRow[] filters)
        {
            var list = new List<EmptyTarget>(1) { new EmptyTarget() };
            OuterViewCache.SetStatus(list[0], PXEntryStatus.Inserted);
            return list;
        }

        private static Guid? DummyRefIdGetter(PXCache cache, object obj) => Guid.NewGuid();

        private static IEnumerable SelectHandler()
        {
            yield break;
        }

        private static IEnumerable<(int, int)> SplitProcess(int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return (0, 0);
            }
        }
    }

    [PXHidden]
    public class Log : PXBqlTable, IBqlTable
    {
        #region ID
        [PXDBIdentity(IsKey = true)]
        public virtual int? ID { get; set; }
        public abstract class iD : BqlInt.Field<iD> { }
        #endregion

        #region Text
        [PXDBString]
        public virtual string Text { get; set; }
        public abstract class text : BqlString.Field<text> { }
        #endregion
    }

    public class ParallelTest : PXGraph<ParallelTest>
    {
        public PXFilter<DummyTable> Filter;
        public EmptyProcessing<DummyTable> Processing;

        public ParallelTest()
        {
            Processing.RegisterParallelProcesses(5);
            Processing.SetProcessDelegate((ParallelTest graph, EmptyTarget t) => Process());
            //Processing.SetProcessDelegate(Process);
        }

        private static void Process()
        {
            Log("Process Call");

            //var graph = CreateInstance<SOOrderEntry>();
            //for (int i = 0; i < 200; i++)
            //{
            //    ProcessOrder(graph,
            //        new SOOrder
            //        {
            //            OrderType = "SO",
            //            OrderDesc = "Order from thread " + Thread.CurrentThread.ManagedThreadId,
            //            CustomerID = GetDefaultCustomerID(graph)
            //        });
            //    Log("Order created");
            //}

            Log("Process finished");
        }

        private static void Log(string info)
        {
            PXDatabase.Insert<Log>(new PXDataFieldAssign<Log.text>(
                $"Thread: {Thread.CurrentThread.ManagedThreadId}. {info}"));
        }

        private static void ProcessOrder(SOOrderEntry graph, SOOrder order)
        {
            graph.Clear();
            graph.Document.Insert(order);

            SOLine line = graph.Transactions.Insert();
            line.InventoryID = 692;
            line.OrderQty = 1;
            graph.Transactions.Update(line);

            graph.Persist();
        }

        private static int? GetDefaultCustomerID(PXGraph graph)
        {
            Customer customer = SelectFrom<Customer>.View.Select(graph).TopFirst;
            return customer?.BAccountID;
        }
    }
}