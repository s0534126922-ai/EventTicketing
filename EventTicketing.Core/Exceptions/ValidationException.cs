using System;
using System.Collections.Generic;
using System.Text;


namespace EventTicketing.Core.Exceptions;

public sealed class ValidationException : AppException
{
    public ValidationException(string message)
        : base(message)
    {
    }
}
