using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace MoneyRecolectorControl.MoneyRecolector
{
    public class MoneyRecolectorController
    {
        private QuioscoServer.QuiscoService.QuiscoServiceClient client;

        private static MoneyRecolectorController instance = null;

        private MoneyRecolectorController()
        {
            var url = "https://localhost:5001";

            var channel = GrpcChannel.ForAddress(url);

            client = new QuioscoServer.QuiscoService.QuiscoServiceClient(channel);
        }

        public static MoneyRecolectorController Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new MoneyRecolectorController();
                }
                return instance;
            }
        }

        Dictionary<String, AbstracMoneyRecolector> recolectors = new Dictionary<string, AbstracMoneyRecolector>();

        public void RegisterDevice(AbstracMoneyRecolector _recolector )
        {

            String id = _recolector.RegisterDevice(this.RecolectorSignalcallback, this.RecolectorSignalErrorcallback);

            recolectors.Add(id, _recolector);

        }

        public void StartMoneyRecolection(double maxRecolectorMoney)
        {
            foreach( var r in recolectors)
            {
                if ( r.Value.IsOpen() )
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Money Recolector { r.Value.Id } already open");
                    Console.ResetColor();
                }
                else
                {
                    r.Value.StartMoneyRecolection(maxRecolectorMoney);
                }
                
            }
        }

        public void SendMoneyToVault()
        {
            foreach (var r in recolectors)
            {
                if (!r.Value.IsOpen())
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Money Recolector { r.Value.Id } not open");
                    Console.ResetColor();
                }
                else
                {
                    r.Value.StopMoneyRecolection();
                    r.Value.SendMoneyToVault();
                }

            }
        }

        public void ReturnMoneyToClient()
        {
            foreach (var r in recolectors)
            {
                if (!r.Value.IsOpen())
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Money Recolector { r.Value.Id } not open");
                    Console.ResetColor();
                }
                else
                {
                    r.Value.StopMoneyRecolection();
                    r.Value.ReturnMoneyToClient();
                }

            }
        }

        public List<MoneyContainer> GetUsedCapacity()
        {
            List<MoneyContainer> returnValue = new List<MoneyContainer>();
            foreach (var r in recolectors)
            {
                var usedCapacity = r.Value.GetUsedCapacity();
                Console.WriteLine($"Device {r.Value.Id}");
                foreach ( var c in usedCapacity)
                {
                    Console.WriteLine("\ttype: "+ c.type + "\t count: " +  c.count.ToString().PadLeft(3,'0')  + " \t capacity: " + c.capacity + "\tused: " + (c.count / c.capacity * 100.0) + "%");
                }

                returnValue.AddRange(usedCapacity);
            }

            return returnValue;
        }

        public void RecolectorSignalcallback(string id, double amount, string type, double total)
        {
            Console.WriteLine($"Devide {id} type: {type} amount {amount}");
            client.SetUserAmountAsync(new QuioscoServer.MoneyReceive { DeviceId=id, Total = total});
        }

        public void RecolectorSignalErrorcallback(string id, string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.BackgroundColor = ConsoleColor.Red;
            Console.WriteLine($"Devide {id} ErrorMessage: {message}");
            Console.ResetColor();

            this.ReturnMoneyToClient();
        }

    }
}
