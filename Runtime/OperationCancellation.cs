using System;

namespace PGIA
{
    /// <summary>
    /// Used as a means of flagging when an operation should be cancelled by a third-party listener of events.
    /// </summary>
    public class OperationCancellation
    {
        readonly Action CancelAction;

        bool _Cancelled;

        /// <summary>
        /// This is a one-way latched operation. Once this flag is
        /// set there is no going back.
        /// </summary>
        public bool Cancelled
        {
            get => _Cancelled;
            set
            {
                if (value && !_Cancelled)
                {
                    _Cancelled = true;
                    CancelAction?.Invoke();
                }
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancelAction"></param>
        public OperationCancellation(Action cancelAction)
        {
            CancelAction = cancelAction;
        }
    }
}
