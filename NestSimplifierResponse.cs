using System;
using System.Collections.Generic;
using System.Text;

namespace NestSimplifier
{
    public class NestSimplifierResponse
    {
        public bool IsValid { get; private set; }
        public string Message { get; private set; }

        public NestSimplifierResponse(bool isValid, string message)
        {
            IsValid = isValid;
            Message = message;

            if (isValid && message.ToLower().Contains("valid nest response"))
            {
                Message = "SUCCESS";
            }
        }
    }
}
