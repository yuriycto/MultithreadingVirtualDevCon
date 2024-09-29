using PX.Data;
using PX.Objects.PO;
using System;
using System.Reflection;
using System.Threading;

namespace JsonDivergenceParallel
{
    public class POOrderEntryExtension : PXGraphExtension<POOrderEntry>
    {
        // Define the button action
        public PXAction<POOrder> SerializePOOrder;

        [PXButton(CommitChanges = true)]
        [PXUIField(DisplayName = "Serialize PO to JSON (Newtonsoft 10.0.3)", MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
        protected void serializePOOrder()
        {
            // Output storage for the serialized JSON result
            string jsonOutput = null;
            object lockthis = new object();

            try
            {
                // Create a new thread for the operation to isolate the library loading
                Thread thread = new Thread(t =>
                {
                    // Path to the Newtonsoft.Json 10.0.3 DLL
                    string jsonDllPath = @"d:\NewtonSoft\10.0.3\Newtonsoft.Json.dll";

                    // Load the Newtonsoft.Json 10.0.3 assembly
                    Assembly jsonAssembly = Assembly.UnsafeLoadFrom(jsonDllPath);

                    // Get the type of the JsonConvert class
                    Type jsonConvertType = jsonAssembly.GetType("Newtonsoft.Json.JsonConvert");

                    // Get the method info for JsonConvert.SerializeObject
                    MethodInfo serializeMethod = jsonConvertType.GetMethod("SerializeObject", new Type[] { typeof(object) });

                    // Get the current POOrder object from the graph
                    POOrder currentOrder = Base.Document.Current;

                    // Ensure the current order exists
                    if (currentOrder != null)
                    {
                        // Invoke SerializeObject method using reflection to serialize the POOrder object
                        object result = serializeMethod.Invoke(null, new object[] { currentOrder });

                        // Store the result in the calling thread using a lock
                        lock (lockthis)
                        {
                            jsonOutput = result.ToString();
                        }
                    }
                    else
                    {
                        throw new PXException("No current POOrder found to serialize.");
                    }
                });

                // Start and join the thread
                thread.Start();
                thread.Join();
            }
            catch (Exception e)
            {
                // Log any errors that occur during execution
                PXTrace.WriteError(e);
            }

            // Display the output from the thread (the JSON string)
            if (jsonOutput != null)
            {
                PXTrace.WriteInformation($"Serialized JSON for POOrder: {jsonOutput}");
            }
            else
            {
                PXTrace.WriteInformation("No output produced from the serialization.");
            }
        }
    }
}
