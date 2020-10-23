using Grpc.Net.Client;
using System;

namespace SignalRecolectorFacade
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            Console.ReadLine();
            Console.WriteLine("Hello World!");

            var url = "https://localhost:5001";

            var canal = GrpcChannel.ForAddress(url);

            var cliente = new QuioscoServer.QuiscoService.QuiscoServiceClient(canal);

            var resultado = cliente.InformStatus(new QuioscoServer.StatusRequest { Status = QuioscoServer.StatusRequest.Types.Status.Ready });

            Console.ReadLine();

            Console.WriteLine("Hello World!");
        }
    }
}
