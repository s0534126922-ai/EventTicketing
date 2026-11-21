using System;
using System.Collections.Generic;
using System.Text;


namespace EventTicketing.Core.Exceptions;

public sealed class NotFoundException : AppException
{
    public NotFoundException(string message)
        : base(message)
    {
    }
}
