using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace MoneyRecolectorControl.MoneyRecolector
{
    class MoneyRecolectorSimulator : AbstracMoneyRecolector
    {
        //container for example purposes
        Dictionary<String, MoneyContainer> moneyInVault = new Dictionary<String, MoneyContainer>();

        //container for example purposes
        Dictionary<String, MoneyContainer> moneyInTemporaryReceiver = new Dictionary<String, MoneyContainer>();


        /// <summary>
        /// ensure to call base.RegisterDevice(_callback); 
        /// Set device configuration, allow configuration in confFiles
        /// </summary>
        /// <param name="_callback"></param>
        /// <returns></returns>
        public override String RegisterDevice(RecolectorSignalcallback _callback, RecolectorSignalErrorcallback _signalErrorcallback)
        {
            base.RegisterDevice(_callback, _signalErrorcallback);

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Money Recolector register under id " + this.Id);
            Console.ResetColor();

            return this.Id;
        }

        // cancelation token for example use only
        CancellationTokenSource cancellationTokenSource;

        //container for example purposes
        private void AddToTemporaryReceiver(String type, double value, double maxAmountToRecolect)
        {
            
            if (!this.moneyInTemporaryReceiver.ContainsKey(type))
            {
                MoneyContainer money = new MoneyContainer() { type = type, value = value, count = 1, capacity = 10 };
                this.moneyInTemporaryReceiver[type] = money;
            }
            else
            {
                var m = this.moneyInTemporaryReceiver[type];
                m.count++;
                this.moneyInTemporaryReceiver[type] = m;

                if ( m.count >= m.capacity)
                {
                    this.signalErrorcallback(this.Id, "Temporary Receiver Overflow for "+ type +" values");
                }

                if (this.moneyInTemporaryReceiver.Sum( r => r.Value.count * r.Value.value) > maxAmountToRecolect )
                {
                    this.signalErrorcallback(this.Id, "Maxvalue To recolect, return money");
                }

            }
            this.moneyReceivedCallback(this.Id, value, type, this.moneyInTemporaryReceiver.Sum(r => r.Value.value * r.Value.count));
        }

        /// <summary>
        /// Set device to receive money,
        /// set this.isOpen = true
        /// call moneyReceivedCallback each money unit received 
        /// call signalErrorcallback in error case 
        /// </summary>
        public override void StartMoneyRecolection(double maxAmountToRecolect)
        {

            cancellationTokenSource = new CancellationTokenSource();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Money Recolector is open and ready to receive");
            Console.ResetColor();

            this.isOpen = true;

            string userSelectionType;

            Task.Factory.StartNew( () => {
                do
                {
                    //User innteraction for example purposes
                    Console.WriteLine("0.25 - 0.50 - 1 -  2 -  5 -  10 -  20 -  50 -  100 -  200 -  500 (0 to exit)\nwhat kind of money do you enter??");
                    userSelectionType = Console.ReadLine();
                        
                    if( cancellationTokenSource.IsCancellationRequested)
                    {
                        break;
                    }

                    //interpret user interaction for example purposes
                    switch (userSelectionType)
                    {
                        case "0":
                            break;

                        case "0.25":
                        case "0.50":
                        case "0.5":
                        case "1":
                        case "2":
                        case "5":
                        case "10":
                        case "20":
                        case "50":
                        case "100":
                        case "200":
                            double value = Convert.ToDouble(userSelectionType);
                            AddToTemporaryReceiver(userSelectionType, value, maxAmountToRecolect);
                            break;

                        default:
                            Console.WriteLine("type of money not recognized, try again.");
                            break;
                    }

                } while (userSelectionType != "0");

                Console.WriteLine("End receiving money");

                cancellationTokenSource.Dispose();

            } , cancellationTokenSource.Token);
        }

        /// <summary>
        /// Clouse and secure device, can't receive more money
        /// set this.isOpen = false
        /// call signalErrorcallback in error case
        /// </summary>
        public override void StopMoneyRecolection()
        {
            if (this.isOpen)
            {
                cancellationTokenSource.Cancel();
                this.isOpen = false;

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Money Recolector is clouse");
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Return money to client
        /// ensure this.isOpen === false
        /// call signalErrorcallback in error case
        /// </summary>
        public override void ReturnMoneyToClient()
        {
            if ( !this.isOpen)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Return money to Client");
                Console.ResetColor();

                this.moneyInTemporaryReceiver.Clear();
            }
        }

        /// <summary>
        /// send money to vault
        /// ensure this.isOpen === false
        /// call signalErrorcallback in error case
        /// </summary>
        /// <returns></returns>
        public override void SendMoneyToVault()
        {
            if (!this.isOpen)
            {
                foreach( var m in this.moneyInTemporaryReceiver.Values)
                {
                    if( !this.moneyInVault.ContainsKey(m.type))
                    {
                        this.moneyInVault[m.type] = m;
                    }
                    else
                    {
                        var temp = this.moneyInVault[m.type];
                        temp.count += m.count;
                        this.moneyInVault[m.type]=temp;

                        if (temp.count >= temp.capacity)
                        {
                            this.signalErrorcallback(this.Id, "Vault Overflow for " + m.type + " values");
                        }
                    }
                }

                this.moneyInTemporaryReceiver.Clear();

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Send Money To Vault");
                Console.ResetColor();
            }
           
        }


        /// <summary>
        /// Return Container used capacity by any kind of type
        /// </summary>
        /// <returns></returns>
        public override List<MoneyContainer> GetUsedCapacity()
        {
            return this.moneyInVault.Values.ToList();
        }
    }
}
