using PX.Common;
using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Objects.AR;
using PX.Objects.SO;
using System;
using System.Collections;

namespace AcuPower.VirtualDevcon
{
    [PXHidden]
    public class SOOrderBlueprint : SOOrder
    {
        [PXDBIdentity(IsKey = true)]
        public int? ID { get; set; }

        public override string OrderType { get; set; }
        public override string OrderNbr { get; set; }
    }

    public class SOOrderMassProcess : PXGraph<SOOrderMassProcess>
    {
        public PXProcessing<SOOrderBlueprint> OrderList;

        protected IEnumerable orderList()
        {
            return OrderList.Cache.Inserted;
        }

        public SOOrderMassProcess()
        {
            // Set up the processing delegate with parallel processing options
            OrderList.SetProcessDelegate<SOOrderEntry>(ProcessOrder);
            OrderList.ParallelProcessingOptions = settings =>
            {
                settings.IsEnabled = true;
                settings.BatchSize = 50;
            };
        }

        public void PopulateOrderList()
        {
            // Populate the processing list with 10,000 orders
            if (OrderList.Cache.Cached.Count() == 0)
            {
                for (int i = 0; i < 10000; i++)
                {
                    var order = new SOOrderBlueprint
                    {
                        OrderType = "SO",
                        CustomerID = GetDefaultCustomerID()
                    };
                    OrderList.Cache.Insert(order);
                }
            }
        }

        public static void ProcessOrder(SOOrderEntry graph, SOOrderBlueprint order)
        {
            graph.Clear();
            // Insert the order and set required fields
            SOOrder newOrder = graph.Document.Insert(new()
            {
                OrderType = order.OrderType,
            });

            newOrder.CustomerID = order.CustomerID;
            graph.Document.UpdateCurrent();
            // Optionally, add order lines or additional details here

            SOLine soLine = graph.Transactions.Insert();
            soLine.InventoryID = 692; // Replace with a valid InventoryID
            soLine.OrderQty = 2;
            graph.Transactions.Update(soLine);

            soLine = graph.Transactions.Insert();
            soLine.InventoryID = 692; // Replace with a valid InventoryID
            soLine.OrderQty = 2;
            graph.Transactions.Update(soLine);

            graph.Actions.PressSave();
        }

        private int? GetDefaultCustomerID()
        {
            // Replace with logic to retrieve a valid CustomerID from your demo data
            Customer customer =
                SelectFrom<Customer>.View.Select(this).TopFirst;
            return customer?.BAccountID;
        }
    }

    public class SOOrderEntryExtension : PXGraphExtension<SOOrderEntry>
    {
        public static bool IsActive() => true;

        public PXAction<SOOrder> generateOrders;
        [PXButton]
        [PXUIField(DisplayName = "Generate Orders New Way")]
        protected virtual IEnumerable GenerateOrders(PXAdapter adapter)
        {
            AcuPower.VirtualDevCon2024.Log.Write($"Started {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
            PXLongOperation.StartOperation(Base, delegate
            {
                SOOrderMassProcess processGraph = PXGraph.CreateInstance<SOOrderMassProcess>();

                // Populate the processing list
                processGraph.PopulateOrderList();

                // Enable processing and start it
                processGraph.OrderList.SetProcessAllEnabled(true);
                processGraph.Actions["ProcessAll"].Press();
                AcuPower.VirtualDevCon2024.Log.Write($"Finished {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
            });

            PXLongOperation.WaitCompletion(Base.UID);
            AcuPower.VirtualDevCon2024.Log.Write($"Finished {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");

            return adapter.Get();
        }
    }
}