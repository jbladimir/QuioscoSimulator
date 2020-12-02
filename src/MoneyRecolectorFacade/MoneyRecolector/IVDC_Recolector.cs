using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MoneyRecolectorControl.MoneyRecolector
{
    public class IVDC_Recolector : AbstracMoneyRecolector
    {

        public CancellationTokenSource cancellationTokenSource;

        public enum IVDC_Comand : byte
        {
            Status = 0x01,
            Incia_cobro = 0x02,
            Pool_evento = 0x03,
            Termina_Cobro = 0x04,
            Obten_conteo = 0x0A,
            Inicia_retiro = 0x11,
            Termina_retiro = 0x13,
        }
        public enum Error_serial
        {
            ERROR_NONE = 0,
            LRC_ERROR = -1,
            Time_Out_TX = -2,
            Time_Out_RX = -3,
            Error_Port = -4,
            Error_com = -5,
            Undefined_error = -6,
        }


        private static byte CALCULA_LRC(byte[] Buffer, int longitud)
        {
            byte result_LRC = 0;
            int i;
            for (i = 0; i < longitud; i++)
            {
                result_LRC ^= Buffer[i];
            }
            return result_LRC;
        }

        private static void construye_msj(byte command, byte[] options, short lenght, ref byte[] Buffer)
        {
            //Construye el mensaje dependiendo del protocolo de comunicacion IVDC
            byte device_number = 2;
            byte LRC = 0;
            byte[] lenght_byte = new byte[2];
            int i;
            lenght_byte = BitConverter.GetBytes(lenght + 5);
            Buffer[0] = device_number;
            Buffer[1] = lenght_byte[1];
            Buffer[2] = lenght_byte[0];
            Buffer[3] = command;
            for (i = 0; i < lenght; i++)
            {
                Buffer[4 + i] = options[i];
            }
            LRC = CALCULA_LRC(Buffer, lenght + 4);
            Buffer[lenght + 4] = LRC;
        }


        private int IVDC_executecomand(byte[] comand, int lenght, ref byte[] response)
        {
            //Envía el comando a la IVDC
            byte[] IVDC_resp = new byte[300];
            int i = 0;
            int result = 0;
            short lenght_recived = 0;
            byte LRC_recept = 0;

            //Verifica el estado del puerto
            if (Puerto_IVDC.IsOpen == false)
            {
                try
                {
                    Puerto_IVDC.Open();                                     //Abre el puerto serie
                }
                catch (System.IO.IOException)
                {
                    result = (int)Error_serial.Error_Port;
                    //TODO cambiar a manejo de errores de la consola a sistema de logs remoto
                    Console.WriteLine("Estado del Cobro:  Error no se pudo abrir el puerto");
                }
            }
            if (result == 0)
            {
                //Envia el comando a la IVDC

                try
                {
                    Puerto_IVDC.Write(comand, 0, lenght);                                 //Escribe en el buffer el comando de salida
                }
                catch (TimeoutException)
                {
                    result = (int)Error_serial.Time_Out_TX;                   //Pon error de time out
                    //TODO cambiar a manejo de errores de la consola a sistema de logs remoto
                    Console.WriteLine("Estado del Cobro: Error al enviar Mensaje");
                }

            }
            //Si todo es correcto Entonces espera respuesta de la IVDC
            if (result == 0)
            {
                try
                {
                    for (i = 0; i < 3; i++)
                    {
                        Puerto_IVDC.Read(IVDC_resp, i, 1);                                 //Espera a leer los primeros 3 bytes de respuesta
                    }
                }
                catch (TimeoutException)
                {
                    result = (int)Error_serial.Time_Out_RX;                   //Pon error de time out
                    //TODO cambiar a manejo de errores de la consola a sistema de logs remoto
                    Console.WriteLine("Estado del Cobro: Error no se Recibio Respuesta");
                }
            }
            //Si todo es correcto Entonces espera el final de la respuesta IVDC
            if (result == 0)
            {
                if (IVDC_resp[0] == 01)                                                      //Verifica que el primer byte sea 01
                {
                    lenght_recived = (short)(IVDC_resp[1] * 256);                           //Calcula los bytes que se recibiran
                    lenght_recived += IVDC_resp[2];
                    try
                    {
                        for (i = 0; i < lenght_recived - 3; i++)
                        {
                            Puerto_IVDC.Read(IVDC_resp, i + 3, 1);                                 //Espera a leer los primeros 3 bytes de respuesta
                        }
                    }
                    catch (TimeoutException)
                    {
                        result = (int)Error_serial.Time_Out_RX;                   //Pon error de time out
                        //TODO cambiar a manejo de errores de la consola a sistema de logs remoto
                        Console.WriteLine("Estado del Cobro: Error no se Recibio Respuesta");
                    }
                }
                else
                {
                    result = (int)Error_serial.Error_com;                          //Pon error de Comunicacion
                    //TODO cambiar a manejo de errores de la consola a sistema de logs remoto
                    Console.WriteLine("Estado del Cobro: Error Respuesta Incorrecta"); 
                }

                //Calcula el LRC
                LRC_recept = CALCULA_LRC(IVDC_resp, lenght_recived - 1);                     //Calcula el LRC recibido
                if (LRC_recept != IVDC_resp[lenght_recived - 1])
                {
                    result = (int)Error_serial.LRC_ERROR;
                }
                else
                {
                    for (i = 0; i < lenght_recived - 4; i++)
                    {
                        response[i] = IVDC_resp[i + 3];
                    }
                    //TODO cambiar a manejo de errores de la consola a sistema de logs remoto
                    Console.WriteLine("Estado del Cobro: Comando Ejecutado");
                }
            }
            return result;                                                          //Regresa el resultado o error
        }




        static public SerialPort Puerto_IVDC = new SerialPort();


        public IVDC_Recolector()
        {
            //Inicializa el puerto serie 
            Puerto_IVDC.BaudRate = 9600;                            //El baudrate a usar es 9600 bauds
            Puerto_IVDC.DataBits = 8;                               //Datos de 8 bits
            Puerto_IVDC.Parity = System.IO.Ports.Parity.None;     //Sin paridad
            Puerto_IVDC.StopBits = StopBits.One;                    //un bit de parada
            Puerto_IVDC.Handshake = Handshake.None;
            Puerto_IVDC.RtsEnable = true;
            Puerto_IVDC.ReadTimeout = 2000;                             //Deja 200 ms de time out de lectura
            Puerto_IVDC.WriteTimeout = 2000;                             //Deja 200 ms de time out de escritura
            Puerto_IVDC.PortName = "COM1";                          //Deja el COM1 como configuracion inicial

            if (Puerto_IVDC.IsOpen == true)                             //Muestra el mensaje de error si el puerto esta abierto
            {
                this.signalErrorcallback(this.Id, "El puerto :" + Puerto_IVDC.PortName + ", No está disponible"); 
                //TODO cambiar a manejo de errores de la consola a sistema de logs remoto
                Console.WriteLine("El puerto :" + Puerto_IVDC.PortName + ", No está disponible");
            }
            else
            {
                try
                {
                    Puerto_IVDC.Open();                                 //Abre el puerto serie
                }
                catch (System.IO.IOException ex)
                {
                    this.signalErrorcallback(this.Id, "El puerto :" + Puerto_IVDC.PortName + ", No está disponible " + ex.Message);
                    //TODO cambiar a manejo de errores de la consola a sistema de logs remoto
                    Console.WriteLine("El puerto :" + Puerto_IVDC.PortName + ", No está disponible " + ex.Message);                      //Muestra el mensaje de error si el puerto no se puede abrir
                }
                catch (System.UnauthorizedAccessException ex)
                {
                    this.signalErrorcallback(this.Id, "El puerto :" + Puerto_IVDC.PortName + ", No está disponible " + ex.Message);
                    //TODO cambiar a manejo de errores de la consola a sistema de logs remoto
                    Console.WriteLine("El puerto :" + Puerto_IVDC.PortName + ", No está disponible " + ex.Message);                           //Muestra el mensaje de error si el puerto no se puede abrir
                }
                catch (System.Exception ex)
                {
                    this.signalErrorcallback(this.Id, "El puerto :" + Puerto_IVDC.PortName + ", No está disponible " + ex.Message);
                    //TODO cambiar a manejo de errores de la consola a sistema de logs remoto
                    Console.WriteLine("El puerto :" + Puerto_IVDC.PortName + ", No está disponible " + ex.Message);                           //Muestra el mensaje de error si el puerto no se puede abrir
                }
            }
        }





        private void ReadDepositedAmount(double maxAmountToRecolect)
        {
            float[] Denom_table = { 0.05f, 0.1f, 0.2f, 0.5f, 1.0f, 2.0f, 5.0f, 10.0f, 20.0f, 0.0f };             //utiliza la tabla de monedas
            float total_inserted = 0.0f;
            float valor_ingresado = 0.0f;

            byte[] mensaje_IVDC = new byte[9];
            byte[] optionsIVDC = new byte[4];
            byte[] rsponseIVDC = new byte[86];


            //Construye el mensaje que se enviará
            construye_msj((byte)IVDC_Comand.Pool_evento, optionsIVDC, 0, ref mensaje_IVDC);

            //Envia el mensaje
            int msj_result = IVDC_executecomand(mensaje_IVDC, 5, ref rsponseIVDC);

            if (msj_result == 0)
            {
                //Verifica si hubo un evento de entrada (solo se verifican las monedas)
                for (int i = 2; i < 22; i += 2)
                {
                    ushort Evento = (ushort)(rsponseIVDC[i] * 256);
                    Evento += rsponseIVDC[i + 1];

                    if (Evento != 0)
                    {
                        total_inserted += Denom_table[(i - 2) / 2] * Evento; //Incrementa el total insertado
                        valor_ingresado = (Denom_table[(i - 2) / 2]);     //Actualiza el valor insertado

                        String type = Denom_table[(i - 2) / 2] * Evento > 0 ? Denom_table[(i - 2) / 2].ToString() : "--";
                        //Enviar la recolección a la plataforma
                        this.moneyReceivedCallback(this.Id, valor_ingresado, type, total_inserted);

                        if (total_inserted > maxAmountToRecolect)
                        {
                            this.signalErrorcallback(this.Id, "Se excede el máximo del monto por recolectar");
                        }

                    }
                }
            }
        }


        /// <summary>
        /// Set device to receive money,
        /// set this.isOpen = true
        /// have to call moneyReceivedCallback each money unit received 
        /// call signalErrorcallback in error case
        /// </summary>
        public override void StartMoneyRecolection(double maxAmountToRecolect)
        {

            float valor_aCobrar = 0.0f;
            byte[] optionsIVDC = new byte[4];

            byte[] mensaje_IVDC = new byte[9];
            int msj_result = 0;
            byte[] rsponseIVDC = new byte[5];
            //Verifica el tipo de cobro
           

            optionsIVDC = BitConverter.GetBytes(valor_aCobrar);
            Array.Reverse(optionsIVDC);


            //Construye el mensaje que se enviará
            construye_msj((byte)IVDC_Comand.Incia_cobro, optionsIVDC, 4, ref mensaje_IVDC);

            //Envia el mensaje
            msj_result = IVDC_executecomand(mensaje_IVDC, 9, ref rsponseIVDC);

            if (rsponseIVDC[0] == 06 && msj_result == 0)                     //Verifica si se recibio ACK 
            {
                //TODO cambiar a manejo de errores de la consola a sistema de logs remoto
                Console.WriteLine("Estado del Cobro: Iniciado el Proceso de Cobro");

                Task.Factory.StartNew(() =>
                {
                    cancellationTokenSource = new CancellationTokenSource();

                    do
                    {
                        this.ReadDepositedAmount(maxAmountToRecolect);
                        Thread.Sleep(300);

                        if (cancellationTokenSource.IsCancellationRequested)
                        {
                            break;
                        }
                    } while (!cancellationTokenSource.IsCancellationRequested);

                });
            }
            else
            {
                //TODO cambiar a manejo de errores de la consola a sistema de logs remoto
                Console.WriteLine("Estado del Cobro: Error de Comando, no se ejecutó el Comando pedido");
            }
        }

        /// <summary>
        /// Clouse and secure device, can't receive more money
        /// set this.isOpen = false
        /// call signalErrorcallback in error case
        /// </summary>
        public override void StopMoneyRecolection()
        {
            //se cancela la recepción
            cancellationTokenSource.Cancel();

            byte[] optionsIVDC = new byte[4];
            byte[] mensaje_IVDC = new byte[9];
            byte[] rsponseIVDC = new byte[86];

            construye_msj((byte)IVDC_Comand.Termina_Cobro, optionsIVDC, 0, ref mensaje_IVDC);
            int msj_result = IVDC_executecomand(mensaje_IVDC, 5, ref rsponseIVDC);

            if ((msj_result == 0 && rsponseIVDC[1] == 04))
            {
                //TODO consultar tipos de respuestas
            }
        }

        /// <summary>
        /// Return money to client
        /// ensure this.isOpen === false
        /// call signalErrorcallback in error case
        /// </summary>
        public override void ReturnMoneyToClient()
        {
            throw new NotImplementedException("ReturnMoneyToClient not implemented");
        }

        /// <summary>
        /// send money to vault
        /// ensure this.isOpen === false
        /// call signalErrorcallback in error case
        /// </summary>
        /// <returns></returns>
        public override void SendMoneyToVault()
        {

            //do nothing
            //throw new NotImplementedException("SendMoneyToVault not implemented");
        }

        /// <summary>
        /// Return Container used capacity by any kind of type
        /// call signalErrorcallback in error case
        /// </summary>
        /// <returns></returns>
        public override List<MoneyContainer> GetUsedCapacity()
        {
            throw new NotImplementedException("GetUsedCapacity not implemented");
        }
    }
}
