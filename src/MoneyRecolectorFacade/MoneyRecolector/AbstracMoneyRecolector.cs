using System;
using System.Collections.Generic;
using System.Text;

namespace MoneyRecolectorControl.MoneyRecolector
{
    public abstract class AbstracMoneyRecolector
    {
        public delegate void RecolectorSignalcallback(String id, double amount, string type, double total);

        public delegate void RecolectorSignalErrorcallback(string id, string message);
        public String Id { get; set; }

        protected RecolectorSignalcallback moneyReceivedCallback;

        protected RecolectorSignalErrorcallback signalErrorcallback;

        protected bool isOpen = false;

        public bool IsOpen()
        {
            return this.isOpen;
        }

        /// <summary>
        /// ensure to call base.RegisterDevice(_callback, _signalErrorcallback); 
        /// Set device configuration, allow configuration in confFiles
        /// </summary>
        /// <param name="_callback"></param>
        /// <returns></returns>
        public virtual String RegisterDevice(RecolectorSignalcallback _callback, RecolectorSignalErrorcallback _signalErrorcallback)
        {
            this.Id = Guid.NewGuid().ToString();
            this.moneyReceivedCallback = _callback;
            this.signalErrorcallback = _signalErrorcallback;
            return Id;
        }

        /// <summary>
        /// Set device to receive money,
        /// set this.isOpen = true
        /// have to call moneyReceivedCallback each money unit received 
        /// call signalErrorcallback in error case
        /// </summary>
        public virtual void StartMoneyRecolection(double maxAmountToRecolect)
        {
            throw new NotImplementedException("StartMoneyRecolection not implemented");
        }

        /// <summary>
        /// Clouse and secure device, can't receive more money
        /// set this.isOpen = false
        /// call signalErrorcallback in error case
        /// </summary>
        public virtual void StopMoneyRecolection()
        {
            throw new NotImplementedException("StopMoneyRecolection not implemented");
        }

        /// <summary>
        /// Return money to client
        /// ensure this.isOpen === false
        /// call signalErrorcallback in error case
        /// </summary>
        public virtual void ReturnMoneyToClient()
        {
            throw new NotImplementedException("ReturnMoneyToClient not implemented");
        }

        /// <summary>
        /// send money to vault
        /// ensure this.isOpen === false
        /// call signalErrorcallback in error case
        /// </summary>
        /// <returns></returns>
        public virtual void SendMoneyToVault()
        {
            throw new NotImplementedException("SendMoneyToVault not implemented");
        }

        /// <summary>
        /// Return Container used capacity by any kind of type
        /// call signalErrorcallback in error case
        /// </summary>
        /// <returns></returns>
        public virtual List<MoneyContainer> GetUsedCapacity()
        {
            throw new NotImplementedException("GetUsedCapacity not implemented");
        }
    
    }
}
