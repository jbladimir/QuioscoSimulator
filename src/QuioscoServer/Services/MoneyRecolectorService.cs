using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using grpc = global::Grpc.Core;

namespace QuioscoServer
{
    public class MoneyRecolectorService : QuiscoService.QuiscoServiceBase
    {




        public override Task<StatusResponse> InformStatus(StatusRequest request, ServerCallContext context)
        {
            Console.WriteLine("Reciving:" + request.Status);
            return Task.FromResult(new StatusResponse { Recived = true });
            //throw new RpcException(new Status(StatusCode.Unimplemented, ""));
        }


        public override async Task ReceiveMoney(Empty request, IServerStreamWriter<ClientCount> responseStream, ServerCallContext context)
        {
            int count = 0;
            while (!context.CancellationToken.IsCancellationRequested)
            {
                Console.WriteLine("count to send: ");
                count = Convert.ToInt32(Console.ReadLine());
                Console.WriteLine("Sending " + count + " to responseClient");
                await responseStream.WriteAsync(new ClientCount() { Count = count });
                count++;
            }

            Console.WriteLine("Context IsCancellationRequested");

        }


        public override Task<Empty> SetUserAmount(MoneyReceive request, ServerCallContext context)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Reciving: device: {request.DeviceId}  type: {request.Type}  count: {request.Count}  total: {request.Total}");
            Console.ResetColor();

            return Task.FromResult(new Empty());
        }

    }
}
