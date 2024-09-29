using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.AR;
using PX.Objects.SO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace AcuPower.VirtualDevCon2024
{
    public class SOOrderEntryExt : PXGraphExtension<SOOrderEntry>
    {
        public static bool IsActive() => true;

        public PXAction<SOOrder> CreateOrdersParallelPregenerated;
        public PXAction<SOOrder> CreateOrdersParallel;


        [PXButton]
        [PXUIField(DisplayName = "Create Orders (Parallel, Pregenerated)")]
        protected IEnumerable createOrdersParallelPregenerated(PXAdapter adapter)
        {
            AcuPower.VirtualDevCon2024.Log.Write($"Started {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
            PXLongOperation.StartOperation(Base, () =>
            {
                var process = PXGraph.CreateInstance<CreateOrdersParallelPreparedProcess>();
                process.PopulateOrderList(10000);
                process.RunProcess();
            });
            PXLongOperation.WaitCompletion(Base.UID);
            AcuPower.VirtualDevCon2024.Log.Write($"Finished {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
            return adapter.Get();
        }

        [PXButton]
        [PXUIField(DisplayName = "Create Orders (Parallel, Dynamically generated)")]
        protected IEnumerable createOrdersParallel(PXAdapter adapter)
        {
            AcuPower.VirtualDevCon2024.Log.Write($"Started {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
            PXLongOperation.StartOperation(Base, () =>
            {
                var process = CreateOrdersParallelGeneratedProcess.CreateInstance(10000, 50);
                process.RunProcess();
            });
            PXLongOperation.WaitCompletion(Base.UID);
            AcuPower.VirtualDevCon2024.Log.Write($"Finished {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
            return adapter.Get();
        }
    }

    [PXHidden]
    public class SOOrderBlueprint : SOOrder
    {
        [PXDBIdentity(IsKey = true)]
        public int? ID { get; set; }

        public override string OrderType { get; set; }
        public override string OrderNbr { get; set; }

        public List<SOLine> Lines { get; set; }
    }

    public class CreateOrdersParallelPreparedProcess : PXGraph<CreateOrdersParallelPreparedProcess>
    {
        public PXProcessing<SOOrderBlueprint> Orders;

        public void RunProcess()
        {
            Actions["ProcessAll"].Press();
        }

        protected IEnumerable orders()
        {
            return Orders.Cache.Inserted;
        }

        public CreateOrdersParallelPreparedProcess()
        {
            // Set up the processing delegate with parallel processing options
            Orders.SetProcessDelegate<SOOrderEntry>(CreateOrder);
            Orders.ParallelProcessingOptions = settings =>
            {
                settings.IsEnabled = true;
                settings.BatchSize = 50;
            };
        }

        public void PopulateOrderList(int count)
        {
            PXCache cache = Orders.Cache;
            if (cache.Inserted.Any_())
            {
                cache.Clear();
            }

            for (int i = 0; i < count; i++)
            {
                var order = new SOOrderBlueprint
                {
                    OrderType = "SO",
                    CustomerID = GetDefaultCustomerID()
                };

                order.Lines = new List<SOLine>();
                for (int l = 0; l < 2; l++)
                {
                    order.Lines.Add(new SOLine
                    {
                        InventoryID = 692,
                        Qty = 2,
                    });
                }

                cache.Insert(order);
            }
        }

        public static void CreateOrder(SOOrderEntry graph, SOOrderBlueprint blueprint)
        {
            graph.Clear();

            SOOrder order = graph.Document.Insert(
                new SOOrder
                {
                    OrderType = blueprint.OrderType
                });

            order.CustomerID = blueprint.CustomerID;
            graph.Document.Update(order);

            order.OrderDesc = $"Created by thread {Thread.CurrentThread.ManagedThreadId}";

            blueprint.Lines ??= new List<SOLine>
            {
                new SOLine()
                {
                    InventoryID = 692,
                    OrderQty = 2
                },
                new SOLine()
                {
                    InventoryID = 692,
                    OrderQty = 2
                }
            };

            if (blueprint.Lines?.Count > 0)
            {
                foreach (var lineBlueprint in blueprint.Lines)
                {
                    SOLine soline = graph.Transactions.Insert();

                    soline.InventoryID = lineBlueprint.InventoryID;
                    soline.OrderQty = lineBlueprint.OrderQty;
                    soline.UOM = "EA"; // have to hard code it for the sake of demo
                    soline.SalesAcctID = 1193; // one more for the sake of demo
                    soline.SalesSubID = 496;


                    SOLine orderLine = graph.Transactions.Update(soline);
                    if (orderLine == null)
                    {
                        PXTrace.WriteError("something wrong");
                    }

                }
            }

            graph.Actions.PressSave();
            Log.Write($"Order {order.OrderNbr} created");
        }

        private int? GetDefaultCustomerID()
        {
            // Replace with logic to retrieve a valid CustomerID from your demo data
            Customer customer =
                SelectFrom<Customer>.View.Select(this).TopFirst;
            return customer?.BAccountID;
        }
    }

    [PXHidden]
    public class EmptyTarget : PXBqlTable, IBqlTable
    {
        #region Selected
        [PXBool]
        public bool? Selected => true;
        public abstract class selected : BqlBool.Field<selected> { }
        #endregion
    }

    public class EmptyProcessing : PXProcessing<EmptyTarget>
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
    public class ProcessInfo : PXBqlTable, IBqlTable
    {
        public int? RecordsCount;
        public int? Processes;
    }

    public class CreateOrdersParallelGeneratedProcess : PXGraph<CreateOrdersParallelGeneratedProcess>
    {
        public PXFilter<ProcessInfo> ProcessInfo;
        public EmptyProcessing Processing;

        public CreateOrdersParallelGeneratedProcess()
        {
            Init();
        }

        public static CreateOrdersParallelGeneratedProcess CreateInstance(int recordsCount, int processes)
        {
            var graph = CreateInstance<CreateOrdersParallelGeneratedProcess>();
            var info = graph.ProcessInfo.Current;
            info.RecordsCount = recordsCount;
            info.Processes = processes;

            graph.Init();

            return graph;
        }

        private void Init()
        {
            var info = ProcessInfo.Current;
            if (info.Processes > 1)
            {
                Processing.RegisterParallelProcesses(info.Processes.Value);
            }

            Processing.SetProcessDelegate((SOOrderEntry graph, EmptyTarget _) =>
            {
                CreateOrdersBatch(graph, info);
            });
        }

        public void RunProcess()
        {
            Actions["ProcessAll"].Press();
        }

        private static void CreateOrdersBatch(SOOrderEntry graph, ProcessInfo processInfo)
        {
            Log.Write("Process Started");

            for (int i = 0; i < processInfo.RecordsCount / processInfo.Processes; i++)
            {
                CreateOrdersParallelPreparedProcess.CreateOrder(
                     graph,
                     new SOOrderBlueprint
                     {
                         OrderType = "SO",
                         CustomerID = GetDefaultCustomerID(graph)
                     });
            }

            Log.Write("Process finished");
        }

        private static int? GetDefaultCustomerID(PXGraph graph)
        {
            Customer customer = SelectFrom<Customer>.View.Select(graph).TopFirst;
            return customer?.BAccountID;
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

        public static void Write(string info)
        {
            PXDatabase.Insert<Log>(new PXDataFieldAssign<text>(
                $"Thread: {Thread.CurrentThread.ManagedThreadId}. {info}"));
        }
    }
}
