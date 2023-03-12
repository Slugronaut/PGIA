using System;

namespace PGIA
{
    /// <summary>
    /// Similar to OperationCacellation, this objects is a struct rather than a class
    /// and cantains no state. It simply invokes the stored method upon being called.
    /// </summary>
    public struct OperationCancelAction
    {
        readonly Action CancelAction;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancelAction"></param>
        public OperationCancelAction(Action cancelAction)
        {
            CancelAction = cancelAction;
        }

        /// <summary>
        /// Invokes the action that was passed to this object's contructor.
        /// </summary>
        public void Cancel()
        {
            CancelAction?.Invoke();
        }
    }
}
