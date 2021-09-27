using System;
using System.Collections.Generic;
using System.Text;

namespace BladeEngine.Core.Utils
{
    public class ServiceResponse
    {
        public bool Succeeded { get; set; }
        public string Status { get; set; }
        public Exception Exception { get; set; }
        public void TrySetStatus(string status)
        {
            if (string.IsNullOrEmpty(Status))
            {
                Status = status;
                Succeeded = status.IndexOf("success", StringComparison.OrdinalIgnoreCase) >= 0 || status.IndexOf("succeed", StringComparison.OrdinalIgnoreCase) >= 0;
            }
        }
        public void SetStatus(string status)
        {
            Status = status;
            Succeeded = status.IndexOf("success", StringComparison.OrdinalIgnoreCase) >= 0 || status.IndexOf("succeed", StringComparison.OrdinalIgnoreCase) >= 0;
        }
        public virtual void Copy(ServiceResponse sr)
        {
            this.Succeeded = sr.Succeeded;
            this.Status = sr.Status;
            this.Exception = sr.Exception;
        }
    }
    public class ServiceResponse<T>: ServiceResponse
    {
        public T Data { get; set; }
    }
}
