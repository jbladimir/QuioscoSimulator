using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using MoneyRecolectorControl.MoneyRecolector;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MoneyRecolectorControl
{
    class Program
    {


        static async Task Main(string[] args)
        {
            MoneyRecolectorController recolectorController = MoneyRecolectorController.Instance;

            //MoneyRecolectorSimulator simulator = new MoneyRecolectorSimulator();

            IVDC_Recolector simulator = new IVDC_Recolector();

            recolectorController.RegisterDevice(simulator);

            
            var url = "https://localhost:5001";

            var canal = GrpcChannel.ForAddress(url);

            var cliente = new QuioscoServer.QuiscoService.QuiscoServiceClient(canal);


            CancellationTokenSource tokenSource = new CancellationTokenSource();
 

            var countResponse = cliente.ReceiveMoney( new Empty() , cancellationToken: tokenSource.Token);


            try
            {
                await foreach( var item in countResponse.ResponseStream.ReadAllAsync(tokenSource.Token))
                {
                    Console.WriteLine($"Reciving {item.Count} From Server");

                    if ( item.Count == 1)
                    {
                        recolectorController.StartMoneyRecolection(50);
                    }

                    else if ( item.Count == 2)
                    {
                        recolectorController.SendMoneyToVault();
                       
                    }
                    else if (item.Count == 3)
                    {
                        recolectorController.ReturnMoneyToClient();

                    }

                    else if (item.Count == 4)
                    {
                        recolectorController.GetUsedCapacity();

                    }
                    if ( item.Count < 0)
                    {
                        tokenSource.Cancel();
                    }

                }


            }catch( RpcException ex)
            {
                Console.WriteLine($"RpcError Error: {ex.Message}");
            }


            do
            {
                Thread.Sleep(TimeSpan.FromHours(1));
            } while (true);
        }
    }
}
