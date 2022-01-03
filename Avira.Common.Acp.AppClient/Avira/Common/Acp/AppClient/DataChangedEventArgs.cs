using System;

namespace Avira.Common.Acp.AppClient
{
    public class DataChangedEventArgs<T> : EventArgs
    {
        private T Data { get; set; }

        public DataChangedEventArgs(T data)
        {
            Data = data;
        }
    }
}